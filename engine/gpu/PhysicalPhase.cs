namespace CuriousContraptions.Gpu;

/// <summary>The f32 phase of a physical time within its step ordinal: a finite value in [−2048, 2048) of the ordinal's 4096
/// phase units, never negative at ordinal zero. The one phase rule every physical, activation and animation time obeys.</summary>
public static class PhysicalPhase
{
    public const float Minimum = -2048f;
    public const float Limit = 2048f;
    public static bool IsAdmitted(uint ordinal, float phase) =>
        float.IsFinite(phase) && phase >= Minimum && phase < Limit && (ordinal != 0 || phase >= 0f);
}
