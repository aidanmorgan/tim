using System.Text.Json;
using Godot;
using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class AngularVelocityAnimationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Motor=new("motor"),Battery=new("battery");
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static Vector3 Axis(AnimationRotationAxis axis)=>axis switch
    {
        AnimationRotationAxis.X=>Vector3.Right,
        AnimationRotationAxis.Y=>Vector3.Up,
        AnimationRotationAxis.Z=>Vector3.Back,
        _=>throw new ArgumentOutOfRangeException(nameof(axis))
    };
    private static void Angle(Node3D target,Transform3D baseline,AnimationRotationAxis axis,double angle)
    {
        var expected=baseline.Basis*new Basis(Axis(axis),(float)angle);
        Assert.InRange((target.Basis.X-expected.X).Length(),0,2e-6f);
        Assert.InRange((target.Basis.Y-expected.Y).Length(),0,2e-6f);
        Assert.InRange((target.Basis.Z-expected.Z).Length(),0,2e-6f);
        Assert.Equal(baseline.Origin,target.Position);
    }
    private partial class Probe : MotorPart
    {
        public AnimationRotationAxis Axis;
        public AnimationClock Clock;
        public AnimationDirection Direction;
        public override IReadOnlyList<SceneAngularVelocityAnimation> AngularVelocityAnimations=>
            [base.AngularVelocityAnimations[0] with {SourceAxis=Axis,TargetAxis=Axis,Clock=Clock,Direction=Direction}];
    }
    public static IEnumerable<object[]> Modes()
    {
        foreach(var axis in Enum.GetValues<AnimationRotationAxis>())
        foreach(var clock in Enum.GetValues<AnimationClock>())
        foreach(var direction in Enum.GetValues<AnimationDirection>())
            yield return [axis,clock,direction];
    }
    [Theory]
    [MemberData(nameof(Modes))]
    public void RelativeCommittedRatesSupportEveryAxisClockDirectionAndHold(AnimationRotationAxis axis,
        AnimationClock clock,AnimationDirection direction)
    {
        var world=World();
        try
        {
            var motor=new Probe {Definition=world.Registry.Definitions[Motor.Value],Axis=axis,Clock=clock,Direction=direction};
            motor.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Motor.Value});world.AttachPart(motor);
            var declaration=Assert.Single(motor.AngularVelocityAnimations);var target=declaration.Target;var baseline=target.Transform;
            world.Start();
            var body=world.PhysicsAssembly.Body(declaration.Body).Id;
            var reference=world.PhysicsAssembly.Body(declaration.Reference).Id;
            var reads=world.PhysicsAssembly.CapturePublicationReads(world.Physics).ToArray();
            var rotation=RigidRotation.FromRotationVector(new(.4,-.6,.2));
            var local=SceneGeometryAdapter.CaptureVector(Axis(axis));
            var worldAxis=rotation.Apply(local);
            reads[reference.Index]=reads[reference.Index] with
            {
                Pose=new(reference,PhysicsMotionType.Static,new(default,rotation),RigidPose.Identity),
                Velocity=new(default,worldAxis*(.5*Math.Tau))
            };
            void Rate(double relative)=>reads[body.Index]=reads[body.Index] with
                {Velocity=new(default,worldAxis*((relative+.5)*Math.Tau))};
            Rate(1.5);
            var buffer=new CommittedPoseBuffer(new(new(17),new(0),0),reads,[],[],[],[],[]);
            SceneAnimationRun run;
            using(var seed=buffer.Acquire())run=new(world.Parts,world.PhysicsAssembly,seed,new Dictionary<SceneCounterKey,SimulationCounterId>(),new Dictionary<SceneTimerKey,SimulationTimerId>(),new Dictionary<SceneOscillatorKey,SimulationOscillatorId>());
            var sign=direction==AnimationDirection.Forward?1:-1;
            void Commit(int revision)
            {
                buffer.BeginWrite(0);buffer.Stage(new(new(17),new(revision),revision*.25),reads,[],[],[],[],[]);buffer.Publish();
                using var read=buffer.Acquire();run.Publish(read);
            }
            Commit(1);run.Present(.25);Angle(target,baseline,axis,sign*.375*Math.Tau);
            Rate(-.5);Commit(2);run.Present(.25);
            // Simulation changes at the new commit; presentation changes at its last displayed time.
            var phase=clock==AnimationClock.Simulation?.75:.25;
            Angle(target,baseline,axis,sign*phase*Math.Tau);
            Rate(0);Commit(3);run.Present(.25);
            phase+=clock==AnimationClock.Simulation?-.125:0;
            Angle(target,baseline,axis,sign*phase*Math.Tau);
            var held=target.Transform;Commit(4);run.Present(.25);Assert.Equal(held,target.Transform);
            Assert.Equal(0,run.Present(.125).TransformWrites);
            run.Remove();Assert.Equal(baseline,target.Transform);
            Assert.Throws<InvalidOperationException>(()=>run.Present(.1));buffer.Remove();
        }
        finally{world.Free();}
    }

    private partial class Supply : BatteryPart
    {
        public int Visits,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected angular animation failure.");}
    }
    [Theory]
    [InlineData(0,1,0f)]
    [InlineData(0,4,37f)]
    [InlineData(8,1,37f)]
    [InlineData(8,4,0f)]
    public void MotorIndexUsesAcceptedRateWhileShaftKeepsPhysicsAndExactReset(int speed,int failure,float orientation)
    {
        var world=World();
        try
        {
            var motor=(MotorPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Motor.Value,
                Position=[0,6,0],Orientation=PartOrientation.FromEulerDegrees(orientation,orientation,orientation),
                Properties=new(){[PartParameterName.Of(MotorParameter.Speed)]=speed}});
            var supply=new Supply {Definition=world.Registry.Definitions[Battery.Value]};
            supply.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[4,6,0]});world.AttachPart(supply);
            Assert.True(world.Connect(supply,SocketId.Supply,motor,SocketId.PowerIn,ConnectionDomain.Electrical));
            var declaration=Assert.Single(motor.AngularVelocityAnimations);var target=declaration.Target;var baseline=target.Transform;
            var saved=Saved(world);world.Start();
            Assert.DoesNotContain(target,world.PhysicsAssembly.PresentationTargets);
            var physical=world.PhysicsAssembly.PresentationTargets.Single(node=>node!=motor&&motor.IsAncestorOf(node));
            Assert.NotSame(physical,target);Assert.False(physical.IsAncestorOf(target));
            var body=world.PhysicsAssembly.Body(declaration.Body).Id;var reference=world.PhysicsAssembly.Body(declaration.Reference).Id;
            double phase=0,previousRate=0;
            void Step()
            {
                world.Step();phase+=previousRate*(double)MachineWorld.Tick;
                using var read=world.ReadCommittedPoses();
                var axis=read.Read(PoseSample.Current,reference.Index).Pose.Rotation.Apply(new(0,0,1));
                previousRate=CollisionVector.Dot(read.ReadVelocity(PoseSample.Current,body).AngularRadiansPerSecond-
                    read.ReadVelocity(PoseSample.Current,reference).AngularRadiansPerSecond,axis);
            }
            for(var i=0;i<6;i++)Step();
            Assert.Equal(baseline,target.Transform);Assert.Equal(saved,Saved(world));
            var state=world.Physics.Capture().BodyStates.ToArray();var work=motor.SuppliedWork;var travel=motor.ShaftTravel;
            motor.Active=false;world.Running=false;world.PresentFrame(.1,1);
            Angle(target,baseline,AnimationRotationAxis.Z,phase);
            var shown=target.Transform;world.PresentFrame(2,1);Assert.Equal(shown,target.Transform);
            Assert.Equal(state,world.Physics.Capture().BodyStates.ToArray());Assert.Equal(work,motor.SuppliedWork);Assert.Equal(travel,motor.ShaftTravel);
            if(speed==0)Assert.Equal(baseline,target.Transform);else Assert.NotEqual(baseline,target.Transform);
            motor.Active=true;world.Running=true;
            var command=world.QueueBinaryInput(new(supply,BatteryPart.EnableInput),BinaryInputState.Disabled);
            supply.FailAt=supply.Visits+failure;
            Assert.Throws<InvalidOperationException>(world.Step);Assert.Equal(state,world.Physics.Capture().BodyStates.ToArray());
            Assert.False(world.TryPeekControlResult(out _));world.PresentFrame(.1,1);Assert.Equal(shown,target.Transform);
            supply.FailAt=0;Step();Step();Assert.False(motor.Active);
            Assert.True(world.TryPeekControlResult(out var result));Assert.Equal(command,result.Id.Sequence);world.AcknowledgeControlResult(result.Id);
            world.PresentFrame(.1,1);Angle(target,baseline,AnimationRotationAxis.Z,phase);
            if(speed!=0){Assert.NotEqual(shown,target.Transform);Assert.True(Math.Abs(previousRate)>0);}
            Assert.Equal(work,motor.SuppliedWork);
            var resumed=world.QueueBinaryInput(new(supply,BatteryPart.EnableInput),BinaryInputState.Enabled);
            var changed=world.QueueScalarInput(new(motor,MotorPart.SpeedInput),4);
            for(var i=0;i<12;i++)Step();
            Assert.True(motor.Active);
            Assert.True(world.TryPeekControlResult(out result));Assert.Equal(resumed,result.Id.Sequence);world.AcknowledgeControlResult(result.Id);
            Assert.True(world.TryPeekControlResult(out result));Assert.Equal(changed,result.Id.Sequence);world.AcknowledgeControlResult(result.Id);
            Assert.Equal(4,world.ReadScalarInput(new(motor,MotorPart.SpeedInput)));
            Assert.InRange(Math.Abs(previousRate),3.999,4.001);
            world.PresentFrame(.1,1);Angle(target,baseline,AnimationRotationAxis.Z,phase);
            world.Restore();Assert.Equal(saved,Saved(world));
            var restored=(MotorPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(baseline,Assert.Single(restored.AngularVelocityAnimations).Target.Transform);
        }
        finally{world.Free();}
    }

    public enum InvalidDeclaration { SourceAxis,TargetAxis,Clock,Direction,UnknownBody,ForeignReference,PhysicalTarget,PhysicalAncestor }
    private partial class InvalidMotor : MotorPart
    {
        public InvalidDeclaration Invalid;
        public MachinePart Foreign=null!;
        private static readonly BodySlot Missing=new(_=>RigidPose.Identity,p=>ShaftBody.Dynamics(p),
            BodyQueryPolicy.Include,p=>ShaftBody.Material(p),_=>[]);
        public override IReadOnlyList<SceneAngularVelocityAnimation> AngularVelocityAnimations
        {
            get
            {
                var declaration=base.AngularVelocityAnimations[0];
                return [Invalid switch
                {
                    InvalidDeclaration.SourceAxis=>declaration with {SourceAxis=(AnimationRotationAxis)99},
                    InvalidDeclaration.TargetAxis=>declaration with {TargetAxis=(AnimationRotationAxis)99},
                    InvalidDeclaration.Clock=>declaration with {Clock=(AnimationClock)99},
                    InvalidDeclaration.Direction=>declaration with {Direction=(AnimationDirection)99},
                    InvalidDeclaration.UnknownBody=>declaration with {Body=new(this,Missing)},
                    InvalidDeclaration.ForeignReference=>declaration with {Reference=new(Foreign,RootBody)},
                    InvalidDeclaration.PhysicalAncestor=>declaration with {Target=MotorPart.ShaftBody.Presentation(this)[0].Target.GetParent<Node3D>()},
                    InvalidDeclaration.PhysicalTarget=>declaration with {Target=MotorPart.ShaftBody.Presentation(this)[0].Target},
                    _=>throw new ArgumentOutOfRangeException()
                }];
            }
        }
    }
    [Theory]
    [InlineData(InvalidDeclaration.SourceAxis)]
    [InlineData(InvalidDeclaration.TargetAxis)]
    [InlineData(InvalidDeclaration.Clock)]
    [InlineData(InvalidDeclaration.Direction)]
    [InlineData(InvalidDeclaration.UnknownBody)]
    [InlineData(InvalidDeclaration.ForeignReference)]
    [InlineData(InvalidDeclaration.PhysicalTarget)]
    [InlineData(InvalidDeclaration.PhysicalAncestor)]
    public void InvalidSourcesAndCompetingPhysicalWriterReject(InvalidDeclaration invalid)
    {
        var world=World();
        try
        {
            var motor=new InvalidMotor {Definition=world.Registry.Definitions[Motor.Value],Invalid=invalid};
            motor.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Motor.Value});world.AttachPart(motor);
            motor.Foreign=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[4,0,0]});
            if(invalid is InvalidDeclaration.PhysicalTarget or InvalidDeclaration.PhysicalAncestor)Assert.Throws<InvalidOperationException>(world.Start);
            else Assert.Throws<ArgumentException>(world.Start);
        }
        finally{world.Free();}
    }
}
