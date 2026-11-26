using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class BalloonArtTests(NativeSceneFixture godot)
{
    [Fact]
    public async Task NativeEnvelopeAndNeckFollowCommittedRadiusWithAnIndependentDryCarrierAndCenterTie()
    {
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();
        var part = registry.Create(new("balloon"));
        try
        {
            godot.Tree.Root.AddChild(part); part.EnsureConstructed();
            var envelope = part.GetNode<MeshInstance3D>("Envelope");
            var sphere = Assert.IsType<SphereMesh>(envelope.Mesh);
            Assert.Equal(1f, sphere.Radius);
            Assert.Equal(new Color(237f / 255f, 99f / 255f, 120f / 255f),
                Assert.IsType<StandardMaterial3D>(sphere.SurfaceGetMaterial(0)).AlbedoColor);
            Assert.Equal(Vector3.Zero, part.GetNode<Node3D>("Loc_Port_Tie").Position);
            Assert.NotNull(part.GetNode<MeshInstance3D>("String").Mesh);
            await using var h = await WorkshopIntegrationHarness.Running(
                TennisBallTests.Scene(BalloonTests.Balloon(y: 5, buoyancy: 0, mass: 2)));
            await h.Advance(360);
            Assert.Equal(GasInventoryPhase.Empty, h.Committed.GasStores[0].State.Phase);
            foreach (var sample in h.Presentation!.Advance(AnimationTestTime.Seconds(3)))
                if (sample.Owner == new GpuBodyId(1)) part.ApplyDeclaredProperty(sample.Binding, sample.Value);
            Assert.Equal(.0625f, envelope.Scale.X);
            Assert.Equal(-.0625f, part.GetNode<Node3D>("Neck").Position.Y);
            Assert.Equal(Vector3.One, part.GetNode<Node3D>("Carrier").Scale);
            Assert.Equal(Vector3.Zero, part.GetNode<Node3D>("Loc_Port_Tie").Position);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { part.GetParent()?.RemoveChild(part); part.Free(); }
    }

    /// <summary>Visible-solid remainder, part C, C8 (owner decision 5, 22 November 2026): the real part draws its envelope at its
    /// compiled collider's radius once the first declared sample has arrived, in build mode, at Run start and after Reset. Each sample's
    /// value comes from the production evaluator at the installed schedule's anchor (<c>Advance(schedule, master)</c>, which holds the
    /// anchor while the world is not running); it reaches the part as in <c>MachineWorld.Gpu.cs:250–252</c>: published to the production
    /// client (<see cref="WorkshopActivationAnimationTests.NativeClient"/>), admitted by <c>BrowserWorkshopClient.TryDeclaredFrame</c> for
    /// the part's owner, and applied per returned property by <c>ApplyDeclaredProperty</c>. The Animation worker's evaluation, sample
    /// encoding and applied-at stamping (<c>CuriousContraptions.Animation.Worker/Program.cs</c>, <c>Emit</c>) are replaced: the test's
    /// samples carry applied-at 0, so the client's applied-at gate always passes; those legs are proven by reading only, browser
    /// qualification pending. The
    /// expected radius is the compiled collider's, read from the installed scene. 0.1 mm is the preparation bound of <c>BalloonTests.NondefaultLiftAndStrongDragChangeStandardMotion</c> for
    /// the same radius: a larger error would draw the envelope visibly inside or outside the surface a ball touches. Boundary and record
    /// of O1: on construction and directly after Reset, before any sample, the part draws the zero-input projection, 0.0625 m (the
    /// momentary first-committed-sample defect owned by ENGINE-ONE-WAY-INSTALL, whose repair changes those two assertions). Control: the
    /// dry carrier keeps its unit scale.</summary>
    [Theory]
    [InlineData(.18f)]
    [InlineData(.36f)]
    [InlineData(.5f)]
    public async Task TheEnvelopeIsDrawnAtTheColliderRadiusInBuildModeAtRunStartAndAfterReset(float radius)
    {
        const float Projection = .0625f, Drawn = 1e-4f;
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();
        var part = registry.Create(new("balloon"));
        try
        {
            godot.Tree.Root.AddChild(part); part.EnsureConstructed();
            var envelope = part.GetNode<Node3D>("Envelope");
            var carrier = part.GetNode<Node3D>("Carrier");
            Assert.Equal(Projection, envelope.Scale.X);
            using var session = new WorkshopActivationAnimationTests.NativeClient();
            var balloon = BalloonTests.Balloon(y: 5, radius: radius);
            await session.Construct(TennisBallTests.Scene(balloon));
            var collider = Assert.Single(session.InstalledScene.Physics.Colliders.ToArray(), c => c.Body == balloon.Id).Radius.Value;
            ulong ordinal = 0;
            void AssertFirstSampleDrawsTheCollider(string state)
            {
                var schedule = session.Schedule;
                foreach (var sample in session.Presentation.Advance(schedule, PhysicalTime.FromNanoseconds(schedule.World.AnchorMaster.Value)))
                    if (sample.Owner == balloon.Id)
                        session.Output(session.Declared(balloon.Id, sample.Binding) with { To = BitConverter.UInt32BitsToSingle(sample.Value.CanonicalBits) },
                            AnimationOutputKind.Sample, ++ordinal);
                session.Frame();
                Assert.True(session.Client.TryDeclaredFrame(session.FrameNumber, default, balloon.Id, out var declared),
                    $"radius {radius}, {state}: the client admitted no declared sample of the Balloon");
                foreach (var property in declared) part.ApplyDeclaredProperty(property.Binding, property.Value);
                Assert.True(MathF.Abs(envelope.Scale.X - collider) <= Drawn,
                    $"radius {radius}, {state}: the envelope is drawn at {envelope.Scale.X} m, not at its collider's {collider} m");
                Assert.Equal(Vector3.One, carrier.Scale);
            }
            AssertFirstSampleDrawsTheCollider("build mode");
            Assert.Equal(WorkshopCommandOutcome.Applied, (await session.Client.Execute(WorkshopCommandKind.Run)).Response.Result.Outcome);
            AssertFirstSampleDrawsTheCollider("Run start");
            await session.Advance(60);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await session.Client.Execute(WorkshopCommandKind.Reset)).Response.Result.Outcome);
            part.ResetPhysicalVisuals();
            Assert.Equal(Projection, envelope.Scale.X);
            AssertFirstSampleDrawsTheCollider("after Reset");
            Assert.Empty(godot.Errors.Drain());
        }
        finally { part.GetParent()?.RemoveChild(part); part.Free(); }
    }
}
