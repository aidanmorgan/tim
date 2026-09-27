using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum TubeMouthId { Start, End }

/// <summary>Local opening at the outside face of its collar; normal points out of the tube.</summary>
public readonly record struct TubeMouth(TubeMouthId Id, Vector3 Position, Vector3 Outward, float BoreRadius);

public interface ITubePart
{
    IEnumerable<TubeMouth> Mouths { get; }
}
