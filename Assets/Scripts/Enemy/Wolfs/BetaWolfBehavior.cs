using UnityEngine;

// Beta kurdun davranışı: alpha'yı uyarma, formasyon halinde saldırma, alpha'yı takip
// etme. Kendi state machine'i yok - kararlarını alphaWolf'un durumuna göre verir
// (orijinal tasarım korunuyor: currentState burada sadece dışarıdan (retreat cascade
// gibi) set edilebilir, kendi kararlarını etkilemez).
public class BetaWolfBehavior : IWolfBehavior
{
    private readonly WolfBehaviorController controller;
    private readonly Transform transform;
    private readonly WolfIdentity identity;
    private readonly IWolfMover mover;
    private readonly IWolfAttacker attacker;
    private readonly IWolfHowler howler;

    // Spawn'da bir kere atanan, bu kurda özgü sabit farklar - pack'in tamamen
    // simetrik/senkronize ("ordu gibi") görünmesini engeller.
    private readonly float personalAngleOffset;
    private readonly float personalDistanceMultiplier;
    private readonly float personalSpeedMultiplier;
    private readonly float noiseSeed;

    private Vector3 debugTarget;

    public Vector3 DebugTarget => debugTarget;

    public BetaWolfBehavior(WolfBehaviorController controller, Transform transform, WolfIdentity identity,
        IWolfMover mover, IWolfAttacker attacker, IWolfHowler howler)
    {
        this.controller = controller;
        this.transform = transform;
        this.identity = identity;
        this.mover = mover;
        this.attacker = attacker;
        this.howler = howler;

        personalAngleOffset = Random.Range(-controller.PersonalAngleJitter, controller.PersonalAngleJitter);
        personalDistanceMultiplier = Random.Range(0.85f, 1.25f);
        personalSpeedMultiplier = Random.Range(0.85f, 1.15f);
        noiseSeed = Random.Range(0f, 1000f);
    }

