using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SlidingBladeDynamicsTests(NativeSceneFixture godot)
{
    public enum BladeKind { PoweredGate, BeamShutter }
    private const string PartId="blade";
    private static string Catalog(BladeKind kind)=>kind switch
    {
        BladeKind.PoweredGate=>"powered_gate",BladeKind.BeamShutter=>"beam_shutter",
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static JointSlot Guide(BladeKind kind)=>kind switch
    {
        BladeKind.PoweredGate=>PoweredGatePart.BladeGuide,BladeKind.BeamShutter=>BeamShutterPart.BladeGuide,
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };

    [Theory]
    [InlineData(BladeKind.PoweredGate,false,false)]
    [InlineData(BladeKind.PoweredGate,false,true)]
    [InlineData(BladeKind.PoweredGate,true,false)]
    [InlineData(BladeKind.PoweredGate,true,true)]
    [InlineData(BladeKind.BeamShutter,false,false)]
    [InlineData(BladeKind.BeamShutter,false,true)]
    [InlineData(BladeKind.BeamShutter,true,false)]
    [InlineData(BladeKind.BeamShutter,true,true)]
    public void ReturnForcesUseSharedJointResponseAndExactResetReplay(BladeKind kind,bool rotated,bool excited)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=world.AddPart(new(){Id=PartId,Kind=Catalog(kind),Position=[0,4,0],
                Orientation = rotated?PartOrientation.FromEulerDegrees(25,40,15):PartOrientation.FromEulerDegrees(0,0,0)});
            var construction=part.Transform;
            PhysicsBodySnapshot[] Run()
            {
                world.Start();var current=world.FindPart(PartId)!;
                var joint=(PhysicsFrameJoint)world.CurrentJoint(new(current,Guide(kind)));
                var row=joint.Travel.Jacobian.Bind(joint.A,joint.B);
                var term=Assert.Single(row.Terms.ToArray(),term=>term.Body==joint.A);
                const double stiffness=14,damping=4;
                var expectedPosition=joint.Travel.Error;
                var expectedSpeed=excited?2d:0;
                if(excited)world.Physics.ApplyImpulse(joint.A.Id,term.Linear*(expectedSpeed/joint.A.InverseMass),joint.A.Center);
                var energy=joint.A.KineticEnergy+.5*stiffness*expectedPosition*expectedPosition;
                var step=(double)(MachineWorld.Tick/MachineWorld.Substeps);
                for(var tick=0;tick<60;tick++)
                {
                    world.Step();
                    var elastic=Assert.Single(world.Physics.Loads.Elastic.ToArray(),load=>load.Joint==joint.Id);
                    var resistance=Assert.Single(world.Physics.Loads.Damping.ToArray(),load=>load.Joint==joint.Id);
                    Assert.Equal(FrameJointKind.Slider,elastic.Kind);Assert.Equal(FrameJointKind.Slider,resistance.Kind);
                    Assert.Equal(stiffness,elastic.Potential.Stiffness);
                    Assert.Equal(damping,resistance.NegativeCoefficient);Assert.Equal(damping,resistance.PositiveCoefficient);
                    for(var i=0;i<MachineWorld.Substeps;i++)
                    {
                        var denominator=1+step*damping/2+step*step*stiffness/4;
                        var nextPosition=((1+step*damping/2-step*step*stiffness/4)*expectedPosition+step*expectedSpeed)/denominator;
                        expectedSpeed=((1-step*damping/2-step*step*stiffness/4)*expectedSpeed-step*stiffness*expectedPosition)/denominator;
                        expectedPosition=nextPosition;
                    }
                    var position=joint.Travel.Error;var speed=joint.Travel.Jacobian.Bind(joint.A,joint.B).Speed;
                    Assert.InRange(Math.Abs(position-expectedPosition),0,1e-8);
                    Assert.InRange(Math.Abs(speed-expectedSpeed),0,1e-8);
                    var nextEnergy=joint.A.KineticEnergy+.5*stiffness*position*position;
                    Assert.InRange(nextEnergy,0,energy+1e-9);energy=nextEnergy;
                }
                Assert.Empty(world.Physics.MotorUse.ToArray());
                return world.Physics.Capture().BodyStates.ToArray();
            }
            var first=Run();world.Restore();
            Assert.Equal(construction,world.FindPart(PartId)!.Transform);
            Assert.False(world.Running);Assert.Equal(0,world.Ticks);
            Assert.Equal(first,Run());
        }
        finally {world.Free();}
    }
}
