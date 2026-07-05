using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FootTerrainIK : MonoBehaviour
{
    [Header("Ray Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastHeightOffset = 0.5f;
    [SerializeField] private float raycastDistance = 1.0f;
    [SerializeField] private float footYOffset = 0.02f; // ayak model tabaný ile hit point arasý boþluk

    [Header("Weights")]
    [SerializeField] private float footIKWeight = 1f;
    [SerializeField] private float bodyOffsetWeight = 0.5f;
    [SerializeField] private float ikSmoothing = 12f;

    [Header("Karakter Hareket Durumu")]
    [Tooltip("Root motion olmadýðý için IK'yý sadece gerekince aktif etmek istersen PlayerMovement'tan isGrounded referansý verebilirsin (opsiyonel)")]
    [SerializeField] private bool onlyWhenGrounded = true;

    private Animator animator;

    private float leftWeight, rightWeight;
    private Vector3 leftIKPos, rightIKPos;
    private Quaternion leftIKRot, rightIKRot;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        if (onlyWhenGrounded && PlayerMovement.Instance != null && !PlayerMovement.Instance.enabled)
            return;

        SolveFoot(AvatarIKGoal.LeftFoot, HumanBodyBones.LeftFoot, ref leftIKPos, ref leftIKRot, ref leftWeight);
        SolveFoot(AvatarIKGoal.RightFoot, HumanBodyBones.RightFoot, ref rightIKPos, ref rightIKRot, ref rightWeight);

        ApplyHipOffset();
    }

    private void SolveFoot(AvatarIKGoal goal, HumanBodyBones bone, ref Vector3 ikPos, ref Quaternion ikRot, ref float weight)
    {
        Vector3 footPos = animator.GetBoneTransform(bone).position;
        Vector3 rayOrigin = footPos + Vector3.up * raycastHeightOffset;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeightOffset + raycastDistance, groundLayer))
        {
            Debug.Log($"[{bone}] Hit: {hit.collider.name} | Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            Vector3 targetPos = hit.point + Vector3.up * footYOffset;
            Quaternion targetRot = Quaternion.FromToRotation(transform.up, hit.normal) * animator.GetBoneTransform(bone).rotation;

            ikPos = Vector3.Lerp(ikPos == Vector3.zero ? footPos : ikPos, targetPos, Time.deltaTime * ikSmoothing);
            ikRot = Quaternion.Slerp(ikRot, targetRot, Time.deltaTime * ikSmoothing);
            weight = Mathf.Lerp(weight, footIKWeight, Time.deltaTime * ikSmoothing);
        }
        else
        {
            weight = Mathf.Lerp(weight, 0f, Time.deltaTime * ikSmoothing);
        }

        animator.SetIKPositionWeight(goal, weight);
        animator.SetIKRotationWeight(goal, weight);
        if (weight > 0.001f)
        {
            animator.SetIKPosition(goal, ikPos);
            animator.SetIKRotation(goal, ikRot);
        }
    }

    private void ApplyHipOffset()
    {
        if (leftWeight <= 0f && rightWeight <= 0f) return;

        float leftFootY = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y;
        float rightFootY = animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y;

        float leftOffset = leftWeight > 0f ? leftIKPos.y - leftFootY : 0f;
        float rightOffset = rightWeight > 0f ? rightIKPos.y - rightFootY : 0f;

        float lowestOffset = Mathf.Min(leftOffset, rightOffset);

        Vector3 hipPos = animator.bodyPosition;
        hipPos.y += lowestOffset * bodyOffsetWeight;
        animator.bodyPosition = hipPos;
    }
}