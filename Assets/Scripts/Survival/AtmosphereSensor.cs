using System.Collections.Generic;
using AdanBye.Survival;
using UnityEngine;

// Tek iş: oyuncunun içinde bulunduğu IndoorZone/GasVolume kümesini izleyip yoğunluğu hesaplamak.
// Trigger olayları Rigidbody'nin olduğu objeye de gelir; bu bileşen Player kökünde (Rigidbody ile aynı obje) durmalı.
// HashSet: aynı hacme birden fazla Enter gelse de kayıt tekrarlanmaz.
// Dikkat: oyuncunun birden fazla collider'ı aynı hacme girerse biri çıkınca hacim düşer;
// bu yüzden Player kökünde tek gövde collider önerilir.
public class AtmosphereSensor : MonoBehaviour, IAtmosphereProvider
{
    [Tooltip("Dış mekanın varsayılan hafif kirli taban yoğunluğu.")]
    [SerializeField] private float baseOutdoorDensity = 0.2f;
    [SerializeField] private float maxDensity = 1f;

    private readonly HashSet<IndoorZone> indoorZones = new HashSet<IndoorZone>();
    private readonly HashSet<GasVolume> gasVolumes = new HashSet<GasVolume>();

    public bool IsIndoor => indoorZones.Count > 0;

    public float GasDensity
    {
        get
        {
            float sum = 0f;
            foreach (var v in gasVolumes) sum += v.Density; // struct enumerator: alloc yok
            return AtmosphereMath.Density(IsIndoor, baseOutdoorDensity, sum, maxDensity);
        }
    }

    private void Awake()
    {
        if (GetComponent<Rigidbody>() == null)
            Debug.LogWarning("[AtmosphereSensor] Aynı objede Rigidbody yok; hacim trigger olayları gelmeyebilir. Player kök objesine koyun.", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        var indoor = other.GetComponent<IndoorZone>();
        if (indoor != null && indoorZones.Add(indoor)) indoor.NotifyEntered(this);

        var gas = other.GetComponent<GasVolume>();
        if (gas != null && gasVolumes.Add(gas)) gas.NotifyEntered(this);
    }

    private void OnTriggerExit(Collider other)
    {
        var indoor = other.GetComponent<IndoorZone>();
        if (indoor != null && indoorZones.Remove(indoor)) indoor.NotifyLeft(this);

        var gas = other.GetComponent<GasVolume>();
        if (gas != null && gasVolumes.Remove(gas)) gas.NotifyLeft(this);
    }

    // Hacim kapanırken/yok olurken çağrılır (exit olayı gelmeyeceği için).
    internal void Forget(AtmosphereZone zone)
    {
        if (zone is IndoorZone i) indoorZones.Remove(i);
        else if (zone is GasVolume g) gasVolumes.Remove(g);
    }

    private void OnDisable()
    {
        indoorZones.Clear();
        gasVolumes.Clear();
    }
}
