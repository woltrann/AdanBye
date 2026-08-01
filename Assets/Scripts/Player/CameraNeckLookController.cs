using UnityEngine;

public class CameraNeckLookController : MonoBehaviour
{
    private HumanPoseHandler poseHandler;
    private HumanPose humanPose;

    [Header("References")]
    public Transform cameraTransform;   // Freelook / Cinemachine kameranýn transform'u
    public Transform bodyRoot;
    public Animator animator;

    [Header("Muscle Weight (0-1 arasý, normalize edilmiþ katký oraný)")]
    [Range(0, 1)] public float neckWeight = 0.4f;
    [Range(0, 1)] public float headWeight = 0.6f;

    [Header("Açý Limitleri (derece, SADECE kamera hesaplamasý için)")]
    public float maxHorizontalAngle = 40f; // Avatar'daki Neck Turn Left-Right derece aralýðýnla eþleþsin
    public float smoothSpeed = 8f;

    private float currentTurnValue;

    private int neckTurnIndex;

    void Start()
    {
        poseHandler = new HumanPoseHandler(animator.avatar, transform);
        neckTurnIndex = HumanTrait.MuscleFromBone((int)HumanBodyBones.Neck, 2);

        // Debug: index gerçekten bulunmuþ mu kontrol et (-1 dönerse bone mapping sorunu var demektir)
        Debug.Log($"[NeckLook] neckTurnIndex = {neckTurnIndex}");
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (cameraTransform == null) return;

        float camYaw = cameraTransform.eulerAngles.y;
        float bodyYaw = bodyRoot.eulerAngles.y;
        float relativeYaw = Mathf.DeltaAngle(bodyYaw, camYaw);
        float clamped = Mathf.Clamp(relativeYaw, -maxHorizontalAngle, maxHorizontalAngle);

        // Dereceyi -1 / 1 normalize edilmiþ muscle aralýðýna çevir
        float normalizedTurn = clamped / maxHorizontalAngle; // artýk -1..1 arasý

        currentTurnValue = Mathf.Lerp(currentTurnValue, normalizedTurn, Time.deltaTime * smoothSpeed);

        poseHandler.GetHumanPose(ref humanPose);

        // ÖNEMLÝ: muscles dizisi HER ZAMAN -1/1 aralýðýndadýr, dereceyle alakasý yok
        humanPose.muscles[neckTurnIndex] += currentTurnValue * neckWeight;
        humanPose.muscles[neckTurnIndex] = Mathf.Clamp(humanPose.muscles[neckTurnIndex], -1f, 1f);

        poseHandler.SetHumanPose(ref humanPose);
    }
}