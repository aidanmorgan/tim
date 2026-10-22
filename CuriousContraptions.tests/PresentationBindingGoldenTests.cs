using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Bundle;
using CuriousContraptions.Presentation;
using Godot;

namespace CuriousContraptions.Tests;

/// <summary>Fixed source values from c1682bc3 MachinePart binding arms, promoted to the current f32 lane.
/// The four newly live cues have separate real-simulation temporal/native tests before these preservation samples.</summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class PresentationBindingGoldenTests(NativeSceneFixture godot)
{
    private readonly record struct Golden(string Node, PresentationProperty Property, float From, float To);

    public static TheoryData<PartId, float> OneShots => new()
    {
        { PartId.ImpactSwitch, .16f }, { PartId.PinballBumper, .32f },
        { PartId.BallDetector, .35f }, { PartId.Receiver, .5f }
    };

    [Theory]
    [MemberData(nameof(OneShots))]
    public void RetainedOneShotBindingsMatchFixedNativeProperties(PartId id, float duration)
    {
        Golden[] golden = id switch
        {
            _ when id == PartId.ImpactSwitch => [
                new("Visual/Button", PresentationProperty.LocalY, .06f, -.02f),
                new("Visual/Button", PresentationProperty.AlbedoRed, .94f, 191f/255),
                new("Visual/Button", PresentationProperty.AlbedoGreen, .43f, 245f/255),
                new("Visual/Button", PresentationProperty.AlbedoBlue, .33f, 176f/255)],
            _ when id == PartId.PinballBumper => [
                new("Visual/BumperHead", PresentationProperty.UniformScale, 1, .88f),
                new("Visual/ImpactRing", PresentationProperty.UniformScale, 1, 1.24f),
                new("Visual/ImpactRing", PresentationProperty.AlbedoRed, 1, 247f/255),
                new("Visual/ImpactRing", PresentationProperty.AlbedoGreen, 240f/255, 203f/255),
                new("Visual/ImpactRing", PresentationProperty.AlbedoBlue, 194f/255, 82f/255)],
            _ when id == PartId.BallDetector => [
                new("Visual/Visual_Indicator", PresentationProperty.AlbedoRed, 85f/255, 247f/255),
                new("Visual/Visual_Indicator", PresentationProperty.AlbedoGreen, 101f/255, 203f/255),
                new("Visual/Visual_Indicator", PresentationProperty.AlbedoBlue, 115f/255, 82f/255)],
            _ when id == PartId.Receiver => [
                new("Visual/Visual_Halo", PresentationProperty.UniformScale, 1.02f, 1.02f),
                new("Visual/Visual_Halo", PresentationProperty.AlbedoRed, 189f/255, 1),
                new("Visual/Visual_Halo", PresentationProperty.AlbedoGreen, 244f/255, 1),
                new("Visual/Visual_Halo", PresentationProperty.AlbedoBlue, 189f/255, 1)],
            _ => throw new ArgumentOutOfRangeException(nameof(id))
        };
        var registry = new PartRegistry(); registry.Load();
        var part = registry.Create(id);
        try
        {
            godot.Tree.Root.AddChild(part);
            var declarations = part.Definition.Presentation.Bindings;
            Assert.Equal(golden.Length, declarations.Count);
            for (var i = 0; i < golden.Length; i++)
            {
                Assert.Equal(golden[i].Node, declarations[i].Node);
                Assert.Equal(golden[i].Property, declarations[i].Property);
                Assert.Equal(golden[i].From, declarations[i].From);
                Assert.Equal(golden[i].To, declarations[i].To);
            }
            var curve = CosmeticCurveDeclaration.For(id);
            Assert.Equal(duration, curve.Duration);
            var batch = new AnimationBatch(1);
            AnimationHandle handle;
            if (id == PartId.PinballBumper)
            {
                Assert.Equal(AnimationImpulseCurve.SineSquaredPulse, curve.ImpulseCurve);
                handle = batch.RegisterImpulses(new(new(1), AnimationProperty.ColourBlend),
                    new(new(duration), curve.ImpulseCurve, curve.Overlap, AnimationImpulseVisibility.AdvanceWhileHidden,
                        AnimationClock.Presentation, 64, AnimationImpulseTiming.EventTime, new(.5f)),
                    new(new AnimationColourBlend(0)), new(new AnimationColourBlend(1)));
                batch.EnqueueImpulse(handle, new(1), new(1), 0);
            }
            else
            {
                Assert.Equal(AnimationCurve.SmoothStep, curve.Curve);
                handle = batch.Register(new(new(1), AnimationProperty.ColourBlend),
                    new(new(new AnimationColourBlend(0)), new(new AnimationColourBlend(1)), new(duration),
                        curve.Curve, AnimationRepeat.Once, AnimationClock.Presentation));
                batch.Start(handle);
            }
            // Analytic quarter/half/end samples, independent of the evaluator implementation.
            float[] amounts = id == PartId.PinballBumper ? [0, .5f, 1, .5f, 0] : [0, .103515625f, .5f, .896484375f, 1];
            for (var step = 0; step <= 4; step++)
            {
                batch.Advance((ulong)step + 1, (double)duration * step / 4, 0);
                part.ApplyCosmetic(new(BitConverter.UInt32BitsToSingle(batch.Read(handle).Value.CanonicalBits), AnimationTimerPhase.None));
                foreach (var expected in golden)
                {
                    var value = amounts[step] == 0 ? expected.From : amounts[step] == 1 ? expected.To :
                        (float)((double)expected.From + ((double)expected.To - expected.From) * amounts[step]);
                    Assert.Equal(value, Read(part, expected));
                }
            }
            // MachineWorld Restore applies neutral to the retained one-shot relay before restoring physical/declared properties.
            part.ApplyCosmetic(WorkshopCosmeticSample.Neutral);
            part.ResetPhysicalVisuals();
            foreach (var expected in golden) Assert.Equal(expected.From, Read(part, expected));
        }
        finally { part.Free(); }
    }

    [Fact]
    public void UnsupportedPropertyCurveAndMissingNodeAreRejectedAtTheirAdmissionBoundary()
    {
        var registry = new PartRegistry(); registry.Load();
        var entry = DeclaredPhysics.Of(PartId.Clock);
        var pulse = entry.Presentation.Bindings[1];
        PresentationBinding[] invalid = [
            pulse with { Duration = 0 }, pulse with { Driver = PresentationDriver.CommittedSupply },
            pulse with { From = -1 }, pulse with { To = 2 },
            pulse with { Property = PresentationProperty.UniformScale, From = 0 },
            pulse with { Curve = PresentationCurve.ExponentialFollow, Response = 1 },
            pulse with { Overlap = PresentationOverlap.Replace },
            pulse with { Response = 1 }, pulse with { Intermediate = float.NaN }
        ];
        foreach (var binding in invalid)
        {
            var changed = entry with { Presentation = entry.Presentation with { Bindings = [binding] } };
            Assert.NotEmpty(BundleValidator.Validate([new NamedEntry("clock", changed)]));
        }
        var definition = registry.Definitions[PartId.Clock];
        var missing = entry with { Presentation = entry.Presentation with { Bindings = [pulse with { Node = "Visual/Missing" }] } };
        var candidate = definition.Scene!.Instantiate<MachinePart>();
        try
        {
            var changed = new PartDefinition(definition.PartId, definition.Title, definition.Description, definition.Category,
                definition.Color, definition.IconPath, definition.Scene, missing);
            candidate.Configure(changed);
            Assert.Throws<ArgumentException>(() => candidate.EnsureConstructed());
            Assert.Same(entry, DeclaredPhysics.Of(PartId.Clock));
        }
        finally { candidate.Free(); }
    }

    private static float Read(MachinePart part, Golden expected)
    {
        var node = part.GetNode<Node3D>(expected.Node);
        if (expected.Property == PresentationProperty.LocalY) return node.Position.Y;
        if (expected.Property == PresentationProperty.UniformScale) return node.Scale.X;
        var colour = Assert.IsType<StandardMaterial3D>(Assert.IsType<MeshInstance3D>(node).MaterialOverride).AlbedoColor;
        return expected.Property switch
        {
            PresentationProperty.AlbedoRed => colour.R,
            PresentationProperty.AlbedoGreen => colour.G,
            PresentationProperty.AlbedoBlue => colour.B,
            _ => throw new ArgumentException("Unsupported golden property.")
        };
    }
}
