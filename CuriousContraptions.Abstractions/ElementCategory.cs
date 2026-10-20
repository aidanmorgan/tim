namespace CuriousContraptions.Abstractions;

/// <summary>The palette group an element is listed under: the catalogue's player-facing categories (Story 9A.4 D1). Zero is not a
/// category, so an unset value is rejected.</summary>
public enum ElementCategory : byte
{
    Motion = 1,
    Goals = 2,
    Power = 3,
    Control = 4,
    Structure = 5
}
