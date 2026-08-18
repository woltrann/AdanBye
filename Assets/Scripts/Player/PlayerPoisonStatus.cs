using System.Collections;
using UnityEngine;

// Eskiden PlayerMovement içindeydi - hareketle hiçbir ilgisi yok.
// Tek iş: açık alanda zehirlenme durumunu yönetmek.
public class PlayerPoisonStatus : MonoBehaviour
{
    [SerializeField] private MainCharacter mainCharacter;
    [SerializeField] private float poisonInterval = 2f;
    [SerializeField] private float poisonAmountPerTick = 1f;

    public bool isOutSide = true;

    private Coroutine poisonRoutine;

    private void Awake()
    {
        // PlayerManager zaten mainCharacter referansını tutuyor - tekrar aynı veriyi
        // Inspector'da elle atamak yerine oradan al (tek doğruluk kaynağı).
        if (mainCharacter == null)
        {
            mainCharacter = GetComponent<PlayerManager>().mainCharacter;
        }
    }

    private void Update()
    {
        if (isOutSide && poisonRoutine == null)
        {
            poisonRoutine = StartCoroutine(PoisonOverTime());
        }
        else if (!isOutSide && poisonRoutine != null)
        {
            StopCoroutine(poisonRoutine);
            poisonRoutine = null;
        }
    }

    private IEnumerator PoisonOverTime()
    {
        while (true)
        {
            if (UXobjects.Instance.gassFilter <= 0)
            {
                mainCharacter.IncreasePoison(poisonAmountPerTick);
            }
            yield return new WaitForSeconds(poisonInterval);
        }
    }
}
