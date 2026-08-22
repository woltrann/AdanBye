using UnityEngine;

// Deterministik curve-driven hız üretici: aynı input + aynı geçen süre her zaman aynı
// çıktıyı verir (Vector3.SmoothDamp'in önceki frame'e bağımlı üstel davranışının aksine).
// Time.* çağrısı yapmaz - "now" dışarıdan verilir, böylece FixedUpdate (Time.fixedTime)
// veya Update (Time.time) tabanlı çağıranlarla aynı sınıf kullanılabilir.
public class CurveMovementSolver
{
    private readonly MovementCurveProfile profile;

    private bool wasActive;
    private float stateStartTime;
    private Vector3 lastDirection;
    private float lastTargetMaxSpeed;

    public CurveMovementSolver(MovementCurveProfile profile)
    {
        this.profile = profile;
    }

    public Vector3 Evaluate(Vector3 desiredDirection, float targetMaxSpeed, bool hasInput, float now)
    {
        Vector3 dir = desiredDirection.sqrMagnitude > 0.0001f ? desiredDirection.normalized : lastDirection;
        if (desiredDirection.sqrMagnitude > 0.0001f)
            lastDirection = dir;

        if (hasInput)
            lastTargetMaxSpeed = targetMaxSpeed;
        float effectiveMaxSpeed = hasInput ? targetMaxSpeed : lastTargetMaxSpeed;

        if (hasInput != wasActive)
        {
            stateStartTime = now;
            wasActive = hasInput;
        }

        float duration = hasInput ? profile.accelerationDuration : profile.decelerationDuration;
        AnimationCurve curve = hasInput ? profile.accelerationCurve : profile.decelerationCurve;

        float progress = duration > 0.0001f ? Mathf.Clamp01((now - stateStartTime) / duration) : 1f;
        float fraction = Mathf.Clamp01(curve.Evaluate(progress));

        return dir * (fraction * effectiveMaxSpeed);
    }
}
