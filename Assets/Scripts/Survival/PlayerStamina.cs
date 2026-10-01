using System;
using AdanBye.Survival;
using UnityEngine;

// Tek iş: StaminaModel'i oyuna bağlamak (aktiviteyi motordan türetmek, kapıları/kilidi sunmak, kaydetmek).
// Veri MainCharacter'a konmaz: stamina'nın tek doğruluk kaynağı bu bileşen.
public class PlayerStamina : MonoBehaviour, IRunGate, IMovementLock, IStaminaReadout, ICampRestReceiver, ISaveable
{
    [SerializeField] private StaminaConfig config = new StaminaConfig();

    private StaminaModel model;
    private PlayerMotor motor;

    // WP-9 (yorgunluk kaynağı) bağlanana kadar sabit; Tick'e yine de parametre olarak iletilir.
    private const float FatigueMultiplier = 1f;

    // Restore/RestAtCamp Awake'ten önce çağrılabileceği için model tembel kurulur.
    private StaminaModel Model => model ??= new StaminaModel(config);

    public event Action Collapsed
    {
        add => Model.Collapsed += value;
        remove => Model.Collapsed -= value;
    }

    public float Current => Model.Current;
    public float Ceiling => Model.Ceiling;
    public float Max => Model.Max;
    public bool IsCollapsed => Model.IsCollapsed;

    public bool CanRun => Model.CanRun;
    public float SpeedMultiplier => Model.SpeedMultiplier;
    public bool IsMovementLocked => Model.IsCollapsed;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        if (motor == null)
            Debug.LogWarning("[PlayerStamina] PlayerMotor bulunamadı; aktivite hep Idle sayılacak (stamina koşuyla tükenmez).", this);
    }

    private void Update()
    {
        // Motorun bir önceki kare kararı okunur; bir kare gecikme kabul.
        var activity = StaminaActivity.Idle;
        if (motor != null)
        {
            Vector3 v = motor.CurrentVelocity;
            float horizontalSpeed = new Vector2(v.x, v.z).magnitude;
            activity = StaminaActivityResolver.Resolve(horizontalSpeed, motor.IsRunning);
        }
        Model.Tick(Time.deltaTime, activity, FatigueMultiplier);
    }

    public void RestAtCamp() => Model.RestAtCamp();

    public void CaptureState(SaveData data)
    {
        data.currentStamina = Model.Current;
        data.staminaCeiling = Model.Ceiling;
    }

    public void RestoreState(SaveData data) => Model.Restore(data.currentStamina, data.staminaCeiling);
}
