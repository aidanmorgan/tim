using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class CompliantContactStateTests
{
    public enum Shape { Sphere, Box, RoundedHull }
    private static CompoundGeometry Geometry(Shape shape)=>new([new(shape switch
    {
        Shape.Sphere=>(ConvexGeometry)new ConvexSphere(.1),
        Shape.Box=>new ConvexBox(new(.1,.1,.1)),
        Shape.RoundedHull=>new ConvexRounded(new ConvexHull([new(-.05,-.05,-.05),new(.05,-.05,-.05),new(0,.05,.05)]),.05),
        _=>throw new ArgumentOutOfRangeException(nameof(shape))
    },AffineTransform.Identity)]);
    private static readonly CompoundGeometry FrameGeometry=new([new(new ConvexSphere(.001),AffineTransform.Identity)]);
    private static CompliantContactLoad Load(PhysicsBody body,PhysicsBody frame,CompliantContactInitialState initial)=>
        new(body.Id,frame.Id,2,2,.3,.5,100,0,initial);
    private sealed record Fixture(PhysicsWorld World,PhysicsBody Body,PhysicsBody Frame,CompliantContactLoad Load);
    private static Fixture Create(Shape shape=Shape.Sphere,CompliantContactInitialState initial=CompliantContactInitialState.Unloaded)
    {
        var frame=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,
            RigidPose.At(new(0,initial==CompliantContactInitialState.Preloaded?.3:.6,0)),new(0,-1,0),default,1,new(1,1,1));
        var load=Load(body,frame,initial);
        var world=new PhysicsWorld([],[new(body,Geometry(shape),new(0,0,0)),new(frame,FrameGeometry,new(0,0,0))],[],
            new(default,maximumStep:.005));
        world.ReplaceLoads(new(){Compliant=[load]});
        return new(world,body,frame,load);
    }

    [Theory]
    [InlineData(Shape.Sphere)]
    [InlineData(Shape.Box)]
    [InlineData(Shape.RoundedHull)]
    public void WorldOwnsEntryReleaseAndExactContactReplay(Shape shape)
    {
        var f=Create(shape);var world=f.World;var initial=world.Capture();
        Assert.Equal(CompliantContactPhase.Ready,Assert.Single(world.CompliantContacts.ToArray()).Phase);
        var result=world.Step([],[],.25);
        var state=Assert.Single(world.CompliantContacts.ToArray());
        Assert.Equal(CompliantContactPhase.Engaged,state.Phase);Assert.Equal(1ul,state.EpisodeCount);
        Assert.True(f.Body.LinearVelocity.Y> -1);
        var entry=Assert.Single(world.CompliantEntries.ToArray());
        Assert.Equal(f.Load.Key,entry.Contact);Assert.InRange(entry.Time,.19,.21);Assert.Equal(1,entry.ApproachSpeed);
        var final=world.Capture().BodyStates.ToArray();var entries=world.CompliantEntries.ToArray();
        world.Restore(initial);
        Assert.Equal(initial.CompliantStates.ToArray(),world.CompliantContacts.ToArray());
        Assert.Empty(world.CompliantEntries.ToArray());
        Assert.Equal(result,world.Step([],[],.25));
        Assert.Equal(final,world.Capture().BodyStates.ToArray());
        Assert.Equal(state,Assert.Single(world.CompliantContacts.ToArray()));Assert.Equal(entries,world.CompliantEntries.ToArray());
        world.Step([],[],.4);
        Assert.Equal(CompliantContactPhase.Ready,Assert.Single(world.CompliantContacts.ToArray()).Phase);
        Assert.True(f.Body.LinearVelocity.Y>0);
        Assert.Empty(world.CompliantEntries.ToArray());
    }

    [Fact]
    public void RecreatedDeclarationsRetainPhysicalHistoryAndRemovalDoesNotRearmAnEmbeddedBody()
    {
        var f=Create();f.World.Step([],[],.25);var snapshot=f.World.Capture();
        var state=Assert.Single(f.World.CompliantContacts.ToArray());
        var newLoad=Load(f.Body,f.Frame,CompliantContactInitialState.Unloaded);
        f.World.ReplaceLoads(new(){Compliant=[newLoad]});
        Assert.Equal(state,Assert.Single(f.World.CompliantContacts.ToArray()));
        f.World.Step([],[],.005);var expected=f.World.Capture();
        f.World.Restore(snapshot);f.World.Step([],[],.005);
        Assert.Equal(expected.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
        Assert.Equal(expected.CompliantStates.ToArray(),f.World.CompliantContacts.ToArray());
        f.World.ReplaceLoads(new());Assert.Empty(f.World.CompliantContacts.ToArray());
        f.World.ReplaceLoads(new(){Compliant=[newLoad]});
        Assert.Equal(CompliantContactPhase.Unarmed,Assert.Single(f.World.CompliantContacts.ToArray()).Phase);
        f.World.Restore(snapshot);Assert.Equal(state,Assert.Single(f.World.CompliantContacts.ToArray()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisabledParticipantsCannotRetainAHiddenEngagement(bool disableFrame)
    {
        var f=Create();var initial=f.World.Capture();var id=disableFrame?f.Frame.Id:f.Body.Id;
        var collider=f.World.Collider(id).Declaration;
        f.World.ApplyColliderUpdates([new(id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
        f.World.Step([],[],.25);
        Assert.Equal(CompliantContactPhase.Unarmed,Assert.Single(f.World.CompliantContacts.ToArray()).Phase);
        Assert.Equal(-1,f.Body.LinearVelocity.Y);Assert.Empty(f.World.CompliantEntries.ToArray());
        f.World.ApplyColliderUpdates([new(id,collider.Geometry,collider.Material,CollisionParticipation.Enabled)]);
        f.World.Step([],[],.005);
        Assert.Equal(CompliantContactPhase.Unarmed,Assert.Single(f.World.CompliantContacts.ToArray()).Phase);
        Assert.Equal(-1,f.Body.LinearVelocity.Y);
        f.World.Restore(initial);f.World.Step([],[],.25);
        Assert.Equal(CompliantContactPhase.Engaged,Assert.Single(f.World.CompliantContacts.ToArray()).Phase);
    }

    [Theory]
    [InlineData(CompliantContactInitialState.Unloaded)]
    [InlineData(CompliantContactInitialState.Preloaded)]
    public void CompressedInitialEnergyMustBeDeclaredExplicitly(CompliantContactInitialState initial)
    {
        var frame=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,.3,0)),default,default,1,new(1,1,1));
        var world=new PhysicsWorld([],[new(body,Geometry(Shape.Sphere),new(0,0,0)),new(frame,FrameGeometry,new(0,0,0))],[],new(default));
        world.ReplaceLoads(new(){Compliant=[Load(body,frame,initial)]});world.Step([],[],.001);
        Assert.Equal(initial==CompliantContactInitialState.Preloaded,body.LinearVelocity.Y>0);
        Assert.Empty(world.CompliantEntries.ToArray()); // Preload is initial energy, not a new impact.
    }

    [Fact]
    public void InvalidPreloadDuplicateAndChangedInitialConditionRejectAtomically()
    {
        var f=Create();var before=f.World.Capture();var original=f.World.Loads;
        Assert.Throws<ArgumentException>(()=>f.World.ReplaceLoads(new(){Compliant=[f.Load,f.Load]}));
        Assert.Throws<ArgumentException>(()=>f.World.ReplaceLoads(new(){Compliant=[Load(f.Body,f.Frame,CompliantContactInitialState.Preloaded)]}));
        Assert.Same(original,f.World.Loads);
        Assert.Equal(before.CompliantStates.ToArray(),f.World.CompliantContacts.ToArray());
        f.World.ReplaceLoads(new());
        Assert.Throws<ArgumentException>(()=>f.World.ReplaceLoads(new(){Compliant=[Load(f.Body,f.Frame,CompliantContactInitialState.Preloaded)]}));
        Assert.Empty(f.World.CompliantContacts.ToArray());Assert.Empty(f.World.Loads.Compliant);
    }

    [Fact]
    public void FailedStepRestoresContactHistoryReportsBodiesAndClock()
    {
        var frame=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        PhysicsBody Body(int id,double x)=>new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(new(x,.405,0)),new(0,-1,0),default,1,new(1,1,1));
        var a=Body(0,-.3);var b=Body(2,.3);
        var world=new PhysicsWorld([],[new(a,Geometry(Shape.Sphere),new(0,0,0)),new(b,Geometry(Shape.Sphere),new(0,0,0)),
            new(frame,FrameGeometry,new(0,0,0))],[],new(default,maximumStep:.01,maximumEvents:1));
        var first=Load(a,frame,CompliantContactInitialState.Unloaded);
        world.ReplaceLoads(new(){Compliant=[first,Load(b,frame,CompliantContactInitialState.Unloaded)]});
        var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.02));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(before.CompliantStates.ToArray(),world.CompliantContacts.ToArray());
        Assert.Equal(before.Time,world.Time);Assert.Equal(before.StepIndex,world.StepIndex);
        Assert.Empty(world.CompliantEntries.ToArray());Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
        world.ReplaceLoads(new(){Compliant=[first]});world.Step([],[],.02);
        Assert.Single(world.CompliantEntries.ToArray());
    }
}
