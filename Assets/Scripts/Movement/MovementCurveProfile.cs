using UnityEngine;

[CreateAssetMenu(fileName = "NewMovementCurveProfile", menuName = "Movement/Curve Profile")]
public class MovementCurveProfile : ScriptableObject
{
    [Tooltip("X: 0-1 normalize süre ilerlemesi, Y: 0-1 hedef hızın fraksiyonu")]
    public AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("X: 0-1 normalize süre ilerlemesi, Y: 0-1 hedef hızın fraksiyonu")]
    public AnimationCurve decelerationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    public float accelerationDuration = 0.25f;
    public float decelerationDuration = 0.2f;
}
