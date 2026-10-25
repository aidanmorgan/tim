using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using Godot;
using ColliderShapeKind = CuriousContraptions.Abstractions.ColliderShapeKind;
using NumQuaternion = System.Numerics.Quaternion;
using NumVector3 = System.Numerics.Vector3;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story elements-visible-solid (owner, 25 Oct 2026, "visible = solid"): every part a player sees as solid collides with its true
/// shape. Two kinds of proof:
/// <list type="bullet">
/// <item>Collider against art, one case per element package: the scene's art is read through Godot (independent of the bundle's
/// text extractor) and every solid art node must have a collider of its kind, centre, orientation and dimensions, and every collider
/// must be some solid art node (nothing reaches beyond the art). Ring-shaped parts check their 16 boxes against the drawn outer
/// radius and the bore; the beyond-art check of their round fit belongs to engine-hollow-shapes. Every art node is classified
/// (<see cref="ArtSpec"/>).</item>
/// <item>Ball drops and rolls on the production simulation (<see cref="EventRingProducerTests.Ring"/>), on the features named in
/// <see cref="Feature"/>, a switch terminal, the delay housing's face, the ramp's rail and a Wall: each stops the ball at its drawn
/// surface, and the drops return the product restitution of the documented materials. A ball that passes where the old switch box
/// reached beyond the button touches nothing and fires nothing, and a fast ball goes over a rail, never through it.</item>
/// </list>
/// </summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class VisibleSolidColliderTests
{
    // ---------- collider against art ----------

    /// <summary>Position and dimension agreement (m). The compared values are at most 2 m; the f32 spacing there is 2.4e-7 m. Each
    /// reader parses the scene once into f32, composes at most three transforms and evaluates one parameter product, so the two
    /// sides differ by at most about eight roundings: 2e-6 m.</summary>
    private const float Tol = 2e-6f;
    /// <summary>Axis agreement, 1 − |cos θ| between matched axes. A turn θ moves a point at the smallest compared half-extent L
    /// (the 2.6 mm minimum-width rail) by L·θ; holding that within <see cref="Tol"/> gives θ ≈ Tol / L = 7.6e-4 rad, and
    /// 1 − cos θ ≈ θ² / 2 = 2.9e-7.</summary>
    private const float AxisTol = Tol / .0026f * (Tol / .0026f) / 2;

    /// <summary>One element's art, every mesh classified (an entry ending "/*" names a node's whole subtree):
    /// <list type="bullet">
    /// <item><paramref name="Solid"/>: matched one to one with the colliders of the body that owns it;</item>
    /// <item><paramref name="Ring"/>: a hollow collar drawn as a tube, checked as a ring of boxes;</item>
    /// <item><paramref name="Pending"/>: a round hollow part deferred to engine-hollow-shapes. When <paramref name="Approximated"/>, the
    /// element's colliders are that slice's box approximation of it and are not matched;</item>
    /// <item><paramref name="Decorative"/>: printing flat on a face, a glow, a part inside another solid, or a part a presentation
    /// binding moves, which a static collider cannot follow;</item>
    /// <item><paramref name="Uncollided"/>: a visible part standing proud of the body with no collider, on an element outside the
    /// elements-visible-solid list. Making these solid is an open item, listed here so none is hidden.</item>
    /// </list></summary>
    /// <remarks><paramref name="Fit"/> widens the dimension agreement for a stated drawing reason (m).</remarks>
    private sealed record ArtSpec(string[] Solid, string? Ring, string[] Pending, string[] Decorative, string? MovingRoot,
        string[]? Uncollided = null, bool Approximated = false, float Fit = 0f);

    private sealed record Solid(string Path, ColliderShapeKind Kind, NumVector3 Centre, NumQuaternion Rotation, NumVector3 Extents);

    private static readonly Dictionary<PartId, ArtSpec> Specs = new()
    {
        [PartId.SignalLamp] = new(["Visual/Visual_Base", "Visual/Visual_Stem", "Visual/Bulb"], null, [], [], null),
        [PartId.ImpactSwitch] = new(["Visual/Visual_Base", "Visual/Button", "Visual/Visual_Terminal1", "Visual/Visual_Terminal2",
            "Visual/Visual_Socket"], null, [], [], null),
        // The countdown hand turns with the timer (a presentation binding); a static collider cannot follow it.
        [PartId.Delay] = new(["Visual/Visual_Body", "Visual/Visual_Face", "Visual/Visual_Base", "Visual/Visual_CenterPip",
            "Visual/Indicator", "Visual/Visual_PortIn", "Visual/Visual_PortOut"], null, [], ["Visual/CountdownHand/Visual_HandMesh"], null),
        [PartId.Ramp] = new(["Visual/Visual_Body", "Visual/Visual_RailA", "Visual/Visual_RailB"], null, [], [], null),
        // The four charge marks and the plus symbol are the gauge and label printed on the body.
        [PartId.Battery] = new(["Visual/Visual_Body", "Visual/Visual_Stripe", "Visual/Terminal"], null, [],
            ["Visual/Mark_0", "Visual/Mark_1", "Visual/Mark_2", "Visual/Mark_3", "Visual/Visual_PlusH", "Visual/Visual_PlusV"], null),
        // The chevron is the arrow printed on the housing.
        [PartId.BallDetector] = new(["Visual/Visual_Housing", "Visual/Visual_Socket", "Visual/Visual_Indicator"],
            "Visual/Visual_Collar", [], ["Visual/Visual_Chevron/Arm_1", "Visual/Visual_Chevron/Arm_2"], null),
        // The 0.78 m collars need a ring kind: a box ring for them would be a new box approximation of a round hollow part, which
        // this slice may not add. Deferred to engine-hollow-shapes (coordinator, 25 Oct 2026), which makes them solid.
        [PartId.PoweredGate] = new(
            ["Visual/Visual_Housing", "Visual/Visual_Rail_Start", "Visual/Visual_Rail_End", "Visual/Blade/Visual_Blade",
                "Visual/Visual_Socket", "Visual/Visual_Indicator"],
            "Visual/Visual_Sleeve", ["Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"], [], "Visual/Blade"),

        // Every other element package, classified the same way.
        [PartId.Basketball] = new(["Visual/Visual_Sphere"], null, [], [], null, Uncollided: ["Visual/Visual_Stripe"]),
        [PartId.BowlingBall] = new(["Visual/Visual_Sphere"], null, [], [], null),
        // The base lies inside the head; the impact ring is scaled by the impact cue.
        [PartId.PinballBumper] = new(["Visual/BumperHead"], null, [], ["Visual/Visual_Base", "Visual/ImpactRing"], null,
            Uncollided: ["Visual/Visual_Cap", "Visual/Visual_Ring2"]),
        [PartId.Domino] = new(["Visual/Visual_Body"], null, [], [], null, Uncollided: ["Visual/Visual_Pip1", "Visual/Visual_Pip2"]),
        // The stripes are flush with the faces. The body is drawn at 0.998 of the thickness so the stripes do not z-fight with it; the
        // collider takes the full thickness, 0.001·t thicker per face, at most 2 mm at the 2 m maximum thickness.
        [PartId.Wall] = new(["Visual/Visual_Body"], null, [], ["Visual/Visual_StripeLeft", "Visual/Visual_StripeRight"], null, Fit: .002f),
        // The body is drawn at mass m by a uniform scale 0.136734·m + 0.453070 of its 0.35166413 m radius, and the collider's radius
        // is 0.04808397·m + 0.15932827: the declared constants differ by 0.35166413·(0.136734·m + 0.453070) − (0.04808397·m +
        // 0.15932827), at most 4.0e-6 m at the 8 kg bound (the half-height by 3.2e-6 m).
        [new PartId("weight")] = new(["Visual/Visual_Body"], null, [], [], null,
            Uncollided: ["Visual/Visual_Base", "Visual/Visual_BandLower", "Visual/Visual_BandUpper", "Visual/Visual_Eye"], Fit: 4e-6f),
        // The halo is a glow.
        [PartId.Receiver] = new(["Visual/Visual_Floor", "Visual/Visual_Left", "Visual/Visual_Right", "Visual/Visual_Back", "Visual/Visual_Front"],
            null, [], ["Visual/Visual_Halo"], null),
        [PartId.PressurePlate] = new(["Visual/Visual_Base", "Visual/Visual_Top"], null, [], [], null,
            Uncollided: ["Visual/Stud_*", "Visual/Visual_Socket_*", "Visual/Visual_Ring"]),
        // The stripe is printed on the plate; the coil is drawn by the spring's compression.
        [PartId.Springboard] = new(["Visual/Visual_Base", "Visual/Plate/Visual_Plate"], null, [], ["Visual/Plate/Visual_Stripe", "Visual/Coil/*"],
            "Visual/Plate"),
        [PartId.Pipe] = new([], null, ["Visual/Visual_Shell", "Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"], [], null, Approximated: true),
        [PartId.PipeBend45] = new([], null, ["Visual/Visual_Shell/*", "Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"], [], null,
            Approximated: true),
        [PartId.PipeBend90] = new([], null, ["Visual/Visual_Shell/*", "Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"], [], null,
            Approximated: true),
        [PartId.Funnel] = new([], null, ["Visual/Visual_Shell", "Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"], [], null,
            Approximated: true),
        // The logic parts: the pictogram, truth dots, marks and paths are printed on the face.
        [new PartId("both_gate")] = Logic(["Visual/UpperPath", "Visual/LowerPath"]),
        [new PartId("electrical_nand")] = Logic(["Visual/Pictogram/*", "Visual/TruthDot*"]),
        [new PartId("electrical_nor")] = Logic(["Visual/Pictogram/*", "Visual/TruthDot*"]),
        [new PartId("electrical_or")] = Logic(["Visual/Pictogram/*", "Visual/TruthDot*"]),
        [new PartId("electrical_xor")] = Logic(["Visual/Pictogram/*", "Visual/TruthDot*"]),
        // The track is printed on the face; the remaining-time bar is driven by the timer.
        [new PartId("hold_timer")] = new(["Visual/Body", "Visual/Foot"], null, [], ["Visual/Track", "Visual/Remaining/*"], null,
            Uncollided: ["Visual/Face", "Visual/Indicator", "Visual/Port*"]),
        [PartId.Counter] = new(["Visual/Housing", "Visual/Foot"], null, [], [], null,
            Uncollided: ["Visual/Face", "Visual/DotLamp_*", "Visual/Visual_Socket_*"]),
        // The rocker turns with the latch's state; the set, reset and out markers are printed on the face.
        [PartId.Latch] = new(["Visual/Housing", "Visual/Foot"], null, [], ["Visual/Rocker", "Visual/SetMarker", "Visual/ResetMarker/*",
            "Visual/OutMarker/*"], null, Uncollided: ["Visual/Face", "Visual/IndicatorLamp", "Visual/Visual_Socket_*"]),
        // The recess is set into the face; the pendulum swings with the clock.
        [PartId.Clock] = new(["Visual/Housing", "Visual/Foot"], null, [], ["Visual/Recess", "Visual/PendulumPivot/*"], null,
            Uncollided: ["Visual/Face", "Visual/PivotCap", "Visual/Visual_Indicator", "Visual/Visual_Socket_*"]),
    };

    private static ArtSpec Logic(string[] printed) => new(["Visual/Body", "Visual/Foot"], null, [],
        [.. printed, "Visual/FirstMark", "Visual/SecondMarkLeft", "Visual/SecondMarkRight"], null,
        Uncollided: ["Visual/Face", "Visual/FirstLamp", "Visual/SecondLamp", "Visual/OutputLamp", "Visual/Port*"]);

    /// <summary>Whether an art path is named by a classification entry: exactly, or by its prefix when the entry ends with "*".</summary>
    private static bool Names(string entry, string path) =>
        entry.EndsWith('*') ? path.StartsWith(entry[..^1], StringComparison.Ordinal) : path == entry;

    public static TheoryData<PartId> Listed => new(Specs.Keys);

    /// <summary>The parameter values the art and colliders are compared at: the defaults, then each collider-bound parameter alone at
    /// its declared minimum and at its maximum (the others at their defaults), so mixed bounds are covered.</summary>
    private static IEnumerable<IReadOnlyDictionary<string, float>> ParameterSets(BundleEntry entry)
    {
        yield return BundleGeometry.Parameters(entry);
        foreach (var parameter in entry.Metadata.Parameters.Where(p => entry.ColliderBindings.Any(b => b.Parameter == p.Key)))
            foreach (var bound in new[] { parameter.Minimum, parameter.Maximum })
                yield return BundleGeometry.Parameters(entry, new Dictionary<string, float> { [parameter.Key] = bound });
    }

    [Theory]
    [MemberData(nameof(Listed))]
    public void EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt(PartId element)
    {
        var spec = Specs[element];
        var entry = TestBundle.Entry(element);
        foreach (var parameters in ParameterSets(entry))
        {
            var art = Art(entry, spec.MovingRoot, parameters);
            string[] entries = [.. spec.Solid, .. spec.Pending, .. spec.Decorative, .. spec.Uncollided ?? [], .. spec.Ring is { } r ? new[] { r } : []];
            Assert.All(art.Keys, path => Assert.True(entries.Count(e => Names(e, path)) == 1, $"{element}: art '{path}' is not classified once"));
            Assert.All(entries, e => Assert.True(art.Keys.Any(path => Names(e, path)), $"{element}: '{e}' names no art"));

            var colliders = BundleGeometry.Colliders(entry, parameters).ToList();
            var moving = entry.Mechanical?.Sliders.SelectMany(s => s.MovingBody.Colliders).ToList() ?? [];
            if (spec.Ring is { } ring)
            {
                var rings = RingBoxes(art[ring]!, colliders);
                AssertRing(art[ring]!, rings, entry.TubeMouths.Select(m => m.BoreRadius).Distinct().Single());
                colliders.RemoveAll(rings.Contains);
            }
            var unmatched = new List<string>();
            foreach (var path in spec.Solid)
            {
                var solid = art[path] ?? throw new InvalidOperationException($"{element}: solid art '{path}' is not a box, full sphere or straight cylinder.");
                var onMoving = spec.MovingRoot is { } root && path.StartsWith(root + "/", StringComparison.Ordinal);
                var pool = onMoving ? moving : colliders;
                var match = pool.FindIndex(collider => Matches(solid, collider, Tol + spec.Fit));
                if (match < 0) unmatched.Add($"'{path}' ({solid.Kind}, centre {solid.Centre}, half {solid.Extents})");
                else pool.RemoveAt(match);
            }
            Assert.True(unmatched.Count == 0, $"{element} at {Describe(parameters)}: solid art without a matching collider: " +
                string.Join("; ", unmatched) + "; colliders left: " +
                string.Join("; ", colliders.Concat(moving).Select(c => $"{c.ShapeKind} at {c.Offset} half {c.HalfExtents}")));
            if (spec.Approximated) continue;
            Assert.True(colliders.Count == 0 && moving.Count == 0, $"{element} at {Describe(parameters)}: collider(s) reach beyond the art: " +
                string.Join("; ", colliders.Concat(moving).Select(c => $"{c.ShapeKind} at {c.Offset} half {c.HalfExtents}")));
        }
    }

    private static string Describe(IReadOnlyDictionary<string, float> parameters) =>
        parameters.Count == 0 ? "no parameters" : string.Join(", ", parameters.Select(p => $"{p.Key} {p.Value}"));

    /// <summary>Every mesh of the element's scene in the frame of the body that owns it, with the declared presentation parameter
    /// scales and offsets applied to their nodes (for example the Ramp's Visual node scales X by length / 3 and Z by width / 1.3). A
    /// mesh that is not a box, a full sphere or a straight cylinder has no primitive shape (null).</summary>
    private static Dictionary<string, Solid?> Art(BundleEntry entry, string? movingRoot, IReadOnlyDictionary<string, float> parameters)
    {
        var root = ResourceLoader.Load<PackedScene>(entry.Metadata.ScenePath).Instantiate<Node3D>();
        try
        {
            var result = new Dictionary<string, Solid?>(StringComparer.Ordinal);
            foreach (var mesh in Descendants(root).OfType<MeshInstance3D>())
            {
                var path = root.GetPathTo(mesh).ToString();
                var transform = Transform3D.Identity;
                for (Node node = mesh; node != root; node = node.GetParent())
                {
                    var nodePath = root.GetPathTo(node).ToString();
                    if (movingRoot == nodePath) break;
                    var local = ((Node3D)node).Transform;
                    var (scale, origin) = (new Godot.Vector3(1, 1, 1), local.Origin);
                    foreach (var binding in entry.Presentation.Parameters.Where(b => b.Node == nodePath))
                    {
                        var value = binding.Multiplier * parameters[binding.Parameter] + binding.Offset;
                        (scale, origin) = binding.Property switch
                        {
                            PresentationProperty.UniformScale => (new Godot.Vector3(value, value, value), origin),
                            PresentationProperty.ScaleX => (scale with { X = value }, origin),
                            PresentationProperty.ScaleY => (scale with { Y = value }, origin),
                            PresentationProperty.ScaleZ => (scale with { Z = value }, origin),
                            PresentationProperty.LocalX => (scale, origin with { X = value }),
                            PresentationProperty.LocalY => (scale, origin with { Y = value }),
                            PresentationProperty.LocalZ => (scale, origin with { Z = value }),
                            _ => throw new InvalidOperationException($"Unexpected presentation parameter {binding.Property} on {nodePath}.")
                        };
                    }
                    local = new Transform3D(local.Basis * Basis.FromScale(scale), origin);
                    transform = local * transform;
                }
                var s = transform.Basis.Scale;
                var q = transform.Basis.GetRotationQuaternion();
                (ColliderShapeKind Kind, NumVector3 Extents)? shape = mesh.Mesh switch
                {
                    BoxMesh box => (ColliderShapeKind.Box, new NumVector3(box.Size.X * s.X, box.Size.Y * s.Y, box.Size.Z * s.Z) * .5f),
                    SphereMesh sphere when sphere.Height == 2 * sphere.Radius && s.X == s.Y && s.Y == s.Z =>
                        (ColliderShapeKind.Sphere, new NumVector3(sphere.Radius * s.X)),
                    CylinderMesh cylinder when cylinder.TopRadius == cylinder.BottomRadius =>
                        (ColliderShapeKind.Cylinder, new NumVector3(cylinder.TopRadius * s.X, cylinder.Height * .5f * s.Y, cylinder.TopRadius * s.Z)),
                    _ => null
                };
                result.Add(path, shape is { } primitive ? new(path, primitive.Kind, new(transform.Origin.X, transform.Origin.Y, transform.Origin.Z),
                    new(q.X, q.Y, q.Z, q.W), primitive.Extents) : null);
            }
            return result;
        }
        finally
        {
            root.Free();
        }
    }

    private static IEnumerable<Node> Descendants(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            yield return child;
            foreach (var below in Descendants(child)) yield return below;
        }
    }

    private static bool Near(NumVector3 a, NumVector3 b, float tolerance = Tol) =>
        MathF.Abs(a.X - b.X) <= tolerance && MathF.Abs(a.Y - b.Y) <= tolerance && MathF.Abs(a.Z - b.Z) <= tolerance;
    private static bool Parallel(NumVector3 a, NumVector3 b) => MathF.Abs(NumVector3.Dot(a, b)) >= 1f - AxisTol;
    private static NumVector3 Axis(NumQuaternion rotation, NumVector3 axis) => NumVector3.Transform(axis, rotation);

    /// <summary>Same kind, centre and dimensions, and the same axes: all three for a box, the axis for a cylinder, none for a
    /// sphere.</summary>
    private static bool Matches(Solid art, ColliderDescriptor collider, float tolerance)
    {
        if (collider.ShapeKind != art.Kind || !Near(collider.Offset, art.Centre, tolerance) || !Near(collider.HalfExtents, art.Extents, tolerance))
            return false;
        return art.Kind switch
        {
            ColliderShapeKind.Sphere => true,
            ColliderShapeKind.Cylinder => Parallel(Axis(collider.Rotation, NumVector3.UnitY), Axis(art.Rotation, NumVector3.UnitY)),
            _ => new[] { NumVector3.UnitX, NumVector3.UnitY, NumVector3.UnitZ }
                .All(axis => Parallel(Axis(collider.Rotation, axis), Axis(art.Rotation, axis)))
        };
    }

    /// <summary>The boxes of a ring: centred on the ring's mid-plane inside its drawn radius, with their local Y radial from the
    /// ring's axis.</summary>
    private static List<ColliderDescriptor> RingBoxes(Solid ring, IReadOnlyList<ColliderDescriptor> colliders)
    {
        var axis = Axis(ring.Rotation, NumVector3.UnitY);
        return colliders.Where(c =>
        {
            if (c.ShapeKind != ColliderShapeKind.Box) return false;
            var offset = c.Offset - ring.Centre;
            var along = NumVector3.Dot(offset, axis);
            var radial = offset - along * axis;
            var distance = radial.Length();
            return MathF.Abs(along) <= Tol && distance > ring.Extents.X * .5f && distance < ring.Extents.X &&
                Parallel(Axis(c.Rotation, NumVector3.UnitY), radial / distance);
        }).ToList();
    }

    /// <summary>A ring of n boxes is solid from the bore to the drawn outer radius R: each box's inner face lies at the bore and its
    /// outer face at R, it spans the drawn length, and its tangential half is R·tan(π/n), so neighbouring outer faces just meet. The
    /// scenes write that half rounded up to f32; the bounds allow <see cref="Tol"/> either side for the f32 evaluation of R·tan(π/n).
    /// The n boxes stand at the n equal angles about the first, each angle taken in (−π, π]. A box's corner then reaches R / cos(π/n),
    /// beyond the drawn surface; that round fit is the beyond-art check engine-hollow-shapes owns.</summary>
    private static void AssertRing(Solid ring, List<ColliderDescriptor> boxes, float bore)
    {
        const int n = 16;
        Assert.Equal(n, boxes.Count);
        var axis = Axis(ring.Rotation, NumVector3.UnitY);
        var outer = ring.Extents.X;
        var reference = NumVector3.Normalize(boxes[0].Offset - ring.Centre);
        var angles = new List<double>();
        foreach (var box in boxes)
        {
            var radial = box.Offset - ring.Centre;
            var distance = radial.Length();
            Assert.True(MathF.Abs(distance - box.HalfExtents.Y - bore) <= Tol,
                $"a ring box's inner face is at {distance - box.HalfExtents.Y}, not the bore {bore}");
            Assert.True(MathF.Abs(distance + box.HalfExtents.Y - outer) <= Tol,
                $"a ring box's outer face is at {distance + box.HalfExtents.Y}, not the drawn {outer}");
            Assert.True(Parallel(Axis(box.Rotation, NumVector3.UnitX), axis), "a ring box's length is not along the ring's axis");
            Assert.True(MathF.Abs(box.HalfExtents.X - ring.Extents.Y) <= Tol,
                $"a ring box's half-length {box.HalfExtents.X} is not the drawn {ring.Extents.Y}");
            var closing = outer * MathF.Tan(MathF.PI / n);
            Assert.True(box.HalfExtents.Z >= closing - Tol,
                $"a ring box's tangential half {box.HalfExtents.Z} leaves its outer face open (needs {closing})");
            Assert.True(box.HalfExtents.Z <= closing + Tol,
                $"a ring box's tangential half {box.HalfExtents.Z} overhangs the closed polygon's {closing} by more than {Tol}");
            var unit = radial / distance;
            angles.Add(Math.Atan2(NumVector3.Dot(NumVector3.Cross(reference, unit), axis), NumVector3.Dot(reference, unit)));
        }
        // Atan2 gives each angle in (−π, π]; the nearest of the n steps k·2π/n (k from −n/2 + 1 to n/2) must lie within 1e-5 rad, and
        // each step must be taken once.
        var steps = angles.Select(a =>
        {
            var k = (int)Math.Round(a * n / (2 * Math.PI));
            Assert.True(Math.Abs(a - k * 2 * Math.PI / n) <= 1e-5, $"a ring box stands at {a} rad, not on a 2π/{n} step");
            return k == -n / 2 ? n / 2 : k;
        }).Order().ToArray();
        Assert.Equal(Enumerable.Range(-n / 2 + 1, n), steps);
    }

    // ---------- the production simulation ----------

    private const double T = 1.0 / 120, G = 9.81;
    /// <summary>The Basketball (CAT-001): radius 0.34 m, restitution 0.55, linear drag 0.04 1/s.</summary>
    private const double BallRadius = .34, BallRestitution = .55, BallDrag = .04;
    /// <summary>The envelope's temporary visual penetration bound: 5 % of the smaller body's extent, the ball's radius here.</summary>
    private const double Penetration = .05 * BallRadius;
    private const double Y = 3;
    private static readonly GpuBodyId Element = new(1), Lamp = new(2), Ramp = new(3), Ball = new(9);

    private static WorkshopConstruction Bench(IWorkshopInstance[] instances, params WorkshopConnection[] links)
    {
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(instances), Connections: new(links));
        Assert.Equal(SimulationCadence.Hz120, construction.Settings.Simulation); // T is one 120 Hz tick
        return construction;
    }

    private static WorkshopGenericInstance Place(PartId part, GpuBodyId id, NumVector3 at, NumQuaternion rotation,
        InstanceParameterCollection? parameters = null) =>
        WorkshopInput.Generic(part, id, at.X, at.Y, at.Z, rotation.X, rotation.Y, rotation.Z, rotation.W, parameters);
    private static WorkshopGenericInstance BallAt(NumVector3 at) => Place(PartId.Basketball, Ball, at, NumQuaternion.Identity);

    private static NumVector3 Position(WorkshopRead read)
    {
        Assert.True(read.Bodies.TryGet(Ball, out var row));
        return new((float)((row.Body.Cell.X + (double)row.Body.Local.X) / 16), (float)((row.Body.Cell.Y + (double)row.Body.Local.Y) / 16),
            (float)((row.Body.Cell.Z + (double)row.Body.Local.Z) / 16));
    }
    private static NumVector3 Velocity(WorkshopRead read)
    {
        Assert.True(read.Bodies.TryGet(Ball, out var row));
        return new(row.Body.Velocity.X, row.Body.Velocity.Y, row.Body.Velocity.Z);
    }

    /// <summary>A flat top of each newly solid feature; for the ring (the detector collar, turned over) its drawn outer surface; for
    /// the gate, whose collars wait for a ring kind, its housing, which carries the frame's documented restitution.</summary>
    public enum Feature { LampBulb, SwitchButton, DelayHousing, DelayFace, RampSlab, BatteryTerminal, DetectorCollar, GateHousing }

    /// <summary>The element, its pose, the feature's top above the element's origin, the drop's x offset, and the static restitution
    /// its element doc records.</summary>
    private static (PartId Part, NumQuaternion Rotation, double Top, float X, double Restitution) Drop(Feature feature) => feature switch
    {
        Feature.LampBulb => (PartId.SignalLamp, NumQuaternion.Identity, .22 + .43, 0, 1),          // CAT-035: bulb r 0.43 at y 0.22
        Feature.SwitchButton => (PartId.ImpactSwitch, NumQuaternion.Identity, .06 + .09, 0, 1),    // CAT-063: button h 0.18 at y 0.06
        Feature.DelayHousing => (PartId.Delay, NumQuaternion.Identity, .625, 0, 1),                // CAT-022: 1.25 m housing at the origin
        // CAT-022: lying on its back (−90° about X, the face up): the r 0.55 face disc's top is 0.32 + 0.0175 = 0.3375, 3.75 cm
        // above the housing's 0.3 face. Dropped 0.4 m off the axis, clear of the hub pip and the hand.
        Feature.DelayFace => (PartId.Delay, NumQuaternion.CreateFromAxisAngle(NumVector3.UnitX, -MathF.PI / 2), .32 + .0175, .4f, 1),
        Feature.RampSlab => (PartId.Ramp, NumQuaternion.Identity, .09, 0, 1),                      // CAT-054: 0.18 m slab
        Feature.BatteryTerminal => (PartId.Battery, NumQuaternion.Identity, .59 + .07, 0, 1),      // CAT-005: terminal h 0.14 at y 0.59
        // CAT-002: turned over about X so the housing hangs below and the collar's top is clear: the drawn outer surface, 0.82 m.
        Feature.DetectorCollar => (PartId.BallDetector, new(1, 0, 0, 0), .82, 0, .15),
        Feature.GateHousing => (PartId.PoweredGate, NumQuaternion.Identity, 1.65 + .325, 0, .1),   // CAT-051: housing h 0.65 at y 1.65
        _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, "Unknown feature.")
    };

    /// <summary>The Basketball dropped 0.5 m onto each feature. It stops at the drawn surface: its centre comes
    /// within 5 mm of the top plus its radius, and never sinks deeper than the envelope's temporary penetration. Its rebound is the
    /// pair's product restitution e = 0.55 · e_static (the solver's pair law, as in the legacy engine): sampled at the 120 Hz commit,
    /// the first rising speed over the last falling speed lies in [e − g·T·(1 + e)/v_in − 2·c·T·e, e·(1 + g·T/v_in)] (gravity acts
    /// for at most one period around the impact, and drag c for at most two), widened by 2⁻²⁰ for the f32 ratio.</summary>
    [Theory]
    [InlineData(Feature.LampBulb)]
    [InlineData(Feature.SwitchButton)]
    [InlineData(Feature.DelayHousing)]
    [InlineData(Feature.DelayFace)]
    [InlineData(Feature.RampSlab)]
    [InlineData(Feature.BatteryTerminal)]
    [InlineData(Feature.DetectorCollar)]
    [InlineData(Feature.GateHousing)]
    public async Task ABallDroppedOntoEachSolidFeatureStopsAtItsDrawnSurfaceAndReboundsAtTheProductRestitution(Feature feature)
    {
        var (part, rotation, top, x, staticRestitution) = Drop(feature);
        await AssertDropOnto(Place(part, Element, new(0, (float)Y, 0), rotation), x, top, staticRestitution, feature.ToString());
    }

    /// <summary>The restitution control for every static that keeps the neutral 1.0: the default Wall (3 × 2 × 0.25 m, CAT-066
    /// restitution 1) returns the Basketball at its own 0.55 under the product law, by the same drop and bounds.</summary>
    [Fact]
    public async Task ABallDroppedOntoAStaticWallReboundsAtItsOwnRestitution() =>
        await AssertDropOnto(WorkshopInput.Generic(PartId.Wall, Element, 0, Y, 0, 0, 0, 0, 1, WallFixture.Default), 0,
            WallFixture.Default[new("height")] * .5, 1, "Wall");

    private static async Task AssertDropOnto(IWorkshopInstance element, float x, double top, double staticRestitution, string feature)
    {
        var rest = Y + top + BallRadius;
        await using var ring = await EventRingProducerTests.Ring.Running(Bench([element, BallAt(new(x, (float)(rest + .5), 0))]));
        await ring.Advance(120);
        double vIn = 0, lowest = double.PositiveInfinity; double? vOut = null; var landed = false;
        foreach (var read in ring.Reads)
        {
            var (y, vy) = (Position(read).Y, Velocity(read).Y);
            if (!landed && vIn > 1 && y - rest < .05 && vy > -.5 * vIn) landed = true;
            if (!landed && vy < 0) vIn = -vy;
            if (landed) lowest = Math.Min(lowest, y);
            if (landed && vOut is null && vy > 0) vOut = vy;
        }
        Assert.True(landed, $"the ball never landed on the {feature}");
        Assert.True(lowest >= rest - Penetration, $"the ball sank to {lowest:F4} m, below the {feature}'s surface contact {rest:F4} m");
        Assert.True(lowest <= rest + .005, $"the ball stopped at {lowest:F4} m, above the {feature}'s surface contact {rest:F4} m");
        var e = (double)((float)BallRestitution * (float)staticRestitution);
        Assert.True(vOut is not null, $"the ball did not rebound from the {feature}");
        var ratio = vOut!.Value / vIn;
        var low = e - G * T * (1 + e) / vIn - 2 * BallDrag * T * e - Math.Pow(2, -20);
        var high = e * (1 + G * T / vIn) + Math.Pow(2, -20);
        Assert.True(ratio >= low && ratio <= high, $"the {feature} rebound ratio {ratio:F4} lies outside [{low:F4}, {high:F4}] for e {e:F4}");
    }

    /// <summary>
    /// The switch tipped so its button's axis is world +X and turned 45° about that axis (the removed box's diagonal then faces the
    /// fall), with the Basketball falling at x 0.33 m. Seen along the fall, the button covers x ∈ [−0.03, 0.15], |z| ≤ 0.38; the
    /// base x ∈ [−0.275, −0.025]; the removed button box (0.8 × 0.18 × 0.75 at y 0.05) covered x ∈ [−0.04, 0.14] and
    /// |z| ≤ (0.4 + 0.375)/√2 = 0.548. Clearances from the ball's centre line, against r = 0.34:
    /// <list type="bullet">
    /// <item>z 0: the button is 0.18 away, so the ball strikes the top of the button's curved side first (the base is 0.355 away;
    /// the terminal and socket spheres lie lower), and the switch fires;</item>
    /// <item>z 0.75: the button is √(0.18² + 0.37²) = 0.411 away, the base 0.355, the upper terminal sphere (r 0.08, at world
    /// x 0, z 0.41) √(0.33² + 0.34²) − 0.08 = 0.394 and the socket sphere (r 0.09, at z −0.035) 0.76, so the ball touches nothing,
    /// fires nothing and falls past, though the removed box's corner, √(0.19² + 0.202²) = 0.277 away, reached 0.063 m into it.</item>
    /// </list>
    /// A contact would give the falling ball a horizontal velocity; drag alone keeps a zero component exactly zero.
    /// </summary>
    [Theory]
    [InlineData(0f, true)]
    [InlineData(.75f, false)]
    public async Task ABallFallingBesideTheButtonInsideTheOldBoxCornerPassesFreelyAndOnlyTheButtonFiresTheSwitch(float z, bool fires)
    {
        var tip = NumQuaternion.CreateFromAxisAngle(NumVector3.UnitZ, -MathF.PI / 2) * NumQuaternion.CreateFromAxisAngle(NumVector3.UnitY, MathF.PI / 4);
        Assert.True((NumVector3.Transform(NumVector3.UnitY, tip) - NumVector3.UnitX).Length() < 1e-6f, "the tipped button's axis is world +X");
        await using var ring = await EventRingProducerTests.Ring.Running(Bench([
                Place(PartId.ImpactSwitch, Element, new(0, (float)Y, 0), tip),
                Place(PartId.SignalLamp, Lamp, new(6, (float)Y, 0), NumQuaternion.Identity), BallAt(new(.33f, (float)Y + 1, z))],
            new WorkshopConnection(Element, WorkshopSocket.ActivationOut, Lamp, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        await ring.Advance(90);
        // Below the tipped base's lowest corner, (0.55 + 0.5)/√2 = 0.742 m under the origin, less the radius, the ball is past it.
        var past = Y - (.55 + .5) / Math.Sqrt(2) - BallRadius;
        var near = ring.Reads.Where(read => Position(read).Y > past).ToArray();
        var struck = near.Any(read => Velocity(read).X != 0f || Velocity(read).Z != 0f);
        var fired = ring.Records.Any(record => record.Kind == SimulationEventKind.TriggerActivated && record.Source == Element.Value);
        Assert.Equal((fires, fires), (struck, fired));
        if (!fires) Assert.True(Position(ring.Reads[^1]).Y < past, "the ball fell past the switch within the run");
    }

    /// <summary>An impact anywhere on the switch fires it: its contact trigger is the whole body (CAT-063). The Basketball dropped
    /// 0.5 m onto the +X terminal sphere (r 0.08 at (0.58, 0, 0)) of an upright switch meets the terminal first: until it rises
    /// again its centre stays at or above the terminal's top plus its radius, 0.08 + 0.34 above the origin, less the envelope's
    /// penetration (the base's top corner, at (0.55, −0.025), would stop it 0.106 m lower, at −0.025 + √(0.34² − 0.03²)). The switch
    /// fires.</summary>
    [Fact]
    public async Task AnImpactOnASwitchTerminalFiresTheSwitch()
    {
        var rest = Y + .08 + BallRadius;
        await using var ring = await EventRingProducerTests.Ring.Running(Bench([
                Place(PartId.ImpactSwitch, Element, new(0, (float)Y, 0), NumQuaternion.Identity),
                Place(PartId.SignalLamp, Lamp, new(6, (float)Y, 0), NumQuaternion.Identity), BallAt(new(.58f, (float)(rest + .5), 0))],
            new WorkshopConnection(Element, WorkshopSocket.ActivationOut, Lamp, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        await ring.Advance(90);
        double lowest = double.PositiveInfinity, fastest = 0;
        foreach (var read in ring.Reads)
        {
            var (y, vy) = (Position(read).Y, Velocity(read).Y);
            lowest = Math.Min(lowest, y);
            if (fastest > 1 && vy > 0) break; // the first rebound: the impact is over
            fastest = Math.Max(fastest, -vy);
        }
        // Sampled at the 120 Hz commit, the lowest committed centre lies within one tick's travel (fastest · T) above the contact.
        Assert.True(lowest >= rest - Penetration && lowest <= rest + fastest * T,
            $"the ball first stopped at {lowest:F4} m, not on the terminal at {rest:F4} m");
        Assert.Contains(ring.Records, record => record.Kind == SimulationEventKind.TriggerActivated && record.Source == Element.Value);
    }

    /// <summary>The Delay lying on its side on a Ramp tilted 5° (downhill +X along the ramp), its housing's top face turned uphill:
    /// that face is at ramp-local x 0.7 − 0.625 = 0.075, while the base sits beyond the housing on the downhill side. Its lowest
    /// point is a port socket sphere (0.72 + 0.075 below its centre), so it is placed 2 mm above the slab on that. The
    /// Basketball released at ramp-local x −1.2 rolls downhill at (5/7)·g·(sin 5° − 0.035), reaches the face at about 0.8 m/s and is
    /// stopped there: its centre reaches the face less its radius (within 5 mm) and never passes it by more than the envelope's
    /// temporary penetration. Without the housing collider it would roll on until the lower port socket (r 0.075, at ramp-local
    /// x 0.7, 0.263 m below the ball's centre line) stopped it at about x 0.7 − √(0.415² − 0.263²) = 0.38, 0.65 m further.</summary>
    [Fact]
    public async Task ABallRolledAtTheDelayHousingIsStoppedAtItsFace()
    {
        var slope = NumQuaternion.CreateFromAxisAngle(NumVector3.UnitZ, -5f * MathF.PI / 180);
        var ramp = new NumVector3(0, (float)Y, 0);
        NumVector3 World(float x, float y) => ramp + NumVector3.Transform(new(x, y, 0), slope);
        var delay = Place(PartId.Delay, Element, World(.7f, .09f + .72f + .075f + .002f), slope * NumQuaternion.CreateFromAxisAngle(NumVector3.UnitZ, MathF.PI / 2));
        await using var ring = await EventRingProducerTests.Ring.Running(Bench([
            Place(PartId.Ramp, Ramp, ramp, slope), delay, BallAt(World(-1.2f, .09f + (float)BallRadius + .002f))]));
        await ring.Advance(480);
        var face = .7 - .625;
        var farthest = ring.Reads.Max(read => (double)NumVector3.Transform(Position(read) - ramp, NumQuaternion.Inverse(slope)).X);
        Assert.True(farthest <= face - BallRadius + Penetration, $"the ball reached ramp-local x {farthest:F4}, past the housing face {face - BallRadius:F4}");
        Assert.True(farthest >= face - BallRadius - .005, $"the ball stopped at ramp-local x {farthest:F4}, short of the housing face {face - BallRadius:F4}");
    }

    /// <summary>The Ramp (default 3 × 1.3 m) tilted 5° about its length so its +Z rail is downhill. The Basketball released on the
    /// centre line rolls across at (5/7)·g·(sin 5° − 0.035) and meets the rail's top inner edge (0.0475 m above the slab, inner face
    /// at z 0.598 − 0.0275 = 0.5705) at about 0.54 m/s. Climbing over the rail needs (7/10)·v² ≥ g·0.0475, v ≥ 0.82 m/s, so it may
    /// rock up against the edge but never crosses it: its centre stays inside the rail's inner face throughout. It comes to rest
    /// against that edge on the slab, its centre at y 0.09 + 0.34 and z 0.5705 − √(0.34² − 0.2925²) = 0.397, within the envelope's
    /// 0.5 mm resting penetration. Without the rail it would roll off the slab's edge at z 0.65.</summary>
    [Fact]
    public async Task ABallRollingAcrossTheRampIsContainedByItsRail()
    {
        var slope = NumQuaternion.CreateFromAxisAngle(NumVector3.UnitX, 5f * MathF.PI / 180);
        var ramp = new NumVector3(0, (float)Y, 0);
        await using var ring = await EventRingProducerTests.Ring.Running(Bench([Place(PartId.Ramp, Ramp, ramp, slope),
            BallAt(ramp + NumVector3.Transform(new(0, .09f + (float)BallRadius + .002f, 0), slope))]));
        await ring.Advance(480);
        var local = ring.Reads.Select(read => NumVector3.Transform(Position(read) - ramp, NumQuaternion.Inverse(slope))).ToArray();
        const double innerFace = .598 - .0275, resting = .0005;
        var edge = innerFace - Math.Sqrt(BallRadius * BallRadius - .2925 * .2925);
        var farthest = local.Max(p => (double)p.Z);
        Assert.True(farthest < innerFace, $"the ball's centre reached ramp-local z {farthest:F4}, over the rail's inner face {innerFace:F4}");
        var (y, z) = ((double)local[^1].Y, (double)local[^1].Z);
        Assert.True(Math.Abs(z - edge) <= resting && Math.Abs(y - (.09 + BallRadius)) <= resting,
            $"the ball rests at ramp-local (y {y:F4}, z {z:F4}), not against the rail's edge at (y {.09 + BallRadius:F4}, z {edge:F4})");
    }

    /// <summary>A fast crossing of a rail at the Ramp's minimum width (0.125 m: the rail is 0.055 × 0.125 / 1.3 = 5.3 mm deep, under
    /// the envelope's 1 cm tunnelling guarantee) and its maximum (4 m: 0.169 m deep). A chute (a default Ramp tilted 30° about z,
    /// downhill +X, so its rails run along the roll) launches the Basketball along +X at about 4 m/s, some 9 mm per 480 Hz substep,
    /// into a level test Ramp turned so its length runs along z and its near rail's centre is at x 1.6. The test slab's top (2.33 m)
    /// is set so the ball arrives just above it, below the top of the rail, which stands 4.75 cm above the slab; the test checks that
    /// arrival height rather than assuming it. It may go over or bounce back, but never through: whenever its
    /// centre lies within the rail's depth its lowest point is at or above the rail's top, less the envelope's penetration. It must
    /// reach the rail (its centre comes within its radius, plus 5 mm, of the rail's outer face) and arrive low enough to meet it (its
    /// lowest point under the rail's top there, before it crosses).</summary>
    [Theory]
    [InlineData(.125f)]
    [InlineData(4f)]
    public async Task AFastBallCrossingARailAtEitherWidthBoundGoesOverItNeverThrough(float width)
    {
        var chute = NumQuaternion.CreateFromAxisAngle(NumVector3.UnitZ, -MathF.PI / 6);
        var chuteAt = new NumVector3(0, (float)Y, 0);
        const float railX = 1.6f, slabTop = 2.33f;
        var depth = .0275f * width / 1.3f; // the rail's half-depth, as its binding scales it
        var turn = NumQuaternion.CreateFromAxisAngle(NumVector3.UnitY, MathF.PI / 2); // local z → world +x
        var test = Place(PartId.Ramp, Ramp, new(railX + .46f * width, slabTop - .09f, 0), turn,
            new([new(new("length"), 3f), new(new("width"), width)]));
        await using var ring = await EventRingProducerTests.Ring.Running(Bench([Place(PartId.Ramp, new(4), chuteAt, chute), test,
            BallAt(chuteAt + NumVector3.Transform(new(-1.2f, .09f + (float)BallRadius + .002f, 0), chute))]));
        await ring.Advance(240);
        var railTop = slabTop + .0475;
        var path = ring.Reads.Select(Position).ToArray();
        var trace = string.Join(" ", path.Where((_, i) => i % 10 == 0).Select(p => $"({p.X:F3},{p.Y:F3})"));
        Assert.True(path.Max(p => p.X) >= railX - depth - BallRadius - .005, $"the ball never reached the rail: {trace}");
        // It arrives low enough to meet the rail: before it first crosses the rail's mid-plane, within its radius of the rail's outer
        // face, its lowest point is under the rail's top (it cannot simply fly over).
        Assert.True(path.TakeWhile(p => p.X < railX).Any(p => p.X >= railX - depth - BallRadius && p.Y - BallRadius < railTop),
            $"the ball arrived above the {railTop:F4} rail top: {trace}");
        // Each crossing of the rail's mid-plane between two committed ticks, at the height interpolated between them (a ballistic
        // arc departs from its chord by at most g·T²/8 = 0.85 mm over one tick).
        for (var i = 0; i + 1 < path.Length; i++)
        {
            var (a, b) = (path[i], path[i + 1]);
            if (!(a.X < railX && b.X >= railX)) continue;
            var y = a.Y + (railX - a.X) / (b.X - a.X) * (b.Y - a.Y);
            Assert.True(y - BallRadius >= railTop - Penetration - G * T * T / 8, $"the ball crossed the {depth * 2:F4} m rail with its " +
                $"lowest point at {y - BallRadius:F4}, under the rail's top {railTop:F4}: {trace}");
        }
    }
}
