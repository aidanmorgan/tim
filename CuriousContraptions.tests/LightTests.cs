using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class LightTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world = new MachineWorld { Gravity = 0 };
        godot.Tree.Root.AddChild(world);
        return world;
    }
    private static (FlashlightPart Torch, SolarPanelPart Panel, MotorPart Motor) Setup(MachineWorld world)
    {
        var torch = (FlashlightPart)world.AddPart(new() { Id = "torch", Kind = "flashlight", Position = [-2, 3, 0] });
        var panel = (SolarPanelPart)world.AddPart(new() { Id = "panel", Kind = "solar_panel", Position = [1, 3, 0] });
        var motor = (MotorPart)world.AddPart(new() { Id = "motor", Kind = "motor", Position = [4, 1, 2] });
        Assert.True(world.Connect(panel, motor));
        return (torch, panel, motor);
    }
    [Fact]
    public void VisibleConePreservesUnblockedRaysBesideMovingOccluder()
    {
        var world = World();
        try
        {
            var torch = (FlashlightPart)world.AddPart(new() { Id = "torch", Kind = "flashlight", Position = [0, 5, 0] });
            var ball = world.AddPart(new() { Id = "blocker", Kind = "ball", Position = [1.6f, 5, .3f] });
            torch.Active = true;
            var source = torch.LightSource!.Value;
            var rays = LightConeVisual.Sample(world, torch, source, 1);
            Assert.Contains(rays, r => r.Distance < 2);
            Assert.Contains(rays, r => r.Distance == source.Range);
            ball.Position = new(1.6f, 5, 3);
            Assert.All(LightConeVisual.Sample(world, torch, source, 1), r => Assert.Equal(source.Range, r.Distance));
        }
        finally { world.Free(); }
    }
    [Theory]
    [InlineData(0f, 1.02f, true)]
    [InlineData(.45f, 1.02f, true)]
    [InlineData(1f, 1.02f, false)]
    [InlineData(0f, 1.1f, true)]
    [InlineData(.45f, 1.1f, false)]
    [InlineData(1f, 1.1f, false)]
    public void PlacementNudgeAloneRecoversPanelAtBeamBoundary(float precision, float error, bool expectedWin)
    {
        var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString("res://content/puzzles.json")).Single(p => p.Id == "solar_motor");
        var data = MachineCodec.Clone(puzzle.CreateMachine());
        data.Parts.AddRange(puzzle.Solution);
        data.Connections = puzzle.SolutionConnections;
        data = MachineCodec.Clone(data);
        data.Parts.Single(p => p.Id == "panel_1").Position[2] += error;
        // Keep impact triggering identical: only placement correction changes across difficulties.
        foreach (var part in data.Parts)
        foreach (var knot in part.Difficulty) knot.TriggerThreshold = .8f;
        var world = World();
        world.Precision = precision;
        try
        {
            world.LoadMachine(data);
            world.Start();
            for (var i = 0; i < 600 && world.Running; i++) world.Step();
            Assert.True(world.FindPart("torch")!.Active);
            Assert.Equal(expectedWin, world.Won);
            var panel = (SolarPanelPart)world.FindPart("panel_1")!;
            Assert.Equal(expectedWin, panel.Irradiance >= SolarPanelPart.Threshold);
            Assert.InRange(Mathf.Abs(panel.Position.Z - (error - (precision == 0 ? .25f : precision == 1 ? 0 : .1f))), 0, .001f);
        }
        finally { world.Free(); }
    }
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 90, 0)]
    [InlineData(25, 40, 15)]
    public void VisibleConeUsesPhysicalAngleRangeAndRotatedOcclusion(float x, float y, float z)
    {
        var world = World();
        try
        {
            var torch = (FlashlightPart)world.AddPart(new() { Id = "torch", Kind = "flashlight",
                Position = [0, 5, 0], Rotation = [x, y, z] });
            torch.Active = true;
            var source = torch.LightSource!.Value;
            var clear = LightConeVisual.Sample(world, torch, source, 1);
            Assert.Equal(LightConeVisual.Sectors, clear.Length);
            foreach (var ray in clear)
            {
                Assert.InRange(Mathf.Abs(ray.Direction.Length() - 1), 0, .00001f);
                Assert.InRange(Mathf.Abs(ray.Direction.Dot(source.Direction) - source.ConeCosine), 0, .00001f);
                Assert.InRange(ray.Distance, 0, source.Range);
            }
            var wall = world.AddPart(new() { Id = "wall", Kind = "wall",
                Properties = new() { ["width"] = 4, ["height"] = 4, ["thickness"] = .2f } });
            wall.Transform = torch.Transform * new Transform3D(Basis.FromEuler(new(0, Mathf.Pi / 2, 0)), new(3, 0, 0));
            var blocked = LightConeVisual.Sample(world, torch, source, 1);
            Assert.All(blocked, ray =>
            {
                var end = source.At + ray.Direction * ray.Distance;
                Assert.InRange(end.X, 2.899f, 2.901f);
                Assert.True(ray.Distance < source.Range);
            });
            using var visual = new LightConeVisual();
            torch.AddChild(visual);
            visual.Refresh(world, torch, source);
            Assert.Equal(1, visual.Mesh.GetSurfaceCount());
            Assert.Equal(LightConeVisual.ShellCount * LightConeVisual.Sectors * 9,
                visual.Mesh.SurfaceGetArrays(0)[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array().Length);
            torch.RemoveChild(visual);
        }
        finally { world.Free(); }
    }
    [Fact]
    public void LightRequiresActivationFacingRangeAndWireAndDoesNotLatchSupply()
    {
        var world = World();
        try
        {
            var (torch, panel, motor) = Setup(world);
            world.Start();
            world.Step();
            Assert.Equal(0, panel.Irradiance);
            Assert.False(motor.Active);
            world.Activate(torch);
            world.Step();
            Assert.True(panel.Irradiance > SolarPanelPart.Threshold);
            Assert.True(motor.Active);
            var intensity = panel.Irradiance;
            panel.RotationDegrees = new(0, 180, 0);
            world.Step();
            Assert.Equal(0, panel.Irradiance);
            Assert.False(motor.Active);
            panel.RotationDegrees = Vector3.Zero;
            panel.Position = new(7, 3, 0);
            world.Step();
            Assert.Equal(0, panel.Irradiance);
            panel.Position = new(1, 3, 0);
            world.Step();
            Assert.Equal(intensity, panel.Irradiance);
            world.Connections.Clear();
            world.Step();
            Assert.True(panel.Active);
            Assert.False(motor.Active);
            torch.Active = false;
            world.Step();
            Assert.Equal(0, panel.Irradiance);
            Assert.False(panel.Active);
        }
        finally { world.Free(); }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OcclusionUsesRotatedBoxesAndDynamicBodiesAndRecovers(bool dynamic)
    {
        var world = World();
        try
        {
            var (torch, panel, motor) = Setup(world);
            var blocker = world.AddPart(new()
            {
                Id = "blocker", Kind = dynamic ? "weight" : "wall", Position = [-.3f, 3, 0],
                Rotation = [0, 90, 0],
                Properties = dynamic ? new() { [WeightParameters.Mass] = 8 } : new() { ["width"] = 3, ["height"] = 3, ["thickness"] = .4f }
            });
            world.Start();
            world.Activate(torch);
            world.Step();
            Assert.False(motor.Active);
            var blocked = panel.Irradiance;
            blocker.Position = new(-.3f, 3, 3);
            world.Step();
            Assert.True(motor.Active);
            Assert.True(panel.Irradiance > blocked);
        }
        finally { world.Free(); }
    }
    [Fact]
    public void SmallOccluderReducesRatherThanErasesAllPanelSamples()
    {
        var world = World();
        try
        {
            var (torch, panel, _) = Setup(world);
            torch.Active = true;
            LightNetwork.Solve(world);
            var full = panel.Irradiance;
            world.AddPart(new() { Id = "ball", Kind = "ball", Position = [.6f, 3, 0] });
            LightNetwork.Solve(world);
            Assert.InRange(panel.Irradiance, .001f, full - .001f);
        }
        finally { world.Free(); }
    }
    [Fact]
    public void OutputsAreOrderIndependentAndResetClearsOpticalState()
    {
        var world = World();
        try
        {
            Setup(world);
            var data = world.Snapshot();
            world.LoadMachine(data);
            world.Start();
            world.Activate(world.FindPart("torch")!);
            for (var i = 0; i < 120; i++) world.Step();
            var flux = ((SolarPanelPart)world.FindPart("panel")!).Irradiance;
            var travel = ((MotorPart)world.FindPart("motor")!).ShaftTravel;
            world.Restore();
            Assert.Equal(0, ((SolarPanelPart)world.FindPart("panel")!).Irradiance);
            Assert.False(world.FindPart("torch")!.Active);
            data.Parts.Reverse();
            world.LoadMachine(data);
            world.Start();
            world.Activate(world.FindPart("torch")!);
            for (var i = 0; i < 120; i++) world.Step();
            Assert.Equal(flux, ((SolarPanelPart)world.FindPart("panel")!).Irradiance);
            Assert.Equal(travel, ((MotorPart)world.FindPart("motor")!).ShaftTravel);
        }
        finally { world.Free(); }
    }
    [Theory]
    [InlineData("solar_motor", 0f)]
    [InlineData("solar_motor", .45f)]
    [InlineData("solar_motor", 1f)]
    [InlineData("solar_shadow", 0f)]
    [InlineData("solar_shadow", .45f)]
    [InlineData("solar_shadow", 1f)]
    public void SolarLessonsNeedLightAndWireAndReplay(string id, float precision)
    {
        var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString("res://content/puzzles.json")).Single(p => p.Id == id);
        var data = MachineCodec.Clone(puzzle.CreateMachine());
        data.Parts.AddRange(puzzle.Solution);
        data.Connections = puzzle.SolutionConnections;
        var world = World();
        world.Precision = precision;
        try
        {
            world.LoadMachine(data);
            world.Start();
            for (var i = 0; i < 600 && world.Running; i++) world.Step();
            Assert.True(world.Won);
            var ticks = world.Ticks;
            world.Restore();
            Assert.Equal(0, ((SolarPanelPart)world.FindPart("panel_1")!).Irradiance);
            world.Start();
            for (var i = 0; i < 600 && world.Running; i++) world.Step();
            Assert.True(world.Won);
            Assert.Equal(ticks, world.Ticks);
            foreach (var breakLight in new[] { false, true })
            {
                var broken = MachineCodec.Clone(data);
                if (breakLight) broken.Parts.RemoveAll(p => p.Id == "trigger");
                else broken.Connections.Clear();
                world.LoadMachine(broken);
                world.Start();
                for (var i = 0; i < 600; i++) world.Step();
                Assert.False(world.Won);
            }
            if (id == "solar_shadow")
            {
                var blocked = MachineCodec.Clone(data);
                blocked.Parts.Single(p => p.Id == "panel_1").Position = [-1, 3, 0];
                world.LoadMachine(blocked);
                world.Start();
                for (var i = 0; i < 600; i++) world.Step();
                Assert.False(world.Won);
                Assert.Equal(0, ((SolarPanelPart)world.FindPart("panel_1")!).Irradiance);
            }
        }
        finally { world.Free(); }
    }
    [Fact]
    public void ConeRejectsDepthMissAndFloorBlocksLight()
    {
        var world = World();
        try
        {
            var (torch, panel, _) = Setup(world);
            torch.Active = true;
            panel.Position += new Vector3(0, 0, 3);
            LightNetwork.Solve(world);
            Assert.Equal(0, panel.Irradiance);
            var distance = WorldGeometry.Trace(TraceMedium.Light,world, new(0, 2, 0), Vector3.Down, 8, torch);
            Assert.InRange(distance, 2.459f, 2.461f);
        }
        finally { world.Free(); }
    }
}
