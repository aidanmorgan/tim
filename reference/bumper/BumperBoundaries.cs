using System.Text.Json;
using CuriousContraptions.Gpu;

internal static class BumperBoundaries
{
    private enum Fixture { TransformedSphere, InitialOverlap, MaximumStrength, RepeatedReturn }
    internal static void Emit()
    {
        var profile = new WorkshopGpuProfile(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
        var cases = new List<object>();
        foreach (var fixture in Enum.GetValues<Fixture>())
        {
            var transformed = fixture == Fixture.TransformedSphere;
            // Body Z rotation and collider X rotation do not commute. Translation
            // participates in the body rotation; sphere rotation leaves its shape invariant.
            var ball = WorkshopInput.Basketball(new(1), transformed ? 3 : 0,
                transformed ? 7.5 : fixture == Fixture.InitialOverlap ? 4.5 : 5.1,
                transformed ? -1.75 : 0, 0, 0, 0, 1);
            var bumper = WorkshopInput.Bumper(new(2), transformed ? 3 : 0, transformed ? 5 : 4,
                transformed ? -2 : 0, 0, 0, transformed ? Math.Sqrt(.5) : 0,
                transformed ? Math.Sqrt(.5) : 1,
                BumperWork.FromCanonicalStrength(fixture == Fixture.MaximumStrength ? (Half)20 : (Half)8));
            var compiled = WorkshopPhysicsCompiler.Compile(new(new(1), WorkshopCadenceSettings.Default(),
                new(new IWorkshopInstance[] { ball, bumper })), new(15, 2));
            var bodies = compiled.Bodies.ToArray().Where(b => b.Id.Value <= 2).ToArray();
            for (var i = 0; i < bodies.Length; i++)
                if (bodies[i].Motion == RigidMotionKind.Dynamic)
                    bodies[i] = bodies[i] with { Mass = new((Half)1), LinearDrag = new((Half)0),
                        Gravity = fixture is Fixture.RepeatedReturn or Fixture.TransformedSphere
                            ? new((Half)0, (Half)(-9.81), (Half)0) : default,
                        Velocity = fixture is Fixture.MaximumStrength or Fixture.RepeatedReturn
                            ? new((Half)0, (Half)(-4d / 32), (Half)0) : default };
            var colliders = compiled.Colliders.ToArray().Where(c => c.Body.Value <= 2).ToArray();
            if (transformed)
            {
                var colliderRotation = WorkshopInput.Basketball(new(3), 0, 0, 0,
                    Math.Sqrt(.5), 0, 0, Math.Sqrt(.5)).Rotation;
                for (var i = 0; i < colliders.Length; i++)
                    if (colliders[i].Body.Value == 2)
                        colliders[i] = colliders[i] with {
                            Pose = new(new((Half).5, (Half)0, (Half).25), colliderRotation) };
            }
            var ids = colliders.Select(c => c.Material).ToHashSet();
            var materials = compiled.Materials.ToArray().Where(m => ids.Contains(m.Id)).ToArray();
            var scene = new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity,
                bodies, colliders, materials, [], [], [],
                fixture is Fixture.MaximumStrength or Fixture.RepeatedReturn
                    ? compiled.ContactWorks.ToArray() : []);
            var name = fixture switch {
                Fixture.TransformedSphere => "sphere-transformed",
                Fixture.InitialOverlap => "sphere-initial-overlap",
                Fixture.MaximumStrength => "work-maximum",
                Fixture.RepeatedReturn => "work-repeated-return",
                _ => throw new ArgumentException()
            };
            cases.Add(new { name, ticks = fixture == Fixture.TransformedSphere ? 120 :
                fixture == Fixture.RepeatedReturn ? 230 : fixture == Fixture.InitialOverlap ? 1 : 24,
                source = Convert.ToBase64String(PhysicsGpuAbi.Admission(scene, new(1), profile)) });
        }
        Console.WriteLine(JsonSerializer.Serialize(new { stateBytes = PhysicsGpuAbi.ByteLength,
            preamble = PhysicsGpuAbi.ShaderPreamble(), cases }));
    }
}
