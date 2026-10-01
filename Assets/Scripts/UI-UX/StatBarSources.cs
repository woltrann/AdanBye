using AdanBye.Survival;

// MainCharacter'daki current/max çiftini okuyan ortak taban: açlık ve susuzluk yalnızca hangi alanları okuduklarında ayrışır.
public abstract class CharacterStatSource : IStatBarSource
{
    protected readonly MainCharacter Character;

    protected CharacterStatSource(MainCharacter character) { Character = character; }

    protected abstract float Current { get; }
    protected abstract float Max { get; }

    public float Ratio => StaminaFillMath.Ratio(Current, Max);
    public float LimitRatio => 1f;
    public bool IsAlert => false;
}

public sealed class HungerSource : CharacterStatSource
{
    public HungerSource(MainCharacter character) : base(character) { }
    protected override float Current => Character.currentHunger;
    protected override float Max => Character.maxHunger;
}

public sealed class ThirstSource : CharacterStatSource
{
    public ThirstSource(MainCharacter character) : base(character) { }
    protected override float Current => Character.currentThirst;
    protected override float Max => Character.maxThirst;
}

// Koşu staminası (Current) çubuğu; limitFill'deki soluk bölge ana staminayı (Ceiling) gösterir.
public sealed class StaminaSource : IStatBarSource
{
    private readonly IStaminaReadout stamina;

    public StaminaSource(IStaminaReadout stamina) { this.stamina = stamina; }

    public float Ratio => StaminaFillMath.Ratio(stamina.Current, stamina.Max);
    public float LimitRatio => StaminaFillMath.Ratio(stamina.Ceiling, stamina.Max);
    public bool IsAlert => stamina.IsCollapsed || stamina.IsExhausted;
}

// Ana stamina (Ceiling) ayrı bar olarak: 0'a inince bayılma olduğu için asıl "enerji" budur.
public sealed class EnergySource : IStatBarSource
{
    private readonly IStaminaReadout stamina;

    public EnergySource(IStaminaReadout stamina) { this.stamina = stamina; }

    public float Ratio => StaminaFillMath.Ratio(stamina.Ceiling, stamina.Max);
    public float LimitRatio => 1f;
    public bool IsAlert => stamina.IsCollapsed || stamina.IsExhausted;
}
