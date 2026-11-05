using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;
using Xunit;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 10.5 (CAT-065): the Trampoline is its package's declaration data only, a static frame (navy back plate, two side and two end
/// rails with their tops on the rest plane, four gold corner posts, feet) and one compliant surface, a massless unilateral spring-damper
/// over a 2.4 × 1.8 m footprint at rest 0.2 m above the origin with a 0.65 m stroke. The bed meets a body at its four support samples,
/// each with one committed contact state (Outside, OnTop, Under) and offset: a sample over the bed (within the half-extents plus a 1 cm
/// footprint edge) that reached it from above the rest plane, or crossed its edge no deeper than the 0.10 m arrival depth, is OnTop and
/// bears the bed while below the plane, pushed by (k/4)·(d − offset); deeper it is Under and gets nothing until it rises to the plane.
/// The frame carries every sample outside the footprint. Every behaviour here runs the real Workshop construction through
/// <see cref="WorkshopPhysicsCompiler"/> and the shipped C# engine on the worker transport (the energy-hole law fact runs the engine
/// directly on an unframed surface). Expectations come from the declared f32 values, the declared payload geometry and the analytic
/// spring-damper, never from observed output. The production Basketball is a 1 kg, 0.34 m-radius sphere (the story's 0.6 kg / 0.12 m
/// figures do not describe the shipped part), and the Weight supplies the 0.25–8 kg payloads.
/// </summary>
public sealed class TrampolineTests(ITestOutputHelper output)
{
    private static readonly PartId Trampoline = new("trampoline");
    private static readonly PartId Ball = new("ball");
    private static readonly ParameterId TensionKey = new("tension");
    private static readonly ParameterId DampingKey = new("damping_ratio");
    private const int Rate = 120;
    private const float G = 9.81f;
    // The declared membrane: half extents, rest height and stroke in the frame (one truth with the package declaration).
    private const float HalfX = 1.2f, HalfZ = .9f, Rest = .2f, Stroke = .65f;
    /// <summary>The law's footprint edge e (CAT-065 §2): a sample is over the bed within the half-extents plus 1 cm.</summary>
    private const float Edge = 1e-2f;
    /// <summary>The law's four sample directions, −n ± ε·right ± ε·forward, with ε = 1/64 (CAT-065 §2).</summary>
    private const float SampleSpread = 1f / 64f;
    /// <summary>The designed rails (CAT-065 §2): 0.2 m tall and 0.16 m wide, tops on the rest plane, inner faces on the footprint edges.</summary>
    private const float RailHeight = .2f, RailWidth = .16f;
    private const float BallRadius = .34f, BallMass = 1f, BallMoment = .04624f;
    /// <summary>The bed visual's declared rest height (its slab's top face on the rest plane).</summary>
    private const float BedVisualRestY = .19f;
    /// <summary>The bed visual's travel per metre of committed deflection: (stroke − 1 mm)/stroke, so it stops 1 mm above the back plate.</summary>
    private const float BedTravel = (Stroke - .001f) / Stroke;
    private static readonly Vector3 Frame = new(0, 3, 0);
    /// <summary>World height of the untilted bed's rest plane and of the stroke floor (the back plate's top face).</summary>
    private static readonly float BedY = Frame.Y + Rest, FloorY = Frame.Y + Rest - Stroke;
    /// <summary>The lesson's aimed bed: −30° about Z (CAT-065 harvest row 28).</summary>
    private static readonly Quaternion Aimed = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -MathF.PI / 6);
    /// <summary>The Ramp's declared slab: 3 m long (half 1.5) and 0.18 m thick (half 0.09), its top face the rolling surface.</summary>
    private const float RampHalfLength = 1.5f, RampHalfThickness = .09f;

    // Gameplay tolerances, chosen from what a player would see, before observing any run.
    /// <summary>The game-grade envelope's at-rest linear speed (its sleep threshold): slower motion is at rest.</summary>
    private const float RestSpeed = .01f;
    /// <summary>Settled drift: 5 mm, under 2% of the Basketball's radius, so no resting load visibly creeps over the window.</summary>
    private const float RestingDrift = .005f;
    /// <summary>Lateral speed a vertical launch may pick up: 0.05 m/s drifts 7.5 cm over a 1.5 s flight, a tenth of the Receiver's
    /// 0.66 m capture half-width, so a straight bounce stays straight.</summary>
    private const float LateralSpeed = .05f;
    /// <summary>Lateral drift through a bounce: 1 cm, invisible beside the 2.4 m bed and far inside any receiver's capture margin.</summary>
    private const float LateralDrift = .01f;
    /// <summary>The upward speed a struck frame may leave a payload with and still count as no launch: 0.3 m/s rises under 5 mm.</summary>
    private const float NoLaunchSpeed = .3f;
    /// <summary>The static-compression band the intent sets for settled loads: ±1.5 cm.</summary>
    private const double SettleBand = .015;
    /// <summary>The intent matrix's energy envelope: no tick holds more than 1.005 × its starting energy, bed store included.</summary>
    private const double EnergyEnvelope = 1.005;
    /// <summary>"Reaches the back plate": a body resting on the plate sits within the 0.2 mm contact slop of it, so a lowest point within
    /// 1 cm of the stroke floor is a body on the plate, while a held load stays decimetres above it.</summary>
    private const float PlateClearance = .01f;
    /// <summary>A lesson's stall guard: 10 game seconds, longer than any drop, bounce and capture on the bench.</summary>
    private const int LessonGuard = 10 * Rate;

    private static WorkshopGenericInstance Bed(ulong id = 1, float tension = 300f, float damping = .12f, Quaternion? rotation = null,
        Vector3? at = null)
    {
        var q = rotation ?? Quaternion.Identity; var p = at ?? Frame;
        return WorkshopInput.Generic(Trampoline, new(id), p.X, p.Y, p.Z, q.X, q.Y, q.Z, q.W,
            new([new(TensionKey, tension), new(DampingKey, damping)]));
    }

    private static WorkshopGenericInstance Basketball(ulong id, float x, float y, float z) =>
        WorkshopInput.Generic(Ball, new(id), x, y, z, 0, 0, 0, 1);

    private static WorkshopGenericInstance Posed(string part, ulong id, Vector3 p, Quaternion q) =>
        WorkshopInput.Generic(new PartId(part), new(id), p.X, p.Y, p.Z, q.X, q.Y, q.Z, q.W);

    /// <summary>A default Ramp tilted <paramref name="degrees"/> about Z (falling towards +X) whose lower top edge is at
    /// <paramref name="edge"/>, with its slope rotation and centre.</summary>
    private static (WorkshopGenericInstance Ramp, Quaternion Slope, Vector3 Centre) RampTo(ulong id, Vector3 edge, float degrees)
    {
        var slope = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -degrees * MathF.PI / 180);
        var centre = edge - Vector3.Transform(new Vector3(RampHalfLength, RampHalfThickness, 0), slope);
        return (Posed("ramp", id, centre, slope), slope, centre);
    }

    private static WorkshopConstruction Bench(params IWorkshopInstance[] parts) => new(new(2), WorkshopCadenceSettings.Default(), new(parts));

    private static PhysicsBodyRead Find(WorkshopRead read, GpuBodyId id)
    {
        Assert.True(read.Bodies.TryGet(id, out var body), $"body {id.Value} is read");
        return body;
    }
    private static Vector3 Origin(PhysicsBodyRead b) => new((b.Body.Cell.X + b.Body.Local.X) / 16f,
        (b.Body.Cell.Y + b.Body.Local.Y) / 16f, (b.Body.Cell.Z + b.Body.Local.Z) / 16f);
    private static Vector3 Velocity(PhysicsBodyRead b) => new(b.Body.Velocity.X, b.Body.Velocity.Y, b.Body.Velocity.Z);
    private static Vector3 Spin(PhysicsBodyRead b) => new(b.AngularVelocity.X, b.AngularVelocity.Y, b.AngularVelocity.Z);
    private static Quaternion Rotation(PhysicsBodyRead b) => new(b.Rotation.X, b.Rotation.Y, b.Rotation.Z, b.Rotation.W);
    /// <summary>A world point in a frame placed at <paramref name="at"/> with rotation <paramref name="frame"/>.</summary>
    private static Vector3 InFrame(Vector3 point, Quaternion frame, Vector3 at) => Vector3.Transform(point - at, Quaternion.Inverse(frame));

    /// <summary>The committed deflection of the bed framed by <paramref name="frame"/>.</summary>
    private static float Surface(WorkshopRead read, GpuBodyId frame) =>
        Assert.Single(read.Presentation.Surfaces.ToArray(), surface => surface.Frame == frame).Deflection;

    /// <summary>The evaluated local Y of the bed's <c>Visual/Visual_Bed</c> binding at the harness's current time.</summary>
    private static float BedVisualY(WorkshopIntegrationHarness harness, GpuBodyId frame) =>
        Assert.Single(harness.Presentation!.Advance(harness.Schedule, PhysicalTime.FromNanoseconds(harness.Time.Nanoseconds)),
            sample => sample.Owner == frame && sample.Binding == 0).Value.Metres.Value;

    /// <summary>The membrane deflection a body's lowest point along −n implies, from its observed pose: rest − height clamped to the
    /// stroke, when that point is inside the footprint (zero otherwise). Independent of the engine's own computation.</summary>
    private static double Deflection(Vector3 lowestPoint, Quaternion frame, Vector3 framePosition)
    {
        var local = InFrame(lowestPoint, frame, framePosition);
        if (MathF.Abs(local.X) > HalfX || MathF.Abs(local.Z) > HalfZ) return 0;
        return Math.Clamp(Rest - local.Y, 0, Stroke);
    }

    private static double BallDeflection(PhysicsBodyRead ball, Quaternion? frame = null, Vector3? at = null)
    {
        var q = frame ?? Quaternion.Identity;
        return Deflection(Origin(ball) - BallRadius * Vector3.Transform(Vector3.UnitY, q), q, at ?? Frame);
    }

    /// <summary>The Basketball's total mechanical energy with the membrane's stored ½k·d², potential measured from the height at which
    /// its lowest point meets the untilted stroke floor, so the 0.5 % envelope is a few millimetres of rise, not a share of the
    /// arbitrary world height.</summary>
    private static double BallEnergy(PhysicsBodyRead ball, float tension, Quaternion? frame = null)
    {
        var v = Velocity(ball); var w = Spin(ball); var d = BallDeflection(ball, frame);
        return .5 * BallMass * Vector3.Dot(v, v) + .5 * BallMoment * Vector3.Dot(w, w) +
            BallMass * G * (Origin(ball).Y - (FloorY + BallRadius)) + .5 * tension * d * d;
    }

    // ---------------- declared payload geometry: the test's own support queries ----------------

    /// <summary>One dynamic payload as its package compiles it: mass, mass frame and colliders, the declared data the engine runs.</summary>
    private sealed record DeclaredBody(float Mass, Vector3 Centre, Quaternion Principal, Vector3 Moments, ColliderDeclaration[] Colliders);

    private static DeclaredBody Declared(WorkshopConstruction construction, GpuBodyId id)
    {
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == id);
        var m = body.MassProperties;
        return new(body.Mass.Value, new(m.LocalCentreOfMass.X, m.LocalCentreOfMass.Y, m.LocalCentreOfMass.Z),
            new(m.PrincipalFrame.X, m.PrincipalFrame.Y, m.PrincipalFrame.Z, m.PrincipalFrame.W),
            new(m.X.Moment, m.Y.Moment, m.Z.Moment), scene.Colliders.ToArray().Where(c => c.Body == id).ToArray());
    }

    /// <summary>The payload's farthest point along the unit direction <paramref name="d"/>, at origin <paramref name="at"/> and rotation
    /// <paramref name="q"/>, from the declared sphere, box, cylinder and full-torus geometry.</summary>
    private static Vector3 Support(DeclaredBody body, Vector3 at, Quaternion q, Vector3 d)
    {
        var best = Vector3.Zero; var reach = float.NegativeInfinity;
        foreach (var c in body.Colliders)
        {
            var r = q * new Quaternion(c.Pose.Rotation.X, c.Pose.Rotation.Y, c.Pose.Rotation.Z, c.Pose.Rotation.W);
            var centre = at + Vector3.Transform(new Vector3(c.Pose.Translation.X, c.Pose.Translation.Y, c.Pose.Translation.Z), q);
            var axis = Vector3.Transform(Vector3.UnitY, r);
            var radial = d - Vector3.Dot(d, axis) * axis;
            radial = radial.LengthSquared() > 1e-12f ? Vector3.Normalize(radial) : Vector3.Zero;
            Vector3 Face(Vector3 u, float h) => (Vector3.Dot(d, u) >= 0f ? h : -h) * u;
            var point = c.Shape switch
            {
                Gpu.ColliderShapeKind.Sphere => centre + c.Radius.Value * d,
                Gpu.ColliderShapeKind.Box => centre + Face(Vector3.Transform(Vector3.UnitX, r), c.HalfExtents.X) + Face(axis, c.HalfExtents.Y) +
                    Face(Vector3.Transform(Vector3.UnitZ, r), c.HalfExtents.Z),
                Gpu.ColliderShapeKind.Cylinder => centre + Face(axis, c.HalfExtents.Y) + c.Radius.Value * radial,
                Gpu.ColliderShapeKind.Torus when c.HalfExtents.Z == ColliderDeclaration.FullSweep =>
                    centre + c.Radius.Value * radial + c.HalfExtents.Y * d,
                _ => throw new ArgumentException($"The test has no support geometry for a {c.Shape} collider.")
            };
            if (Vector3.Dot(point, d) > reach) { reach = Vector3.Dot(point, d); best = point; }
        }
        return best;
    }

    /// <summary>The payload's four support samples on a bed framed by <paramref name="frame"/> (the law's diagonal directions).</summary>
    private static Vector3[] Samples(DeclaredBody body, Vector3 at, Quaternion q, Quaternion frame)
    {
        var n = Vector3.Transform(Vector3.UnitY, frame); var right = Vector3.Transform(Vector3.UnitX, frame);
        var forward = Vector3.Transform(Vector3.UnitZ, frame);
        return [.. new[] { (1, 1), (-1, 1), (1, -1), (-1, -1) }.Select(s =>
            Support(body, at, q, Vector3.Normalize(-n + SampleSpread * (s.Item1 * right + s.Item2 * forward))))];
    }

    private static bool OverBed(Vector3 local) => MathF.Abs(local.X) <= HalfX + Edge && MathF.Abs(local.Z) <= HalfZ + Edge;

    /// <summary>The deepest over-bed sample's depth below the rest plane, within the stroke (zero when none is below it).</summary>
    private static double SampleDepth(DeclaredBody body, PhysicsBodyRead read, Quaternion frame, Vector3 framePosition) =>
        Samples(body, Origin(read), Rotation(read), frame).Select(s => InFrame(s, frame, framePosition))
            .Where(OverBed).Select(local => Math.Clamp(Rest - local.Y, 0, Stroke)).DefaultIfEmpty(0).Max();

    /// <summary>A payload's mechanical energy from its declared mass frame (translation of the centre of mass, rotation about the
    /// principal axes, gravity from the untilted stroke floor) plus the bed store <paramref name="stored"/>.</summary>
    private static double Energy(DeclaredBody body, PhysicsBodyRead read, double stored)
    {
        var q = Rotation(read); var v = Velocity(read);
        var w = Vector3.Transform(Spin(read), Quaternion.Inverse(q * body.Principal));
        var centre = Origin(read) + Vector3.Transform(body.Centre, q);
        return .5 * body.Mass * Vector3.Dot(v, v) +
            .5 * (body.Moments.X * w.X * w.X + body.Moments.Y * w.Y * w.Y + body.Moments.Z * w.Z * w.Z) +
            body.Mass * G * (centre.Y - FloorY) + stored;
    }

    // ---------------- declaration and compile ----------------

    [Fact]
    public void TheTrampolineIsAStaticFrameCarryingOneDeclaredCompliantSurface()
    {
        var entry = DeclaredPhysics.Of(Trampoline);
        Assert.Equal(CuriousContraptions.Abstractions.Mechanical.KinematicMode.Static, entry.Mechanical!.KinematicMode);
        var surface = Assert.IsType<CuriousContraptions.Abstractions.Mechanical.CompliantSurfaceDescriptor>(entry.Mechanical.CompliantSurface);
        Assert.Equal((new Vector2(HalfX, HalfZ), Rest, Stroke, ArrivalDepth),
            (surface.HalfExtents, surface.RestHeight, surface.MaxStroke, surface.ArrivalDepth));
        Assert.Equal("tension", surface.Tension.Parameter);
        Assert.Equal("damping_ratio", surface.DampingRatio.Parameter);
        Assert.Equal([("tension", 120f, 1200f, 300f), ("damping_ratio", .08f, .8f, .12f)],
            entry.Metadata.Parameters.Select(p => (p.Key, p.Minimum, p.Maximum, p.Default)).ToArray());
        Assert.Empty(entry.Ports);
        Assert.Empty(entry.Events);
        Assert.False(DeclaredPhysics.IsPayload(Trampoline));
        Assert.Equal(PartAllowance.Unlimited, WorkbenchCapacityTests.FreePalette[Trampoline]);
        var defaults = WorkshopInput.Generic(Trampoline, new(1), 0, 3, 0, 0, 0, 0, 1);
        Assert.Equal((300f, .12f), (defaults.Parameters[TensionKey], defaults.Parameters[DampingKey]));
        // The bed visual is the one presentation binding: its local Y follows the committed deflection, 0.19 at rest, lowered by
        // (0.65 − 0.001)/0.65 m per metre, so at full stroke the 0.02 m slab's top face stays 1 mm above the back plate's top face (the
        // faces never coincide); the 1 mm is f32 arithmetic on declared values, compared to a micrometre.
        var sag = Assert.Single(entry.Presentation.Bindings);
        Assert.Equal(("Visual/Visual_Bed", PresentationProperty.LocalY, PresentationDriver.CommittedSurfaceDeflection, BedVisualRestY, BedVisualRestY - BedTravel),
            (sag.Node, sag.Property, sag.Driver, sag.From, sag.To));
        var fullStrokeTop = sag.From + (sag.To - sag.From) * Stroke + .01f;
        Assert.InRange(fullStrokeTop - (Rest - Stroke), .001f - 1e-6f, .001f + 1e-6f);
    }

    // The frame as designed (DESIGN.md "Passive trampoline", CAT-065 §2): the navy back plate's top face is the stroke floor under the
    // whole footprint; two side and two end rails, 0.2 m tall, have their tops on the rest plane and their inner faces on the footprint
    // edges; four gold corner posts stand on the plate and reach the rails' underside, each outside the footprint, so none intrudes into
    // the stroke volume; the feet hang below the plate. Nothing else collides between the rails' underside and the plate, so the sides
    // stay open and the compression reads from the side, and no frame solid enters the stroke volume (the open footprint between the
    // stroke floor and the rest plane; the rails' inner faces lie on the footprint edges, so the footprint itself, not the footprint plus
    // e, bounds it). The posts collide as drawn (cylinders of r 1/16 m, the engine's minimum cylinder radius), and every frame solid is
    // inelastic.
    [Fact]
    public void TheFrameIsBuiltAsDesignedWithOpenSidesAndIsInelastic()
    {
        var bed = Bed(tension: 450f, damping: .3f);
        var scene = WorkshopPhysicsCompiler.Compile(Bench(bed), new(1, 2));
        var compiled = Assert.Single(scene.CompliantSurfaces.ToArray());
        Assert.Equal(new CompliantSurfaceDeclaration(Bench(bed).Instances.Identities[bed.Id].CompliantSurface, bed.Id,
            HalfX, HalfZ, Rest, Stroke, ArrivalDepth, 450f, .3f), compiled);
        Assert.Equal(RigidMotionKind.Static, Assert.Single(scene.Bodies.ToArray(), b => b.Id == bed.Id).Motion);
        var colliders = scene.Colliders.ToArray().Where(c => c.Body == bed.Id).ToArray();
        const float slack = 1e-5f;
        static bool Near(float value, float target) => MathF.Abs(value - target) <= slack;
        static float Top(ColliderDeclaration c) => c.Pose.Translation.Y + c.HalfExtents.Y;
        static float Bottom(ColliderDeclaration c) => c.Pose.Translation.Y - c.HalfExtents.Y;
        var boxes = colliders.Where(c => c.Shape == Gpu.ColliderShapeKind.Box).ToArray();
        var cylinders = colliders.Where(c => c.Shape == Gpu.ColliderShapeKind.Cylinder).ToArray();
        var plate = Assert.Single(boxes, c => Near(Top(c), Rest - Stroke));
        Assert.True(plate.HalfExtents.X >= HalfX && plate.HalfExtents.Z >= HalfZ, "the plate covers the footprint");
        var rails = boxes.Where(c => Near(Top(c), Rest)).ToArray();
        Assert.Equal(4, rails.Length);
        Assert.All(rails, rail => Assert.True(Near(Bottom(rail), Rest - RailHeight), "a rail is 0.2 m tall"));
        Assert.Equal(2, rails.Count(r => Near(MathF.Abs(r.Pose.Translation.X) - r.HalfExtents.X, HalfX) && r.HalfExtents.Z >= HalfZ));
        Assert.Equal(2, rails.Count(r => Near(MathF.Abs(r.Pose.Translation.Z) - r.HalfExtents.Z, HalfZ) && r.HalfExtents.X >= HalfX));
        var posts = cylinders.Where(c => Near(Bottom(c), Rest - Stroke)).ToArray();
        var feet = cylinders.Where(c => !posts.Contains(c)).ToArray();
        Assert.Equal(4, posts.Length);
        Assert.All(posts, post =>
        {
            var t = post.Pose.Translation; var r = post.Radius.Value;
            Assert.True(Near(Bottom(post), Rest - Stroke) && Near(Top(post), Rest - RailHeight), "a post spans the plate to the rails");
            Assert.True(Near(r, 1f / 16), "a post is drawn and collides at r 1/16 m");
            Assert.True(MathF.Abs(t.X) - r >= HalfX && MathF.Abs(t.Z) - r >= HalfZ, "a post stands outside the footprint");
        });
        Assert.Equal(4, posts.Select(p => (MathF.Sign(p.Pose.Translation.X), MathF.Sign(p.Pose.Translation.Z))).Distinct().Count());
        Assert.All(feet, foot => Assert.True(Top(foot) <= Bottom(plate) + slack, "feet hang below the plate"));
        Assert.All(colliders.Where(c => !posts.Contains(c)), c =>
            Assert.True(Bottom(c) >= Rest - RailHeight - slack || Top(c) <= Rest - Stroke + slack, "the sides are open below the rails"));
        Assert.All(colliders, c =>
        {
            // The collider's frame-local bounds: |R|·(half extents), with a cylinder's half extents (r, half height, r).
            var q = new Quaternion(c.Pose.Rotation.X, c.Pose.Rotation.Y, c.Pose.Rotation.Z, c.Pose.Rotation.W);
            var h = c.Shape switch
            {
                Gpu.ColliderShapeKind.Box => new Vector3(c.HalfExtents.X, c.HalfExtents.Y, c.HalfExtents.Z),
                Gpu.ColliderShapeKind.Cylinder => new Vector3(c.Radius.Value, c.HalfExtents.Y, c.Radius.Value),
                _ => throw new ArgumentException($"The test has no bounds for a {c.Shape} frame collider.")
            };
            var reach = new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ }.Aggregate(Vector3.Zero, (sum, axis) =>
                sum + Vector3.Abs(Vector3.Transform(axis, q)) * Vector3.Dot(axis, h));
            var t = new Vector3(c.Pose.Translation.X, c.Pose.Translation.Y, c.Pose.Translation.Z);
            var inside = MathF.Abs(t.X) - reach.X < HalfX - slack && MathF.Abs(t.Z) - reach.Z < HalfZ - slack &&
                t.Y - reach.Y < Rest - slack && t.Y + reach.Y > Rest - Stroke + slack;
            Assert.False(inside, $"a {c.Shape} collider at {t} enters the stroke volume");
        });
        var frameMaterials = colliders.Select(c => c.Material).ToHashSet();
        Assert.All(scene.Materials.ToArray().Where(m => frameMaterials.Contains(m.Id)), m => Assert.Equal(new Restitution(0f), m.Restitution));
    }

    // CAT-065 §2: a body entering under a rail through the open side must arrive Under, so the frame's side opening (the rest plane down
    // to the rails' underside, read from the Trampoline's own declared frame colliders) is at least twice its declared arrival depth.
    [Fact]
    public void TheFramesSideOpeningIsAtLeastTwiceItsDeclaredArrivalDepth()
    {
        var bed = Bed();
        var scene = WorkshopPhysicsCompiler.Compile(Bench(bed), new(1, 2));
        var surface = Assert.Single(scene.CompliantSurfaces.ToArray());
        var rails = scene.Colliders.ToArray().Where(c => c.Body == bed.Id && c.Shape == Gpu.ColliderShapeKind.Box &&
            MathF.Abs(c.Pose.Translation.Y + c.HalfExtents.Y - surface.RestHeight) <= 1e-5f).ToArray();
        Assert.Equal(4, rails.Length);
        var opening = surface.RestHeight - rails.Max(c => c.Pose.Translation.Y - c.HalfExtents.Y);
        output.WriteLine($"side opening {opening} m, arrival depth {surface.ArrivalDepth} m");
        Assert.True(opening >= 2 * surface.ArrivalDepth, $"opening {opening} m is under twice the arrival depth {surface.ArrivalDepth} m");
    }

    [Theory]
    [InlineData(120f, .08f)]
    [InlineData(1200f, .8f)]
    public void TheBoundaryParametersCompileAndRoundTripTheWorldRecordExactly(float tension, float damping)
    {
        var bed = Bed(tension: tension, damping: damping);
        var scene = WorkshopPhysicsCompiler.Compile(Bench(bed), new(1, 2));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        Assert.Equal(scene.CompliantSurfaces[0], PhysicsGpuAbi.ReadCompliantSurface(bytes, 0));
        Assert.Equal((tension, damping), (scene.CompliantSurfaces[0].Tension, scene.CompliantSurfaces[0].DampingRatio));
    }

    // The compiled surface reaches the workers: the Construct command's compiled scene carries it through the wire codec unchanged
    // (a scene that dropped it would run every Trampoline as a bare frame), beside an Impact Lever's revolute in the same scene, so
    // neither trailing wire field displaces the other.
    [Fact]
    public void TheCompiledSceneWireCarriesTheCompliantSurface()
    {
        var construction = Bench(Bed(tension: 640f, damping: .55f, rotation: Quaternion.CreateFromAxisAngle(Vector3.UnitZ, .3f)),
            WorkshopInput.Generic(new PartId("impact_lever"), new(2), 6, 3, 0, 0, 0, 0, 1));
        var scene = CompiledSceneFixture.Compile(construction);
        Assert.Single(scene.Physics.Revolutes.ToArray());
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1), scene,
            Session: new(1, 2), Cadence: new(1), Projection: new(1)));
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(bytes).Scene!;
        Assert.Equal(scene.Physics.CompliantSurfaces.ToArray(), decoded.Physics.CompliantSurfaces.ToArray());
        Assert.Equal((640f, .55f), (decoded.Physics.CompliantSurfaces[0].Tension, decoded.Physics.CompliantSurfaces[0].DampingRatio));
        Assert.Equal(scene.Physics.Revolutes.ToArray(), decoded.Physics.Revolutes.ToArray());
    }

    public static TheoryData<string, float, WorkshopSaveRejection> RefusedParameters() => new()
    {
        { "tension", 119.9f, WorkshopSaveRejection.OutOfRange }, { "tension", 1200.1f, WorkshopSaveRejection.OutOfRange },
        { "tension", float.NaN, WorkshopSaveRejection.NonFinite }, { "tension", float.PositiveInfinity, WorkshopSaveRejection.NonFinite },
        { "tension", float.NegativeInfinity, WorkshopSaveRejection.NonFinite },
        { "damping_ratio", .079f, WorkshopSaveRejection.OutOfRange }, { "damping_ratio", .801f, WorkshopSaveRejection.OutOfRange },
        { "damping_ratio", float.NaN, WorkshopSaveRejection.NonFinite }, { "damping_ratio", float.PositiveInfinity, WorkshopSaveRejection.NonFinite },
        { "damping_ratio", float.NegativeInfinity, WorkshopSaveRejection.NonFinite },
    };

    // An out-of-range or non-finite tension or damping ratio is refused atomically at every seam: the parameter collection, placement,
    // construction admission and the save decode. Nothing is clamped or defaulted, and the admitted construction is unchanged.
    [Theory]
    [MemberData(nameof(RefusedParameters))]
    public void AnOutOfRangeOrNonFiniteParameterIsRefusedAtEverySeamAndChangesNothing(string key, float value, WorkshopSaveRejection save)
    {
        var tension = key == "tension" ? value : 300f; var damping = key == "damping_ratio" ? value : .12f;
        var refused = new InstanceParameterCollection([new(TensionKey, tension), new(DampingKey, damping)]);
        Assert.Throws<ArgumentException>(() => refused.Validate(Trampoline));
        Assert.Throws<ArgumentException>(() => Bed(tension: tension, damping: damping));
        var admitted = Bench(Bed());
        var current = Assert.IsType<WorkshopGenericInstance>(Assert.Single(admitted.Instances));
        Assert.ThrowsAny<ArgumentException>(() => admitted.WithInstance(current with { Parameters = refused }).Validate());
        Assert.Equal(300f, Assert.IsType<WorkshopGenericInstance>(Assert.Single(admitted.Instances)).Parameters[TensionKey]);
        var bytes = PayloadInstanceTests.GenericSave("trampoline", [("tension", tension), ("damping_ratio", damping)]);
        SaveFixture.Refused(save, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
    }

    // The compiled declaration refuses what the authoring seams never produce: out-of-range or non-finite lanes, a dynamic or absent
    // owner, two surfaces on one body, and an identity not allocated below the scene's next identity.
    [Fact]
    public void TheCompiledDeclarationRefusesInvalidSurfaces()
    {
        var ball = Basketball(2, 3, 2, 0);
        var valid = WorkshopPhysicsCompiler.Compile(Bench(Bed(), ball), new(1, 2));
        var surface = valid.CompliantSurfaces[0];
        foreach (var invalid in new[]
        {
            surface with { HalfX = 0f }, surface with { HalfZ = float.NaN }, surface with { RestHeight = 17f },
            surface with { MaxStroke = 0f }, surface with { Tension = 119f }, surface with { Tension = float.PositiveInfinity },
            surface with { DampingRatio = .07f }, surface with { DampingRatio = .81f }, surface with { Id = new(0) },
            surface with { ArrivalDepth = 0f }, surface with { ArrivalDepth = float.NaN },
            surface with { ArrivalDepth = surface.MaxStroke / 2f },
        }) Assert.ThrowsAny<ArgumentException>(invalid.Validate);
        PhysicsSceneDeclaration Scene(ulong next, params CompliantSurfaceDeclaration[] surfaces) => new(valid.Document, next,
            valid.Bodies.ToArray(), valid.Colliders.ToArray(), valid.Materials.ToArray(), [], [], compliantSurfaces: surfaces);
        _ = Scene(valid.NextIdentity, surface);
        Assert.Throws<ArgumentException>(() => Scene(valid.NextIdentity, surface with { Body = ball.Id }));
        Assert.Throws<ArgumentException>(() => Scene(valid.NextIdentity, surface with { Body = new(9999) }));
        // A second surface on the same frame, under a free identity (the scene's next identity is raised to make one).
        Assert.Equal("Compliant surface requires a unique static owner body.", Assert.Throws<ArgumentException>(() =>
            Scene(valid.NextIdentity + 1, surface, surface with { Id = new(valid.NextIdentity) })).Message);
        // The same scene and colliders with the surface at the scene's next identity: only the identity allocation can refuse it.
        Assert.Equal("Physical identities must be allocated once in the document.", Assert.Throws<ArgumentException>(() =>
            Scene(valid.NextIdentity, surface with { Id = new(valid.NextIdentity) })).Message);
        // An identity already allocated to the frame's first collider.
        Assert.Equal("Physical identities must be allocated once in the document.", Assert.Throws<ArgumentException>(() =>
            Scene(valid.NextIdentity, surface with { Id = new(valid.Colliders[0].Id.Value) })).Message);
    }

    private static byte[] AdmittedBedAndBall()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Bench(Bed(), Basketball(2, 0, 6, 0)), new(1, 2));
        return PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
    }

    // A malformed compliant-surface row (a body slot past the table, a dynamic frame, a zero, non-finite, negative or half-stroke arrival
    // depth, a nonzero padding word, an out-of-domain tension, a non-finite, negative or beyond-stroke deflection) is refused by the
    // record reader and by candidate admission, and the accepted source still reads its declaration.
    [Theory]
    [InlineData(PhysicsGpuAbi.CompliantSurfaceBodyOffset, 999u)]
    [InlineData(PhysicsGpuAbi.CompliantSurfaceBodyOffset, 1u)] // the Basketball's slot: a dynamic frame
    [InlineData(PhysicsGpuAbi.CompliantSurfaceArrivalDepthOffset, 0u)] // 0 m
    [InlineData(PhysicsGpuAbi.CompliantSurfaceArrivalDepthOffset, 0x7FC00000u)] // NaN
    [InlineData(PhysicsGpuAbi.CompliantSurfaceArrivalDepthOffset, 0xBF800000u)] // −1 m
    [InlineData(PhysicsGpuAbi.CompliantSurfaceArrivalDepthOffset, 0x3EA66666u)] // 0.325 m, half the 0.65 m stroke
    [InlineData(PhysicsGpuAbi.CompliantSurfaceTensionOffset, 0x42EE0000u)] // 119 N/m
    [InlineData(PhysicsGpuAbi.CompliantSurfaceDeflectionOffset, 0x7FC00000u)] // NaN
    [InlineData(PhysicsGpuAbi.CompliantSurfaceDeflectionOffset, 0xBF800000u)] // −1 m
    [InlineData(PhysicsGpuAbi.CompliantSurfaceDeflectionOffset, 0x3F800000u)] // 1 m, beyond the 0.65 m stroke
    [InlineData(PhysicsGpuAbi.CompliantSurfacePaddingOffset, 1u)]
    public void AMalformedCompliantSurfaceRecordIsRefusedAndTheAcceptedSourceIsUnchanged(int field, uint value)
    {
        var source = AdmittedBedAndBall();
        var declared = PhysicsGpuAbi.ReadCompliantSurface(source, 0);
        PhysicsGpuAbi.ValidateCandidate(source, source, new(0), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty);
        var malformed = source.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(malformed.AsSpan(PhysicsGpuAbi.Layout(malformed).CompliantSurfaceOffset(0) + field), value);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadCompliantSurface(malformed, 0));
        Assert.Throws<ArgumentException>(() =>
            PhysicsGpuAbi.ValidateCandidate(malformed, source, new(0), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty));
        Assert.Equal(declared, PhysicsGpuAbi.ReadCompliantSurface(source, 0));
    }

    // Candidate admission also refuses records that change committed meaning: an admitted declaration lane changed to another in-range
    // value (tension 300 → 500) and a deflected bed with no sample on top. Control: an OnTop sample makes the same deflection admissible.
    [Fact]
    public void CandidateAdmissionRefusesAChangedSurfaceAndAnUnloadedDeflection()
    {
        var source = AdmittedBedAndBall();
        var layout = PhysicsGpuAbi.Layout(source);
        byte[] With(int offset, uint value)
        {
            var bytes = source.ToArray();
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
            return bytes;
        }
        var surface = layout.CompliantSurfaceOffset(0);
        var engagement = layout.CompliantEngagementOffset(0, 0);
        var changed = With(surface + PhysicsGpuAbi.CompliantSurfaceTensionOffset, BitConverter.SingleToUInt32Bits(500f));
        Assert.Equal(500f, PhysicsGpuAbi.ReadCompliantSurface(changed, 0).Tension);
        var unloaded = With(surface + PhysicsGpuAbi.CompliantSurfaceDeflectionOffset, BitConverter.SingleToUInt32Bits(.1f));
        Assert.Equal(.1f, PhysicsGpuAbi.ReadCompliantSurfaceDeflection(unloaded, 0).Deflection);
        foreach (var candidate in new[] { changed, unloaded })
            Assert.Throws<ArgumentException>(() =>
                PhysicsGpuAbi.ValidateCandidate(candidate, source, new(0), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty));
        var onTop = With(engagement + PhysicsGpuAbi.CompliantSampleStateOffset + 4 * 2, (uint)CompliantSampleState.OnTop);
        BinaryPrimitives.WriteSingleLittleEndian(onTop.AsSpan(surface + PhysicsGpuAbi.CompliantSurfaceDeflectionOffset), .1f);
        PhysicsGpuAbi.ValidateCandidate(onTop, source, new(0), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty);
    }

    /// <summary>One malformed field of the first sample row (the Basketball, dynamic ordinal 0, on the only surface).</summary>
    public enum SampleRowDefect
    {
        UndefinedState, NonFiniteOffset, NegativeOffset, OffsetAboveArrivalDepth, OffsetBeyondStroke, OffsetOnOutside, OffsetOnUnder
    }

    // The per-sample engagement row (four states, then four f32 offsets) is refused by its record reader and by candidate admission when
    // a state is undefined, or an offset is non-finite, negative, 1 mm above the surface's declared 0.10 m arrival depth (an OnTop
    // offset is an entry depth, never deeper than a), beyond the 0.65 m stroke, or non-zero on a sample that is not OnTop; the accepted
    // source still reads every sample Outside at offset 0. Control: an OnTop sample at offset exactly a is admitted.
    [Theory]
    [InlineData(SampleRowDefect.UndefinedState)]
    [InlineData(SampleRowDefect.NonFiniteOffset)]
    [InlineData(SampleRowDefect.NegativeOffset)]
    [InlineData(SampleRowDefect.OffsetAboveArrivalDepth)]
    [InlineData(SampleRowDefect.OffsetBeyondStroke)]
    [InlineData(SampleRowDefect.OffsetOnOutside)]
    [InlineData(SampleRowDefect.OffsetOnUnder)]
    public void AMalformedSampleRowIsRefusedAndTheAcceptedSourceIsUnchanged(SampleRowDefect defect)
    {
        var source = AdmittedBedAndBall();
        var row = PhysicsGpuAbi.Layout(source).CompliantEngagementOffset(0, 0);
        const int sample = 1;
        var stateAt = row + PhysicsGpuAbi.CompliantSampleStateOffset + 4 * sample;
        var offsetAt = row + PhysicsGpuAbi.CompliantSampleOffsetOffset + 4 * sample;
        byte[] Row(CompliantSampleState state, float offset)
        {
            var bytes = source.ToArray();
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(stateAt), (uint)state);
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(offsetAt), offset);
            return bytes;
        }
        var malformed = defect switch
        {
            SampleRowDefect.UndefinedState => Row((CompliantSampleState)3, 0f),
            SampleRowDefect.NonFiniteOffset => Row(CompliantSampleState.OnTop, float.NaN),
            SampleRowDefect.NegativeOffset => Row(CompliantSampleState.OnTop, -.01f),
            SampleRowDefect.OffsetAboveArrivalDepth => Row(CompliantSampleState.OnTop, ArrivalDepth + .001f),
            SampleRowDefect.OffsetBeyondStroke => Row(CompliantSampleState.OnTop, MathF.BitIncrement(Stroke)),
            SampleRowDefect.OffsetOnOutside => Row(CompliantSampleState.Outside, .01f),
            SampleRowDefect.OffsetOnUnder => Row(CompliantSampleState.Under, .01f),
            _ => throw new ArgumentOutOfRangeException(nameof(defect))
        };
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadCompliantSample(malformed, 0, 0, sample));
        Assert.Throws<ArgumentException>(() =>
            PhysicsGpuAbi.ValidateCandidate(malformed, source, new(0), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty));
        Assert.All(Enumerable.Range(0, PhysicsGpuAbi.CompliantSampleCount), i =>
            Assert.Equal(new CompliantSampleRead(CompliantSampleState.Outside, 0f), PhysicsGpuAbi.ReadCompliantSample(source, 0, 0, i)));
        var admitted = Row(CompliantSampleState.OnTop, ArrivalDepth);
        Assert.Equal(new CompliantSampleRead(CompliantSampleState.OnTop, ArrivalDepth), PhysicsGpuAbi.ReadCompliantSample(admitted, 0, 0, sample));
        PhysicsGpuAbi.ValidateCandidate(admitted, source, new(0), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty);
    }

    // The committed presentation read holds one deflection row per frame in ascending frame order (the evaluator matches a binding's
    // owner to one row): a repeated frame or a descending pair is refused, as for ropes, and ordered unique frames are admitted.
    [Fact]
    public void ThePresentationReadRefusesDuplicateAndDescendingSurfaceFrames()
    {
        CompliantSurfaceRead low = new(new(3), .1f), high = new(new(5), .2f);
        Assert.Equal([low, high], new PhysicsPresentationRead([], [], surfaces: [low, high]).Surfaces.ToArray());
        Assert.Throws<ArgumentException>(() => new PhysicsPresentationRead([], [], surfaces: [low, low]));
        Assert.Throws<ArgumentException>(() => new PhysicsPresentationRead([], [], surfaces: [high, low]));
    }

    public enum SurfaceReadWireDefect { NonfiniteDeflection, NegativeDeflection, NoFrame }

    // The committed deflection crosses the response wire as one row per surface; a row with a non-finite or negative deflection, or
    // naming no frame, is refused at DecodeResponse (the committed-read rows' refusal, as for ropes) with a typed refusal, and the valid
    // response still decodes the loaded bed's deflection.
    [Theory]
    [InlineData(SurfaceReadWireDefect.NonfiniteDeflection)]
    [InlineData(SurfaceReadWireDefect.NegativeDeflection)]
    [InlineData(SurfaceReadWireDefect.NoFrame)]
    public async Task AMalformedCompliantSurfaceReadIsRefusedAtTheResponseBoundary(SurfaceReadWireDefect defect)
    {
        var bed = Bed();
        await using var world = await WorkshopIntegrationHarness.Running(Bench(bed, Basketball(2, 0, 4, 0)));
        while (Surface(world.Committed, bed.Id) == 0f)
        {
            Assert.True(world.Committed.Tick.Value < (ulong)Rate, "the ball reached the bed");
            await world.Advance(1);
        }
        var before = world.Committed;
        var valid = WorkshopWireFlatBuffers.EncodeResponse(world.Latest);
        Assert.Equal(before.Presentation.Surfaces.ToArray(), WorkshopWireFlatBuffers.DecodeResponse(valid).Read.Presentation.Surfaces.ToArray());
        var bytes = (byte[])valid.Clone();
        var buffer = new Google.FlatBuffers.ByteBuffer(bytes);
        var root = new Google.FlatBuffers.Table(4 + buffer.GetInt(4), buffer);
        var vector = root.__vector(root.__offset(4 + 29 * 2)); // WorkshopResponse.compliant_surfaces
        var row = new Google.FlatBuffers.Table(root.__indirect(vector), root.bb);
        int Slot(int field) { var offset = row.__offset(4 + 2 * field); Assert.NotEqual(0, offset); return row.bb_pos + offset; }
        switch (defect)
        {
            case SurfaceReadWireDefect.NonfiniteDeflection: BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(Slot(1)), float.NaN); break;
            case SurfaceReadWireDefect.NegativeDeflection: BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(Slot(1)), -.1f); break;
            case SurfaceReadWireDefect.NoFrame: BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(Slot(0)), 0); break;
            default: throw new ArgumentOutOfRangeException(nameof(defect));
        }
        Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(bytes));
        Assert.Equal(before.Presentation.Surfaces.ToArray(), WorkshopWireFlatBuffers.DecodeResponse(valid).Read.Presentation.Surfaces.ToArray());
    }

    // ---------------- membrane behaviour ----------------

    private sealed record Contact(int Start, int End, double PeakDeflection);

    /// <summary>The first membrane contact: the first tick with a deflection, the tick it returns to zero, and its peak.</summary>
    private static Contact FirstContact(List<WorkshopRead> reads, Func<WorkshopRead, double> deflection)
    {
        var start = -1; var peak = 0.0;
        for (var i = 0; i < reads.Count; i++)
        {
            var d = deflection(reads[i]);
            if (start < 0 && d > 0) start = i;
            if (start >= 0 && d <= 0) return new(start, i, peak);
            if (start >= 0) peak = Math.Max(peak, d);
        }
        throw new InvalidOperationException(start < 0 ? "no membrane contact" : "the contact never ended");
    }

    // AC1: a Basketball dropped from y = 7 onto the default Trampoline at (0, 3, 0) loads the membrane, stays clear of the back plate
    // (a membrane-only rebound), and is launched back up. No tick's mechanical energy, membrane store included, exceeds the initial
    // energy by more than 0.5 %, and the rebound apex stays below the drop height.
    [Fact]
    public async Task ADroppedBasketballCompressesTheMembraneAndIsLaunchedUpWithoutEnergyGain()
    {
        var ball = Basketball(2, 0, 7, 0);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Bed(), ball), 4 * Rate, includeBuilt: true);
        var contact = FirstContact(reads, read => BallDeflection(Find(read, ball.Id)));
        var e0 = BallEnergy(Find(reads[0], ball.Id), 300f);
        var worst = reads.Max(read => BallEnergy(Find(read, ball.Id), 300f) / e0);
        var separation = Find(reads[contact.End], ball.Id);
        var apex = reads.Skip(contact.End).Max(read => Origin(Find(read, ball.Id)).Y);
        output.WriteLine($"contact {contact.Start}..{contact.End}, peak {contact.PeakDeflection:F4} m, separation v {Velocity(separation)}, apex {apex:F3}, worst E/E0 {worst:F5}");
        // Undamped, the 8.2 m/s arrival would compress 0.51 m at 300 N/m; it must stay over 1 cm clear of the plate.
        Assert.InRange(contact.PeakDeflection, .3, Stroke - .01);
        Assert.True(Velocity(separation).Y > 3f, $"launched upward at {Velocity(separation).Y} m/s");
        // ζ = 0.12 returns about exp(−2ζπ) ≈ 47 % of the 3.46 m drop: the apex clears the bed by over a metre and stays below 7 m.
        Assert.InRange(apex, BedY + BallRadius + 1, 7);
        Assert.True(worst <= EnergyEnvelope, $"E/E0 reached {worst}");
    }

    // The committed read is the bed's state: on every tick of a drop and rebound its deflection is the Basketball's own pose-derived
    // depth below the rest plane (a millimetre covers the samples' offset from the very bottom, invisible on a 2.4 m bed), zero before
    // and after contact, and the bed visual sits at its 0.19 m rest height lowered by that deflection times its declared travel.
    [Fact]
    public async Task TheCommittedDeflectionFollowsTheLoadAndTheBedVisualSagsByIt()
    {
        var bed = Bed(); var ball = Basketball(2, .3f, 5, -.2f);
        await using var harness = await WorkshopIntegrationHarness.Running(Bench(bed, ball));
        Assert.Equal(BedVisualRestY, BedVisualY(harness, bed.Id));
        var deepest = 0f;
        for (var tick = 0; tick < 2 * Rate; tick++)
        {
            await harness.Advance(1);
            var committed = Surface(harness.Committed, bed.Id);
            var expected = BallDeflection(Find(harness.Committed, ball.Id), at: Frame);
            Assert.InRange(committed, expected - .001, expected + .001);
            Assert.InRange(BedVisualY(harness, bed.Id), BedVisualRestY - committed * BedTravel - 1e-6f, BedVisualRestY - committed * BedTravel + 1e-6f);
            deepest = MathF.Max(deepest, committed);
        }
        Assert.True(deepest > .1f, $"the drop loaded the bed ({deepest} m)");
    }

    // AC2: off-centre drops deflect only along the frame normal (+Y): no lateral throw and no wedging against a rail through the rebound.
    // The cases are the corners of the region where a 7 m drop of the Basketball clears the rails, |x| ≤ HalfX − r = 0.86 m and
    // |z| ≤ HalfZ − r = 0.56 m, and the ±0.8 m points on its long axis. The lateral speed stays within the lateral-speed bound over the
    // observed window (the fall, the contact and half a second of the rebound), so the drift is bounded by that speed times the window's
    // duration.
    [Theory]
    [InlineData(.8f, .5f)]
    [InlineData(-.8f, -.5f)]
    [InlineData(.8f, -.5f)]
    [InlineData(-.8f, .5f)]
    [InlineData(.8f, 0f)]
    [InlineData(-.8f, 0f)]
    public async Task AnOffCentreDropReboundsAlongTheNormalWithoutLateralThrow(float x, float z)
    {
        var ball = Basketball(2, x, 7, z);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Bed(), ball), 3 * Rate, includeBuilt: true);
        var contact = FirstContact(reads, read => BallDeflection(Find(read, ball.Id)));
        var window = reads.Take(contact.End + Rate / 2).Select(read => Find(read, ball.Id)).ToArray();
        var drift = window.Max(b => MathF.Max(MathF.Abs(Origin(b).X - x), MathF.Abs(Origin(b).Z - z)));
        var lateral = window.Max(b => MathF.Max(MathF.Abs(Velocity(b).X), MathF.Abs(Velocity(b).Z)));
        output.WriteLine($"({x}, {z}): drift {drift}, lateral {lateral}, separation vy {Velocity(Find(reads[contact.End], ball.Id)).Y}");
        Assert.InRange(drift, 0, LateralSpeed * window.Length / Rate);
        Assert.InRange(lateral, 0, LateralSpeed);
        Assert.True(Velocity(Find(reads[contact.End], ball.Id)).Y > 3f, "launched upward");
    }

    /// <summary>Whether the Basketball touches an end rail's inner top edge (z = ±HalfZ on the rest plane): its centre within its radius
    /// plus 2 mm of that edge line.</summary>
    private static bool TouchesEndRailEdge(PhysicsBodyRead ball) =>
        new Vector2(MathF.Abs(Origin(ball).Z) - HalfZ, Origin(ball).Y - BedY).Length() <= BallRadius + .002f;

    // A drop near an end rail, at (0, ±0.75), outside the clear region (|z| ≤ 0.56 m): the 0.34 m ball meets the rail's inner top edge
    // as it sinks. The rigid, inelastic rail edge deflects it with no energy gain: after the strike it moves away from the struck rail
    // faster than the lateral-speed bound, it never passes outward over the struck rail (its centre stays inside the rail's outer face),
    // it is not thrown sideways along the rail (its x speed stays within the lateral-speed bound on every tick), it never reaches the
    // back plate while over the bed, and it is never held at rest against the rail edge. While it is over the frame its speed across the
    // rails stays within half its arrival speed V:
    // the bed pushes only along its normal, so the only horizontal source is an inelastic rail edge, which turns at most V·sin θ·cos θ ≤
    // V/2 of a ball's speed across (no energy is gained, so no later contact meets it faster than V). CAT-065's clear-region note records
    // this outcome.
    [Theory]
    [InlineData(.75f)]
    [InlineData(-.75f)]
    public async Task ANearRailDropGainsNoEnergyAndIsNeverPinnedAgainstTheRail(float z)
    {
        var ball = Basketball(2, 0, 7, z);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Bed(), ball), 4 * Rate, includeBuilt: true);
        var balls = reads.Select(read => Find(read, ball.Id)).ToArray();
        var e0 = BallEnergy(balls[0], 300f);
        var worst = balls.Max(b => BallEnergy(b, 300f) / e0);
        var contact = FirstContact(reads, read => Surface(read, new(1)));
        var away = -MathF.Sign(z);
        output.WriteLine($"z {z}: contact {contact}, worst E/E0 {worst:F5}, final {Origin(balls[^1])}, " +
            $"largest speed away from the rail {balls.Skip(contact.Start).Max(b => away * Velocity(b).Z):F3}");
        Assert.True(worst <= EnergyEnvelope, $"E/E0 reached {worst}");
        // Over the bed means part of the ball overlaps the footprint: its centre within a radius of it.
        Assert.All(balls.Where(b => MathF.Abs(Origin(b).X) < HalfX + BallRadius && MathF.Abs(Origin(b).Z) < HalfZ + BallRadius),
            b => Assert.True(Origin(b).Y - BallRadius > FloorY + PlateClearance, "never reaches the back plate while over the bed"));
        Assert.DoesNotContain(balls, b => Velocity(b).Length() < RestSpeed && TouchesEndRailEdge(b));
        Assert.Contains(balls.Skip(contact.Start), b => away * Velocity(b).Z > LateralSpeed);
        Assert.All(balls, b => Assert.True(-away * Origin(b).Z < HalfZ + RailWidth, "never passes outward over the struck rail"));
        Assert.All(balls, b => Assert.InRange(Velocity(b).X, -LateralSpeed, LateralSpeed));
        var arrival = Velocity(balls[contact.Start - 1]).Length();
        Assert.All(balls.Where(b => MathF.Abs(Origin(b).Z) < HalfZ + RailWidth + BallRadius), b =>
            Assert.InRange(Velocity(b).Z, -arrival / 2, arrival / 2));
    }

    // Grazing: a Basketball reaching the bed from above with tangential speed at least 4× its normal speed, inside the clear region. The
    // bed is rotated −80° about Z, so its normal is (sin 80°, cos 80°, 0) and a ball falling straight down meets it at tan 80° ≈ 5.7
    // times more tangential than normal speed; the ball is dropped 0.6 m so that it first reaches the bed 0.5 m up-slope of its centre.
    // Through that contact it rebounds along +n without gaining normal speed, and the frictionless bed changes its tangential velocity
    // only by gravity's pull along the bed and the Basketball's declared linear drag λ over the contact's T seconds,
    // v_t(T) = v_t(0)·e^(−λT) + g_t·(1 − e^(−λT))/λ, to within the lateral-speed bound (no sideways throw). It is never wedged:
    // it leaves over the lower rail, above the rest plane, and no tick gains energy.
    [Fact]
    public async Task AGrazingBallReboundsAlongTheNormalWithoutThrowOrWedging()
    {
        var tilt = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -80f * MathF.PI / 180);
        var n = Vector3.Transform(Vector3.UnitY, tilt); var right = Vector3.Transform(Vector3.UnitX, tilt);
        var arrival = Frame + (Rest + BallRadius) * n - .5f * right;
        var start = arrival + new Vector3(0, .6f, 0);
        var bed = Bed(rotation: tilt); var ball = Basketball(2, start.X, start.Y, start.Z);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(bed, ball), 2 * Rate, includeBuilt: true);
        var contact = FirstContact(reads, read => Surface(read, bed.Id));
        var arriving = Velocity(Find(reads[contact.Start - 1], ball.Id));
        var leaving = Velocity(Find(reads[contact.End], ball.Id));
        var duration = (contact.End - (contact.Start - 1)) / (float)Rate;
        var drag = MassFixtures.Evaluate(DeclaredPhysics.Of(Ball).Mechanical!.MassFrame, []).LinearDrag;
        var decay = MathF.Exp(-drag * duration);
        var expectedTangential = Vector3.Dot(arriving, right) * decay + Vector3.Dot(new Vector3(0, -G, 0), right) * (1 - decay) / drag;
        var arrivedAt = InFrame(Origin(Find(reads[contact.Start], ball.Id)), tilt, Frame);
        var e0 = BallEnergy(Find(reads[0], ball.Id), 300f, tilt);
        var worst = reads.Max(read => BallEnergy(Find(read, ball.Id), 300f, tilt) / e0);
        output.WriteLine($"contact {contact}, arriving {arriving}, leaving {leaving}, arrived at {arrivedAt}, worst E/E0 {worst:F5}");
        Assert.True(MathF.Abs(Vector3.Dot(arriving, right)) >= 4 * MathF.Abs(Vector3.Dot(arriving, n)), $"grazing arrival {arriving}");
        Assert.InRange(arrivedAt.X, -(HalfX - BallRadius), HalfX - BallRadius);
        Assert.InRange(Vector3.Dot(leaving, n), 0f, -Vector3.Dot(arriving, n));
        Assert.InRange(Vector3.Dot(leaving, right) - expectedTangential, -LateralSpeed, LateralSpeed);
        Assert.InRange(leaving.Z - arriving.Z, -LateralSpeed, LateralSpeed);
        Assert.Contains(reads, read =>
        {
            var local = InFrame(Origin(Find(read, ball.Id)), tilt, Frame);
            return local.X > HalfX + RailWidth + BallRadius && local.Y > Rest + BallRadius;
        });
        Assert.True(worst <= EnergyEnvelope, $"E/E0 reached {worst}");
    }

    // AC3: a Trampoline tilted −30° about Z launches along its rotated normal (sin 30°, cos 30°, 0): the ball leaves with positive X
    // and Y velocity and travels towards the receiver position at x = 5.4.
    [Fact]
    public async Task ATiltedBedLaunchesAlongItsRotatedNormalTowardsTheTarget()
    {
        var ball = Basketball(2, 0, 7, 0);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Bed(rotation: Aimed), ball), 4 * Rate, includeBuilt: true);
        var contact = FirstContact(reads, read => BallDeflection(Find(read, ball.Id), Aimed));
        var leaving = Velocity(Find(reads[contact.End], ball.Id));
        var normal = Vector3.Transform(Vector3.UnitY, Aimed);
        var farthest = reads.Skip(contact.End).Max(read => Origin(Find(read, ball.Id)).X);
        output.WriteLine($"contact {contact.Start}..{contact.End}, peak {contact.PeakDeflection:F4}, leaving {leaving}, farthest x {farthest:F3}");
        Assert.True(Vector3.Dot(leaving, normal) > 2f, $"normal launch speed {Vector3.Dot(leaving, normal)}");
        Assert.True(leaving.X > 1f && leaving.Y > 0f, $"leaving velocity {leaving}");
        Assert.True(farthest > 2.5f, $"travelled to x = {farthest} towards the receiver at 5.4");
    }

    /// <summary>Runs the lesson "A gentle rebound" (CAT-065 harvest row 28): the Basketball at (0, 7, 0), the Receiver at (5.4, 1.2, 0)
    /// and one default Trampoline at <paramref name="at"/> rotated by <paramref name="rotation"/>, until the ball is captured or the
    /// stall guard ends the run. Returns whether it was captured and the bed's deepest committed deflection.</summary>
    private static async Task<(bool Captured, float Deepest)> GentleRebound(Quaternion rotation, Vector3 at)
    {
        var bed = Bed(rotation: rotation, at: at); var ball = Basketball(2, 0, 7, 0);
        var receiver = WorkshopInput.Generic(new PartId("basket"), new(3), 5.4f, 1.2f, 0, 0, 0, 0, 1);
        await using var harness = await WorkshopIntegrationHarness.Running(Bench(bed, ball, receiver));
        bool Captured() => harness.Records.Any(record => record.Kind == SimulationEventKind.Captured && record.Other == ball.Id.Value);
        var deepest = 0f;
        for (var tick = 0; tick < LessonGuard && !Captured(); tick++)
        {
            await harness.Advance(1);
            deepest = MathF.Max(deepest, Surface(harness.Committed, bed.Id));
        }
        return (Captured(), deepest);
    }

    // The lesson goal (harvest rows 28–29): the bed at (0, 3, 0) aimed −30° about Z sends the dropped Basketball into the Receiver at
    // default parameters. Controls differing in one cause: the same bed flat is struck but misses, and the aimed bed moved 2 m back
    // (z = 2) is never touched.
    [Fact]
    public async Task TheAimedBedDeliversTheBallIntoTheReceiverAndTheControlsDoNot()
    {
        var aimed = await GentleRebound(Aimed, Frame);
        var flat = await GentleRebound(Quaternion.Identity, Frame);
        var behind = await GentleRebound(Aimed, Frame + new Vector3(0, 0, 2));
        output.WriteLine($"aimed {aimed}, flat {flat}, behind {behind}");
        Assert.True(aimed.Captured, "the aimed bed delivers the ball into the receiver");
        Assert.True(aimed.Deepest > 0f, "through the membrane");
        Assert.False(flat.Captured, "the flat bed misses");
        Assert.True(flat.Deepest > 0f, "the flat bed is struck");
        Assert.False(behind.Captured);
        Assert.Equal(0f, behind.Deepest);
    }

    /// <summary>The Pipe on the aimed bed's rebound path (CAT-065 harvest row 23 and §5 integrations), at its default 3.6 m length. The
    /// lesson drop leaves the aimed bed at about (6.6, 1.6) m/s and peaks near (1.8, 3.32), so over the pipe's 3.78 m from mouth to mouth
    /// the flight bends away from any straight line by g·w²/(8·vx²) ≈ 0.38 m. The pipe is centred at the harvest's x = 3 m with its axis
    /// along the flight's chord between the mouths (−15.33° about Z) and raised by about half that bend (centre y 2.94), which keeps the
    /// ball's centre within about 0.2 m of the axis, inside the 0.65 m bore radius.</summary>
    private static readonly Vector3 PipeCentre = new(3f, 2.94f, 0);
    private static readonly Quaternion PipeAxis = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -15.33f * MathF.PI / 180);
    /// <summary>Each mouth plane of the default pipe: half its 3.6 m length plus the 0.09 m mouth reach, along its local X axis.</summary>
    private const float PipeMouth = 1.8f + .09f;
    /// <summary>The declared bore radius of the pipe: a ball whose centre stays within it passes through (touching the bore is a pass).</summary>
    private const float PipeBore = .65f;

    /// <summary>Whether the ball's centre crosses the pipe's start mouth plane and then its end mouth plane, within the bore radius of the
    /// axis at every tick between (outside it the ball has passed beside the pipe, not through it).</summary>
    private static bool ThroughPipe(List<WorkshopRead> reads, GpuBodyId ball, Vector3 centre)
    {
        var inside = false; float? previous = null;
        foreach (var read in reads)
        {
            var local = InFrame(Origin(Find(read, ball)), PipeAxis, centre);
            var radial = new Vector2(local.Y, local.Z).Length();
            if (previous is { } last && last < -PipeMouth && local.X >= -PipeMouth) inside = true;
            if (inside && local.X <= PipeMouth && radial > PipeBore) inside = false;
            if (inside && local.X > PipeMouth) return true;
            if (local.X < -PipeMouth) inside = false;
            previous = local.X;
        }
        return false;
    }

    // The drop → trampoline → pipe route (harvest row 23): the aimed bed sends the dropped Basketball through the Pipe on its rebound
    // path. Controls differing in one cause: the same bed flat bounces it straight back up and never sends it through, and the same Pipe
    // moved back to z = 2 is never threaded.
    [Fact]
    public async Task TheAimedBedSendsTheBallThroughAPipeOnItsReboundPathAndTheControlsDoNot()
    {
        async Task<bool> Route(Quaternion bed, Vector3 pipe)
        {
            var ball = Basketball(2, 0, 7, 0);
            var construction = Bench(Bed(rotation: bed), ball, Posed("pipe", 3, pipe, PipeAxis));
            var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(construction, 3 * Rate, includeBuilt: true);
            return ThroughPipe(reads, ball.Id, pipe);
        }
        Assert.True(await Route(Aimed, PipeCentre), "the aimed bed sends the ball through the pipe");
        Assert.False(await Route(Quaternion.Identity, PipeCentre), "the flat bed does not");
        Assert.False(await Route(Aimed, PipeCentre + new Vector3(0, 0, 2)), "the pipe moved to z = 2 is not threaded");
    }

    // Two Trampolines on one bench each work on their own: a Basketball dropped over each (the second from 1.5 m higher, so their loads
    // do not coincide) loads its own bed and is launched back up. On every tick each bed's Visual_Bed sits at its rest height lowered by
    // its own committed deflection, and a bed reads flat while only the other one is loaded.
    [Fact]
    public async Task TwoBedsOnOneBenchEachLaunchTheirOwnLoad()
    {
        var left = Bed(1, at: new(-2, 3, 0)); var right = Bed(2, tension: 600f, at: new(2, 3, 0));
        var a = Basketball(3, -2, 6, 0); var b = Basketball(4, 2, 7.5f, 0);
        await using var harness = await WorkshopIntegrationHarness.Running(Bench(left, right, a, b));
        var reads = new List<WorkshopRead>();
        var visuals = new List<(float Left, float Right)>();
        for (var tick = 0; tick < 2 * Rate; tick++)
        {
            reads.AddRange(await harness.Advance(1));
            visuals.Add((BedVisualY(harness, left.Id), BedVisualY(harness, right.Id)));
        }
        for (var tick = 0; tick < reads.Count; tick++)
            foreach (var (bed, visual) in new[] { (left, visuals[tick].Left), (right, visuals[tick].Right) })
                Assert.InRange(visual, BedVisualRestY - Surface(reads[tick], bed.Id) * BedTravel - 1e-6f,
                    BedVisualRestY - Surface(reads[tick], bed.Id) * BedTravel + 1e-6f);
        Assert.Contains(Enumerable.Range(0, reads.Count), t => Surface(reads[t], left.Id) > 0f && visuals[t].Right == BedVisualRestY);
        Assert.Contains(Enumerable.Range(0, reads.Count), t => Surface(reads[t], right.Id) > 0f && visuals[t].Left == BedVisualRestY);
        foreach (var (bed, ball) in new[] { (left, a), (right, b) })
        {
            var contact = FirstContact(reads, read => Surface(read, bed.Id));
            Assert.True(contact.PeakDeflection > .1, $"bed {bed.Id.Value} loaded ({contact.PeakDeflection} m)");
            Assert.True(Velocity(Find(reads[contact.End], ball.Id)).Y > 3f, $"ball {ball.Id.Value} launched");
        }
    }

    // Wall mount (harvest row 14): with the bed rotated 90° about X its normal is +Z. A Basketball thrown into it along −n by a
    // Springboard laid on its side rebounds along +n with no gain in normal speed. The bed is frictionless along its plane, so across the
    // contact the tangential velocity changes only by gravity's pull along the bed (−g over the contact ticks in Y) and not at all in X:
    // the bed itself adds no tangential speed beyond the lateral-speed bound.
    [Fact]
    public async Task AWallMountedBedReturnsAThrownBallAlongItsNormal()
    {
        var mount = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2);
        var bed = Bed(rotation: mount);
        var normal = Vector3.Transform(Vector3.UnitY, mount);
        // The ball waits 0.5 m in front of the bed face; the Springboard, its plate 2.5 cm behind the ball, throws it along −Z.
        var ballZ = Frame.Z + Rest + .5f + BallRadius;
        var aim = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2);
        var spring = PlacedParts.Springboard(new(3), 0, Frame.Y, ballZ + BallRadius + .04f, aim.X, aim.Y, aim.Z, aim.W, .2f, 1200f);
        var ball = Basketball(2, 0, Frame.Y, ballZ);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(bed, spring, ball), Rate, includeBuilt: true);
        var contact = FirstContact(reads, read => Surface(read, bed.Id));
        var arriving = Velocity(Find(reads[contact.Start - 1], ball.Id));
        var leaving = Velocity(Find(reads[contact.End], ball.Id));
        var gravity = -G * (contact.End - (contact.Start - 1)) / (float)Rate;
        output.WriteLine($"contact {contact}, arriving {arriving}, leaving {leaving}, gravity Δvy {gravity}");
        Assert.True(Vector3.Dot(arriving, normal) < -2f, $"thrown into the bed at {arriving}");
        Assert.InRange(Vector3.Dot(leaving, normal), .25f * -Vector3.Dot(arriving, normal), -Vector3.Dot(arriving, normal));
        Assert.InRange(leaving.X - arriving.X, -LateralSpeed, LateralSpeed);
        Assert.InRange(leaving.Y - arriving.Y - gravity, -LateralSpeed, LateralSpeed);
    }

    // ---------------- engagement ----------------

    /// <summary>The settle window of a load placed on the default bed: its decay envelope A·ω·e^(−ζωt) with A = m·g/k the initial
    /// amplitude and ω = √(k/m) falls to a quarter of the at-rest speed. The quarter is the margin: the at-rest bound still holds if the
    /// realised damping is a quarter below ζ.</summary>
    private static int SettleTicks(float mass, float tension, float zeta)
    {
        var omega = MathF.Sqrt(tension / mass); var amplitude = mass * G / tension;
        return (int)MathF.Ceiling(MathF.Log(amplitude * omega / (RestSpeed / 4)) / (zeta * omega) * Rate);
    }

    // A Basketball placed resting on the bed (its lowest point on the rest plane) is OnTop from the first update with an offset of the
    // micrometres it falls in one unforced substep, so the bed holds it:
    // it settles at its static compression m·g/k within the intent's settle band and never reaches the back plate. The window starts
    // once the decay envelope of the default ζ 0.12 has settled it (about 2.6 s).
    [Fact]
    public async Task ABasketballPlacedRestingOnTheBedSettlesOnIt()
    {
        var bed = Bed(); var ball = Basketball(2, .2f, BedY + BallRadius, -.1f);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(bed, ball), 4 * Rate);
        var expected = BallMass * G / 300f;
        var settled = SettleTicks(BallMass, 300f, .12f);
        Assert.InRange(settled, 1, reads.Count - Rate / 2);
        Assert.All(reads, read => Assert.True(Origin(Find(read, ball.Id)).Y - BallRadius > FloorY, "never reaches the back plate"));
        foreach (var read in reads.Skip(settled))
        {
            Assert.InRange(Surface(read, bed.Id), expected - SettleBand, expected + SettleBand);
            Assert.InRange(Velocity(Find(read, ball.Id)).Length(), 0, RestSpeed);
        }
    }

    // A Basketball rolled off a 5° Ramp whose lower edge meets the −X rail's outer top edge rolls across the rail top at rim height
    // and tips in over the rail's inner edge: its samples cross into the footprint at the rest plane (OnTop) and the bed catches it. For
    // two seconds after the bed
    // first deflects it stays held by the bed (deflected on every tick) with its lowest point above the stroke floor.
    [Fact]
    public async Task ABallRolledInOverARailTopIsCaughtByTheBed()
    {
        var bed = Bed();
        var (ramp, slope, centre) = RampTo(3, new(-(HalfX + RailWidth), BedY, 0), 5f);
        var start = centre + Vector3.Transform(new Vector3(0, RampHalfThickness + BallRadius, 0), slope);
        var ball = Basketball(2, start.X, start.Y, start.Z);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(bed, ramp, ball), 6 * Rate);
        var entry = reads.FindIndex(read => Surface(read, bed.Id) > 0f);
        Assert.True(entry > 0, "the bed caught the ball");
        Assert.Contains(reads.Take(entry), read =>
        {
            var p = Origin(Find(read, ball.Id));
            return p.X > -(HalfX + RailWidth) && p.X < -HalfX && MathF.Abs(p.Y - (BedY + BallRadius)) < LateralDrift;
        });
        Assert.True(entry + 2 * Rate <= reads.Count, "the window is observed");
        Assert.All(reads.Skip(entry).Take(2 * Rate), read =>
        {
            Assert.True(Surface(read, bed.Id) > 0f, "held by the bed");
            Assert.True(Origin(Find(read, ball.Id)).Y - BallRadius > FloorY, "above the stroke floor");
        });
    }

    /// <summary>Runs a flat load placed on the lesson's −30° bed with its downhill edge 5 cm from the downhill rail. <paramref name="reach"/>
    /// is the distance from the load's origin to that edge along the bed and <paramref name="lift"/> the height of its origin above its
    /// base. It must slide down the frictionless bed to the rail (its edge within 2 cm, the contact plus a few degrees of tilt under the
    /// uneven sag) and, for the three seconds after its first second, stay engaged (the bed deflected on every tick) with its base above
    /// the stroke floor.</summary>
    private async Task SlidesToTheDownhillRailAndStaysEngaged(Func<Vector3, Quaternion, WorkshopGenericInstance> place, Quaternion pose,
        float reach, float lift)
    {
        var bed = Bed(rotation: Aimed);
        var load = place(Frame + Vector3.Transform(new Vector3(HalfX - reach - .05f, Rest + lift, 0), Aimed), Aimed * pose);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(bed, load), 4 * Rate);
        var local = reads.Select(read => InFrame(Origin(Find(read, load.Id)), Aimed, Frame)).ToArray();
        output.WriteLine($"{load.PartId.Value}: final local {local[^1]}, final deflection {Surface(reads[^1], bed.Id)}");
        Assert.True(local[^1].X + reach > HalfX - .02f, "it slid down to the rail");
        Assert.All(reads.Skip(Rate), read => Assert.True(Surface(read, bed.Id) > 0f, "engaged"));
        Assert.All(local.Skip(Rate), p => Assert.True(p.Y - lift > Rest - Stroke, "above the stroke floor"));
    }

    // A flat load that slides down the lesson's −30° bed to the downhill rail stays there pressed flush and engaged, so a load against a
    // rail is held and never drops through to the back plate. A 4 kg Weight's samples are rim points on the diagonals, about 0.29 r
    // inside its rim, so they stay well inside the footprint even when its rim touches the rail. A Domino lying on its broad face is a
    // box whose samples are its bottom corners: pressed against the rail, two of them sit at the footprint edge plus the contact slop,
    // inside half + e, so they keep bearing the bed; a corner pushed beyond it is Outside, carried by the rail, and is OnTop again with its
    // offset at its depth when it comes back over the bed.
    [Fact]
    public async Task AFlatLoadSlidingToTheDownhillRailStaysOnTheBedAndEngaged()
    {
        const float mass = 4f;
        await SlidesToTheDownhillRailAndStaysEngaged((p, q) => WeightTests.Placed(2, p.X, p.Y, p.Z, mass, q.X, q.Y, q.Z, q.W),
            Quaternion.Identity, .04808397f * mass + .15932827f, 0f);
        // The Domino's declared box is 0.25 × 1.1 × 0.65 m; laid flat (90° about Z) its 1.1 m length runs along the bed's X axis.
        await SlidesToTheDownhillRailAndStaysEngaged(
            (p, q) => Assert.IsType<WorkshopGenericInstance>(PlacedParts.Domino(new(2), p.X, p.Y, p.Z, q.X, q.Y, q.Z, q.W)),
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2), .55f, .125f);
    }

    // A Basketball authored inside the bed (its lowest point 0.3 m below the rest plane, inside the footprint, far deeper than the
    // 0.10 m arrival depth) is Under, so the bed gives it nothing: it falls to the back plate and rests there, never rising,
    // and the bed reads undeflected. (A stateless bed would release ½k·d² ≈ 13.5 J it never received and throw it about 5 m/s upward.)
    [Fact]
    public async Task ABodyAuthoredInsideTheBedIsNeverLaunched()
    {
        var bed = Bed(); var ball = Basketball(2, .2f, BedY - .3f + BallRadius, -.1f);
        var (built, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(bed, ball), 2 * Rate);
        Assert.All(reads, read => Assert.Equal(0f, Surface(read, bed.Id)));
        Assert.All(reads, read => Assert.True(Origin(Find(read, ball.Id)).Y <= Origin(Find(built, ball.Id)).Y + RestingDrift, "never rises"));
        Assert.InRange(Origin(Find(reads[^1], ball.Id)).Y, FloorY + BallRadius - RestingDrift, FloorY + BallRadius + RestingDrift);
    }

    // The arrival depth's boundary (CAT-065 §2, a = 0.10 m): a Basketball authored inside the footprint with its lowest point 1 cm
    // shallower than a is OnTop with its offset at its entry depth, so the bed holds it without ever returning compression it did not
    // take (it never rises above its start and never reaches the back plate); 1 cm deeper than a it is Under, never pushed, and falls to
    // the plate with the bed undeflected. One centimetre clears the first unforced substep's fall (micrometres) and the samples' spread
    // about the ball's bottom (under 0.1 mm in height).
    [Theory]
    [InlineData(-.01f)]
    [InlineData(.01f)]
    public async Task ABodyAuthoredInsideTheBedIsHeldWithinTheArrivalDepthAndFallsBeyondIt(float beyond)
    {
        var bed = Bed(); var depth = ArrivalDepth + beyond;
        var ball = Basketball(2, .2f, BedY - depth + BallRadius, -.1f);
        var run = await RunWithSamples(Bench(bed, ball), 2 * Rate);
        var start = Origin(Find(run.Reads[0], ball.Id)).Y;
        Assert.All(run.Reads, read => Assert.True(Origin(Find(read, ball.Id)).Y <= start + RestingDrift, "never rises above its start"));
        if (beyond < 0f)
        {
            Assert.All(run.Samples.Skip(1), samples => Assert.All(samples, sample =>
            {
                Assert.Equal(CompliantSampleState.OnTop, sample.State);
                Assert.InRange(sample.Offset, depth - Agreement, depth + Agreement);
            }));
            Assert.All(run.Reads, read => Assert.True(Origin(Find(read, ball.Id)).Y - BallRadius > FloorY + PlateClearance, "held off the plate"));
        }
        else
        {
            Assert.All(run.Samples.Skip(1), samples => Assert.All(samples, sample =>
                Assert.Equal(new CompliantSampleRead(CompliantSampleState.Under, 0f), sample)));
            Assert.All(run.Reads, read => Assert.Equal(0f, Surface(read, bed.Id)));
            Assert.InRange(Origin(Find(run.Reads[^1], ball.Id)).Y, FloorY + BallRadius - RestingDrift, FloorY + BallRadius + RestingDrift);
        }
    }

    // A Basketball resting on a slab under an upright bed, inside the footprint and below its back plate, is never in the bed's
    // stroke volume from above: it stays at rest and the bed stays undeflected.
    [Fact]
    public async Task ABodyRestingBelowTheBackPlateStaysAtRest()
    {
        var flat = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2);
        // A Wall laid flat: its 0.25 m thickness is vertical, so its top face is at 1.125.
        var slab = WorkshopInput.Generic(new PartId("wall"), new(3), 0, 1, 0, flat.X, flat.Y, flat.Z, flat.W);
        var bed = Bed(); var ball = Basketball(2, .2f, 1.125f + BallRadius + .002f, -.1f);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(bed, slab, ball), 2 * Rate);
        var settled = Origin(Find(reads[Rate / 2], ball.Id));
        Assert.All(reads, read => Assert.Equal(0f, Surface(read, bed.Id)));
        Assert.All(reads.Skip(Rate / 2), read => Assert.InRange(Vector3.Distance(Origin(Find(read, ball.Id)), settled), 0, RestingDrift));
        Assert.InRange(Velocity(Find(reads[^1], ball.Id)).Length(), 0, RestSpeed);
    }

    // Side entry through the open side: the gap between a rail's underside and the back plate is 0.45 m, too low for a Basketball
    // (0.68 m) or a Bowling ball (0.56 m), so the body that skims in is a 0.25 kg Weight (0.27 m tall) sliding off a 25° Ramp whose lower
    // edge meets the plate's outer top edge. It passes under the −X rail into the footprint at least 0.2 m below the rest plane, deeper than
    // the 0.10 m arrival depth: every sample is Under, so the bed never pushes it or deflects, and it comes to rest standing on the back plate under the bed.
    [Fact]
    public async Task ABodyEnteringUnderARailThroughTheOpenSideIsNeverEngagedAndRestsOnTheBackPlate()
    {
        const float mass = .25f;
        var bed = Bed();
        var (ramp, slope, centre) = RampTo(3, new(-(HalfX + RailWidth), FloorY, 0), 25f);
        var start = centre + Vector3.Transform(new Vector3(.5f, RampHalfThickness, 0), slope);
        var weight = WeightTests.Placed(2, start.X, start.Y, start.Z, mass, slope.X, slope.Y, slope.Z, slope.W);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(bed, ramp, weight), 4 * Rate);
        var final = Find(reads[^1], weight.Id);
        output.WriteLine($"final {Origin(final)}, speed {Velocity(final).Length()}");
        Assert.All(reads, read => Assert.Equal(0f, Surface(read, bed.Id)));
        Assert.InRange(Origin(final).X, -HalfX, HalfX);
        Assert.InRange(Origin(final).Y, FloorY - RestingDrift, FloorY + RestingDrift);
        Assert.InRange(Velocity(final).Length(), 0, RestSpeed);
    }

    /// <summary>A 1 kg Weight (0.33 m tall) dropped 3.5 m onto the soft (120 N/m, ζ 0.08) −30° bed at <paramref name="along"/> metres
    /// downhill of its centre: its base's position in the bed frame and the bed's committed deflection on every tick.</summary>
    private static async Task<(Vector3 Base, float Deflection)[]> SoftBedDrop(float along)
    {
        var bed = Bed(tension: 120f, damping: .08f, rotation: Aimed);
        var over = Frame + Vector3.Transform(new Vector3(along, Rest, 0), Aimed);
        var weight = WeightTests.Placed(2, over.X, over.Y + 3.5f, over.Z, 1f);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(bed, weight), 2 * Rate);
        return reads.Select(read => (InFrame(Origin(Find(read, weight.Id)), Aimed, Frame), Surface(read, bed.Id))).ToArray();
    }

    // A body leaving the footprint below the rest plane (CAT-065 §2, known limit "under-rail exit"): each of its samples is Outside, with
    // no force, once it is past the footprint edge. A Basketball cannot do it in the shipped frame (it cannot pass under a rail), so the
    // body is a 1 kg Weight (0.33 m tall) dropped near the soft −30° bed's downhill rail: it sinks deeper than the rail's underside plus
    // its own height and slides downhill under the rail; once its base is out of the footprint below the plane the bed reads undeflected
    // and it is never lifted back above the rest plane.
    // Control differing only in where it lands (0.3 m further uphill): it never leaves the footprint below the rest plane, and the bed
    // relaunches it above the rest plane.
    [Fact]
    public async Task ABodyLeavingTheFootprintBelowTheRestPlaneIsDisarmed()
    {
        var height = 2 * (.03818433f + .12652539f);
        var escaped = await SoftBedDrop(.6f);
        var kept = await SoftBedDrop(.3f);
        var outside = Array.FindIndex(escaped, s => s.Base.X > HalfX && s.Base.Y < Rest);
        Assert.True(outside > 0, "the weight left the footprint below the rest plane");
        Assert.True(escaped.Take(outside).Max(s => s.Deflection) > RailHeight + height, "it sank below the rail's underside");
        Assert.All(escaped.Skip(outside), s => Assert.True(s.Deflection == 0f && s.Base.Y < Rest, "disarmed, never relaunched"));
        var deepest = Array.FindIndex(kept, s => s.Deflection == kept.Max(k => k.Deflection));
        Assert.All(kept.Where(s => s.Base.Y < Rest), s => Assert.True(s.Base.X < HalfX, "the control never leaves the footprint below the rest plane"));
        Assert.True(kept.Skip(deepest).Max(s => s.Base.Y) > Rest + .1f, "the control is relaunched above the rest plane");
    }

    // A tucked sample re-entering the footprint below the plane (CAT-065 §2, invariant 3), on an unframed surface run directly by the
    // engine (no frame to catch anything): rest plane at world y 0.2, 2.4 × 1.8 m, 120 N/m and ζ 0.08 (the softest, least damped bed, so
    // the 0.4 kg tile sinks about m·g/k ≈ 3.3 cm). A Domino-sized tile lying flat enters from 5 mm above the plane near the +X edge, its
    // outer end 0.25 m inside it, moving down at 0.3 m/s and outwards at 1 m/s on the frictionless bed: it bears the bed and sinks. Its
    // outer corners then slide past the edge plus e below the plane (Outside, offset 0) onto an elastic shelf 3.5 cm below the plane (about
    // the tile's sag), it strikes an elastic wall 0.3 m beyond the edge and comes back, and the corners re-enter the footprint below the
    // plane. Every re-entering corner obeys the law: no deeper than the arrival depth it is OnTop with its offset at its entry depth (read
    // at the end of the tick it entered in, that is between its depth then, less the most it could have sunk within the tick at its normal
    // speed, and its depth then), deeper it is Under; the shelf makes at least one re-entry shallow. So the bed pushes a re-entered corner only by compression it
    // takes from then on. The tile and the wall are elastic (restitution 1, no friction), so the tile's energy, with the bed store counted on its bearing
    // samples from their committed offsets, is conserved apart from the bed's damping: it never exceeds 1.005 × its start. (Without
    // offsets the re-entering corners would be pushed back with ½(k/4)·d² nobody paid for.)
    [Fact]
    public void ABodyTuckedBelowThePlaneOutsideTheFootprintSlidesBackInWithoutGainingEnergy()
    {
        const float tension = 120f, inside = .25f, wallGap = .3f;
        var elastic = new LawMaterial(1f, 0f, 0f, 0f);
        var scene = new LawScene(bench: false);
        var frame = scene.StaticBox(new(0, -5, 0), new(.1f, .1f, .1f));
        scene.CompliantSurface(frame, HalfX, HalfZ, 5f + Rest, Stroke, ArrivalDepth, tension, .08f);
        scene.StaticBox(new(HalfX + wallGap + .1f, -1.2f, 0), new(.1f, 1.8f, 1f), material: elastic);
        const float shelfDepth = .035f, shelfInner = HalfX + Edge + .002f;
        scene.StaticBox(new((shelfInner + HalfX + wallGap) / 2, Rest - shelfDepth - .1f, 0), new((HalfX + wallGap - shelfInner) / 2, .1f, 1f),
            material: elastic);
        var flat = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2);
        var half = LawTile.HalfExtents; // 0.125 × 0.55 × 0.325: laid flat, 0.25 m thick and 1.1 m along X
        var tile = scene.Box(new(HalfX - inside - half.Y, Rest + half.X + .005f, 0), half, LawTile.Mass, elastic, flat,
            velocity: new(1f, -.3f, 0));
        var world = scene.Admit();
        var mass = (double)LawTile.Mass;
        var moments = new Vector3(half.Y * half.Y + half.Z * half.Z, half.X * half.X + half.Z * half.Z, half.X * half.X + half.Y * half.Y)
            * LawTile.Mass / 3f;
        Vector3[] Corners(LawBody body) => BoxSamples(body, half);
        double Energy(LawBody body, CompliantSampleRead[] committed)
        {
            var w = Vector3.Transform(body.Angular, Quaternion.Inverse(body.Rotation));
            var corners = Corners(body);
            var stored = Enumerable.Range(0, corners.Length)
                .Where(i => committed[i].State == CompliantSampleState.OnTop && MathF.Abs(corners[i].X) <= HalfX + Edge &&
                    MathF.Abs(corners[i].Z) <= HalfZ + Edge && corners[i].Y < Rest)
                .Sum(i => .5 * tension / 4 * Math.Pow(Rest - corners[i].Y - committed[i].Offset, 2));
            return .5 * mass * Vector3.Dot(body.Velocity, body.Velocity) + .5 * (moments.X * w.X * w.X + moments.Y * w.Y * w.Y + moments.Z * w.Z * w.Z) +
                mass * G * (body.Position.Y - (Rest - Stroke)) + stored;
        }
        CompliantSampleRead[] outside = [.. Enumerable.Repeat(new CompliantSampleRead(CompliantSampleState.Outside, 0f), 4)];
        var e0 = Energy(world.Initial(tile), outside);
        var states = new List<(LawBody Body, CompliantSampleRead[] Samples)>();
        world.Step(Rate, (_, state) => states.Add((state[tile],
            [.. Enumerable.Range(0, PhysicsGpuAbi.CompliantSampleCount).Select(i => PhysicsGpuAbi.ReadCompliantSample(state.World, 0, 0, i))])));
        var engaged = states.FindIndex(s => Enumerable.Range(0, 4).Any(i =>
            s.Samples[i].State == CompliantSampleState.OnTop && Corners(s.Body)[i].Y < Rest));
        var tucked = states.FindIndex(s => Corners(s.Body).Any(c => c.X > HalfX + Edge && c.Y < Rest));
        var reentries = Enumerable.Range(Math.Max(1, tucked + 1), Math.Max(0, states.Count - tucked - 1))
            .SelectMany(tick => Enumerable.Range(0, 4).Select(i => (Tick: tick, Sample: i)))
            .Where(r => states[r.Tick - 1].Samples[r.Sample].State == CompliantSampleState.Outside &&
                states[r.Tick].Samples[r.Sample].State != CompliantSampleState.Outside && Corners(states[r.Tick].Body)[r.Sample].Y < Rest)
            .ToArray();
        var worst = states.Max(s => Energy(s.Body, s.Samples) / e0);
        output.WriteLine($"engaged {engaged}, tucked {tucked}, re-entries {string.Join(", ", reentries.Select(r => $"{r.Tick}/{r.Sample} at " +
            $"{Rest - Corners(states[r.Tick].Body)[r.Sample].Y:F4} {states[r.Tick].Samples[r.Sample]}"))}, worst E/E0 {worst:F5}");
        Assert.InRange(engaged, 0, tucked - 1);
        Assert.NotEmpty(reentries);
        double Depth((int Tick, int Sample) r) => Rest - Corners(states[r.Tick].Body)[r.Sample].Y;
        float SinkPerTick((int Tick, int Sample) r)
        {
            var body = states[r.Tick].Body;
            var corner = Corners(body)[r.Sample];
            return MathF.Abs((body.Velocity + Vector3.Cross(body.Angular, corner - body.Position)).Y) / Rate;
        }
        Assert.Contains(reentries, r => Depth(r) <= ArrivalDepth - Agreement);
        Assert.All(reentries, r =>
        {
            var committed = states[r.Tick].Samples[r.Sample];
            if (Depth(r) <= ArrivalDepth - Agreement)
            {
                Assert.Equal(CompliantSampleState.OnTop, committed.State);
                Assert.InRange(committed.Offset, Depth(r) - SinkPerTick(r) - Agreement, Depth(r) + Agreement);
            }
            else if (Depth(r) >= ArrivalDepth + Agreement) Assert.Equal(new CompliantSampleRead(CompliantSampleState.Under, 0f), committed);
        });
        Assert.True(worst <= EnergyEnvelope, $"E/E0 reached {worst}");
    }

    /// <summary>A box's four support samples on an unframed upright surface (the law's diagonal directions, −y ± ε(x ± z)): the corner
    /// farthest along each, from the box's pose and half extents.</summary>
    private static Vector3[] BoxSamples(LawBody body, Vector3 half) => [.. new[] { (1, 1), (-1, 1), (1, -1), (-1, -1) }.Select(s =>
    {
        var d = Vector3.Normalize(-Vector3.UnitY + SampleSpread * new Vector3(s.Item1, 0, s.Item2));
        var corner = body.Position;
        foreach (var (axis, h) in new[] { (Vector3.UnitX, half.X), (Vector3.UnitY, half.Y), (Vector3.UnitZ, half.Z) })
        {
            var u = Vector3.Transform(axis, body.Rotation);
            corner += (Vector3.Dot(d, u) >= 0f ? h : -h) * u;
        }
        return corner;
    })];

    /// <summary>An unframed upright surface run directly by the engine: rest plane at world y 0.2 over the 2.4 × 1.8 m footprint centred
    /// on the origin, with <paramref name="stroke"/> and declared arrival depth <paramref name="arrivalDepth"/>, at the default 300 N/m and
    /// ζ 0.12. Nothing else is in the scene.</summary>
    private static LawScene UnframedSurface(float stroke, float arrivalDepth)
    {
        var scene = new LawScene(bench: false);
        var frame = scene.StaticBox(new(0, -5, 0), new(.1f, .1f, .1f));
        scene.CompliantSurface(frame, HalfX, HalfZ, 5f + Rest, stroke, arrivalDepth, 300f, .12f);
        return scene;
    }

    /// <summary>One tick of an unframed run: the only dynamic body's pose, its four committed samples and the bed's committed deflection.</summary>
    private sealed record LawTick(LawBody Body, CompliantSampleRead[] Samples, float Deflection);

    private static List<LawTick> StepUnframed(LawScene scene, GpuBodyId body, int ticks)
    {
        var world = scene.Admit();
        var run = new List<LawTick>();
        world.Step(ticks, (_, state) => run.Add(new(state[body],
            [.. Enumerable.Range(0, PhysicsGpuAbi.CompliantSampleCount).Select(i => PhysicsGpuAbi.ReadCompliantSample(state.World, 0, 0, i))],
            PhysicsGpuAbi.ReadCompliantSurfaceDeflection(state.World, 0).Deflection)));
        return run;
    }

    private static readonly CompliantSampleRead OutsideSample = new(CompliantSampleState.Outside, 0f);
    private static readonly CompliantSampleRead UnderSample = new(CompliantSampleState.Under, 0f);
    private static readonly CompliantSampleRead PlaneSample = new(CompliantSampleState.OnTop, 0f);

    // Classification on the unclamped depth (CAT-065 §2): a sample entering the footprint is compared with the arrival depth by its
    // depth below the rest plane, not by its stroke-clamped depth, so a deep entry is Under whatever the stroke. An unframed surface with
    // a 0.08 m stroke and a = 0.03 m; a 0.2 m, 1 kg cube without gravity slides in through the −X edge at 2 m/s with its base 0.2 m below
    // the plane (below the stroke floor as well). On every tick each sample is Outside or Under at offset 0, every sample is Under once
    // the cube is inside the footprint, the cube is never pushed (nothing else acts on it, so its normal velocity stays exactly zero) and
    // the bed reads undeflected. Control differing only in entry depth (0.02 m, under a): inside the footprint every sample is OnTop with
    // its offset at that entry depth.
    [Theory]
    [InlineData(.2f)]
    [InlineData(.02f)]
    public void AnEntryDeeperThanTheArrivalDepthIsUnderWhateverTheStroke(float entryDepth)
    {
        var scene = UnframedSurface(stroke: .08f, arrivalDepth: .03f);
        var half = new Vector3(.1f);
        var cube = scene.Box(new(-HalfX - half.X - .1f, Rest - entryDepth + half.Y, 0), half, 1f, new LawMaterial(0f, 0f, 0f, 0f),
            velocity: new(2f, 0, 0), gravity: 0f);
        var run = StepUnframed(scene, cube, Rate);
        var inside = run.Where(t => BoxSamples(t.Body, half).All(c => MathF.Abs(c.X) <= HalfX && MathF.Abs(c.Z) <= HalfZ)).ToArray();
        Assert.NotEmpty(inside);
        if (entryDepth > .03f)
        {
            Assert.All(run, t => Assert.All(t.Samples, sample => Assert.True(sample == OutsideSample || sample == UnderSample, $"{sample}")));
            Assert.All(inside, t => Assert.All(t.Samples, sample => Assert.Equal(UnderSample, sample)));
            Assert.All(run, t => Assert.Equal(0f, t.Body.Velocity.Y));
            Assert.All(run, t => Assert.Equal(0f, t.Deflection));
        }
        else
            Assert.All(inside, t => Assert.All(t.Samples, sample =>
            {
                Assert.Equal(CompliantSampleState.OnTop, sample.State);
                Assert.InRange(sample.Offset, entryDepth - Agreement, entryDepth + Agreement);
            }));
    }

    /// <summary>A 0.4 × 0.2 × 0.4 m, 1 kg box wholly over the unframed Trampoline-sized surface (0.65 m stroke, a = 0.10 m), its base
    /// <paramref name="depth"/> below the plane, launched straight up at <paramref name="speed"/>, observed for 2 s; and the first tick on
    /// which all four of its samples are above the plane (by the 1 mm agreement margin, so the engine's geometry agrees that they have
    /// reached it).</summary>
    private static (List<LawTick> Run, int Risen) RisingThroughThePlane(float depth, float speed)
    {
        var scene = UnframedSurface(Stroke, ArrivalDepth);
        var half = new Vector3(.2f, .1f, .2f);
        var box = scene.Box(new(.3f, Rest - depth + half.Y, -.2f), half, 1f, new LawMaterial(0f, 0f, 0f, 0f), velocity: new(0, speed, 0));
        var run = StepUnframed(scene, box, 2 * Rate);
        return (run, run.FindIndex(t => BoxSamples(t.Body, half).All(c => c.Y >= Rest + Agreement)));
    }

    /// <summary>After <paramref name="risen"/>, the bed bears the box again from the plane: it lands (the bed deflects), at its deepest
    /// tick every sample is OnTop at offset 0 (the compression is measured from the plane), and the bed pushes it back up.</summary>
    private static void BorneAgainFromThePlane(List<LawTick> run, int risen)
    {
        var landed = run.FindIndex(risen, t => t.Deflection > 0f);
        Assert.True(landed > risen, "the box lands on the bed after rising through the plane");
        var deepest = run.Skip(landed).MaxBy(t => t.Deflection)!;
        Assert.All(deepest.Samples, sample => Assert.Equal(PlaneSample, sample));
        Assert.Contains(run.Skip(landed), t => t.Body.Velocity.Y > 0f && t.Deflection > 0f);
    }

    // Under → OnTop (CAT-065 §2): a sample that entered deeper than a is Under, and once it rises to the plane over the footprint it is
    // OnTop at offset 0, so the bed bears it again from the plane. The box starts 0.2 m below the plane (Under on the first tick) and rises
    // at 3 m/s; while it is below the plane every sample deeper than 1 mm stays Under (never pushed); on the first tick all four samples
    // are at or above the plane they are (OnTop, 0); it falls back, and the bed deflects and pushes it up.
    [Fact]
    public void ASampleUnderTheBedThatRisesToThePlaneIsBorneAgainFromThePlane()
    {
        var (run, risen) = RisingThroughThePlane(.2f, 3f);
        Assert.All(run[0].Samples, sample => Assert.Equal(UnderSample, sample));
        Assert.True(risen > 0, "the box rises through the plane");
        Assert.All(run.Take(risen), t => Assert.All(t.Samples.Zip(BoxSamples(t.Body, new(.2f, .1f, .2f))).Where(s => s.Second.Y < Rest - Agreement),
            s => Assert.Equal(UnderSample, s.First)));
        Assert.All(run[risen].Samples, sample => Assert.Equal(PlaneSample, sample));
        BorneAgainFromThePlane(run, risen);
    }

    // Offset shrink (CAT-065 §2): an OnTop sample's offset only shrinks, oᵢ = min(oᵢ, dᵢ), so a load that entered with an offset and
    // rises to the plane bears from the plane afterwards. The box enters 3 cm below the plane rising at 1 m/s: on the first tick every
    // sample is OnTop with an offset over 1 cm (its entry depth, less the rise within the tick); on the first tick all four samples are
    // at or above the plane they are (OnTop, 0); its next landing deflects from the plane and the bed pushes it up.
    [Fact]
    public void AnOnTopOffsetShrinksToZeroWhenTheLoadRisesToThePlane()
    {
        var (run, risen) = RisingThroughThePlane(.03f, 1f);
        Assert.All(run[0].Samples, sample =>
        {
            Assert.Equal(CompliantSampleState.OnTop, sample.State);
            Assert.InRange(sample.Offset, .01f, .03f);
        });
        Assert.True(risen > 0, "the box rises through the plane");
        Assert.All(run[risen].Samples, sample => Assert.Equal(PlaneSample, sample));
        BorneAgainFromThePlane(run, risen);
    }

    /// <summary>The inverted bed's rest plane: 1 m above the Basketball's top when it sits on a fully preloaded Springboard at the
    /// origin, so the launched ball (about 2 m of rise) enters it from below.</summary>
    private const float InvertedFrameY = .015f + 2 * BallRadius + 1f + Rest;

    // The inverted frame (180° about Z) acts along its downward normal. A Basketball launched upward from a preloaded Springboard
    // enters the downward-facing bed from outside (it is never authored in it), is stopped within the stroke and driven back down,
    // faster than gravity alone would turn it: it turns below the apex it reaches with no bed, and the bed reads deflected.
    [Fact]
    public async Task AnInvertedBedReboundsABallEnteringFromBelowAlongItsDownwardNormal()
    {
        var spring = PlacedParts.Springboard(new(2), 0, 0, 0, 0, 0, 0, 1, .2f, 1200f);
        var ball = Basketball(3, 0, .015f + BallRadius + .002f, 0);
        var inverted = Bed(rotation: Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI), at: new(0, InvertedFrameY, 0));
        var (_, free, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(spring, ball), 2 * Rate);
        var (_, under, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(spring, ball, inverted), 2 * Rate);
        var freeApex = free.Max(read => Origin(Find(read, ball.Id)).Y);
        var contact = FirstContact(under, read => Surface(read, inverted.Id));
        var turned = under.Max(read => Origin(Find(read, ball.Id)).Y);
        var leaving = Velocity(Find(under[contact.End], ball.Id));
        output.WriteLine($"free apex {freeApex}, contact {contact}, turned at {turned}, leaving {leaving}");
        Assert.True(freeApex + BallRadius > InvertedFrameY - Rest + .2f, "without the bed the ball would rise well into its stroke");
        Assert.True(contact.PeakDeflection > .05, $"the bed was loaded ({contact.PeakDeflection} m)");
        Assert.True(turned < freeApex - .1f, $"turned at {turned}, free apex {freeApex}");
        Assert.True(leaving.Y < 0f, $"driven back down at {leaving}");
        Assert.InRange(MathF.Abs(leaving.X) + MathF.Abs(leaving.Z), 0, LateralSpeed);
    }

    // From the back (harvest row 16): a Basketball dropped onto the inverted frame's back plate, now uppermost, meets the inelastic
    // plate and rests on it. It is on the plate side of the bed, never above its rest plane, so the membrane never launches it.
    [Fact]
    public async Task ABallDroppedOntoAnInvertedFramesBackPlateRestsThereUnlaunched()
    {
        var inverted = Bed(rotation: Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI));
        var ball = Basketball(2, 0, 5, 0);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(inverted, ball), 2 * Rate);
        var plateTop = Frame.Y + Stroke - Rest + .12f;
        var final = Find(reads[^1], ball.Id);
        output.WriteLine($"inverted rest: final {Origin(final)}, max vy {reads.Max(r => Velocity(Find(r, ball.Id)).Y)}");
        Assert.All(reads, read => Assert.Equal(0f, Surface(read, inverted.Id)));
        Assert.InRange(Origin(final).Y, plateTop + BallRadius - RestingDrift, plateTop + BallRadius + RestingDrift);
        Assert.InRange(reads.Max(read => Velocity(Find(read, ball.Id)).Y), float.NegativeInfinity, NoLaunchSpeed);
    }

    // ---------------- the invariant sweep ----------------

    /// <summary>The swept payloads: the Basketball (a sphere), a 1 kg Weight (a cylinder), a Domino lying flat (a box), and the Weight at its
    /// declared mass extremes, 0.25 kg and 8 kg (1.09 m across).</summary>
    public enum SweepShape { Basketball, Weight, Domino, LightWeight, HeavyWeight }
    public enum SweepEntry
    {
        CentreDrop, StraddlingRail, SlidOverRail, Tumbling, AimedBed,
        StraddleAtLessonSpeed, StraddleAtFastestArrival, RailFirstTip, FastSlide, TiltedOnRailFast, TiltedOnRailSlow
    }

    /// <summary>The entries that cross a rail at speed or tip in (review loop 4), swept at the shipped 300 N/m and the stiffest 1200 N/m.</summary>
    private static readonly SweepEntry[] ArrivalEntries =
    [
        SweepEntry.StraddleAtLessonSpeed, SweepEntry.StraddleAtFastestArrival, SweepEntry.RailFirstTip, SweepEntry.FastSlide,
        SweepEntry.TiltedOnRailFast, SweepEntry.TiltedOnRailSlow
    ];

    public static TheoryData<SweepShape, SweepEntry, float> InvariantCases()
    {
        var cases = new TheoryData<SweepShape, SweepEntry, float>();
        foreach (var shape in new[] { SweepShape.Basketball, SweepShape.Weight, SweepShape.Domino })
        {
            foreach (var entry in new[] { SweepEntry.CentreDrop, SweepEntry.StraddlingRail, SweepEntry.SlidOverRail, SweepEntry.Tumbling, SweepEntry.AimedBed })
                cases.Add(shape, entry, 300f);
            foreach (var entry in ArrivalEntries)
            {
                cases.Add(shape, entry, 300f);
                cases.Add(shape, entry, StiffTension);
            }
        }
        foreach (var shape in new[] { SweepShape.LightWeight, SweepShape.HeavyWeight })
            foreach (var entry in new[] { SweepEntry.StraddlingRail, SweepEntry.StraddleAtLessonSpeed, SweepEntry.StraddleAtFastestArrival,
                SweepEntry.RailFirstTip, SweepEntry.TiltedOnRailFast, SweepEntry.TiltedOnRailSlow })
                cases.Add(shape, entry, 300f);
        return cases;
    }

    private static IWorkshopInstance SweepPart(SweepShape shape, ulong id, Vector3 at, Quaternion q) => shape switch
    {
        SweepShape.Basketball => Posed("ball", id, at, q),
        SweepShape.Weight => WeightTests.Placed(id, at.X, at.Y, at.Z, 1f, q.X, q.Y, q.Z, q.W),
        SweepShape.LightWeight => WeightTests.Placed(id, at.X, at.Y, at.Z, .25f, q.X, q.Y, q.Z, q.W),
        SweepShape.HeavyWeight => WeightTests.Placed(id, at.X, at.Y, at.Z, 8f, q.X, q.Y, q.Z, q.W),
        SweepShape.Domino => PlacedParts.Domino(new(id), at.X, at.Y, at.Z, q.X, q.Y, q.Z, q.W),
        _ => throw new ArgumentOutOfRangeException(nameof(shape))
    };

    /// <summary>Each shape's resting pose: the Basketball and the Weight upright, the Domino lying on its broad face (90° about Z).</summary>
    private static Quaternion Lying(SweepShape shape) =>
        shape == SweepShape.Domino ? Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2) : Quaternion.Identity;

    /// <summary>The origin that puts the payload, rotated by <paramref name="q"/>, with its lowest point along −<paramref name="up"/>
    /// at <paramref name="gap"/> above the plane through <paramref name="surface"/>, from its declared geometry.</summary>
    private static Vector3 Resting(SweepShape shape, Quaternion q, Vector3 surface, Vector3 up, float gap)
    {
        var body = Declared(Bench(SweepPart(shape, 2, new(0, 10, 0), q)), new(2));
        return surface + (gap - Vector3.Dot(Support(body, Vector3.Zero, q, -up), up)) * up;
    }

    /// <summary>The lesson's arrival speed: the Basketball dropped from y = 7 meets the default bed's rest plane after its lowest point
    /// falls 7 − 0.34 − 3.2 = 3.46 m, √(2g·3.46) ≈ 8.2 m/s (CAT-065 harvest row 28).</summary>
    private const float LessonArrival = 8.2f;
    /// <summary>The intent matrix's fastest arrival: the bottoming-out row's 12 m/s.</summary>
    private const float FastestArrival = 12f;
    /// <summary>The stiffest admitted bed, 1200 N/m, the second tension of the arrival entries beside the shipped 300 N/m.</summary>
    private const float StiffTension = 1200f;
    /// <summary>The rail-first entry's tilt: 30° about Z lowering the outboard (+X) side, so a Weight's rim or a Domino's end lands on the
    /// rail first while its centre of mass, 5 cm inside the rail's inner edge, tips it into the bed.</summary>
    private const float RailFirstTilt = 30f;
    /// <summary>The fast slide: a 70° Ramp whose lower top edge meets the −X rail's outer top edge, the payload released 2.4 m up the
    /// slope from that edge (a lying Domino's upper end 5 cm short of the ramp's top). Estimated before running: the joint turns the
    /// motion by 70°, so the normal speed into the rail top is about v·sin 70°, about 5–6 m/s for the three shapes (rolling, or sliding
    /// on the Domino's 0.42 combined friction), well above the lesson drop's rail-top arrival and under the matrix's 12 m/s.</summary>
    private const float FastSlideDegrees = 70f, FastSlideRelease = 2.4f;
    /// <summary>The tilted-on-rail entries: tilted 20° about Z lowering the outboard (+X) side, centre of mass 5 cm inside the rail's inner
    /// edge, dropped so that they arrive at the lesson's 8.2 m/s, or slowly from 0.5 m.</summary>
    private const float RailTilt = 20f, SlowDrop = .5f;
    /// <summary>The Trampoline's declared arrival depth a (CAT-065 §2; the descriptor fact pins the package to it): a sample that crosses
    /// into the footprint no deeper than 0.10 m below the rest plane is OnTop; deeper, it is Under.</summary>
    private const float ArrivalDepth = .1f;

    /// <summary>The origin that puts the payload, rotated by <paramref name="q"/>, with its lowest point at height <paramref name="lowest"/>
    /// and its centre of mass at <paramref name="x"/>, z −0.1, from its declared geometry.</summary>
    private static Vector3 CentredAt(SweepShape shape, Quaternion q, float x, float lowest)
    {
        var body = Declared(Bench(SweepPart(shape, 2, new(0, 10, 0), q)), new(2));
        var origin = Resting(shape, q, new(0, lowest, -.1f), Vector3.UnitY, 0f);
        return origin + new Vector3(x - (origin + Vector3.Transform(body.Centre, q)).X, 0, 0);
    }

    // The bed's invariants (CAT-065 §2), swept over payload × entry × tension through the shipped package. On every tick, every sample
    // clearly over the bed and below its rest plane is committed OnTop (each of these bodies reaches the bed on top), and no tick gains
    // energy (E(t) ≤ 1.005·E0, the bed store on the bearing samples counted from their committed offsets). The payload never reaches the
    // back plate while any part of it is over the footprint wherever the stroke can stop it: where the bearing samples' capacity bound
    // (n/4)·½k·(S − 1 cm − a)² covers its whole fall to the stroke floor, m·g·(h₀ − floor), damping and the rail ignored; elsewhere
    // reaching the plate is the stroke's designed bottoming, and only the state and energy invariants apply. Entries: a centre drop from
    // 1 m; a drop from 0.5 m straddling the +X side rail (centred 5 cm inside its inner edge); a slide in over the −X rail top from a
    // 35° Ramp at rim height (steep enough for the Domino, whose combined friction with the Ramp, √(0.6 × 0.3) ≈ 0.42, holds it below
    // 23°), released 1.5 m up the slope so the 1.1 m Domino crosses the 0.16 m rail top instead of coming to rest across it; a tumble,
    // dropped from 0.3 m tilted 40° about a horizontal diagonal; a 1 m drop onto the lesson's −30° bed. The arrival entries (review
    // loop 4), at 300 and 1200 N/m: the straddle arriving at the lesson's 8.2 m/s and at the matrix's 12 m/s; a rail-first landing at
    // 8.2 m/s tilted 30° (centre of mass 5 cm inside the rail's inner edge; the sphere has no tilt, so it meets the rail's inner top
    // edge a few millimetres before the bed); a fast slide off a 70° Ramp into the rail-top joint; and the same placement tilted 20°,
    // arriving at 8.2 m/s or slowly from 0.5 m. The 0.25 kg and 8 kg Weights run the straddle and tip-in entries. n is 4 for the
    // drops onto the open bed and 2 for every entry across a rail. Every case must reach the bed (the bed reads deflected at some
    // tick), or it proves nothing.
    [Theory]
    [MemberData(nameof(InvariantCases))]
    public async Task EveryShapeReachingTheBedFromAboveIsHeldWhileOverItAndGainsNoEnergy(SweepShape shape, SweepEntry entry, float tension)
    {
        var (construction, bed, frame, bearing) = SweepCase(shape, entry, tension);
        await HeldWhileOverTheBedWithoutGain($"{shape} {entry} {tension}", construction, bed, frame, tension, bearing);
    }

    /// <summary>One sweep case's construction (the payload is body 2), its bed, the bed's rotation and the number of the payload's samples
    /// expected to bear the bed (4 on the open bed, 2 across a rail).</summary>
    private static (WorkshopConstruction Construction, WorkshopGenericInstance Bed, Quaternion Frame, int Bearing) SweepCase(
        SweepShape shape, SweepEntry entry, float tension)
    {
        var frame = entry == SweepEntry.AimedBed ? Aimed : Quaternion.Identity;
        var bed = Bed(rotation: frame, tension: tension);
        var bearing = entry is SweepEntry.CentreDrop or SweepEntry.Tumbling or SweepEntry.AimedBed ? 4 : 2;
        var parts = new List<IWorkshopInstance> { bed };
        Vector3 at;
        Quaternion q;
        switch (entry)
        {
            case SweepEntry.CentreDrop:
                q = Lying(shape); at = Resting(shape, q, new(.2f, BedY + 1f, -.1f), Vector3.UnitY, 0f); break;
            case SweepEntry.StraddlingRail:
                q = Lying(shape); at = Resting(shape, q, new(HalfX - .05f, BedY + .5f, -.1f), Vector3.UnitY, 0f); break;
            case SweepEntry.StraddleAtLessonSpeed or SweepEntry.StraddleAtFastestArrival:
                var speed = entry == SweepEntry.StraddleAtLessonSpeed ? LessonArrival : FastestArrival;
                q = Lying(shape); at = Resting(shape, q, new(HalfX - .05f, BedY + speed * speed / (2 * G), -.1f), Vector3.UnitY, 0f); break;
            case SweepEntry.RailFirstTip:
                q = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -RailFirstTilt * MathF.PI / 180) * Lying(shape);
                at = CentredAt(shape, q, HalfX - .05f, BedY + LessonArrival * LessonArrival / (2 * G)); break;
            case SweepEntry.TiltedOnRailFast or SweepEntry.TiltedOnRailSlow:
                q = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -RailTilt * MathF.PI / 180) * Lying(shape);
                at = CentredAt(shape, q, HalfX - .05f,
                    BedY + (entry == SweepEntry.TiltedOnRailFast ? LessonArrival * LessonArrival / (2 * G) : SlowDrop)); break;
            case SweepEntry.SlidOverRail or SweepEntry.FastSlide:
                var (degrees, release) = entry == SweepEntry.SlidOverRail ? (35f, 1.5f) : (FastSlideDegrees, FastSlideRelease);
                var (ramp, slope, centre) = RampTo(3, new(-(HalfX + RailWidth), BedY, 0), degrees);
                parts.Add(ramp);
                q = slope * Lying(shape);
                at = Resting(shape, q, centre + Vector3.Transform(new Vector3(RampHalfLength - release, RampHalfThickness, 0), slope),
                    Vector3.Transform(Vector3.UnitY, slope), .005f);
                break;
            case SweepEntry.Tumbling:
                q = Quaternion.CreateFromAxisAngle(Vector3.Normalize(new(1, 0, 1)), 40f * MathF.PI / 180) * Lying(shape);
                at = Resting(shape, q, new(.1f, BedY + .3f, -.1f), Vector3.UnitY, 0f); break;
            case SweepEntry.AimedBed:
                q = Lying(shape); at = Resting(shape, q, Frame + Vector3.Transform(new Vector3(0, Rest, 0), Aimed) + Vector3.UnitY, Vector3.UnitY, 0f); break;
            default: throw new ArgumentOutOfRangeException(nameof(entry));
        }
        parts.Add(SweepPart(shape, 2, at, q));
        return (Bench([.. parts]), bed, frame, bearing);
    }

    /// <summary>A run of a construction observed every tick from the authored start: the committed reads and the committed state and
    /// offset of each of one dynamic body's four samples on the only surface (body <paramref name="payload"/>, 2 by default), read
    /// through the engagement table's defined ABI row read. A body's engagement row is its ordinal among the compiled scene's dynamic
    /// bodies.</summary>
    private sealed record SampledRun(List<WorkshopRead> Reads, List<CompliantSampleRead[]> Samples);

    private static async Task<SampledRun> RunWithSamples(WorkshopConstruction construction, int ticks, ulong payload = 2)
    {
        var dynamic = WorkshopPhysicsCompiler.Compile(construction, new(1, 2)).Bodies.ToArray()
            .Where(b => b.Motion == RigidMotionKind.Dynamic).Select(b => b.Id).ToList();
        var ordinal = dynamic.IndexOf(new GpuBodyId(payload));
        Assert.True(ordinal >= 0, $"body {payload} is dynamic");
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        CompliantSampleRead[] Committed()
        {
            var world = harness.Device.CommittedWorld.ToArray();
            return [.. Enumerable.Range(0, PhysicsGpuAbi.CompliantSampleCount)
                .Select(i => PhysicsGpuAbi.ReadCompliantSample(world, 0, ordinal, i))];
        }
        var run = new SampledRun([harness.Committed], [Committed()]);
        for (var tick = 0; tick < ticks; tick++)
        {
            await harness.Advance(1);
            run.Reads.Add(harness.Committed);
            run.Samples.Add(Committed());
        }
        return run;
    }

    /// <summary>The bed's stored energy under a body, from its committed samples: Σ over its bearing samples (OnTop, over the bed and below
    /// the rest plane) of the force law's potential, ½(k/4)·(d − o)² within the stroke and linear beyond it, with o the committed offset.</summary>
    private static double Stored(DeclaredBody body, PhysicsBodyRead read, CompliantSampleRead[] committed, Quaternion frame, float tension)
    {
        var total = 0.0; var k = tension / 4.0;
        var samples = Samples(body, Origin(read), Rotation(read), frame);
        for (var i = 0; i < samples.Length; i++)
        {
            var local = InFrame(samples[i], frame, Frame);
            if (committed[i].State != CompliantSampleState.OnTop || !OverBed(local) || local.Y >= Rest) continue;
            double d = Rest - local.Y, o = committed[i].Offset, compressed = Math.Min(d, Stroke) - o;
            total += .5 * k * compressed * compressed + (d > Stroke ? k * (Stroke - o) * (d - Stroke) : 0);
        }
        return total;
    }

    /// <summary>The margin keeping the test's support geometry and the engine's apart from a classification boundary: 1 mm, far above their
    /// f32 disagreement and far below any sample's travel in a tick.</summary>
    private const float Agreement = .001f;

    /// <summary>Runs <paramref name="construction"/> (its payload is body 2, its only dynamic body, so the payload's energy is the whole
    /// moving energy) for 4 s and asserts the bed's invariants: the bed reads
    /// deflected at some tick (it bore the payload); on every tick every payload sample clearly over the bed and below its rest plane is
    /// committed OnTop; no tick holds more than 1.005 × the starting energy, the store counted from the committed offsets; and, where
    /// the <paramref name="bearing"/> samples' capacity bound (n/4)·½k·(S − 1 cm − a)² covers the payload's whole fall m·g·(h₀ − floor)
    /// to the lowest point of the stroke floor, the payload never reaches the back plate while any part of it is over the footprint.
    /// Returns the run.</summary>
    private async Task<SampledRun> HeldWhileOverTheBedWithoutGain(string label, WorkshopConstruction construction,
        WorkshopGenericInstance bed, Quaternion frame, float tension, int bearing)
    {
        var body = Declared(construction, new(2));
        Assert.Single(WorkshopPhysicsCompiler.Compile(construction, new(1, 2)).Bodies.ToArray(), b => b.Motion == RigidMotionKind.Dynamic);
        var run = await RunWithSamples(construction, 4 * Rate);
        var n = Vector3.Transform(Vector3.UnitY, frame); var right = Vector3.Transform(Vector3.UnitX, frame);
        var forward = Vector3.Transform(Vector3.UnitZ, frame);
        double Along(PhysicsBodyRead read, Vector3 d) => Vector3.Dot(Support(body, Origin(read), Rotation(read), d) - Frame, d);
        bool AnyPartOverBed(PhysicsBodyRead read) => Along(read, right) > -HalfX && Along(read, -right) > -HalfX &&
            Along(read, forward) > -HalfZ && Along(read, -forward) > -HalfZ;
        var payloads = run.Reads.Select(read => Find(read, new(2))).ToArray();
        var energies = payloads.Select((payload, tick) => Energy(body, payload, Stored(body, payload, run.Samples[tick], frame, tension))).ToArray();
        var worst = energies.Max() / energies[0];
        var floor = new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) }
            .Min(c => (Frame + Vector3.Transform(new Vector3(c.Item1 * HalfX, Rest - Stroke, c.Item2 * HalfZ), frame)).Y);
        var fall = body.Mass * G * (Support(body, Origin(payloads[0]), Rotation(payloads[0]), -Vector3.UnitY).Y - floor);
        var capacity = bearing / 4.0 * .5 * tension * Math.Pow(Stroke - PlateClearance - ArrivalDepth, 2);
        var lowest = payloads.Where(AnyPartOverBed).Select(read => -Along(read, -n)).DefaultIfEmpty(double.NaN).Min();
        output.WriteLine($"{label}: worst E/E0 {worst:F5}, lowest over the bed {lowest:F4} (floor {Rest - Stroke}), deepest " +
            $"{run.Reads.Max(r => Surface(r, bed.Id)):F4}, fall {fall:F1} J vs capacity {capacity:F1} J");
        Assert.Contains(run.Reads, read => Surface(read, bed.Id) > 0f);
        for (var tick = 0; tick < payloads.Length; tick++)
        {
            var samples = Samples(body, Origin(payloads[tick]), Rotation(payloads[tick]), frame);
            for (var i = 0; i < samples.Length; i++)
            {
                var local = InFrame(samples[i], frame, Frame);
                var clearlyOver = MathF.Abs(local.X) <= HalfX + Edge - Agreement && MathF.Abs(local.Z) <= HalfZ + Edge - Agreement;
                if (clearlyOver && local.Y < Rest - Agreement)
                    Assert.True(run.Samples[tick][i].State == CompliantSampleState.OnTop,
                        $"{label}: tick {tick} sample {i} at depth {Rest - local.Y:F4} is {run.Samples[tick][i].State}");
            }
        }
        Assert.True(worst <= EnergyEnvelope, $"E/E0 reached {worst}");
        if (capacity >= fall)
            Assert.All(payloads.Where(AnyPartOverBed), read =>
                Assert.True(-Along(read, -n) > Rest - Stroke + PlateClearance, "never reaches the back plate while over the bed"));
        return run;
    }

    // The matrix's fastest arrival across a rail: a 1 kg Weight straddling the +X side rail, its centre of mass 5 cm inside the rail's
    // inner edge, arrives at 12 m/s on the shipped 300 N/m bed (the run reaches it: the last free-fall tick is no slower than 12 m/s less
    // one tick of gravity and 1 cm/s). The rim impact does not end the bed's part: afterwards the bed bears the load, its committed
    // deflection reaching at least the load's static sag m·g/k (any load the bed comes to bear passes through it), with the sweep's state
    // and energy invariants.
    [Fact]
    public async Task AWeightStraddlingARailAtTheFastestArrivalIsBorneByTheBedAfterTheRimImpact()
    {
        var (construction, bed, frame, bearing) = SweepCase(SweepShape.Weight, SweepEntry.StraddleAtFastestArrival, 300f);
        var run = await HeldWhileOverTheBedWithoutGain("fastest straddle", construction, bed, frame, 300f, bearing);
        var payloads = run.Reads.Select(read => Find(read, new(2))).ToList();
        var impact = payloads.FindIndex(payload => Origin(payload).Y <= BedY);
        Assert.True(impact > 0, "the Weight reached the rail top");
        output.WriteLine($"arrival vy {Velocity(payloads[impact - 1]).Y:F3}, deepest after impact {run.Reads.Skip(impact).Max(r => Surface(r, bed.Id)):F4}");
        Assert.True(-Velocity(payloads[impact - 1]).Y >= FastestArrival - G / Rate - .01f, "arrives at the matrix's fastest speed");
        Assert.Contains(run.Reads.Skip(impact), read => Surface(read, bed.Id) >= 1f * G / 300f);
    }

    /// <summary>The settled spread between a resting flat body's deepest sample and its mean depth: half the Domino's 1.1 m length times the
    /// sine of the 0.5° resting tilt the flat-box fact allows, about 5 mm.</summary>
    private const float SagSpread = .005f;

    // Support collapse ("hole 1"): at tilt all four diagonal samples sit on a body's lowest end, so the other end sinks unsampled until
    // the support flips to it. Three bodies whose samples flip in below the plane and that end wholly on the bed: the 1.1 m Domino slid
    // in over the −X rail top from the 35° Ramp, which tips in over the rail's inner edge, a 1 kg Weight thrown onto the bed by a 12 m/s
    // straddle, and the 8 kg Weight tilted 20° onto the rail at 8.2 m/s, which tips in and keeps about a centimetre of offset. Over the last second of 8 s (each has made its last landing by 4 s and the bed's ζ 0.12 decay settles it within
    // about 3 s), all four committed states are OnTop on every tick, and the bed's sag averaged over that second lies in
    // [m·g/k, m·g/k + max oᵢ] to the resting spread: the bed's average push Σ(k/4)·(dᵢ − oᵢ) balances the weight while the body stays
    // on the frictionless bed (it may still drift along it), so its mean depth is m·g/k plus its mean offset, and the deepest sample read
    // as the sag is at least the mean.
    [Theory]
    [InlineData(SweepShape.Domino, SweepEntry.SlidOverRail)]
    [InlineData(SweepShape.Weight, SweepEntry.StraddleAtFastestArrival)]
    [InlineData(SweepShape.HeavyWeight, SweepEntry.TiltedOnRailFast)]
    public async Task ABodyThatTipsFullyOntoTheBedSettlesWithEverySampleOnTopAndItsSagWithinTheOffsetBias(SweepShape shape, SweepEntry entry)
    {
        var (construction, bed, _, _) = SweepCase(shape, entry, 300f);
        var body = Declared(construction, new(2));
        var run = await RunWithSamples(construction, 8 * Rate);
        var staticSag = body.Mass * G / 300f;
        var window = Enumerable.Range(7 * Rate, run.Reads.Count - 7 * Rate).ToArray();
        Assert.All(window, tick => Assert.All(run.Samples[tick], sample => Assert.Equal(CompliantSampleState.OnTop, sample.State)));
        var sag = window.Average(tick => Surface(run.Reads[tick], bed.Id));
        var maxOffset = window.Max(tick => run.Samples[tick].Max(sample => sample.Offset));
        output.WriteLine($"{shape} {entry}: mean sag {sag:F4} (m·g/k {staticSag:F4}), largest offset {maxOffset:F4}");
        Assert.InRange(sag, staticSag - SagSpread, staticSag + maxOffset + SagSpread);
    }

    // ---------------- energy, tension and damping ----------------

    /// <summary>A Weight's base-centre lowest point (it stays upright under a symmetric membrane load).</summary>
    private static double WeightDeflection(PhysicsBodyRead weight) => Deflection(Origin(weight), Quaternion.Identity, Frame);

    private static double WeightEnergy(PhysicsBodyRead body, float mass, float tension)
    {
        double m = mass, r = .04808397f * mass + .15932827f, h = .03818433f * mass + .12652539f;
        double ix = m * (r * r / 4 + h * h / 3), iy = m * r * r / 2;
        var w = Vector3.Transform(Spin(body), Quaternion.Inverse(Rotation(body)));
        var v = Velocity(body); var d = WeightDeflection(body);
        return .5 * m * Vector3.Dot(v, v) + .5 * (ix * w.X * w.X + iy * w.Y * w.Y + ix * w.Z * w.Z) +
            m * G * (Origin(body).Y - FloorY) + .5 * tension * d * d;
    }

    private static double Tilt(PhysicsBodyRead body) =>
        Math.Acos(Math.Clamp(1 - 2 * (body.Rotation.X * body.Rotation.X + body.Rotation.Z * body.Rotation.Z), -1, 1)) * 180 / Math.PI;

    // The energy envelope: payloads of 0.25–4 kg arriving at 2–6 m/s on tensions of 120–1200 N/m never hold more mechanical energy,
    // membrane store included, than they started with (0.5 % of the energy above the stroke floor). The stiffest corner is 0.25 kg at
    // 6 m/s on 1200 N/m: centred and undamped (four coincident samples, k in total), ω·h = √(1200/0.25)/480 ≈ 0.14. CAT-065 §2's
    // stability envelope gives the off-centre and damped bound.
    [Theory]
    [InlineData(.5f, 2f, 120f)]
    [InlineData(.5f, 6f, 600f)]
    [InlineData(4f, 2f, 600f)]
    [InlineData(4f, 6f, 120f)]
    [InlineData(2f, 4f, 300f)]
    [InlineData(1f, 6f, 120f)]
    [InlineData(.25f, 6f, 1200f)]
    public async Task NoTickGainsEnergyAcrossTheMassSpeedAndTensionEnvelope(float mass, float speed, float tension)
    {
        var weight = WeightTests.Placed(2, 0, BedY + speed * speed / (2 * G), 0, mass);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Bed(tension: tension), weight), 3 * Rate, includeBuilt: true);
        var e0 = WeightEnergy(Find(reads[0], weight.Id), mass, tension);
        var worst = reads.Max(read => WeightEnergy(Find(read, weight.Id), mass, tension) / e0);
        var peak = reads.Max(read => WeightDeflection(Find(read, weight.Id)));
        output.WriteLine($"{mass} kg at {speed} m/s on {tension} N/m: worst E/E0 {worst:F5}, peak {peak:F4}, tilt {reads.Max(r => Tilt(Find(r, weight.Id))):F4}°");
        Assert.True(peak > 0, "the membrane was loaded");
        Assert.All(reads, read => Assert.True(Tilt(Find(read, weight.Id)) < .5, "upright"));
        Assert.True(worst <= EnergyEnvelope, $"E/E0 reached {worst}");
    }

    // A five-times softer membrane (120 vs 600 N/m) under the same 1 m drop of the Basketball compresses at least 1.8× deeper (√5 ≈ 2.2
    // undamped) and holds the ball longer (contact half-period ∝ 1/√k).
    [Fact]
    public async Task ASofterTensionCompressesDeeperAndHoldsLonger()
    {
        async Task<Contact> Drop(float tension)
        {
            var ball = Basketball(2, 0, BedY + BallRadius + 1, 0);
            var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Bed(tension: tension), ball), 2 * Rate, includeBuilt: true);
            return FirstContact(reads, read => BallDeflection(Find(read, ball.Id)));
        }
        var soft = await Drop(120f); var stiff = await Drop(600f);
        output.WriteLine($"soft {soft}, stiff {stiff}");
        Assert.True(soft.PeakDeflection < Stroke - .01, "the soft bed stays clear of the plate");
        Assert.True(soft.PeakDeflection >= 1.8 * stiff.PeakDeflection, $"{soft.PeakDeflection} vs {stiff.PeakDeflection}");
        Assert.True(soft.End - soft.Start > stiff.End - stiff.Start, $"{soft.End - soft.Start} vs {stiff.End - stiff.Start} ticks");
    }

    /// <summary>The rebound fraction of a Weight dropped flat onto a 1200 N/m bed at 6 m/s: √(rebound apex / drop height), the
    /// ratio of departure to arrival speed.</summary>
    private static async Task<double> ReboundFraction(float mass, float damping)
    {
        const float speed = 6f;
        var drop = speed * speed / (2 * G);
        var bed = Bed(tension: 1200f, damping: damping);
        var weight = WeightTests.Placed(2, 0, BedY + drop, 0, mass);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(bed, weight), 3 * Rate);
        var contact = FirstContact(reads, read => Surface(read, bed.Id));
        Assert.True(contact.PeakDeflection < Stroke - .05, $"{mass} kg stays clear of the plate ({contact.PeakDeflection} m)");
        var apex = reads.Skip(contact.End).Max(read => Origin(Find(read, weight.Id)).Y);
        return Math.Sqrt(Math.Max(0, apex - BedY) / drop);
    }

    /// <summary>The closed-form rebound fraction of the membrane law for a body arriving at <paramref name="speed"/> along −n, with
    /// gravity along −n. While the bed bears it, the depth x obeys m·x″ = m·g − k·x − c·x′ with c = 2ζ√(k·m): with ω = √(k/m),
    /// ω_d = ω√(1 − ζ²) and x_s = m·g/k, x(t) = x_s + e^(−ζωt)(a·cos ω_d t + b·sin ω_d t), a = −x_s, b = (v₀ + ζωa)/ω_d. The unilateral
    /// (clamped) bed lets go where k·x + c·x′ falls to zero, since it never pulls; the two-sided one pulls until x returns to 0. The body
    /// then rises freely: apex above the rest plane v²/2g − x at release. Returns √(apex / drop height). The first release is found by
    /// stepping 10 µs from t = 0 and bisecting the bracketing step.</summary>
    private static double ClosedFormRebound(double mass, double tension, double zeta, double speed, bool unilateral)
    {
        double w = Math.Sqrt(tension / mass), wd = w * Math.Sqrt(1 - zeta * zeta), c = 2 * zeta * Math.Sqrt(tension * mass);
        double xs = mass * G / tension, a = -xs, b = (speed + zeta * w * a) / wd;
        double X(double t) => xs + Math.Exp(-zeta * w * t) * (a * Math.Cos(wd * t) + b * Math.Sin(wd * t));
        double V(double t) => Math.Exp(-zeta * w * t) * ((b * wd - zeta * w * a) * Math.Cos(wd * t) - (a * wd + zeta * w * b) * Math.Sin(wd * t));
        double Bearing(double t) => unilateral ? tension * X(t) + c * V(t) : X(t);
        double low = 1e-5, high = low;
        while (Bearing(high) > 0)
        {
            low = high; high += 1e-5;
            if (high > 2 * Math.PI / wd) throw new InvalidOperationException("the closed form never releases");
        }
        for (var i = 0; i < 60; i++)
        {
            var mid = (low + high) / 2;
            if (Bearing(mid) > 0) low = mid; else high = mid;
        }
        var rising = -V(high);
        var apex = rising * rising / (2 * G) - X(high);
        return Math.Sqrt(Math.Max(0, apex) / (speed * speed / (2 * G)));
    }

    // The damping ratio is a property of the bed, not of the payload: c = 2ζ√(k·m), so equal-arrival-speed drops of 0.5 kg and 4 kg on
    // one bed rebound by matching fractions, each the closed-form unilateral law's (gravity included, so the masses' small difference,
    // 0.69 and 0.66, is the law's own), and a higher damping ratio rebounds lower. The bed never pulls: at ζ 0.5 for 0.5 kg the clamped
    // law with gravity returns about 0.25 where a pulling bed would return about 0.12 (0.30 and 0.16 gravity-free), and the ±0.05 band
    // around the clamped value excludes the pulling one, so removing the clamp fails this fact. A 0.05 change in rebound fraction is
    // about 13 cm of apex on this 1.8 m drop at ζ 0.12 and about 5 cm at ζ 0.5, invisible as a different bounce; a damper not scaled by
    // √m would part the two masses by about 0.25.
    [Fact]
    public async Task EqualArrivalDropsReboundByTheClampedLawsFractionAndMoreDampingReboundsLower()
    {
        const double band = .05;
        var light = await ReboundFraction(.5f, .12f);
        var heavy = await ReboundFraction(4f, .12f);
        var damped = await ReboundFraction(.5f, .5f);
        var (lightLaw, heavyLaw, dampedLaw) = (ClosedFormRebound(.5, 1200, .12, 6, true), ClosedFormRebound(4, 1200, .12, 6, true),
            ClosedFormRebound(.5, 1200, .5, 6, true));
        var pulling = ClosedFormRebound(.5, 1200, .5, 6, false);
        output.WriteLine($"0.5 kg {light:F3} (law {lightLaw:F3}), 4 kg {heavy:F3} (law {heavyLaw:F3}), 0.5 kg at ζ 0.5 {damped:F3} (law {dampedLaw:F3}, pulling {pulling:F3})");
        Assert.True(Math.Abs(dampedLaw - pulling) > 2 * band, $"the band separates the clamped {dampedLaw} from the pulling {pulling}");
        Assert.InRange(light, lightLaw - band, lightLaw + band);
        Assert.InRange(heavy, heavyLaw - band, heavyLaw + band);
        Assert.InRange(light - heavy, -band, band);
        Assert.InRange(damped, dampedLaw - band, dampedLaw + band);
        Assert.True(damped < light - .2, $"ζ 0.5 rebounds {damped}, ζ 0.12 {light}");
    }

    // Bottoming out: 4 kg and 8 kg Weights arriving at 12 m/s run the full stroke onto the back plate, which stops them (no tunnelling
    // beyond a centimetre) inelastically; the stored membrane energy relaunches them, and no tick gains energy.
    [Theory]
    [InlineData(4f)]
    [InlineData(8f)]
    public async Task AnOverloadBottomsOutOnTheBackPlateWithoutAddingEnergy(float mass)
    {
        var drop = 12f * 12f / (2 * G);
        var weight = WeightTests.Placed(2, 0, BedY + drop, 0, mass);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Bed(), weight), 4 * Rate, includeBuilt: true);
        var lowest = reads.Min(read => Origin(Find(read, weight.Id)).Y);
        var e0 = WeightEnergy(Find(reads[0], weight.Id), mass, 300f);
        var worst = reads.Max(read => WeightEnergy(Find(read, weight.Id), mass, 300f) / e0);
        var bottom = reads.FindIndex(read => Origin(Find(read, weight.Id)).Y == lowest);
        var apex = reads.Skip(bottom).Max(read => Origin(Find(read, weight.Id)).Y);
        output.WriteLine($"{mass} kg: lowest base {lowest:F4} (floor {FloorY}), apex after {apex:F3}, worst E/E0 {worst:F5}");
        Assert.InRange(lowest, FloorY - .01f, FloorY + .005f);
        Assert.True(worst <= EnergyEnvelope, $"E/E0 reached {worst}");
        Assert.True(apex < BedY + drop, "no rebound regains the drop");
    }

    public static TheoryData<float, float> RimStrikes() => new()
    {
        // Side rails (x from 1.2 to 1.36): inner and outer halves, off the rail's centre along it.
        { 1.24f, .3f }, { 1.32f, .3f }, { -1.24f, -.4f }, { -1.32f, -.4f },
        // End rails (z from 0.9 to 1.06): inner and outer halves, off the rail's centre along it.
        { .5f, .94f }, { .5f, 1.02f }, { -.6f, -.94f }, { -.6f, -1.02f },
    };

    /// <summary>How far the Basketball's samples sit from its very bottom: r·ε√2 ≈ 7.5 mm, so 1 cm covers them.</summary>
    private const float SampleReach = .01f;

    // Rim controls (matrix row "zero membrane launch"): a Basketball dropped onto a rail's top face, over its inner or outer half and
    // off-centre along it, meets the inelastic frame and is never launched by the strike (its upward speed after impact stays within the
    // no-launch bound). The bed bears it only once part of it is over the bed: on every tick its bottom lies beyond the footprint plus the
    // edge e and the samples' reach, the bed reads undeflected. (A ball struck on a rail's inner half that rolls into the bed may then be
    // caught; that is the law.) A centred drop from the same height is the positive control.
    [Theory]
    [MemberData(nameof(RimStrikes))]
    public async Task ARimStrikeIsNeverLaunchedAndTheBedBearsTheBallOnlyOverTheBed(float x, float z)
    {
        var bed = Bed(); var ball = Basketball(2, x, 5, z);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(bed, ball), 2 * Rate, includeBuilt: true);
        var near = reads.FindIndex(read => Origin(Find(read, ball.Id)).Y < BedY + BallRadius + .05f);
        Assert.True(near > 0, "the ball reached the rail");
        var rise = reads.Skip(near).Max(read => Velocity(Find(read, ball.Id)).Y);
        output.WriteLine($"rim ({x}, {z}): max vy after impact {rise}, final {Origin(Find(reads[^1], ball.Id))}, deepest {reads.Max(r => Surface(r, bed.Id))}");
        Assert.InRange(rise, float.NegativeInfinity, NoLaunchSpeed);
        Assert.All(reads.Where(read =>
        {
            var bottom = Origin(Find(read, ball.Id));
            return MathF.Abs(bottom.X) > HalfX + Edge + SampleReach || MathF.Abs(bottom.Z) > HalfZ + Edge + SampleReach;
        }), read => Assert.Equal(0f, Surface(read, bed.Id)));

        var centre = Basketball(2, 0, 5, 0);
        var (_, control, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Bed(), centre), 2 * Rate, includeBuilt: true);
        Assert.True(control.Max(read => Velocity(Find(read, centre.Id)).Y) > 3f, "the centred drop is launched");
    }

    // A ball dropped beside the frame misses it entirely: it falls past the bed undeflected and straight.
    [Fact]
    public async Task ADropBesideTheFrameMissesTheBed()
    {
        var bed = Bed(); var ball = Basketball(2, 2f, 5, 0);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(bed, ball), Rate, includeBuilt: true);
        var below = reads.FindIndex(read => Origin(Find(read, ball.Id)).Y < FloorY - 1);
        Assert.True(below > 0, "fell past the frame");
        Assert.All(reads, read => Assert.Equal(0f, Surface(read, bed.Id)));
        Assert.All(reads.Take(below), read =>
        {
            var b = Find(read, ball.Id);
            Assert.InRange(Origin(b).X, 2f - LateralDrift, 2f + LateralDrift);
            Assert.True(Velocity(b).Y <= 0f);
        });
    }

    // Two concurrent loads, 1 kg at x = −0.4 and 2 kg at x = 0.4, bounce as each does alone, in either insertion order: each load's
    // peak compression within 1 cm and its rebound apex within 2 cm of its solo run (a 2% change on the 1 m drop, invisible to a player
    // and far inside a receiver's capture margin), and the forward and reversed runs agree with each other by the same bounds. On every
    // tick of both runs the bed's committed deflection is the larger of the two loads' depths (each its deepest over-bed sample from the
    // declared geometry and the read pose, one quantity computed two ways, so the 1 mm of the deflection read holds).
    [Fact]
    public async Task ConcurrentLoadsReboundIndependentlyInEitherInsertionOrder()
    {
        var light = WeightTests.Placed(2, -.4, BedY + 1, 0, 1f);
        var heavy = WeightTests.Placed(3, .4, BedY + 1, 0, 2f);
        var (_, forward, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Bed(), light, heavy), 2 * Rate);
        var (_, reversed, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(heavy, light, Bed()), 2 * Rate);
        var (_, lightAlone, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Bed(), light), 2 * Rate);
        var (_, heavyAlone, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Bed(), heavy), 2 * Rate);
        var (lightBody, heavyBody) = (Declared(Bench(light), light.Id), Declared(Bench(heavy), heavy.Id));
        foreach (var run in new[] { forward, reversed })
            Assert.All(run, read =>
            {
                var larger = Math.Max(SampleDepth(lightBody, Find(read, light.Id), Quaternion.Identity, Frame),
                    SampleDepth(heavyBody, Find(read, heavy.Id), Quaternion.Identity, Frame));
                Assert.InRange(Surface(read, new(1)), larger - .001, larger + .001);
            });
        (double Peak, double Apex) Bounce(List<WorkshopRead> reads, GpuBodyId id)
        {
            var contact = FirstContact(reads, read => WeightDeflection(Find(read, id)));
            return (contact.PeakDeflection, reads.Skip(contact.End).Max(read => Origin(Find(read, id)).Y));
        }
        static void Same((double Peak, double Apex) expected, (double Peak, double Apex) actual)
        {
            Assert.InRange(actual.Peak, expected.Peak - .01, expected.Peak + .01);
            Assert.InRange(actual.Apex, expected.Apex - .02, expected.Apex + .02);
        }
        foreach (var (id, alone) in new[] { (light.Id, lightAlone), (heavy.Id, heavyAlone) })
        {
            var solo = Bounce(alone, id);
            var (ahead, behind) = (Bounce(forward, id), Bounce(reversed, id));
            output.WriteLine($"body {id.Value}: solo {solo}, forward {ahead}, reversed {behind}");
            Assert.True(solo.Apex > BedY + .2, $"body {id.Value} rebounds alone");
            Same(solo, ahead);
            Same(solo, behind);
            Same(ahead, behind);
        }
    }

    // A stable stacked pair: a 1 kg Weight on a 4 kg Weight's crest (WeightTests' stacking height), placed the way a player places them
    // (the lower standing on the rest plane at the bed's centre, the upper on the lower's crest), on the default bed. The pair settles in
    // static equilibrium at compression (m₁ + m₂)g/k within the intent's 1.5 cm, both upright, in place and at rest, and over the
    // last second every one of the lower Weight's bearing samples is OnTop with an offset under 1 mm: it entered on the rest plane (a
    // placed fixture), so its offsets are its first substep's micrometre fall and the bed bears the whole compression.
    [Fact]
    public async Task AStackedPairSettlesAtTheStaticCompressionOfItsTotalWeightWithEveryBearingSampleOnTop()
    {
        const float lowerMass = 4f, upperMass = 1f;
        var crest = (.5771f + .1575f) * (.136734f * lowerMass + .453070f);
        var lower = WeightTests.Placed(2, 0, BedY, 0, lowerMass);
        var upper = WeightTests.Placed(3, 0, BedY + crest, 0, upperMass);
        var run = await RunWithSamples(Bench(Bed(), lower, upper), 8 * Rate, payload: 2);
        var expected = (lowerMass + upperMass) * G / 300f;
        var window = Enumerable.Range(7 * Rate, run.Reads.Count - 7 * Rate).ToArray();
        output.WriteLine($"settled compression {WeightDeflection(Find(run.Reads[^1], lower.Id)):F4} vs {expected:F4}, largest offset " +
            $"{window.Max(tick => run.Samples[tick].Max(sample => sample.Offset)):F6}");
        foreach (var tick in window)
        {
            var read = run.Reads[tick];
            Assert.InRange(WeightDeflection(Find(read, lower.Id)), expected - SettleBand, expected + SettleBand);
            Assert.All(run.Samples[tick], sample =>
            {
                Assert.Equal(CompliantSampleState.OnTop, sample.State);
                Assert.InRange(sample.Offset, 0f, Agreement);
            });
            foreach (var id in new[] { lower.Id, upper.Id })
            {
                var body = Find(read, id);
                Assert.True(Tilt(body) < .5, $"body {id.Value} tilt {Tilt(body)}°");
                Assert.InRange(new Vector2(Origin(body).X, Origin(body).Z).Length(), 0, RestingDrift);
                Assert.InRange(Velocity(body).Length(), 0, RestSpeed);
            }
        }
    }

    [Fact]
    public async Task AWeightTowerSettlesUprightAtTheStaticCompressionOfItsTotalWeight()
    {
        // A 2 kg Weight on a 1 kg Weight (on its crest, WeightTests' stacking height), placed the way a player places them: the lower
        // standing on the rest plane, the upper on the lower's crest. The bed's distributed support keeps the top-heavy tower upright at
        // (m₁ + m₂)g/k.
        const float lowerMass = 1f, upperMass = 2f;
        var crest = (.5771f + .1575f) * (.136734f * lowerMass + .453070f);
        var lower = WeightTests.Placed(2, 0, BedY, 0, lowerMass);
        var upper = WeightTests.Placed(3, 0, BedY + crest, 0, upperMass);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Bed(damping: .8f), lower, upper), 8 * Rate);
        var expected = (lowerMass + upperMass) * G / 300f;
        foreach (var read in reads.Skip(4 * Rate))
        {
            Assert.InRange(WeightDeflection(Find(read, lower.Id)), expected - SettleBand, expected + SettleBand);
            foreach (var id in new[] { lower.Id, upper.Id })
            {
                var body = Find(read, id);
                Assert.True(Tilt(body) < .5, $"body {id.Value} tilt {Tilt(body)}°");
                Assert.InRange(new Vector2(Origin(body).X, Origin(body).Z).Length(), 0, RestingDrift);
                Assert.InRange(Velocity(body).Length(), 0, RestSpeed);
            }
        }
    }

    // A box lying flat (a Domino on its broad face) at the default damping ratio rests flat at mg/k: the bed supports its four bottom
    // corners, so it neither rocks nor walks, at any yaw on the bed, including 45° where two diagonal samples point at the box's edges
    // midway between its corners. The 4 mm compression band is a third of the 13 mm sag, so a missing load (0 mm) or a four-times-stiff
    // bed (3 mm) would fail it. The bed is frictionless along its plane, so only tilt, drift and speed are bounded. It falls 3 cm onto
    // the open bed, so every sample is over the bed and OnTop from above the plane with offset 0: the sag-bias range [m·g/k,
    // m·g/k + max oᵢ] collapses to m·g/k.
    [Theory]
    [InlineData(0f)]
    [InlineData(45f)]
    public async Task AFlatBoxRestsFlatWithoutRockingOrWalking(float yaw)
    {
        var flat = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw * MathF.PI / 180) * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2);
        var domino = PlacedParts.Domino(new(2), .2f, BedY + .125f + .03f, -.1f, flat.X, flat.Y, flat.Z, flat.W);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Bed(), domino), 4 * Rate);
        var expected = LawTile.Mass * G / 300f;
        foreach (var read in reads.Skip(3 * Rate))
        {
            var body = Find(read, domino.Id);
            // The broad face's normal (local +X, authored up) stays within 0.5° of the bed normal.
            var face = Vector3.Transform(Vector3.UnitX, Rotation(body));
            Assert.True(MathF.Acos(MathF.Min(1f, face.Y)) * 180 / MathF.PI < .5f, $"face tilt {face}");
            Assert.InRange(BedY - (Origin(body).Y - .125f), expected - .004, expected + .004);
            Assert.InRange(new Vector2(Origin(body).X - .2f, Origin(body).Z + .1f).Length(), 0, RestingDrift);
            Assert.InRange(Velocity(body).Length(), 0, RestSpeed);
            // The resting spin bound WeightTests uses for a settled body (0.05 rad/s), about the in-plane axes.
            Assert.InRange(new Vector2(Spin(body).X, Spin(body).Z).Length(), 0, .05f);
        }
    }

    /// <summary>A standing Domino's toppling angle: it falls once its centre of mass passes its base's narrow edge, at
    /// atan(half thickness / half height) = atan(0.125 / 0.55) ≈ 12.8°.</summary>
    private static readonly double DominoToppling = Math.Atan(LawTile.HalfExtents.X / LawTile.HalfExtents.Y) * 180 / Math.PI;

    // An upright Domino placed standing at the bed's centre on the default bed, its lowest base edge on the rest plane (a placed
    // fixture, so its offsets are its first substep's micrometre fall), either exactly upright or leaning a quarter of its toppling angle
    // about its narrow axis (a perfectly upright placement is symmetric and cannot show stability). Upright, its four samples are its
    // base corners (±0.125, ±0.325), whose restoring stiffness about the narrow edge, k·0.125² ≈ 4.7 N·m/rad, exceeds gravity's tipping
    // m·g·h ≈ 2.2 N·m/rad; leaning beyond atan ε ≈ 0.9°, all four sit on the low edge and the bed pushes there as a floor does, so a block
    // leaning less than its toppling angle rocks back. Over the last second of 4 s (the ζ 0.12 bounce has decayed by about e⁻¹⁰ by
    // 3 s): every sample is OnTop; the mean sag lies in [m·g/k, m·g/k + max oᵢ] to the resting spread, as for the bodies that tip onto
    // the bed; it stays standing, its long axis tilted less than half its toppling angle (a gameplay bound: a Domino leaning by half its
    // toppling angle still reads as standing and keeps a 2× margin against falling); and it does not walk, its centre moving at most
    // RestingDrift horizontally across the window.
    [Theory]
    [InlineData(0f)]
    [InlineData(.25f)]
    public async Task AnUprightDominoStandsOnTheBedCentreWithoutTopplingOrWalking(float leanFraction)
    {
        var bed = Bed();
        var half = LawTile.HalfExtents;
        var lean = leanFraction * MathF.Atan(half.X / half.Y);
        var q = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, lean);
        var domino = PlacedParts.Domino(new(2), 0, BedY + half.X * MathF.Sin(lean) + half.Y * MathF.Cos(lean), 0, q.X, q.Y, q.Z, q.W);
        var run = await RunWithSamples(Bench(bed, domino), 4 * Rate);
        var staticSag = LawTile.Mass * G / 300f;
        var window = Enumerable.Range(3 * Rate, run.Reads.Count - 3 * Rate).ToArray();
        var bodies = window.Select(tick => Find(run.Reads[tick], domino.Id)).ToArray();
        var sag = window.Average(tick => Surface(run.Reads[tick], bed.Id));
        var maxOffset = window.Max(tick => run.Samples[tick].Max(sample => sample.Offset));
        var start = new Vector2(Origin(bodies[0]).X, Origin(bodies[0]).Z);
        double Drift(PhysicsBodyRead body) => Vector2.Distance(new(Origin(body).X, Origin(body).Z), start);
        output.WriteLine($"lean {lean * 180 / MathF.PI:F2}°: mean sag {sag:F4} (m·g/k {staticSag:F4}), largest offset {maxOffset:F6}, " +
            $"largest tilt over the run {run.Reads.Max(read => Tilt(Find(read, domino.Id))):F3}°, in the window " +
            $"{bodies.Max(Tilt):F3}° (bound {DominoToppling / 2:F2}°), window drift {bodies.Max(Drift):F5}, final {Origin(Find(run.Reads[^1], domino.Id))}");
        Assert.All(window, tick => Assert.All(run.Samples[tick], sample => Assert.Equal(CompliantSampleState.OnTop, sample.State)));
        Assert.InRange(sag, staticSag - SagSpread, staticSag + maxOffset + SagSpread);
        Assert.All(bodies, body => Assert.True(Tilt(body) < DominoToppling / 2, $"tilt {Tilt(body)}°"));
        Assert.All(bodies, body => Assert.InRange(Drift(body), 0, RestingDrift));
    }

    // A flat-bottomed payload (an upright 4 kg Weight placed off-centre) settles upright on the bed at mg/k without tilting or
    // drifting: its four rim samples sit at equal depth, so the bed adds no net torque. It falls onto the open bed, so its offsets are 0
    // and the sag-bias range collapses to m·g/k.
    [Fact]
    public async Task AFlatBottomedWeightRestsUprightAtItsStaticCompression()
    {
        var weight = WeightTests.Placed(2, .3, BedY + .3, -.2, 4f);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Bed(damping: .8f), weight), 4 * Rate);
        var expected = 4f * G / 300f;
        foreach (var read in reads.Skip(3 * Rate))
        {
            var body = Find(read, weight.Id);
            Assert.InRange(WeightDeflection(body), expected - SettleBand, expected + SettleBand);
            Assert.True(Tilt(body) < .5, $"tilt {Tilt(body)}°");
            Assert.InRange(new Vector2(Origin(body).X - .3f, Origin(body).Z + .2f).Length(), 0, RestingDrift);
            Assert.InRange(Velocity(body).Length(), 0, RestSpeed);
        }
    }

    /// <summary>The rope anchor's tie point, and the 2 kg Weight's tie height above its base (WeightTests' tie geometry).</summary>
    private static readonly Vector3 TetherAnchor = new(0, 6.5f, 0);
    private const float TetherMass = 2f;
    private static readonly float TetherTie = .5771f * (.136734f * TetherMass + .453070f);

    /// <summary>A 2 kg Weight below the rope anchor with its base at <paramref name="baseY"/>, tied by a rope of
    /// <paramref name="ropeLength"/>, over the default bed (body 3).</summary>
    private static WorkshopConstruction Tethered(float baseY, float ropeLength)
    {
        var anchorPart = WorkshopInput.Generic(new("rope_anchor"), new(1), TetherAnchor.X, TetherAnchor.Y, TetherAnchor.Z - .18f, 0, 0, 0, 1);
        var weight = WeightTests.Placed(2, TetherAnchor.X, baseY, TetherAnchor.Z, TetherMass);
        return new(new(2), WorkshopCadenceSettings.Default(), new(anchorPart, weight, Bed(3)),
            Connections: new(new WorkshopConnection(new(1), PortId.Tie, new(2), PortId.Tie, WorkshopConnectionDomain.Rope, ropeLength)));
    }

    // Harvest row 22. Taut: the rope is cut so the hanging Weight's base rests a 2 cm gap above the bed's rest plane, over a bed it
    // would otherwise load: L = anchor height − tie height − rest plane − gap = 6.5 − 0.4193 − 3.2 − 0.02 ≈ 2.861 m, authored taut.
    // The gap is ten times the rope's 2 mm length bound, so a rope within that bound cannot let the base reach the plane. On every tick
    // of 2 s the bed reads undeflected and the rope is no longer than L + 0.002 m. Slack: a 4.5 m rope from a Weight hung 0.5 m below the
    // anchor lets it fall, load the bed and rebound.
    [Fact]
    public async Task ATautTetherHoldsTheWeightAboveTheBedAndASlackOneLetsItRebound()
    {
        var weight = new GpuBodyId(2);
        const float gap = .02f;
        var taut = TetherAnchor.Y - TetherTie - BedY - gap;
        var (_, held, _) = await WorkshopIntegrationHarness.ObserveReset(Tethered(BedY + gap, taut), 2 * Rate, includeBuilt: true);
        output.WriteLine($"taut L {taut:F4}: longest rope {held.Max(read => Assert.Single(read.Presentation.Ropes.ToArray()).CurrentLength):F5}, " +
            $"lowest base {held.Min(read => Origin(Find(read, weight)).Y) - BedY:F4} above the plane");
        Assert.All(held, read => Assert.Equal(0f, Surface(read, new(3))));
        Assert.All(held, read => Assert.True(Assert.Single(read.Presentation.Ropes.ToArray()).CurrentLength <= taut + .002f, "rope within L + 2 mm"));

        var (_, slack, _) = await WorkshopIntegrationHarness.ObserveReset(Tethered(TetherAnchor.Y - .5f - TetherTie, 4.5f), 3 * Rate, includeBuilt: true);
        var contact = FirstContact(slack, read => WeightDeflection(Find(read, weight)));
        output.WriteLine($"slack: peak {contact.PeakDeflection:F4}, leaving vy {Velocity(Find(slack[contact.End], weight)).Y}");
        Assert.True(contact.PeakDeflection > .1, $"peak {contact.PeakDeflection}");
        Assert.True(Velocity(Find(slack[contact.End], weight)).Y > 1f, "rebounds");
    }

    // Rope and bed sharing one load. The 2 kg Weight stands on the rest plane under the anchor (its offsets are its first substep's
    // fall, ½·g·h² ≈ 21 µm), tied by a rope slack by d_r = ½·m·g/k: L = anchor height − (rest plane − d_r) − tie height = 6.5 − 3.2 +
    // 0.0327 − 0.4193 ≈ 2.913 m. It sinks into the bed and the rope tightens at d_r, below the free sag m·g/k, so the rope carries the
    // rest of the weight. Over the last second of 4 s the mean deflection is d_r within the intent's 1.5 cm settle band and the rope is
    // Taut. On every tick the rope is no longer than L + 0.002 m, and the Weight's energy, with the bed store counted on its bearing
    // samples from their committed offsets, never exceeds 1.005 × its start. Control observing the bed's push: a rope slack by 3·m·g/k,
    // more than the undamped drop's overshoot of 2·m·g/k, never tightens, and the bed alone settles at m·g/k within the same band with
    // the rope Slack (the two bands, d_r ± 1.5 cm and m·g/k ± 1.5 cm, do not overlap; a bed that pushed nothing would let the Weight
    // fall to the rope).
    [Fact]
    public async Task ARopeThatTightensOnALoadedBedSharesTheLoadWithoutStretchOrEnergyGain()
    {
        var bed = new GpuBodyId(3);
        var freeSag = TetherMass * G / 300f;
        var length = TetherAnchor.Y - (BedY - freeSag / 2) - TetherTie;
        var construction = Tethered(BedY, length);
        var body = Declared(construction, new(2));
        var run = await RunWithSamples(construction, 4 * Rate);
        double EnergyAt(int tick)
        {
            var read = Find(run.Reads[tick], new(2));
            return Energy(body, read, Stored(body, read, run.Samples[tick], Quaternion.Identity, 300f));
        }
        var e0 = EnergyAt(0);
        var worst = Enumerable.Range(0, run.Reads.Count).Max(tick => EnergyAt(tick) / e0);
        var sag = run.Reads.Skip(3 * Rate).Average(read => Surface(read, bed));
        var longest = run.Reads.Max(read => Assert.Single(read.Presentation.Ropes.ToArray()).CurrentLength);
        output.WriteLine($"L {length:F4}: mean sag {sag:F4} (free {freeSag:F4}, rope tightens at {freeSag / 2:F4}), longest rope {longest:F5}, worst E/E0 {worst:F5}");
        Assert.InRange(sag, freeSag / 2 - SettleBand, freeSag / 2 + SettleBand);
        Assert.All(run.Reads.Skip(3 * Rate), read => Assert.Equal(RopeState.Taut, Assert.Single(read.Presentation.Ropes.ToArray()).State));
        Assert.All(run.Reads, read => Assert.True(Assert.Single(read.Presentation.Ropes.ToArray()).CurrentLength <= length + .002f, "rope within L + 2 mm"));
        Assert.True(worst <= EnergyEnvelope, $"E/E0 reached {worst}");

        var control = await RunWithSamples(Tethered(BedY, TetherAnchor.Y - (BedY - 3 * freeSag) - TetherTie), 4 * Rate);
        var controlSag = control.Reads.Skip(3 * Rate).Average(read => Surface(read, bed));
        output.WriteLine($"control: mean sag {controlSag:F4}");
        Assert.InRange(controlSag, freeSag - SettleBand, freeSag + SettleBand);
        Assert.All(control.Reads.Skip(3 * Rate), read => Assert.Equal(RopeState.Slack, Assert.Single(read.Presentation.Ropes.ToArray()).State));
    }

    // ---------------- Reset, replay and save ----------------

    private static void SameBodies(WorkshopRead expected, WorkshopRead actual)
    {
        // Deterministic replay is the Reset contract: the same committed inputs give the same decoded state on every tick.
        Assert.Equal(expected.Tick, actual.Tick);
        Assert.Equal(expected.Bodies.Count, actual.Bodies.Count);
        for (var i = 0; i < expected.Bodies.Count; i++)
        {
            var (e, a) = (expected.Bodies[i], actual.Bodies[i]);
            Assert.Equal(e.Body.Id, a.Body.Id);
            Assert.Equal(e.Body.Cell, a.Body.Cell);
            Assert.Equal((e.Body.Local.X, e.Body.Local.Y, e.Body.Local.Z), (a.Body.Local.X, a.Body.Local.Y, a.Body.Local.Z));
            Assert.Equal(Rotation(e), Rotation(a));
            Assert.Equal(Velocity(e), Velocity(a));
            Assert.Equal(Spin(e), Spin(a));
        }
        Assert.Equal(expected.Presentation.Surfaces.ToArray(), actual.Presentation.Surfaces.ToArray());
    }

    // AC5: Reset mid-contact (2, 12 and 24 ticks into the load) restores the authored pose at rest exactly, an undeflected bed and a
    // flat bed visual, and a replay repeats the run on every tick. The Basketball is placed resting on the bed, the way a player
    // places it, and the ticks count from the bed's first deflection.
    [Theory]
    [InlineData(2)]
    [InlineData(12)]
    [InlineData(24)]
    public async Task ResetMidContactRestoresTheAuthoredStateAndTheReplayRepeatsTheRun(int ticks)
    {
        var bed = Bed(); var ball = Basketball(2, .2f, BedY + BallRadius, -.1f);
        await using var harness = await WorkshopIntegrationHarness.Building(Bench(bed, ball));
        var built = harness.Committed;
        await harness.Run();
        var first = new List<WorkshopRead>();
        while (Surface(harness.Committed, bed.Id) == 0f)
        {
            Assert.True(first.Count < Rate, "the ball loaded the bed");
            first.AddRange(await harness.Advance(1));
        }
        first.AddRange(await harness.Advance(ticks));
        Assert.True(Surface(harness.Committed, bed.Id) > 0f, "mid-contact");
        Assert.True(BedVisualY(harness, bed.Id) < BedVisualRestY, "the bed visual sags");
        await harness.Reset();
        SameBodies(built, harness.Committed);
        Assert.Equal(Vector3.Zero, Velocity(Find(harness.Committed, ball.Id)));
        Assert.Equal(Vector3.Zero, Spin(Find(harness.Committed, ball.Id)));
        Assert.Equal(0f, Surface(harness.Committed, bed.Id));
        Assert.Equal(BedVisualRestY, BedVisualY(harness, bed.Id));
        await harness.Run();
        var replay = await harness.Advance(first.Count);
        for (var i = 0; i < first.Count; i++) SameBodies(first[i], replay[i]);
    }

    // Save/Load round-trips a tilted Trampoline's pose, rotation, tension 500 and damping 0.4 exactly as its generic record (part id and
    // two declared parameters), and the reloaded construction runs as the saved one.
    [Fact]
    public async Task SaveRoundTripsPoseRotationTensionAndDampingExactly()
    {
        var tilt = Quaternion.Normalize(new Quaternion(.05f, .1f, -.2f, .97f));
        var construction = Bench(Bed(tension: 500f, damping: .4f, rotation: tilt, at: new(.7f, 2.9f, -.3f)), Basketball(2, .7f, 6, -.3f));
        var save = new WorkshopSavedConstruction(construction, new(4));
        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
        Assert.Equal(save, decoded);
        var (expected, actual) = (Assert.IsType<WorkshopGenericInstance>(construction.Instances[0]),
            Assert.IsType<WorkshopGenericInstance>(decoded.Construction.Instances[0]));
        Assert.Equal((expected.Cell, expected.Rotation), (actual.Cell, actual.Rotation));
        Assert.True(F32Bits.Equal(expected.Local, actual.Local));
        foreach (var key in new[] { TensionKey, DampingKey })
            Assert.Equal(BitConverter.SingleToUInt32Bits(expected.Parameters[key]), BitConverter.SingleToUInt32Bits(actual.Parameters[key]));
        var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value;
        Assert.Equal(("trampoline", 2), (record.PartId, record.ParametersLength));
        var (_, run, _) = await WorkshopIntegrationHarness.ObserveReset(construction, 2 * Rate, includeBuilt: true);
        var (_, replay, _) = await WorkshopIntegrationHarness.ObserveReset(decoded.Construction, 2 * Rate, includeBuilt: true);
        Assert.True(run.Max(read => Surface(read, new(1))) > 0f, "the saved bed is loaded in the run");
        for (var i = 0; i < run.Count; i++) SameBodies(run[i], replay[i]);
    }
}
