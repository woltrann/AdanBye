using UnityEngine;

// Yerel gaz kaynağı: içindeyken yoğunluğa density eklenir (iç mekanda da eklenir: "zehirli oda").
public class GasVolume : AtmosphereZone
{
    [SerializeField] private float density = 0.5f;

    public float Density => density;
}
