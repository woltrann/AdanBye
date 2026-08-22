using UnityEngine;

// Alpha kurdun state machine'i: Idle -> Chase -> Search -> Retreat.
// Beta'nın davranışından tamamen bağımsız - yeni bir rol eklemek bu sınıfa
// dokunmayı gerektirmez (OCP). Bağımlılıklar arayüz üzerinden constructor injection
// ile alınır (DIP), controller ise Player'daki PlayerManager gibi paylaşılan somut
// context/ayar kaynağıdır.
public class AlphaWolfBehavior : IWolfBehavior
{
    private const float ApproachSmoothTime = 0.25f; // yaklaşım hedefinin kendisini yumuşatır

    private readonly WolfBehaviorController controller;
    private readonly Transform transform;
    private readonly WolfIdentity identity;
    private readonly IWolfMover mover;
    private readonly IWolfAttacker attacker;
    private readonly IWolfHowler howler;
    private readonly WolfWanderer wanderer;
    private readonly WolfTerritory territory;

    private Vector3 lastKnownPlayerPos;
    private Vector3 debugTarget;
    private float retreatTimer;

    private Vector3 smoothedApproachPoint;
    private Vector3 approachVelocityRef;

    public Vector3 DebugTarget => debugTarget;

    public AlphaWolfBehavior(WolfBehaviorController controller, Transform transform, WolfIdentity identity,
        IWolfMover mover, IWolfAttacker attacker, IWolfHowler howler, WolfWanderer wanderer, WolfTerritory territory)
    {
        this.controller = controller;
        this.transform = transform;
        this.identity = identity;
        this.mover = mover;
        this.attacker = attacker;
        this.howler = howler;
        this.wanderer = wanderer;
        this.territory = territory;
    }

