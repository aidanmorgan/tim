using System;
using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions;

public enum WorkshopVisualProperty { LocalX, LocalY, LocalZ, LocalRotationZ, UniformScale, ScaleX, ScaleY, ScaleZ, AlbedoRed, AlbedoGreen, AlbedoBlue, EmissionRed, EmissionGreen, EmissionBlue, Visible }

/// <summary>Render boundary for immutable property bindings evaluated by shared Animation.</summary>
public sealed class WorkshopVisualBinding
{
    private readonly Node3D _target;
    private readonly WorkshopVisualProperty _property;
    private readonly AnimationValue _neutral, _active;
    public WorkshopVisualBinding(Node3D target, WorkshopVisualProperty property, float neutral, float active)
    {
        if (!Enum.IsDefined(property)) throw new ArgumentException("Unknown visual binding property.");
        _target = target; _property = property;
        _neutral = Value(property, neutral); _active = Value(property, active);
        if (property is >= WorkshopVisualProperty.AlbedoRed and <= WorkshopVisualProperty.EmissionBlue &&
            (target is not MeshInstance3D mesh || mesh.MaterialOverride is not StandardMaterial3D))
            throw new ArgumentException("Colour binding requires its owned material.");
    }
    private static AnimationValue Value(WorkshopVisualProperty property, float value) => property switch
    {
        WorkshopVisualProperty.Visible => new(new AnimationColourBlend(value)),
        WorkshopVisualProperty.LocalRotationZ => new(new AnimationRadians(value)),
        WorkshopVisualProperty.UniformScale or WorkshopVisualProperty.ScaleX or WorkshopVisualProperty.ScaleY or WorkshopVisualProperty.ScaleZ => new(new AnimationScale(value)),
        WorkshopVisualProperty.LocalX or WorkshopVisualProperty.LocalY or WorkshopVisualProperty.LocalZ => new(new AnimationMetres(value)),
        WorkshopVisualProperty.AlbedoRed or WorkshopVisualProperty.EmissionRed => AnimationValue.Channel(AnimationProperty.ColourRed, new(value)),
        WorkshopVisualProperty.AlbedoGreen or WorkshopVisualProperty.EmissionGreen => AnimationValue.Channel(AnimationProperty.ColourGreen, new(value)),
        WorkshopVisualProperty.AlbedoBlue or WorkshopVisualProperty.EmissionBlue => AnimationValue.Channel(AnimationProperty.ColourBlue, new(value)),
        _ => throw new ArgumentException("Unknown visual binding property.")
    };
    public void Apply(AnimationColourBlend progress) => ApplyValue(AnimationValue.Blend(_neutral, _active, progress));
    public void ApplyValue(AnimationValue sample)
    {
        if (sample.Property != _neutral.Property) throw new ArgumentException("Visual sample property differs from its declaration.");
        var value = BitConverter.UInt32BitsToSingle(sample.CanonicalBits);
        if (_property == WorkshopVisualProperty.Visible) { _target.Visible = value >= .5f; return; }
        if (_property is WorkshopVisualProperty.LocalX or WorkshopVisualProperty.LocalY or WorkshopVisualProperty.LocalZ)
        {
            var position = _target.Position;
            if (_property == WorkshopVisualProperty.LocalX) position.X = value;
            else if (_property == WorkshopVisualProperty.LocalY) position.Y = value;
            else position.Z = value;
            _target.Position = position; return;
        }
        if (_property is WorkshopVisualProperty.ScaleX or WorkshopVisualProperty.ScaleY or WorkshopVisualProperty.ScaleZ)
        {
            var scale = _target.Scale;
            if (_property == WorkshopVisualProperty.ScaleX) scale.X = value;
            else if (_property == WorkshopVisualProperty.ScaleY) scale.Y = value;
            else scale.Z = value;
            _target.Scale = scale; return;
        }
        if (_property == WorkshopVisualProperty.LocalRotationZ)
        { var rotation = _target.Rotation; rotation.Z = value; _target.Rotation = rotation; return; }
        if (_property == WorkshopVisualProperty.UniformScale) { _target.Scale = Vector3.One * value; return; }
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
