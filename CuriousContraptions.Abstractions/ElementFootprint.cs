namespace CuriousContraptions.Abstractions;

/// <summary>The rows one placed instance adds to the compiled scene beyond its colliders: its bodies, how many of them move, and its
/// prismatic constraints. A count per part, never a table size.</summary>
public readonly record struct ElementFootprint(
    int Bodies,
    int MovingBodies,
    int PrismaticConstraints,
    int RevoluteConstraints);
