using UnityEngine;

// IndoorZone ve GasVolume'ün ortak tabanı. Tek iş: sensörle karşılıklı kayıt tutmak.
// Neden: trigger bir hacim, oyuncu içindeyken kapanırsa/yok edilirse OnTriggerExit hiç gelmez
// ("exit kaçağı"); bu yüzden hacim kendini sensörden çıkartır.
[RequireComponent(typeof(Collider))]
public abstract class AtmosphereZone : MonoBehaviour
{
    private AtmosphereSensor occupant;

    internal void NotifyEntered(AtmosphereSensor sensor) => occupant = sensor;

    internal void NotifyLeft(AtmosphereSensor sensor)
    {
        if (occupant == sensor) occupant = null;
    }

    private void OnDisable()
    {
        if (occupant == null) return;
        occupant.Forget(this);
        occupant = null;
    }

    // Sahneye konunca collider'ı otomatik trigger yap (katı kalıp oyuncuyu engellemesin).
    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnValidate()
    {
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[{GetType().Name}] Collider isTrigger kapalı; oyuncuya fiziksel engel olur ve sensör olay almaz.", this);
    }
}
