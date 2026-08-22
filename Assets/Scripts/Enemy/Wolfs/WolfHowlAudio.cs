using UnityEngine;

// Tek iş: Howl event'inde AudioSource üzerinden rastgele bir uluma klibi çalmak.
// Pitch/volume'a küçük rastgele varyasyon eklenir - aksi halde sürüdeki her kurt aynı
// klibi birebir aynı tonda çalar, bu da yapay/senkronize bir "koro" hissi verirdi.
[RequireComponent(typeof(AudioSource))]
public class WolfHowlAudio : MonoBehaviour
{
    [SerializeField] private AudioClip[] howlClips;
    [Tooltip("Her ulumada rastgele seçilen pitch aralığı")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);
    [Tooltip("Her ulumada rastgele seçilen ses seviyesi aralığı (AudioSource.volume ile çarpılır)")]
    [SerializeField] private Vector2 volumeRange = new Vector2(0.85f, 1f);

    private AudioSource audioSource;
    private IWolfHowler howler;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        howler = GetComponent<IWolfHowler>();
    }

    private void OnEnable()
    {
        if (howler != null) howler.OnHowl += PlayRandomHowl;
    }

    private void OnDisable()
    {
        if (howler != null) howler.OnHowl -= PlayRandomHowl;
    }

    private void PlayRandomHowl()
    {
        if (audioSource == null || howlClips == null || howlClips.Length == 0) return;

        AudioClip clip = howlClips[Random.Range(0, howlClips.Length)];
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, Random.Range(volumeRange.x, volumeRange.y));
    }
}
