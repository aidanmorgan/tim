using System;
using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions;

public enum WorkshopVisualProperty { LocalY, LocalRotationZ, AlbedoRed, AlbedoGreen, AlbedoBlue, EmissionRed, EmissionGreen, EmissionBlue }

/// <summary>Render boundary for immutable property bindings evaluated by shared Animation.</summary>
public sealed class WorkshopVisualBinding
{
    private readonly Node3D _target;
    private readonly WorkshopVisualProperty _property;
    private readonly AnimationValue _neutral, _active;
    public WorkshopVisualBinding(Node3D target, WorkshopVisualProperty property, Half neutral, Half active)
    {
        if (!Enum.IsDefined(property)) throw new ArgumentException("Unknown visual binding property.");
        _target = target; _property = property;
        _neutral = Value(property, neutral); _active = Value(property, active);
        if (property is not (WorkshopVisualProperty.LocalY or WorkshopVisualProperty.LocalRotationZ) &&
            (target is not MeshInstance3D mesh || mesh.MaterialOverride is not StandardMaterial3D))
            throw new ArgumentException("Colour binding requires its owned material.");
    }
    private static AnimationValue Value(WorkshopVisualProperty property, Half value) => property switch
    {
        WorkshopVisualProperty.LocalRotationZ => new(new AnimationRadians(value)),
        WorkshopVisualProperty.LocalY => new(new AnimationMetres(value)),
        WorkshopVisualProperty.AlbedoRed or WorkshopVisualProperty.EmissionRed => AnimationValue.Channel(AnimationProperty.ColourRed, new(value)),
        WorkshopVisualProperty.AlbedoGreen or WorkshopVisualProperty.EmissionGreen => AnimationValue.Channel(AnimationProperty.ColourGreen, new(value)),
        WorkshopVisualProperty.AlbedoBlue or WorkshopVisualProperty.EmissionBlue => AnimationValue.Channel(AnimationProperty.ColourBlue, new(value)),
        _ => throw new ArgumentException("Unknown visual binding property.")
    };
    public void Apply(AnimationColourBlend progress)
    {
        var value = (float)BitConverter.UInt16BitsToHalf(AnimationValue.Blend(_neutral, _active, progress).CanonicalBits);
        if (_property == WorkshopVisualProperty.LocalY)
        {
            var position = _target.Position; position.Y = value; _target.Position = position; return;
        }
        if (_property == WorkshopVisualProperty.LocalRotationZ)
        { var rotation = _target.Rotation; rotation.Z = value; _target.Rotation = rotation; return; }
        var material = (StandardMaterial3D)((MeshInstance3D)_target).MaterialOverride;
        var emission = _property is WorkshopVisualProperty.EmissionRed or WorkshopVisualProperty.EmissionGreen or WorkshopVisualProperty.EmissionBlue;
        var colour = emission ? material.Emission : material.AlbedoColor;
        switch (_property)
        {
            case WorkshopVisualProperty.AlbedoRed: case WorkshopVisualProperty.EmissionRed: colour.R = value; break;
            case WorkshopVisualProperty.AlbedoGreen: case WorkshopVisualProperty.EmissionGreen: colour.G = value; break;
            case WorkshopVisualProperty.AlbedoBlue: case WorkshopVisualProperty.EmissionBlue: colour.B = value; break;
        }
        if (emission) material.Emission = colour; else material.AlbedoColor = colour;
    }
}
