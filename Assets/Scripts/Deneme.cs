using System.Collections;
using UnityEngine;

public class Deneme : MonoBehaviour
{
    public MainCharacter characterData;


    public void HeadDamage(float damage)
    {
        characterData.DamagePart("head", damage);
    }
    public void TorsoDamage(float damage)
    {
        characterData.DamagePart("torso", damage);
    }
    public void ArmsDamage(float damage)
    {
        characterData.DamagePart("arms", damage);
    }
    public void LegsDamage(float damage)
    {
        characterData.DamagePart("legs", damage);
    }


    public void HeadHeal(float heal)
    {
        characterData.HealPart("head", heal);
    }
    public void TorsoHeal(float heal)
    {
        characterData.HealPart("torso", heal);
    }
    public void ArmsHeal(float heal)
    {
        characterData.HealPart("arms", heal);
    }
    public void LegsHeal(float heal)
    {
        characterData.HealPart("legs", heal);
    }



    public void IncreaseMaxHealth(float heal)
    {
        characterData.IncreaseMaxHealth(heal);
    }



    public void HeadInjured(float damage)
    {
        characterData.Injured("head", damage);
        StartCoroutine(InjuredTime(damage));
    }
    public void OverTimeInjured(float damage)
    {
        characterData.OverTimeInjured("head", damage);

    }
    private IEnumerator InjuredTime(float damage)
    {
        yield return new WaitForSeconds(5f);
        OverTimeInjured(damage);
    }
}
