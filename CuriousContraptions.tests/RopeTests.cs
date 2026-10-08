using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class RopeTests(NativeSceneFixture godot)
{
    private MachineWorld World()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        return world;
    }
    private static MachinePart Weight(MachineWorld world, string id, float mass, Vector3 at) =>
        world.AddPart(new() { Id = id, Kind = "weight", Position = [at.X, at.Y, at.Z],
            Properties = new() { [PartParameterName.Of(WeightParameter.Mass)] = mass } });

    [Theory]
    [InlineData(1f, 4f, false)]
    [InlineData(4f, 1f, false)]
    [InlineData(1f, 1f, false)]
    [InlineData(1f, 4f, true)]
    public void TwoFixedPulleysTransferLoadAccordingToMass(float leftMass, float rightMass, bool reverseLinks)
    {
        var world = World();
        try
        {
            var a = Weight(world, "a", leftMass, new(-2, 2, .12f));
            var b = Weight(world, "b", rightMass, new(2, 2, .12f));
            var left = world.AddPart(new() { Id = "left", Kind = "pulley", Position = [-2, 6, 0] });
            var right = world.AddPart(new() { Id = "right", Kind = "pulley", Position = [2, 6, 0] });
            Assert.True(world.Connect(a, left));
            Assert.True(world.Connect(left, right));
            Assert.True(world.Connect(right, b));
            if (reverseLinks)
            {
                var construction=world.Snapshot();
                construction.Connections=construction.Connections.Select(link=>link with
                    {From=link.To,To=link.From,FromPort=link.ToPort,ToPort=link.FromPort}).Reverse().ToList();
                var ids=new[]{a.Uid,b.Uid,left.Uid,right.Uid};
                world.LoadMachine(construction);
                a=world.FindPart(ids[0])!;b=world.FindPart(ids[1])!;
                left=world.FindPart(ids[2])!;right=world.FindPart(ids[3])!;
            }
            var build = world.Snapshot();
            world.Start();
            for (var i = 0; i < 30; i++) world.Step();
            var acceleration = world.Gravity * (rightMass - leftMass) / (leftMass + rightMass);
            Assert.InRange(Mathf.Abs(world.PhysicsAssembly.Body(new(a,MachinePart.RootBody)).LinearVelocity.Y - acceleration * .25f), 0, .015f);
            Assert.InRange(Mathf.Abs(world.PhysicsAssembly.Body(new(b,MachinePart.RootBody)).LinearVelocity.Y + acceleration * .25f), 0, .015f);
            Assert.InRange(Mathf.Abs(a.Position.Y + b.Position.Y - 4), 0, .001f);
            var rope = Assert.Single(world.Ropes);
            Assert.Equal(RopeState.Taut, rope.State(world));
            Assert.InRange(rope.CurrentLength(world) - rope.Length, -.001f, .001f);
            if (leftMass != rightMass) Assert.NotEqual(0, ((PulleyPart)left).WheelAngle);
            var signature = world.StateSignature();
            world.Restore();
            Assert.Equal(build.Connections.Select(c => c.RopeLength), world.Connections.Select(c => c.RopeLength));
            Assert.All(world.Parts.OfType<PulleyPart>(), p => Assert.Equal(0, p.WheelAngle));
            world.Start();
            for (var i = 0; i < 30; i++) world.Step();
            Assert.Equal(signature, world.StateSignature());
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData("counterweight", 0f)]
    [InlineData("counterweight", .45f)]
    [InlineData("counterweight", 1f)]
    [InlineData("pulley_depth", 0f)]
    [InlineData("pulley_depth", .45f)]
    [InlineData("pulley_depth", 1f)]
    public void LessonsRequireTheirRopeSpansAndHeavyCounterweight(string id, float precision)
    {
        var world = World();
        try
        {
            var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString("res://content/puzzles.json")).Single(p => p.Id == id);
            var solution = puzzle.CreateMachine();
            solution.Parts.AddRange(puzzle.Solution);
            solution.Connections = puzzle.SolutionConnections;
            world.Precision = precision;
            world.LoadMachine(solution);
            world.Start();
            for (var i = 0; i < 1500 && world.Running; i++) world.Step();
            Assert.True(world.Won);
            foreach (var missing in puzzle.SolutionConnections)
            {
                var data = MachineCodec.Clone(solution);
                data.Connections.RemoveAll(c => c.From == missing.From && c.To == missing.To);
                world.LoadMachine(data);
                world.Start();
                for (var i = 0; i < 1500 && world.Running; i++) world.Step();
                Assert.False(world.Won, id + " still solved without " + missing.From + " -> " + missing.To);
            }
            var equal = MachineCodec.Clone(solution);
            equal.Parts.Single(p => p.Id == "weight_1").Properties[PartParameterName.Of(WeightParameter.Mass)] = 1;
            world.LoadMachine(equal);
            world.Start();
            for (var i = 0; i < 1500 && world.Running; i++) world.Step();
            Assert.False(world.Won, "Equal counterweights cannot raise the load to the switch.");
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(0f)]
    [InlineData(20f)]
    [InlineData(float.NaN)]
    public void InvalidWeightMassIsRejectedBeforeAnInstanceIsAdded(float mass)
    {
        var world = World();
        try
        {
            Assert.Throws<ArgumentException>(() => Weight(world, "weight", mass, Vector3.Zero));
            Assert.Empty(world.Parts);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void SlackRopeDoesNotPushAndTautTetherStopsExtension()
    {
        var world = World();
        try
        {
            var anchor = world.AddPart(new() { Id = "anchor", Kind = "rope_anchor", Position = [0, 6, -.18f] });
            var weight = Weight(world, "weight", 1, new(0, 4, 0));
            Assert.True(world.Connect(anchor, weight));
            var construction=world.Snapshot();
            construction.Connections[0]=construction.Connections[0] with
                {RopeLength=construction.Connections[0].RopeLength+1};
            var weightId=weight.Uid;
            world.LoadMachine(construction);
            weight=world.FindPart(weightId)!;
            world.Start();
            world.Step();
            Assert.Equal(RopeState.Slack, Assert.Single(world.Ropes).State(world));
            Assert.True(world.PhysicsAssembly.Body(new(weight,MachinePart.RootBody)).LinearVelocity.Y < 0);
            for (var i = 0; i < 120; i++) world.Step();
            var rope = Assert.Single(world.Ropes);
            Assert.Equal(RopeState.Taut, rope.State(world));
            Assert.InRange(rope.CurrentLength(world), rope.Length - .001f, rope.Length + .001f);
            Assert.InRange(Mathf.Abs(world.PhysicsAssembly.Body(new(weight,MachinePart.RootBody)).LinearVelocity.Y), 0, .001f);
            var body=world.PhysicsAssembly.Body(new(weight,MachinePart.RootBody));
            world.Physics.ApplyImpulse(body.Id,(SceneGeometryAdapter.CaptureVector(Vector3.Up)-body.LinearVelocity)/body.InverseMass,body.Center);
            world.Step();
            Assert.True(world.PhysicsAssembly.Body(new(weight,MachinePart.RootBody)).LinearVelocity.Y > .9f); // Tension cannot resist movement toward the anchor.
        }
        finally { world.Free(); }
    }

    [Fact]
    public void AnUnfinishedPulleyRouteCannotLiftAndSpanCanOnlyBeRemovedAfterReset()
    {
        var world = World();
        try
        {
            var weight = Weight(world, "weight", 1, new(0, 3, 0));
            var pulley = world.AddPart(new() { Id = "pulley", Kind = "pulley", Position = [0, 6, 0] });
            Assert.True(world.Connect(weight, pulley));
            world.Start();
            for (var i = 0; i < 30; i++) world.Step();
            Assert.Equal(RopeState.Open, Assert.Single(world.Ropes).State(world));
            Assert.True(weight.Position.Y < 2.9f);
            Assert.Equal(0, ((PulleyPart)pulley).WheelAngle);
            var link=Assert.Single(world.Connections);
            var physics=world.Physics;
            Assert.Throws<InvalidOperationException>(()=>world.Disconnect(link));
            Assert.Same(physics,world.Physics);
            Assert.Equal(link,Assert.Single(world.Connections));
            Assert.True(world.PhysicsAssembly.Body(new(weight,MachinePart.RootBody)).LinearVelocity.Y < -2);
            world.Restore();
            weight=world.FindPart("weight")!;
            Assert.Equal(new Vector3(0,3,0),weight.Position);
            Assert.Equal(link,Assert.Single(world.Connections));
            Assert.True(world.Disconnect(link));
            world.Start();
            for(var i=0;i<30;i++)world.Step();
            Assert.Empty(world.Ropes);
            Assert.True(world.PhysicsAssembly.Body(new(weight,MachinePart.RootBody)).LinearVelocity.Y < -2);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void AThreeDimensionalPendulumStaysWithinItsRopeWithoutGainingEnergy()
    {
        var world = World();
        try
        {
            var anchor = world.AddPart(new() { Id = "anchor", Kind = "rope_anchor", Position = [0, 6, -.18f] });
            var load = Weight(world, "load", 2, new(2, 5, 2));
            Assert.True(world.Connect(anchor, load));
            var initialEnergy = load.Mass * world.Gravity * load.Position.Y;
            var minDepth = load.Position.Z;
            world.Start();
            for (var i = 0; i < 480; i++)
            {
                world.Step();
                var rope = Assert.Single(world.Ropes);
                Assert.InRange(rope.CurrentLength(world) - rope.Length, -.002f, .002f);
                minDepth = Mathf.Min(minDepth, load.Position.Z);
                var energy = load.Mass * (world.Gravity * load.Position.Y + .5f * world.PhysicsAssembly.Body(new(load,MachinePart.RootBody)).LinearVelocity.LengthSquared);
                Assert.True(energy <= initialEnergy + .5f, "The rope must not manufacture pendulum energy.");
            }
            Assert.True(minDepth < -1, $"The pendulum must swing through depth; minimum Z was {minDepth}.");
        }
        finally { world.Free(); }
    }

    [Fact]
    public void CounterweightContactsCannotPullBodiesThroughTheFloor()
    {
        var world = World();
        try
        {
            var a = Weight(world, "a", 1, new(-2, 1, .12f));
            var b = Weight(world, "b", 4, new(2, 3, .12f));
            var p = world.AddPart(new() { Id = "p", Kind = "pulley", Position = [-2, 6, 0] });
            var q = world.AddPart(new() { Id = "q", Kind = "pulley", Position = [2, 6, 0] });
            Assert.True(world.Connect(a, p));
            Assert.True(world.Connect(p, q));
            Assert.True(world.Connect(q, b));
            world.Start();
            for (var i = 0; i < 900; i++)
            {
                world.Step();
                Assert.True(a.Position.Y >= Workbench.SurfaceY + a.Radius - .001f);
                Assert.True(b.Position.Y >= Workbench.SurfaceY + b.Radius - .001f);
                var rope = Assert.Single(world.Ropes);
                Assert.True(rope.CurrentLength(world) <= rope.Length + .002f);
            }
        }
        finally { world.Free(); }
    }

    [Fact]
    public void ClosedGuideLoopsAreRejectedEvenWithoutLoads()
    {
        var world = World();
        try
        {
            var p = world.AddPart(new() { Id = "p", Kind = "pulley", Position = [-2, 4, 0] });
            var q = world.AddPart(new() { Id = "q", Kind = "pulley", Position = [0, 4, 0] });
            var r = world.AddPart(new() { Id = "r", Kind = "pulley", Position = [2, 4, 0] });
            Assert.True(world.Connect(p, q));
            Assert.True(world.Connect(q, r));
            Assert.False(world.Connect(r, p));
        }
        finally { world.Free(); }
    }

    [Fact]
    public void RopeTopologyRejectsBranchesDuplicatesCyclesAndMissingLengths()
    {
        var world = World();
        try
        {
            var a = Weight(world, "a", 1, new(-2, 2, 0));
            var b = Weight(world, "b", 4, new(2, 2, 0));
            var p = world.AddPart(new() { Id = "p", Kind = "pulley" });
            var q = world.AddPart(new() { Id = "q", Kind = "pulley" });
            var r = world.AddPart(new() { Id = "r", Kind = "pulley" });
            p.Position = new(0, 5, 0); q.Position = new(2, 5, 0); r.Position = new(4, 5, 0);
            Assert.True(world.Connect(a, p));
            Assert.False(world.Connect(a, b));
            Assert.False(world.Connect(p, a)); // Undirected duplicate.
            Assert.True(world.Connect(p, q));
            Assert.False(world.Connect(p, r));
            Assert.True(world.Connect(q, r));
            Assert.False(world.Connect(r, p));
            var bad = world.Snapshot();
            bad.Connections[0] = bad.Connections[0] with { RopeLength = null };
            Assert.Throws<ArgumentException>(() => world.LoadMachine(bad));
            Assert.Same(a, world.FindPart("a"));
        }
        finally { world.Free(); }
    }
}
