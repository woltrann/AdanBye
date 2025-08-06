using System.Collections;
using UnityEngine;

public class Deneme : MonoBehaviour
{
    public MainCharacter characterData;


// Yaralanýp belli bir süre MaxCanýn azalmasý 
    public void Injured(float amount)
    {
        characterData.Injured(amount);
        characterData.TakeDamage(amount);
        StartCoroutine(InjuredOverTime(amount, amount/5f));
    }
    private IEnumerator InjuredOverTime(float amount, float duration)
    {
        yield return new WaitForSeconds(duration);
        characterData.InjuredOverTime(amount);
    }

}
