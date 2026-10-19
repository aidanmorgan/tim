using System;
using System.Linq;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public sealed class PartRegistryTests(NativeSceneFixture godot)
{
    [Fact]
    public void Discover_LoadsEveryDeliveredPart_AndExcludesUndeliveredParts()
    {
        var registry = new PartRegistry();
        registry.Discover();

        Assert.Equal(22, registry.Definitions.Count);

        PartId[] expected =
        [
            PartId.Basketball,
            PartId.BowlingBall,
            PartId.Receiver,
            PartId.Ramp,
            PartId.ImpactSwitch,
            PartId.SignalLamp,
            PartId.Wall,
            PartId.Delay,
            PartId.PinballBumper,
            PartId.Domino,
            PartId.Battery,
            PartId.Springboard,
            PartId.Pipe,
            PartId.BallDetector,
            PartId.PipeBend45,
            PartId.PipeBend90,
            PartId.Funnel,
            PartId.PoweredGate,
            PartId.PressurePlate,
            PartId.Counter,
            PartId.Clock,
            PartId.Latch
        ];

        foreach (var id in expected)
        {
            Assert.True(registry.Definitions.ContainsKey(id), $"Expected registry to contain part '{id}'.");
            var definition = registry.Definitions[id];
            Assert.Equal(id, definition.PartId);
            Assert.False(string.IsNullOrWhiteSpace(definition.Title));
            Assert.NotNull(definition.Scene);
            if (id == PartId.Clock)
            {
                Assert.True(definition.Parameters.ContainsKey("interval_seconds"));
            }
            else
            {
                Assert.Empty(definition.Parameters);
            }
        }

        foreach (var part in UndeliveredParts.All)
            Assert.False(registry.Definitions.ContainsKey(UndeliveredParts.Id(part)), $"Undelivered part '{part}' must not be registered.");
    }

    [Fact]
    public void Create_InstantiatesAndConfiguresEachDiscoveredPart()
    {
        var registry = new PartRegistry();
        registry.Discover();

        foreach (var (id, definition) in registry.Definitions)
        {
            var part = registry.Create(id);
            try
            {
                godot.Tree.Root.AddChild(part);
                Assert.NotNull(part);
                Assert.Same(definition, part.Definition);
                Assert.Equal(id, part.PartId);
            }
            finally
            {
                part.GetParent()?.RemoveChild(part);
                part.Free();
            }
        }
    }

    [Fact]
    public void Create_ThrowsArgumentException_OnUnknownOrUnregisteredPartId()
    {
        var registry = new PartRegistry();
        registry.Discover();

        Assert.Throws<ArgumentException>(() => registry.Create(new PartId("unknown-part")));
    }

    [Fact]
    public void PartId_ValidatesValuesAndSupportsEqualityAndComparison()
    {
        Assert.Throws<ArgumentException>(() => new PartId(null!));
        Assert.Throws<ArgumentException>(() => new PartId(""));
        Assert.Throws<ArgumentException>(() => new PartId("   "));

        var a = new PartId("ball");
        var b = new PartId("ball");
        var c = new PartId("ramp");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.True(a == b);
        Assert.True(a != c);
        Assert.Equal("ball", a.ToString());
        Assert.Equal("ball", a.Value);
        Assert.True(a.CompareTo(c) < 0);
        Assert.True(c.CompareTo(a) > 0);
        Assert.Equal(0, a.CompareTo(b));

        Assert.Equal(PartId.Basketball, PartId.FromNumericId(PartId.Basketball.ToNumericId()));
        Assert.Equal(PartId.Springboard, PartId.FromNumericId(PartId.Springboard.ToNumericId()));
        Assert.Equal(default(PartId), PartId.FromNumericId(0));
        Assert.Throws<ArgumentException>(() => PartId.FromNumericId(999));
    }

    [Fact]
    public void WorkshopInventoryPolicy_Free_PopulatesFromDiscoveredDefinitions()
    {
        var registry = new PartRegistry();
        registry.Discover();

        Assert.Equal(registry.Definitions.Count, WorkshopInventoryPolicy.Free.Count);
        foreach (var id in registry.Definitions.Keys)
        {
            Assert.True(WorkshopInventoryPolicy.Free.ContainsKey(id));
            Assert.Equal(PartAllowance.Unlimited, WorkshopInventoryPolicy.Free[id]);
        }

        Assert.Throws<ArgumentException>(() => WorkshopInventoryPolicy.GenerateFree([]));
    }
}