    public void Tick()
    {
        WolfIdentity alpha = identity.AlphaWolf;
        Transform player = identity.PlayerTransform;
        if (alpha == null || player == null) return;

        WolfBehaviorController alphaController = alpha.GetComponent<WolfBehaviorController>();
        if (alphaController == null) return;

        // Alpha nihayet pes edip eve dönüyorsa (Guard'da iyice uzaklaşma eşiğini aştı), beta da
        // aynı şekilde eve döner. Bu kontrol "alpha'yı uyar" bloğundan ÖNCE yapılıyor - aksi
        // halde menzildeki bir beta, alpha Retreat/Guard'a geçer geçmez onu tekrar Chase'e
        // zorlayıp o kararı iptal ediyordu.
        if (alphaController.CurrentState == WolfState.Retreat)
        {
            mover.LookAt(identity.HomePosition);
            mover.MoveTo(identity.HomePosition, controller.Speed * 1.5f * personalSpeedMultiplier);
            return;
        }

        // Alpha sınırda bekleyip uluyorsa (oyuncu ya da sürü territory dışına çıktı) ya da bu
        // beta kendi başına territory dışına taştıysa (formasyon offseti alpha içeride kalsa
        // bile beta'yı dışarı taşırabilir), eve koşmak yerine aynı şekilde dur ve ulu - oyuncu
        // tekrar sınıra girerse kaldığımız yerden devam ederiz.
        bool selfOutsideTerritory = controller.Territory != null && controller.Territory.IsOutside(transform.position);
        if (alphaController.CurrentState == WolfState.Guard || selfOutsideTerritory)
        {
            // Kovalamıyorken kullandığımız aynı formasyon slotuna (bkz. else dalı altta) girip
            // orada dururuz - alpha zaten oyuncuya dönük olduğu için beta'lar doğal olarak
            // onun etrafında/gerisinde toplanmış görünür. Slot sabit olduğu (alpha hareket
            // etmediği) için formasyona girince kendiliğinden durulur, ek bir gezinme yok.
            Vector3 targetPos = ComputeFollowSlot(alpha);

            debugTarget = targetPos;

            if (Vector3.Distance(transform.position, targetPos) > 0.3f)
            {
                mover.MoveTo(targetPos, controller.Speed * 0.4f * personalSpeedMultiplier);
            }

            mover.LookAt(player.position);
            howler.TryHowl();
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Beta oyuncuyu görürse alpha'yı uyarır - ama oyuncu territory dışındaysa (dolayısıyla
        // tehdit sayılmıyorsa) alpha'yı kovalamaya zorlamaz.
        bool playerInsideTerritory = controller.Territory == null || !controller.Territory.IsOutside(player.position);
        if (distToPlayer <= controller.ChaseDistance && playerInsideTerritory && alphaController.CurrentState != WolfState.Chase)
        {
            alphaController.ChangeState(WolfState.Chase);
        }

        if (alphaController.CurrentState == WolfState.Chase)
        {
            // Oyuncuyu formasyon halinde takip et
            int index = alpha.IndexOfMember(identity);
            float angle = (index + 1) * (180f / (alpha.PackMembers.Count + 1));
            Vector3 offset = Quaternion.Euler(0, angle, 0) * (Vector3.forward * controller.FormationRadius);
            Vector3 targetPos = player.position + offset;

            debugTarget = targetPos;
            float distToTarget = Vector3.Distance(transform.position, targetPos);

            // Çok yakın değilsek her zaman formasyon pozisyonuna doğru hareket et.
            // Oyuncu doğrudan bu beta'nın üzerine koşarsa (collider çakışması,
            // yerinde takılıp titreme), rota oyuncudan uzaklaşan bir bileşenle
            // harmanlanarak yumuşakça yandan dolaştırılır (SteerAroundPlayer).
            if (distToTarget > 0.5f)
            {
                Vector3 steeredTarget = SteerAroundPlayer(targetPos, player.position, attacker.AttackDistance + controller.PersonalSpaceBuffer);
                mover.MoveTo(steeredTarget, controller.Speed * 0.9f * personalSpeedMultiplier);
                mover.LookAt(player.position);
            }

            // Oyuncuya yeterince yakınsak saldır
            if (distToPlayer <= attacker.AttackDistance && attacker.IsReady)
            {
                attacker.Attack();
            }
        }
        else
        {
            // Kovalamıyorken alpha'yı takip et: alpha'nın arkasında, bakış yönüne göre
            // bu kurda özgü (rastgele) bir açı/mesafede bir "trail slot" + slot
            // etrafında sürekli, yumuşak bir yerel sürüklenme (Perlin noise - asla ani
            // sıçramaz). Sonuç: pack artık simetrik bir yelpazede kilitlenmiş
            // yürümüyor, her kurt kendi köşesinde biraz gezinerek takip ediyor.
            Vector3 slot = ComputeFollowSlot(alpha);
            Vector3 targetPos = slot + LocalDrift();

            debugTarget = targetPos;

            if (Vector3.Distance(transform.position, targetPos) > 0.3f)
            {
                mover.MoveTo(targetPos, controller.Speed * 0.8f * personalSpeedMultiplier);
                mover.LookAt(targetPos);
            }
        }
    }

    public void OnStateEntered(WolfState newState)
    {
        // Beta'nın kendi retreat sayacı yok; alpha'nın retreat'i bitince (Idle'a
        // dönünce) tüm sürü otomatik olarak eski davranışına döner.
    }

    private Vector3 ComputeFollowSlot(WolfIdentity alpha)
    {
        int index = alpha.IndexOfMember(identity);
        int count = Mathf.Max(alpha.PackMembers.Count, 1);
        float baseAngle = Mathf.Lerp(-controller.FollowFanAngle, controller.FollowFanAngle, (index + 1) / (float)(count + 1));
        float angle = baseAngle + personalAngleOffset;
        float distance = controller.FollowDistance * personalDistanceMultiplier;

        Quaternion behindRotation = Quaternion.LookRotation(-alpha.transform.forward);
        Vector3 offset = behindRotation * Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
        return alpha.transform.position + offset;
    }

    // Hedefe giden düz rota oyuncunun "kişisel alanı"ndan geçiyorsa (oyuncu formasyon
    // rotasına doğru koşup beta'nın üzerine gelmiş gibi), yönü oyuncudan uzaklaşan bir
    // bileşenle harmanlar - beta oyuncuya saplanıp titremek yerine yandan dolaşarak
    // hedefe ilerler. Oyuncuya yeterince uzaksa (personalSpaceRadius dışında) hiçbir
    // etkisi olmaz, düz rota korunur.
    private Vector3 SteerAroundPlayer(Vector3 targetPos, Vector3 playerPos, float personalSpaceRadius)
    {
        Vector3 toTarget = targetPos - transform.position;
        float distToTarget = toTarget.magnitude;
        Vector3 desiredDir = distToTarget > 0.0001f ? toTarget / distToTarget : transform.forward;

        Vector3 awayFromPlayer = transform.position - playerPos;
        float distToPlayerNow = awayFromPlayer.magnitude;

        if (distToPlayerNow < personalSpaceRadius && distToPlayerNow > 0.0001f)
        {
            Vector3 avoidDir = awayFromPlayer / distToPlayerNow;
            float avoidStrength = 1f - (distToPlayerNow / personalSpaceRadius);
            desiredDir = Vector3.Slerp(desiredDir, avoidDir, avoidStrength).normalized;
        }

        return transform.position + desiredDir * distToTarget;
    }

    private Vector3 LocalDrift()
    {
        float t = Time.time * controller.LocalDriftSpeed;
        float x = (Mathf.PerlinNoise(noiseSeed, t) - 0.5f) * 2f * controller.LocalDriftRadius;
        float z = (Mathf.PerlinNoise(noiseSeed + 100f, t) - 0.5f) * 2f * controller.LocalDriftRadius;
        return new Vector3(x, 0f, z);
    }
}
