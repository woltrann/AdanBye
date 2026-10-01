using System.Collections.Generic;

public enum StatKind { Hunger, Thirst, Stamina }

// StatKind -> kaynak eşlemesinin TEK noktası. Yeni stat: enum'a değer + aşağıya bir satır; StatBarView'e dokunulmaz.
public static class StatBarSourceFactory
{
    // Kaynak henüz kurulamıyorsa (Player geç doğdu) null döner; çağıran sonraki karede tekrar dener.
    private delegate IStatBarSource Creator(PlayerManager player, MainCharacter characterOverride);

    private static readonly Dictionary<StatKind, Creator> Creators = new Dictionary<StatKind, Creator>
    {
        { StatKind.Hunger,  (p, c) => ResolveCharacter(p, c) is MainCharacter m ? new HungerSource(m) : null },
        { StatKind.Thirst,  (p, c) => ResolveCharacter(p, c) is MainCharacter m ? new ThirstSource(m) : null },
        { StatKind.Stamina, (p, c) => p != null && p.GetComponent<IStaminaReadout>() is IStaminaReadout s ? new StaminaSource(s) : null },
    };

    public static IStatBarSource TryCreate(StatKind kind, PlayerManager player, MainCharacter characterOverride)
    {
        return Creators.TryGetValue(kind, out Creator create) ? create(player, characterOverride) : null;
    }

    // Override varsa o, yoksa PlayerManager'ın asset'i: her bara asset sürüklemek gerekmesin.
    private static MainCharacter ResolveCharacter(PlayerManager player, MainCharacter characterOverride)
    {
        if (characterOverride != null) return characterOverride;
        return player != null ? player.mainCharacter : null;
    }
}
