using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Bundle;
using CuriousContraptions.Presentation;
using Godot;

namespace CuriousContraptions.Tests;

/// <summary>Fixed source values from c1682bc3 MachinePart binding arms, promoted to the current f32 lane (the 9A.25a goldens).
/// Story 9A.25b: the one-shot bindings are now driven only by their ring kind, so the goldens are sampled through the production path:
/// a record of the binding's kind from the owner, accepted by <see cref="WorkshopDeclaredPresentation"/> (the Animation worker's
/// evaluator), its samples applied by <see cref="MachinePart.ApplyDeclaredProperty"/> exactly as MachineWorld applies them.</summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class PresentationBindingGoldenTests(NativeSceneFixture godot)
{
    private readonly record struct Golden(string Node, PresentationProperty Property, float From, float To);

    public static TheoryData<PartId, float, SimulationEventKind> OneShots => new()
    {
        { PartId.ImpactSwitch, .16f, SimulationEventKind.TriggerActivated }, { PartId.PinballBumper, .32f, SimulationEventKind.Impact },
        { PartId.BallDetector, .35f, SimulationEventKind.Passage }, { PartId.Receiver, .5f, SimulationEventKind.Captured }
    };

    [Theory]
    [MemberData(nameof(OneShots))]
    public async Task RingDrivenOneShotBindingsMatchFixedNativeProperties(PartId id, float duration, SimulationEventKind kind)
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
                Assert.Equal((PresentationDriver.Event, kind, duration), (declarations[i].Driver, declarations[i].Event, declarations[i].Duration));
            }
            // The production evaluator over a real admitted read of the part alone, and one record of its kind from it at that read's
            // physical instant; the evaluator advances in world time.
            var owner = new GpuBodyId(1);
            var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(WorkshopInput.Generic(id, owner, 0, 2, 0, 0, 0, 0, 1),
                WorkshopInput.Generic(PartId.Basketball, new(2), 6, 3, 0, 0, 0, 0, 1)));
            var read = (await WorkshopPhysicsEngineTests.Run(bench, 1))[1];
            var start = read.Capture!.Value.Time;
            var presentation = new WorkshopDeclaredPresentation(bench, new(default, WorkshopResponseKind.Read,
                new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running, read, new(101, 206), new(1),
                new(1), new(1), new(1), new(read.Tick.Value)));
            var world = new WorkshopWorldProjection(read.Epoch, new(1), read.Tick, new(start.Value), WorldPlayback.Running);
            var substeps = (uint)(WorkshopCadenceSettings.PhysicalFrequency * WorkshopCadenceSettings.Default().SimulationRate.Denominator /
                WorkshopCadenceSettings.Default().SimulationRate.Numerator);
            presentation.AcceptEvents(new(read.Epoch.Value, 0, [new(1, read.Tick.Value, kind, owner.Value, 9,
                checked((uint)(read.Tick.Value * substeps)), 0f, 1f, 1, 0)]), world);
            // Analytic quarter/half/end samples, independent of the evaluator implementation.
            float[] amounts = id == PartId.PinballBumper ? [0, .5f, 1, .5f, 0] : [0, .103515625f, .5f, .896484375f, 1];
            for (var step = 0; step <= 4; step++)
            {
                var samples = presentation.Advance(AnimationTestTime.Seconds(read.Tick.Value * substeps / 480d + (double)duration * step / 4));
                Assert.Equal(golden.Length, samples.Count);
                foreach (var sample in samples) part.ApplyDeclaredProperty(sample.Binding, sample.Value);
                foreach (var expected in golden)
                {
                    var value = amounts[step] == 0 ? expected.From : amounts[step] == 1 ? expected.To :
                        (float)((double)expected.From + ((double)expected.To - expected.From) * amounts[step]);
                    // A 0.00001 property-unit bound is visually negligible for colour, scale and metre offsets.
                    Assert.InRange(Read(part, expected), value - .00001f, value + .00001f);
                }
            }
            // MachineWorld Restore puts every declared binding, one-shot cues included, back at rest.
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