    public void Tick()
    {
        Transform player = identity.PlayerTransform;
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Oyuncu Territory dışına çıksa bile kovalamayı bırakmayız - oyuncu chase'ten kendi
        // başına kaçamadıysa sınıra kadar kovalanır. Ancak KENDİMİZ sınırı geçersek (oyuncuyu
        // izleyerek territory'nin dışına taştıysak) orada durup uluyarak bekleriz (Guard),
        // oyuncu tekrar sınıra girerse kaldığımız yerden devam ederiz. Territory atanmadıysa
        // sınır yok (eski davranış). Idle zaten HomePosition etrafında dar bir alanda geziniyor
        // (WanderRadius), bu yüzden sadece Chase/Search'te kontrol etmek yeterli.
        if (territory != null
            && (controller.CurrentState == WolfState.Chase || controller.CurrentState == WolfState.Search)
            && territory.IsOutside(transform.position))
        {
            controller.ChangeState(WolfState.Guard);
            return;
        }

        switch (controller.CurrentState)
        {
            case WolfState.Idle:
                if (wanderer.Tick(identity.HomePosition, controller.WanderRadius, controller.Speed * 0.5f)
                    && Random.value < controller.IdleHowlChance)
                {
                    howler.TryHowl();
                }

                // Oyuncu territory dışındaysa (sınırın içindeki bir tuzağa düşmediyse) hiç
                // kovalamaya başlamayız - territory "savunulan alan" olduğu için dışarıdaki
                // bir oyuncu tehdit sayılmaz.
                if (distance <= controller.ChaseDistance
                    && (territory == null || !territory.IsOutside(player.position)))
                {
                    howler.TryHowl();
                    controller.ChangeState(WolfState.Chase);
                }
                break;

            case WolfState.Chase:
                // Her zaman oyuncuya bak
                mover.LookAt(player.position);

                // Saldırı menziline kadar yaklaş - hedef oyuncunun MERKEZİ değil,
                // oyuncudan attackDistance kadar geride bir "duruş noktası". Böylece
                // ikisi de collider'a sahip olsa bile kurt oyuncunun içine girmeye
                // çalışmaz. Menzildeyken (cooldown beklerken dahi) hareket tamamen
                // durur - eskiden cooldown hazır değilken de yürümeye devam ediyordu,
                // bu da oyuncuyu sürekli itmesine sebep oluyordu.
                //
                // Bu nokta kurdun KENDİ konumuna göre hesaplanıyor (oyuncuya olan yön),
                // bu yüzden Beta'nın sabit formasyon offsetinin aksine öz-referanslı:
                // kurt yaklaştıkça yön küçük açılarda bile değişebilir. Motor zaten
                // hızı/dönüşü yumuşatıyor ama HEDEFİN kendisi karede bir zıplayabiliyordu -
                // burada ayrıca SmoothDamp ile hedefi de yumuşatıyoruz, sert dönüşleri önler.
                Vector3 rawApproachPoint = ComputeApproachPoint(player.position, attacker.AttackDistance);
                smoothedApproachPoint = Vector3.SmoothDamp(smoothedApproachPoint, rawApproachPoint, ref approachVelocityRef, ApproachSmoothTime);
                debugTarget = smoothedApproachPoint;

                if (distance > attacker.AttackDistance)
                {
                    mover.MoveTo(smoothedApproachPoint, controller.Speed);
                }

                // Menzildeysek ve cooldown hazırsa saldır
                if (distance <= attacker.AttackDistance && attacker.IsReady)
                {
                    attacker.Attack();
                }

                // Oyuncu çok uzaklaşırsa Search state'ine geç
                if (distance > controller.ChaseDistance * 1.5f)
                {
                    lastKnownPlayerPos = player.position;
                    controller.ChangeState(WolfState.Search);
                }
                break;

            case WolfState.Search:
                if (wanderer.Tick(lastKnownPlayerPos, controller.WanderRadius, controller.Speed * 0.5f)
                    && Random.value < controller.IdleHowlChance)
                {
                    howler.TryHowl();
                }

                if (distance <= controller.ChaseDistance) controller.ChangeState(WolfState.Chase);
                break;

            case WolfState.Guard:
                // Sınırda dur, oyuncuya bak ve ulu - TryHowl kendi cooldown'unu yönetiyor,
                // her karede çağırmak yeterli.
                mover.LookAt(player.position);
                howler.TryHowl();

                if (territory != null && !territory.IsOutside(player.position) && distance <= controller.ChaseDistance)
                {
                    controller.ChangeState(WolfState.Chase);
                    break;
                }

                // Oyuncu iyice uzaklaşırsa (Chase->Search geçişiyle aynı eşik) artık pes edip eve dön
                if (distance > controller.ChaseDistance * 1.5f)
                {
                    controller.ChangeState(WolfState.Retreat);
                }
                break;

            case WolfState.Retreat:
                retreatTimer -= Time.deltaTime;
                mover.LookAt(identity.HomePosition);
                mover.MoveTo(identity.HomePosition, controller.Speed * 1.5f);

                bool reachedHome = Vector3.Distance(transform.position, identity.HomePosition) < 1f;
                if (retreatTimer <= 0 || reachedHome) controller.ChangeState(WolfState.Idle);
                break;
        }
    }

    public void OnStateEntered(WolfState newState)
    {
        if (newState == WolfState.Retreat)
        {
            retreatTimer = controller.RetreatDuration;
        }
        else if (newState == WolfState.Chase && identity.PlayerTransform != null)
        {
            // Smoothed hedefi sıfırdan değil, o anki gerçek duruma göre başlat -
            // Chase'e yeni girildiğinde (0,0,0)'dan ani bir zıplama olmasın.
            smoothedApproachPoint = ComputeApproachPoint(identity.PlayerTransform.position, attacker.AttackDistance);
            approachVelocityRef = Vector3.zero;
        }
    }

    // Oyuncudan standOffDistance kadar geride, kurdun mevcut konumuna bakan bir
    // duruş noktası hesaplar - MoveTo hedefi hiçbir zaman oyuncunun tam merkezi
    // olmasın diye (collider çakışması / sürekli itme burada engelleniyor).
    private Vector3 ComputeApproachPoint(Vector3 playerPos, float standOffDistance)
    {
        Vector3 toPlayer = playerPos - transform.position;
        float dist = toPlayer.magnitude;
        Vector3 dir = dist > 0.0001f ? toPlayer / dist : transform.forward;
        return playerPos - dir * standOffDistance;
    }
}
