using System.Collections;
using UnityEngine;

public class Deneme : MonoBehaviour
{
    public MainCharacter characterData;

    public void HeadDamage(float damage)
    {
        characterData.DamagePart(BodyParts.Head, damage);
    }
    public void TorsoDamage(float damage)
    {
        characterData.DamagePart(BodyParts.Torso, damage);
    }
    public void ArmsDamage(float damage)
    {
        characterData.DamagePart(BodyParts.Arms, damage);
    }
    public void LegsDamage(float damage)
    {
        characterData.DamagePart(BodyParts.Legs, damage);
    }

    public void HeadHeal(float heal)
    {
        characterData.HealPart(BodyParts.Head, heal);
    }
    public void TorsoHeal(float heal)
    {
        characterData.HealPart(BodyParts.Torso, heal);
    }
    public void ArmsHeal(float heal)
    {
        characterData.HealPart(BodyParts.Arms, heal);
    }
    public void LegsHeal(float heal)
    {
        characterData.HealPart(BodyParts.Legs, heal);
    }

    public void IncreaseMaxHealth(float heal)
    {
        characterData.IncreaseMaxHealth(heal);
    }


    public void HeadInjured(float damage)
    {
        characterData.Injured(BodyParts.Head, damage);
        StartCoroutine(InjuredTime(damage));
    }
    public void OverTimeInjured(float damage)
    {
        characterData.OverTimeInjured(BodyParts.Head, damage);
        characterData.OverTimeInjured(BodyParts.Torso, damage);
        characterData.OverTimeInjured(BodyParts.Legs, damage);

    }
    private IEnumerator InjuredTime(float damage)
    {
        yield return new WaitForSeconds(5f);
        OverTimeInjured(damage);
    }


    public void IncreaseMaxHunger()
    {
        characterData.IncreaseMaxHunger(50);
    }
}
