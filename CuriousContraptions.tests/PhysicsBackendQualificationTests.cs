#if DEBUG || PLAYTEST
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class PhysicsBackendQualificationTests(HeadlessFixture godot,ITestOutputHelper output)
{
    [Theory]
    [InlineData(PhysicsBackendMotionProbe.Experiment.FastTranslation)]
    [InlineData(PhysicsBackendMotionProbe.Experiment.PureRotation)]
    [InlineData(PhysicsBackendMotionProbe.Experiment.OpposingBodies)]
    public void RecordRealSimulationWithContinuousCollisionDetectionEnabled(PhysicsBackendMotionProbe.Experiment experiment)
    {
        var probe=new PhysicsBackendMotionProbe { Case=experiment };
        godot.Tree.Root.AddChild(probe);
        try
        {
            var elapsed=System.Diagnostics.Stopwatch.StartNew();
            while(!probe.Complete && elapsed.Elapsed.TotalSeconds<5) godot.Engine.Iteration();
            Assert.True(probe.Complete);
            foreach(var frame in probe.Frames)
                output.WriteLine(System.Text.Json.JsonSerializer.Serialize(frame));
            Assert.All(probe.Frames,frame=>Assert.All(frame.Position,value=>Assert.True(float.IsFinite(value))));
        }
        finally { probe.Free(); }
    }

    [Theory]
    [InlineData(PhysicsBackendQualification.Probe.SphereTranslation,true,.44f)]
    [InlineData(PhysicsBackendQualification.Probe.BoxTranslation,true,.44f)]
    [InlineData(PhysicsBackendQualification.Probe.HullTranslation,true,.44f)]
    [InlineData(PhysicsBackendQualification.Probe.CompoundPassage,false,1f)]
    [InlineData(PhysicsBackendQualification.Probe.CompoundWall,true,.29f)]
    public void PackagedPhysicsServerQueriesActualConvexAndCompoundGeometry(
        PhysicsBackendQualification.Probe probe,bool expected,float fraction)
    {
        Assert.NotNull(godot.Tree);
        var observation=PhysicsBackendQualification.Run(probe);
        output.WriteLine(observation.ToString());
        Assert.True(observation.SpaceAvailable);
        Assert.Equal(expected,observation.Collided);
        Assert.InRange(observation.SafeFraction,fraction-.02f,fraction+.02f);
        if(expected)
        {
            Assert.InRange(observation.Contacts,1,4);
            Assert.True(observation.Normal.X<-.9f);
        }
        else Assert.Equal(0,observation.Contacts);
    }
}
#endif
