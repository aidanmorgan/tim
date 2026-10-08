#if PLAYTEST
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralImpactEffectProbe { Launch, Miss, Simultaneous, Rejected }
internal enum GeneralImpactEffectOutcome { Completed, Rejected }
internal sealed record GeneralImpactEffectReport(GeneralImpactEffectProbe Probe,GeneralImpactEffectOutcome Outcome,
    int[] Counts,double[] ContactTimes,double[] Position,double[] Velocity,bool ExactRestore,bool ExactReplay);
internal static class GeneralImpactEffectQualification
{
    private sealed record EffectState(int Count,double Time):PhysicsImpactEffectState;
    private sealed class ProbeFailure:Exception;
    private sealed class Effect(PhysicsBodyId owner,GeneralImpactEffectProbe probe):PhysicsImpactEffect(owner)
    {
        internal EffectState State { get; private set; }=new(0,0);
        public override PhysicsImpactEffectState Capture()=>State;
        public override void Restore(PhysicsImpactEffectState state)=>State=(EffectState)state;
        public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)
        {
            State=new(State.Count+1,context.Impact.Time);
            if(probe==GeneralImpactEffectProbe.Rejected) throw new ProbeFailure();
            if(probe==GeneralImpactEffectProbe.Simultaneous) return new([],[],[]);
            return new([new(context.A.After.Id,new(-10,0,0),context.A.After.Pose.Center)],[],[]);
        }
    }
    internal static GeneralImpactEffectReport Run(GeneralImpactEffectProbe probe)
    {
        if(!Enum.IsDefined(probe)) throw new ArgumentOutOfRangeException(nameof(probe));
        var ball=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(-2,0,0)),
            new(10,0,0),default,1,new(.1,.1,.1));
        PhysicsObject Wall(int id)=>new(new(new(id),PhysicsMotionType.Static,RigidPose.Identity,default,default),
            new([new(new ConvexBox(new(.01,10,10)),AffineTransform.Identity)]),new(1,0,0));
        PhysicsObject[] objects=probe==GeneralImpactEffectProbe.Simultaneous?
            [new(ball,new([new(new ConvexSphere(.5),AffineTransform.Identity)]),new(1,0,0)),Wall(1),Wall(2)]:
            [new(ball,new([new(new ConvexSphere(.5),AffineTransform.Identity)]),new(1,0,0)),Wall(1)];
        Effect[] effects=probe==GeneralImpactEffectProbe.Simultaneous?[new(new(2),probe),new(new(1),probe)]:[new(new(1),probe)];
        var world=new PhysicsWorld(effects,objects,[],new(default,maximumStep:1));
        var duration=probe==GeneralImpactEffectProbe.Miss?.1:.2;
        GeneralImpactEffectOutcome Step()
        {
            try { world.Step([],[],duration); return GeneralImpactEffectOutcome.Completed; }
            catch(ProbeFailure) { return GeneralImpactEffectOutcome.Rejected; }
        }
        var before=world.Capture(); var initial=effects.Select(e=>e.State).ToArray();
        var outcome=Step(); var after=world.Capture(); var state=effects.Select(e=>e.State).ToArray();
        var restoredOnFailure=outcome!=GeneralImpactEffectOutcome.Rejected||
            after.BodyStates.SequenceEqual(before.BodyStates)&&world.Time==0&&world.RetainedContactPairs==0&&state.SequenceEqual(initial);
        world.Restore(before);
        var restored=restoredOnFailure&&world.Capture().BodyStates.SequenceEqual(before.BodyStates)&&
            world.Time==before.Time&&world.StepIndex==before.StepIndex&&effects.Select(e=>e.State).SequenceEqual(initial);
        var replay=Step()==outcome&&world.Capture().BodyStates.SequenceEqual(after.BodyStates)&&
            world.Time==after.Time&&world.StepIndex==after.StepIndex&&effects.Select(e=>e.State).SequenceEqual(state);
        return new(probe,outcome,state.Select(s=>s.Count).ToArray(),state.Select(s=>s.Time).ToArray(),
            [ball.Center.X,ball.Center.Y,ball.Center.Z],[ball.LinearVelocity.X,ball.LinearVelocity.Y,ball.LinearVelocity.Z],restored,replay);
    }
}
#endif
