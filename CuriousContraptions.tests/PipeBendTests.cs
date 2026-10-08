using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class PipeBendTests(NativeSceneFixture godot, ITestOutputHelper output)
{
    public enum TubeKind { Straight, Bend45, Bend90 }
    public enum CampaignCase { GentleBend, QuarterBend }
    private const string PuzzleResource="res://content/puzzles.json";
    private const string JoinedPuzzleId="joined_pipe", JoinedPipeId="pipe_1", CampaignBendId="bend_1";
    private const string BendId="bend", PipeId="pipe", BallId="ball", FirstTubeId="a", SecondTubeId="b";
    private const string BallCatalogId="ball";
    // Explicit external catalogue/content boundaries; fixture logic remains enum-typed.
    private static string Catalog(TubeKind kind)=>kind switch
    {
        TubeKind.Straight=>"pipe",TubeKind.Bend45=>"pipe_bend_45",TubeKind.Bend90=>"pipe_bend_90",
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static string PuzzleId(CampaignCase scenario)=>scenario switch
    {
        CampaignCase.GentleBend=>"gentle_bend",CampaignCase.QuarterBend=>"quarter_bend",
        _=>throw new ArgumentOutOfRangeException(nameof(scenario))
    };

    [Fact]
    public void FixtureBoundariesMapCanonicalValuesAndRejectUndefinedChoices()
    {
        Assert.Equal("pipe",Catalog(TubeKind.Straight));
        Assert.Equal("pipe_bend_45",Catalog(TubeKind.Bend45));
        Assert.Equal("pipe_bend_90",Catalog(TubeKind.Bend90));
        Assert.Equal("gentle_bend",PuzzleId(CampaignCase.GentleBend));
        Assert.Equal("quarter_bend",PuzzleId(CampaignCase.QuarterBend));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Catalog((TubeKind)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PuzzleId((CampaignCase)(-1)));
    }

    [Theory]
    [InlineData(TubeKind.Straight, 6f)]
    [InlineData(TubeKind.Straight, 40f)]
    [InlineData(TubeKind.Bend90, 6f)]
    [InlineData(TubeKind.Bend90, 40f)]
    public void FastBallsCrossJoinedBendSeams(TubeKind upstreamKind, float speed)
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var bend = (PipeBendPart)world.AddPart(new() { Id = BendId, Kind = Catalog(TubeKind.Bend90), Position = [0,8,0], Orientation = PartOrientation.FromEulerDegrees(20,30,40) });
            var pipe = world.AddPart(new() { Id = PipeId, Kind = Catalog(upstreamKind) });
            var inlet = bend.Mouths.Single(m => m.Id == TubeMouthId.Start);
            var pipeEnd = ((ITubePart)pipe).Mouths.Single(m => m.Id == TubeMouthId.End);
            pipe.GlobalBasis = bend.GlobalBasis * new Basis(new Quaternion(pipeEnd.Outward, -inlet.Outward));
            pipe.GlobalPosition = bend.GlobalTransform * inlet.Position - pipe.GlobalBasis * pipeEnd.Position;
            var ball = world.AddPart(new() { Id = BallId, Kind = BallCatalogId });
            var entry = ((ITubePart)pipe).Mouths.Single(m => m.Id == TubeMouthId.Start);
            ball.GlobalPosition = pipe.GlobalTransform * (entry.Position + entry.Outward * .8f);
            ball.InitialVelocity = pipe.GlobalBasis * -entry.Outward * speed;
            world.Start();
            var outlet = bend.Mouths.Single(m => m.Id == TubeMouthId.End);
            var exited = false;
            for (var tick = 0; tick < 600; tick++)
            {
                var previous = ball.Position;
                world.Step();
                Assert.True(ball.Position.DistanceTo(previous) <= speed * MachineWorld.Tick + .002f);
                Assert.InRange(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length, 0, speed + .002f);
                var local = bend.Transform.AffineInverse() * ball.Position;
                var fromOutlet = local - outlet.Position;
                var along = fromOutlet.Dot(outlet.Outward);
                if (along > .5f && (fromOutlet - outlet.Outward * along).Length() < PipePart.BoreRadius)
                {
                    Assert.True(CollisionVector.Dot(world.PhysicsAssembly.Body(new(bend,MachinePart.RootBody)).Pose.Rotation.Inverse().Apply(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity),SceneGeometryAdapter.CaptureVector(outlet.Outward))/world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length > .8f);
                    exited = true;
                    break;
                }
            }
            output.WriteLine($"exit={exited}; final local={bend.Transform.AffineInverse() * ball.Position}; velocity={world.PhysicsAssembly.Body(new(bend,MachinePart.RootBody)).Pose.Rotation.Inverse().Apply(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity)}; visible={ball.Visible}");
            Assert.True(exited);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(0f, .48f, true)]
    [InlineData(.45f, .48f, true)]
    [InlineData(1f, .48f, false)]
    [InlineData(0f, .58f, true)]
    [InlineData(.45f, .58f, false)]
    [InlineData(1f, .58f, false)]
    public void JoinedRouteUsesAuthoredWindowsNotEditorSnapDuringRun(float precision, float error, bool expected)
    {
        var world = new MachineWorld { Precision = precision };
        godot.Tree.Root.AddChild(world);
        try
        {
            var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString(PuzzleResource))
                .Single(p => p.Id == JoinedPuzzleId);
            var data = MachineCodec.Clone(puzzle.CreateMachine());
            foreach (var part in data.Parts) part.Difficulty.Clear();
            data.Parts.AddRange(puzzle.Solution);
            data.Parts.Single(p => p.Id == JoinedPipeId).Position[2] += error;
            world.LoadMachine(data);
            world.Start();
            for (var tick = 0; tick < 3600 && world.Running; tick++) world.Step();
            output.WriteLine($"error {error}, precision {precision}: won {world.Won}, Z {world.FindPart(JoinedPipeId)!.Position.Z}");
            Assert.Equal(expected, world.Won);
            Assert.Equal(expected ? 0 : error, world.FindPart(JoinedPipeId)!.Position.Z, 4);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void JoinedGravityRouteCarriesTheBallAcrossItsSeam()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var bend = (PipeBendPart)world.AddPart(new() { Id = BendId, Kind = Catalog(TubeKind.Bend90), Position = [1,3.5f,0], Orientation = PartOrientation.FromEulerDegrees(0,0,-45) });
            var pipe = (PipePart)world.AddPart(new() { Id = PipeId, Kind = Catalog(TubeKind.Straight), Orientation = PartOrientation.FromEulerDegrees(0,0,-45),
                Properties = new() { [PartParameterName.Of(PipeParameter.Length)] = 2 } });
            var inlet = bend.Mouths.Single(m => m.Id == TubeMouthId.Start);
            var end = pipe.Mouths.Single(m => m.Id == TubeMouthId.End);
            pipe.Position = bend.Transform * inlet.Position - pipe.Basis * end.Position;
            var entry = pipe.Mouths.Single(m => m.Id == TubeMouthId.Start);
            var ball = world.AddPart(new() { Id = BallId, Kind = BallCatalogId });
            ball.Position = pipe.Transform * entry.Position + Vector3.Up * 1.6f;
            output.WriteLine($"pipe {pipe.Position}; ball {ball.Position}");
            world.Start();
            for (var tick = 0; tick < 600 && ball.Position.Y > 1.2f; tick++) world.Step();
            output.WriteLine($"landing {ball.Position}; velocity {world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity}");
            Assert.True(ball.Position.Y <= 1.2f);
            var outlet = bend.Mouths.Single(m => m.Id == TubeMouthId.End);
            var normal = bend.Basis * outlet.Outward;
            Assert.True((ball.Position - bend.Transform * outlet.Position).Dot(normal) > .3f);
            Assert.True(CollisionVector.Dot(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity,SceneGeometryAdapter.CaptureVector(normal))/world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length > .9f);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(TubeKind.Bend45, 0, 0, 0)]
    [InlineData(TubeKind.Bend90, 0, 0, 0)]
    [InlineData(TubeKind.Bend45, 30, 40, 50)]
    [InlineData(TubeKind.Bend90, 30, 40, 50)]
    [InlineData(TubeKind.Bend45, 0, 90, 90)]
    [InlineData(TubeKind.Bend90, 0, 90, 90)]
    public void BallsTurnThroughHollowBendsWithoutAddedEnergy(TubeKind kind, float x, float y, float z)
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var bend = (PipeBendPart)world.AddPart(new() { Id = BendId, Kind = Catalog(kind), Position = [0, 8, 0], Orientation = PartOrientation.FromEulerDegrees(x,y,z) });
            var ball = world.AddPart(new() { Id = BallId, Kind = BallCatalogId });
            var inlet = bend.Mouths.Single(m => m.Id == TubeMouthId.Start);
            var outlet = bend.Mouths.Single(m => m.Id == TubeMouthId.End);
            ball.Position = bend.Transform * (inlet.Position + inlet.Outward * .8f);
            ball.InitialVelocity = bend.Basis * -inlet.Outward * 6;
            var initialVelocity=ball.InitialVelocity;
            var initialPose=ball.Transform;
            world.Start();
            var previous = ball.Position;
            var passed = false;
            for (var i = 0; i < 300; i++)
            {
                world.Step();
                Assert.True(ball.Position.DistanceTo(previous) < .051f);
                Assert.InRange(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length, 0, 6.001f);
                previous = ball.Position;
                var local = bend.Transform.AffineInverse() * ball.Position;
                if ((local - outlet.Position).Dot(outlet.Outward) > .5f)
                {
                    Assert.True(CollisionVector.Dot(world.PhysicsAssembly.Body(new(bend,MachinePart.RootBody)).Pose.Rotation.Inverse().Apply(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity),SceneGeometryAdapter.CaptureVector(outlet.Outward))/world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length > .8f);
                    passed = true;
                    break;
                }
            }
            Assert.True(passed);
            Assert.Empty(bend.Boxes);
            Assert.Single(bend.Bends);
            Assert.Equal(2, bend.Tubes.Count);
            world.Restore();
            Assert.Equal(initialVelocity, world.FindPart(BallId)!.InitialVelocity);
            Assert.Equal(initialPose, world.FindPart(BallId)!.Transform);
            Assert.Throws<InvalidOperationException>(()=>world.Physics);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(TubeKind.Bend45)]
    [InlineData(TubeKind.Bend90)]
    public void BendMouthsSnapToStraightAndOtherBends(TubeKind kind)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var a = (PipeBendPart)world.AddPart(new() { Id = FirstTubeId, Kind = Catalog(kind), Position = [0,4,0], Orientation = PartOrientation.FromEulerDegrees(25,35,45) });
            foreach (var otherKind in Enum.GetValues<TubeKind>())
            {
                var b = world.AddPart(new() { Id = SecondTubeId, Kind = Catalog(otherKind) });
                var target = a.Mouths.Single(m => m.Id == TubeMouthId.End);
                var source = ((ITubePart)b).Mouths.Single(m => m.Id == TubeMouthId.Start);
                var targetNormal = a.GlobalBasis * target.Outward;
                b.GlobalBasis = new Basis(new Quaternion(source.Outward, -targetNormal));
                b.GlobalPosition = a.GlobalTransform * target.Position - b.GlobalBasis * source.Position + Vector3.Up * .1f;
                var snapped = TubePlacementSnap.Find(world, b);
                Assert.NotNull(snapped);
                b.GlobalTransform = snapped!.Value;
                Assert.True((a.GlobalTransform * target.Position).DistanceTo(b.GlobalTransform * source.Position) < .0001f);
                world.RemovePart(b);
            }
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(TubeKind.Bend45)]
    [InlineData(TubeKind.Bend90)]
    public void GravityAloneCarriesAFallingBallAroundTheBend(TubeKind kind)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var bend = (PipeBendPart)world.AddPart(new() { Id = BendId, Kind = Catalog(kind), Position = [0,4,0], Orientation = PartOrientation.FromEulerDegrees(0,0,-90) });
            var inlet = bend.Mouths.Single(m => m.Id == TubeMouthId.Start);
            var ball = world.AddPart(new() { Id = BallId, Kind = BallCatalogId });
            ball.Position = bend.Transform * inlet.Position + Vector3.Up * 1.6f;
            world.Start();
            for (var i = 0; i < 600 && ball.Position.Y > 1.2f; i++) world.Step();
            output.WriteLine($"{kind} at basket height: {ball.Position}; velocity: {world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity}");
            Assert.True(ball.Position.Y <= 1.2f);
            Assert.True(ball.Position.X < -.5f);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(CampaignCase.GentleBend, 0f)]
    [InlineData(CampaignCase.GentleBend, .45f)]
    [InlineData(CampaignCase.GentleBend, 1f)]
    [InlineData(CampaignCase.QuarterBend, 0f)]
    [InlineData(CampaignCase.QuarterBend, .45f)]
    [InlineData(CampaignCase.QuarterBend, 1f)]
    public void PlacementAssistanceAlignsWithinTheAuthoredBendWindow(CampaignCase scenario, float precision)
    {
        var world = new MachineWorld { Precision = precision };
        godot.Tree.Root.AddChild(world);
        try
        {
            var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString(PuzzleResource)).Single(p => p.Id == PuzzleId(scenario));
            var data = MachineCodec.Clone(puzzle.CreateMachine());
            foreach (var fixture in data.Parts) fixture.Difficulty.Clear();
            data.Parts.AddRange(puzzle.Solution);
            data.Parts.Single(p => p.Id == CampaignBendId).Position[2] = .48f;
            world.LoadMachine(data);
            world.Start();
            for (var tick = 0; tick < 3600 && world.Running; tick++) world.Step();
            output.WriteLine($"{scenario}, precision {precision}: won {world.Won}; corrected bend {world.FindPart(CampaignBendId)!.Position}");
            Assert.Equal(precision < 1 || scenario == CampaignCase.GentleBend, world.Won);
            Assert.Equal(precision < 1 ? 0 : .48f, world.FindPart(CampaignBendId)!.Position.Z, 4);
        }
        finally { world.Free(); }
    }

}
