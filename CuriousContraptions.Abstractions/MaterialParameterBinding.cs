using System.Collections.Generic;

namespace CuriousContraptions.Abstractions;

public enum MaterialComponent : byte { Restitution = 1, Friction = 2, RollingResistance = 3, BounceThreshold = 4 }

/// <summary>One declared parameter writes one contact property of an exact package body.</summary>
public readonly record struct MaterialParameterBinding(PackageBodyIndex Body, MaterialComponent Component, ParameterId Parameter);

public interface IParameterizedMaterialElement : IPuzzleElementDeclaration
{
    IReadOnlyList<MaterialParameterBinding> MaterialBindings { get; }
}
