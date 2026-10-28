using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

/// <summary>CAT-062 Springboard (Story 9A.34) on the production compiler and the worker engine, and its committed presentation: the
/// preload launch and its zero/missed controls, the slider stops, and the plate visual's rest pose (0.14 − q along local Y, equal
/// to the compiled moving body). Every bench is built through <see cref="PlacedParts"/>, so the same assertions ran on the record
/// path before the conversion.</summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class SliderInstanceTests(NativeSceneFixture godot)
{
    private const int TicksPerSecond = 120;
    private static readonly GpuBodyId Ball = new(1), Board = new(2);

    private static WorkshopConstruction Free(params IWorkshopInstance[] instances) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances));

    private static float Y(PhysicsBodyRead body) => (body.Body.Cell.Y + body.Body.Local.Y) / 16f;

    private static PhysicsBodyRead Body(WorkshopRead read, GpuBodyId id)
    {
        for (var i = 0; i < read.Bodies.Count; i++) if (read.Bodies[i].Body.Id == id) return read.Bodies[i];
        throw new ArgumentException($"Body {id.Value} is not read.");
    }

    /// <summary>The compiled moving body of the board's one slider.</summary>
    private static GpuBodyId MovingBody(WorkshopConstruction construction) =>
        Assert.Single(WorkshopPhysicsCompiler.Compile(construction, new(1, 2)).Prismatics.ToArray()).BodyB;

    // A ball resting on a board charged 0.2 m is launched well clear; the matched uncharged board (ball resting on its higher plate)
    // gives no launch.
    [Theory]
    [InlineData(.2f, true)]
    [InlineData(0f, false)]
    public async Task APreloadedBoardLaunchesARestingBallAndAnUnchargedOneDoesNot(float compression, bool launches)
    {
        var start = 1 + .14 - compression + .075 + .34 + .005;
        var reads = await WorkshopPhysicsEngineTests.Run(Free(
            WorkshopInput.Generic(PartId.Basketball, Ball, 0, (float)(start), 0, 0, 0, 0, 1),
            PlacedParts.Springboard(Board, 0, 1, 0, 0, 0, 0, 1, compression)), 2 * TicksPerSecond);
        var rise = reads.Max(read => Y(Body(read, Ball))) - (float)start;
        if (launches) Assert.True(rise > .2f, $"charged rise {rise} m");
        else Assert.True(rise < .05f, $"uncharged rise {rise} m");
    }

    // A ball falling 2 m beside a charged board is untouched by it: its path equals the same drop with no board at all.
    [Fact]
    public async Task AMissedBallGetsNothingFromAChargedBoard()
    {
        var ball = WorkshopInput.Generic(PartId.Basketball, Ball, 2, 3, 0, 0, 0, 0, 1);
        var beside = await WorkshopPhysicsEngineTests.Run(Free(ball, PlacedParts.Springboard(Board, 0, 1, 0, 0, 0, 0, 1, .2f)), 2 * TicksPerSecond);
        var alone = await WorkshopPhysicsEngineTests.Run(Free(ball), 2 * TicksPerSecond);
        for (var i = 0; i < alone.Count; i++)
            Assert.InRange(MathF.Abs(Y(Body(beside[i], Ball)) - Y(Body(alone[i], Ball))), 0f, 1e-4f);
    }

    // Stops: a 4 kg ball dropped on the softest undamped board never drives the plate past −0.25 m, and a charged empty plate
    // released on Run never rises past its 0 m rest stop (game-grade 1 cm envelope).
    [Fact]
    public async Task ThePlateHoldsItsLowerAndUpperStops()
    {
        var heavy = Free(WorkshopInput.Generic(PartId.BowlingBall, Ball, 0, 4, 0, 0, 0, 0, 1),
            PlacedParts.Springboard(Board, 0, 1, 0, 0, 0, 0, 1, 0f, stiffness: 120f, damping: 0f));
        var moving = MovingBody(heavy);
        var low = (await WorkshopPhysicsEngineTests.Run(heavy, 2 * TicksPerSecond)).Min(read => Y(Body(read, moving)) - 1.14f);
        Assert.InRange(low, -.26f, -.2f);
        var charged = Free(PlacedParts.Springboard(Board, 0, 1, 0, 0, 0, 0, 1, .2f, stiffness: 1200f, damping: 0f));
        var high = (await WorkshopPhysicsEngineTests.Run(charged, 2 * TicksPerSecond)).Max(read => Y(Body(read, MovingBody(charged))) - 1.14f);
        Assert.InRange(high, -.05f, .01f);
    }

    // The committed presentation: editing the preload 0.2 → 0.1 → 0 → 0.1234567 sets the plate visual's rest pose to exactly
    // 0.14f − q each time (never compounded), and that pose is the compiled moving body's pose in the board frame.
    [Fact]
    public void ThePlateVisualRestsAtTheDeclaredCompressionAndEqualsItsCompiledBody()
    {
        var world = new MachineWorld();
        try
        {
            godot.Tree.Root.AddChild(world);
            world.Registry.Load();
            var construction = typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!;
            foreach (var q in new[] { .2f, .1f, 0f, .1234567f })
            {
                var bench = Free(PlacedParts.Springboard(Board, 0, 1, 0, 0, 0, 0, 1, q));
                construction.SetValue(world, bench);
                world.RestoreConstructionPresentation();
                var parts = (List<MachinePart>)typeof(MachineWorld).GetField("_parts", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(world)!;
                var part = Assert.Single(parts);
                var moving = MovingBody(bench);
                var plate = Assert.IsAssignableFrom<Node3D>(part.PhysicalVisual(moving));
                Assert.Equal(BitConverter.SingleToUInt32Bits(.14f - q), BitConverter.SingleToUInt32Bits(plate.Transform.Origin.Y));
                Assert.Equal((0f, 0f), (plate.Transform.Origin.X, plate.Transform.Origin.Z));
                Assert.Equal(Basis.Identity, plate.Transform.Basis);
                var body = WorkshopPhysicsCompiler.Compile(bench, new(1, 2)).Bodies.ToArray().Single(value => value.Id == moving);
                Assert.InRange(MathF.Abs((body.Cell.Y + body.Local.Y) / 16f - (1f + plate.Transform.Origin.Y)), 0f, 1e-6f);
            }
            Assert.Empty(godot.Errors.Drain());
        }
        finally { world.Free(); }
    }

    // The same generic slider presentation installs the Powered Gate's blade (declared compression Fixed(0)): the blade is the
    // presented visual of the gate's compiled slider body and rests exactly at its scene pose, as before the conversion.
    [Fact]
    public void ThePoweredGateBladeRestsAtItsScenePoseUnderItsSliderIdentity()
    {
        var world = new MachineWorld();
        try
        {
            godot.Tree.Root.AddChild(world);
            world.Registry.Load();
            var reference = world.Registry.Create(PartId.PoweredGate);
            world.AddChild(reference); reference.EnsureConstructed();
            var scenePose = reference.GetNode<Node3D>("Visual/Blade").Transform;
            reference.Free();
            var gate = new GpuBodyId(2);
            var bench = Free(WorkshopInput.Generic(PartId.PoweredGate, gate, 0, 0, 0, 0, 0, 0, 1));
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!.SetValue(world, bench);
            world.RestoreConstructionPresentation();
            var part = Assert.Single((List<MachinePart>)typeof(MachineWorld).GetField("_parts", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(world)!);
            var blade = Assert.IsAssignableFrom<Node3D>(part.PhysicalVisual(bench.Instances.Identities[gate].Sliders[0].Body));
            Assert.Same(part.GetNode<Node3D>("Visual/Blade"), blade);
            Assert.Equal(scenePose, blade.Transform);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { world.Free(); }
    }

    // Exact Run/Reset and Save/Load of a charged board launching a resting ball: every body restores bit for bit and the reloaded
    // board replays the same launch.
    [Fact]
    public async Task RunResetAndSaveLoadAreExact()
    {
        var bench = Free(WorkshopInput.Generic(PartId.Basketball, Ball, 0, (float)(1 + .14 - .2 + .075 + .34 + .005), 0, 0, 0, 0, 1),
            PlacedParts.Springboard(Board, 0, 1, 0, 0, 0, 0, 1, .2f));
        var (built, run, reset) = await NoFixedCapacityTests.RunAndReset(bench, 2 * TicksPerSecond);
        Assert.True(run.Max(read => Y(Body(read, Ball))) > Y(Body(built, Ball)) + .2f, "the charged board launches the ball");
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
        var saved = new WorkshopSavedConstruction(bench, new(3));
        var bytes = WorkshopSaveCodec.Encode(saved);
        var reloaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
        Assert.Equal(saved, reloaded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(reloaded));
        var (_, replay, _) = await NoFixedCapacityTests.RunAndReset(reloaded.Construction, 2 * TicksPerSecond);
        NoFixedCapacityTests.AssertSameState(run[^1], replay[^1], "the reloaded board's run");
    }
}
