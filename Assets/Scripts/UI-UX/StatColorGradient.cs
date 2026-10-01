using UnityEngine;

// Saat göstergelerinin low -> mid -> high renk geçişi. FilledChanger ve SliderColorChanger'da aynı mantığın
// kopyaları var; kapsam dışı olduğu için onlara dokunulmadı, ileride bu yardımcıya taşınabilirler.
public static class StatColorGradient
{
    public static Color Evaluate(Color low, Color mid, Color high, float normalized)
    {
        normalized = Mathf.Clamp01(normalized);
        return normalized < 0.5f
            ? Color.Lerp(low, mid, normalized * 2f)
            : Color.Lerp(mid, high, (normalized - 0.5f) * 2f);
    }
}
