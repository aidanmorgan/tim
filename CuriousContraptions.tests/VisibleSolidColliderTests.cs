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
/// must be some solid art node (nothing reaches beyond the art). Round hollow parts (engine-hollow-shapes) match their true Tube,
/// Frustum and Torus colliders, and a bend's drawn chords its torus segment. Every art node is classified (<see cref="ArtSpec"/>).</item>
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
    /// <item><paramref name="Hollow"/>: a round hollow part (engine-hollow-shapes) matched one to one with its true hollow collider: an
    /// open <c>CylinderMesh</c> (both caps off) with the Tube (equal radii) or Frustum (tapered) whose outer radii, half-length, centre
    /// and axis are the drawn ones and whose inner radii are the element's mouth bores; a <c>TorusMesh</c> with the full solid Torus of
    /// its drawn centreline and tube;</item>
    /// <item><paramref name="Arc"/>: the straight open cylinders a bend's curved shell is drawn with (Godot has no torus-segment mesh),
    /// matched together with the one partial Torus collider they draw: each chord's radius is the torus's outer radius, its centre lies
    /// in the torus's plane within the chord's sagitta inside the centreline, its axis is tangent there, and the chords together span
    /// the sweep;</item>
    /// <item><paramref name="Decorative"/>: printing flat on a face, a glow, a part inside another solid, or a part a presentation
    /// binding moves, which a static collider cannot follow;</item>
    /// </list></summary>
    /// <remarks><paramref name="Fit"/> widens the dimension agreement for a stated drawing reason (m).</remarks>
    private sealed record ArtSpec(string[] Solid, string[]? Hollow, string[] Arc, string[] Decorative, string? MovingRoot,
        float Fit = 0f);

    /// <summary>One drawn primitive in its body's frame. Extents: a box's half sizes, a sphere's radius, a cylinder's (r, h, r); an open
    /// cylinder's (top radius, half-height, bottom radius) as <see cref="ColliderShapeKind.Tube"/> or
    /// <see cref="ColliderShapeKind.Frustum"/>; a torus mesh's (centreline radius, tube radius, 0) as
    /// <see cref="ColliderShapeKind.Torus"/>.</summary>
    private sealed record Solid(string Path, ColliderShapeKind Kind, NumVector3 Centre, NumQuaternion Rotation, NumVector3 Extents, float MeshError);

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
            ["Visual/Visual_Collar"], [], ["Visual/Visual_Chevron/Arm_1", "Visual/Visual_Chevron/Arm_2"], null),
        [PartId.PoweredGate] = new(
            ["Visual/Visual_Housing", "Visual/Visual_Rail_Start", "Visual/Visual_Rail_End", "Visual/Blade/Visual_Blade",
                "Visual/Visual_Socket", "Visual/Visual_Indicator"],
            ["Visual/Visual_Sleeve", "Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"], [], [], "Visual/Blade"),

        // Every other element package, classified the same way.
        // The seam is a torus on a dynamic body: engine-multi-collider-dynamic.
        [PartId.Basketball] = new(["Visual/Visual_Sphere"], ["Visual/Visual_Stripe"], [], [], null),
        [PartId.BowlingBall] = new(["Visual/Visual_Sphere"], null, [], [], null),
        // The base lies inside the head; the impact ring is scaled by the impact cue. The upper ring is the
        // drawn full torus (engine-hollow-shapes).
        [PartId.PinballBumper] = new(["Visual/BumperHead", "Visual/Visual_Cap"], ["Visual/Visual_Ring2"], [], ["Visual/Visual_Base", "Visual/ImpactRing"], null),
        // The pips: engine-multi-collider-dynamic.
        [PartId.Domino] = new(["Visual/Visual_Body", "Visual/Visual_Pip1", "Visual/Visual_Pip2"], null, [], [], null),
        // The stripes are flush with the faces. The body is drawn at 0.998 of the thickness so the stripes do not z-fight with it; the
        // collider takes the full thickness, 0.001·t thicker per face, at most 2 mm at the 2 m maximum thickness.
        [PartId.Wall] = new(["Visual/Visual_Body"], null, [], ["Visual/Visual_StripeLeft", "Visual/Visual_StripeRight"], null, Fit: .002f),
        // The body is drawn at mass m by a uniform scale 0.136734·m + 0.453070 of its 0.35166413 m radius, and the collider's radius
        // is 0.04808397·m + 0.15932827: the declared constants differ by 0.35166413·(0.136734·m + 0.453070) − (0.04808397·m +
        // 0.15932827), at most 4.0e-6 m at the 8 kg bound (the half-height by 3.2e-6 m).
        // The base, and the bands and eye (tori on a dynamic body): engine-multi-collider-dynamic.
        [new PartId("weight")] = new(["Visual/Visual_Body", "Visual/Visual_Base"], ["Visual/Visual_BandLower", "Visual/Visual_BandUpper", "Visual/Visual_Eye"], [], [], null, Fit: 4e-6f),
        // The halo is a glow.
        [PartId.Receiver] = new(["Visual/Visual_Floor", "Visual/Visual_Left", "Visual/Visual_Right", "Visual/Visual_Back", "Visual/Visual_Front"],
            null, [], ["Visual/Visual_Halo"], null),
        // The studs and the ring torus would be colliders past the fifth, reaching the contact-load row: engine-multi-collider-dynamic.
        [PartId.PressurePlate] = new(["Visual/Visual_Base", "Visual/Visual_Top", "Visual/Visual_Socket_PowerIn", "Visual/Visual_Socket_Supply",
            "Visual/Visual_Socket_ActivationOut", "Visual/Stud_0", "Visual/Stud_1", "Visual/Stud_2", "Visual/Stud_3"], ["Visual/Visual_Ring"], [], [], null),
        // The stripe is printed on the plate; the coil is drawn by the spring's compression.
        [PartId.Springboard] = new(["Visual/Visual_Base", "Visual/Plate/Visual_Plate"], null, [], ["Visual/Plate/Visual_Stripe", "Visual/Coil/*"],
            "Visual/Plate"),
        // engine-hollow-shapes: the pipe family's shells, stubs and collars are its true hollow colliders; a bend's shell chords draw
        // its torus segment.
        [PartId.Pipe] = new([], ["Visual/Visual_Shell", "Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"], [], [], null),
        [PartId.PipeBend45] = new([], ["Visual/Visual_Shell/Visual_Stub_*", "Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"],
            ["Visual/Visual_Shell/Visual_Shell_Seg_*"], [], null),
        [PartId.PipeBend90] = new([], ["Visual/Visual_Shell/Visual_Stub_*", "Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"],
            ["Visual/Visual_Shell/Visual_Shell_Seg_*"], [], null),
        [PartId.Funnel] = new([], ["Visual/Visual_Shell", "Visual/Visual_Stub_*", "Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"], [], [],
            null),
        // Static control labels are flat printing, independently checked in FaceMarkingTests.
        [new PartId("both_gate")] = Logic(["Visual/UpperPath", "Visual/LowerPath"]),
        [new PartId("electrical_nand")] = Logic(["Visual/Pictogram/*", "Visual/TruthDot*"]),
        [new PartId("electrical_nor")] = Logic(["Visual/Pictogram/*", "Visual/TruthDot*"]),
        [new PartId("electrical_or")] = Logic(["Visual/Pictogram/*", "Visual/TruthDot*"]),
        [new PartId("electrical_xor")] = Logic(["Visual/Pictogram/*", "Visual/TruthDot*"]),
        // The remaining-time bar is driven by the timer; its static track is printed.
        [new PartId("hold_timer")] = new(["Visual/Body", "Visual/Foot", "Visual/Face", "Visual/Indicator", "Visual/PortTrigger",
            "Visual/PortInput", "Visual/PortOutput"], null, [], ["Visual/Remaining/*", "Visual/Track"], null),
        // The ActivationOut socket (r 0.075 at (0, −0.72, 0.1)) lies wholly inside the foot (|y + 0.72| ≤ 0.08, z ≤ 0.425).
        [PartId.Counter] = new(["Visual/Housing", "Visual/Foot", "Visual/Face", .. Enumerable.Range(0, 9).Select(i => $"Visual/DotLamp_{i}"),
            "Visual/Visual_Socket_ActivationIn", "Visual/Visual_Socket_PowerIn", "Visual/Visual_Socket_Supply"], null, [],
            ["Visual/Visual_Socket_ActivationOut"], null),
        // The rocker turns with state; the set, reset and output cues are printed.
        [PartId.Latch] = new(["Visual/Housing", "Visual/Foot", "Visual/Face", "Visual/IndicatorLamp", "Visual/Visual_Socket_SetIn",
            "Visual/Visual_Socket_ResetIn", "Visual/Visual_Socket_PowerIn", "Visual/Visual_Socket_Supply", "Visual/Visual_Socket_ActivationOut"],
            null, [], ["Visual/Rocker", "Visual/SetMarker", "Visual/ResetMarker/*", "Visual/OutMarker/*"], null),
        // The pendulum swings with the clock. The recess stands 2 cm proud of the face. The pivot cap: engine-multi-collider-dynamic.
        [PartId.Clock] = new(["Visual/Housing", "Visual/Foot", "Visual/Face", "Visual/PivotCap", "Visual/Visual_Indicator", "Visual/Visual_Socket_PowerIn",
            "Visual/Visual_Socket_ActivationOut", "Visual/Recess"], null, [], ["Visual/PendulumPivot/*"], null),
    };

    private static ArtSpec Logic(string[] printed) => new(["Visual/Body", "Visual/Foot", "Visual/Face", "Visual/FirstLamp", "Visual/SecondLamp",
            "Visual/OutputLamp", "Visual/PortFirst", "Visual/PortSecond", "Visual/PortPower", "Visual/PortOutput"], null, [],
        [.. printed, "Visual/FirstMark", "Visual/SecondMarkLeft", "Visual/SecondMarkRight"], null);

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
            string[] entries = [.. spec.Solid, .. spec.Hollow ?? [], .. spec.Arc, .. spec.Decorative];
            Assert.All(art.Keys, path => Assert.True(entries.Count(e => Names(e, path)) == 1, $"{element}: art '{path}' is not classified once"));
            Assert.All(entries, e => Assert.True(art.Keys.Any(path => Names(e, path)), $"{element}: '{e}' names no art"));

            var colliders = BundleGeometry.Colliders(entry, parameters).ToList();
            var moving = entry.Mechanical?.Sliders.SelectMany(s => s.MovingBody.Colliders).ToList() ?? [];
            var bores = entry.TubeMouths.Select(m => m.BoreRadius).Distinct().ToArray();
            var unmatched = new List<string>();
            foreach (var path in art.Keys.Where(path => (spec.Hollow ?? []).Any(e => Names(e, path))))
            {
                var hollow = art[path] ?? throw new InvalidOperationException($"{element}: hollow art '{path}' is not an open cylinder or a torus.");
                Assert.InRange(hollow.MeshError, 0f, .005f);
                var match = colliders.FindIndex(collider => MatchesHollow(hollow, collider, bores, Tol + spec.Fit));
                if (match < 0) unmatched.Add($"'{path}' ({hollow.Kind}, centre {hollow.Centre}, extents {hollow.Extents})");
                else colliders.RemoveAt(match);
            }
            var chords = art.Keys.Where(path => spec.Arc.Any(e => Names(e, path))).Select(path => art[path]!).ToArray();
            if (chords.Length != 0)
            {
                var arc = colliders.FindIndex(collider => collider.ShapeKind == ColliderShapeKind.Torus && collider.HalfExtents.Z != ColliderDeclaration.FullSweep);
                Assert.True(arc >= 0, $"{element}: no torus segment collider for its drawn arc");
                AssertArc(chords, colliders[arc], bores);
                colliders.RemoveAt(arc);
            }
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
            // A collider proxy (a col_ node: engine-hollow-shapes' hollow proxies are hidden mesh instances) is no art.
            foreach (var mesh in Descendants(root).OfType<MeshInstance3D>().Where(mesh => !mesh.Name.ToString().StartsWith("col_", StringComparison.OrdinalIgnoreCase)))
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
                    // engine-hollow-shapes: an open cylinder (both caps off) is a tube or frustum wall; a torus mesh a full torus.
                    CylinderMesh open when !open.CapTop && !open.CapBottom =>
                        (open.TopRadius == open.BottomRadius ? ColliderShapeKind.Tube : ColliderShapeKind.Frustum,
                            new NumVector3(open.TopRadius * s.X, open.Height * .5f * s.Y, open.BottomRadius * s.X)),
                    TorusMesh torus when MathF.Abs(s.X - s.Y) <= Tol && MathF.Abs(s.Y - s.Z) <= Tol =>
                        (ColliderShapeKind.Torus, new NumVector3((torus.InnerRadius + torus.OuterRadius) * .5f * s.X,
                            (torus.OuterRadius - torus.InnerRadius) * .5f * s.X, 0f)),
                    CylinderMesh cylinder when cylinder.TopRadius == cylinder.BottomRadius =>
                        (ColliderShapeKind.Cylinder, new NumVector3(cylinder.TopRadius * s.X, cylinder.Height * .5f * s.Y, cylinder.TopRadius * s.Z)),
                    _ => null
                };
                // Errors from both circular tessellation directions add: using their
                // maximum alone misses points between both sets of mesh vertices.
                var meshError = mesh.Mesh switch
                {
                    CylinderMesh cylinder => MathF.Max(cylinder.TopRadius, cylinder.BottomRadius) * s.X *
                        (1f - MathF.Cos(MathF.PI / cylinder.RadialSegments)),
                    TorusMesh torus => s.X * (torus.OuterRadius * (1f - MathF.Cos(MathF.PI / torus.Rings)) +
                        (torus.OuterRadius - torus.InnerRadius) * .5f * (1f - MathF.Cos(MathF.PI / torus.RingSegments))),
                    _ => 0f
                };
                result.Add(path, shape is { } primitive ? new(path, primitive.Kind, new(transform.Origin.X, transform.Origin.Y, transform.Origin.Z),
                    new(q.X, q.Y, q.Z, q.W), primitive.Extents, meshError) : null);
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

    /// <summary>A drawn hollow part's true collider (engine-hollow-shapes): the same kind, centre and axis; for an open cylinder its
    /// drawn outer radii and half-length (a frustum turned the drawn way: its −Y end the drawn bottom) with inner radii that are the
    /// element's mouth bores; for a torus mesh its drawn centreline and tube, solid, over the full turn.</summary>
    private static bool MatchesHollow(Solid art, ColliderDescriptor collider, float[] bores, float tolerance)
    {
        if (collider.ShapeKind != art.Kind || !Near(collider.Offset, art.Centre, tolerance)) return false;
        var axis = Axis(collider.Rotation, NumVector3.UnitY);
        var drawn = Axis(art.Rotation, NumVector3.UnitY);
        if (!Parallel(axis, drawn)) return false;
        bool Same(float a, float b) => MathF.Abs(a - b) <= tolerance;
        bool Bore(float radius) => bores.Any(bore => Same(radius, bore));
        var h = collider.HalfExtents;
        return art.Kind switch
        {
            ColliderShapeKind.Tube => Same(collider.Radius, art.Extents.X) && Same(h.Y, art.Extents.Y) && Bore(h.X) && h.Z == 0f,
            ColliderShapeKind.Frustum => NumVector3.Dot(axis, drawn) > 0f && Same(collider.Radius, art.Extents.Z) && Same(h.Z, art.Extents.X) &&
                Same(h.Y, art.Extents.Y) && Bore(collider.Radius - h.X) && Bore(h.Z - h.X),
            ColliderShapeKind.Torus => Same(collider.Radius, art.Extents.X) && Same(h.Y, art.Extents.Y) && h.X == 0f &&
                h.Z == ColliderDeclaration.FullSweep,
            _ => false
        };
    }

    /// <summary>A bend's drawn chords against its torus segment collider (R, bore r_i, wall r_o, sweep θ): each chord is an open
    /// cylinder of the wall's radius r_o, centred in the torus's plane at a distance from its centre between R less the chord's
    /// sagitta, R − √(R² − h²) for its drawn half-length h (3.5 mm for the bends' 0.13 m), and R, within the sweep, with its axis
    /// tangent there; the chords' angular spans (±atan(h / ρ) about each) cover [0, θ] without a gap; and the bore r_i is a mouth's.
    /// So the collider's round wall lies within the chords' sagitta of the drawn polyline everywhere.</summary>
    private static void AssertArc(Solid[] chords, ColliderDescriptor arc, float[] bores)
    {
        var (bend, inner, outer, sweep) = (arc.Radius, arc.HalfExtents.X, arc.HalfExtents.Y, arc.HalfExtents.Z);
        Assert.True(bores.Any(bore => MathF.Abs(inner - bore) <= Tol), $"the arc's bore {inner} is no mouth's");
        var axis = Axis(arc.Rotation, NumVector3.UnitY);
        var (start, quarter) = (Axis(arc.Rotation, NumVector3.UnitX), Axis(arc.Rotation, NumVector3.UnitZ));
        var spans = new List<(double Angle, double Reach)>();
        foreach (var chord in chords)
        {
            Assert.Equal(ColliderShapeKind.Tube, chord.Kind);
            Assert.True(MathF.Abs(chord.Extents.X - outer) <= Tol, $"the chord '{chord.Path}' is drawn at {chord.Extents.X}, not the wall's {outer}");
            var offset = chord.Centre - arc.Offset;
            Assert.True(MathF.Abs(NumVector3.Dot(offset, axis)) <= Tol, $"the chord '{chord.Path}' lies off the arc's plane");
            var rho = offset.Length();
            var half = chord.Extents.Y;
            var sagitta = bend - MathF.Sqrt(bend * bend - half * half);
            Assert.InRange(sagitta + chord.MeshError, 0f, .005f);
            Assert.True(rho <= bend + Tol && rho >= bend - sagitta - Tol,
                $"the chord '{chord.Path}' is centred {rho} from the arc's centre, outside [{bend - sagitta}, {bend}]");
            var tangent = Axis(chord.Rotation, NumVector3.UnitY);
            Assert.True(MathF.Abs(NumVector3.Dot(tangent, axis)) <= 1e-5f && MathF.Abs(NumVector3.Dot(tangent, offset / rho)) <= 1e-5f,
                $"the chord '{chord.Path}' is not tangent to the arc");
            var angle = Math.Atan2(NumVector3.Dot(offset, quarter), NumVector3.Dot(offset, start));
            Assert.True(angle >= 0 && angle <= sweep, $"the chord '{chord.Path}' stands at {angle} rad, outside the {sweep} rad sweep");
            spans.Add((angle, Math.Atan(half / rho)));
        }
        spans.Sort();
        Assert.True(spans[0].Angle - spans[0].Reach <= 1e-6 && spans[^1].Angle + spans[^1].Reach >= sweep - 1e-6,
            "the chords do not reach both ends of the sweep");
        for (var i = 1; i < spans.Count; i++)
            Assert.True(spans[i].Angle - spans[i].Reach <= spans[i - 1].Angle + spans[i - 1].Reach, "the chords leave a gap in the sweep");
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

    /// <summary>A flat top of each newly solid feature; for the hollow parts (engine-hollow-shapes) their drawn outer surfaces: the
    /// detector's and the gate's collar tubes (each turned over so its housing hangs below), the pipe's shell top and a pipe collar's
    /// torus top; the gate's housing, which carries the frame's documented restitution; for the logic family, a face panel, a lamp or
    /// indicator and a socket; for the pressure plate, a socket at the plate's own restitution.</summary>
    public enum Feature
    {
        LampBulb, SwitchButton, DelayHousing, DelayFace, RampSlab, BatteryTerminal, DetectorCollar, GateHousing, LogicFace, LogicLamp,
        CounterLamp, LatchIndicator, LatchSocket, ClockFace, HoldTimerFace, PlateSocket, NorFace, OrFace, XorFace, PipeShell, PipeCollar,
        GateCollar, Bend45Collar, Bend90Collar, FunnelCollar
    }

    /// <summary>On its back: −90° about X, the face up; a part-local point (x, y, z) is then at (x, z, −y) from the origin.</summary>
    private static readonly NumQuaternion Back = NumQuaternion.CreateFromAxisAngle(NumVector3.UnitX, -MathF.PI / 2);

    /// <summary>The element, its pose, the feature's top above the element's origin, the drop's x and z offsets, and the static
    /// restitution its element doc records.</summary>
    private static (PartId Part, NumQuaternion Rotation, double Top, float X, float Z, double Restitution) Drop(Feature feature) => feature switch
    {
        Feature.LampBulb => (PartId.SignalLamp, NumQuaternion.Identity, .22 + .43, 0, 0, 1),       // CAT-035: bulb r 0.43 at y 0.22
        Feature.SwitchButton => (PartId.ImpactSwitch, NumQuaternion.Identity, .06 + .09, 0, 0, 1), // CAT-063: button h 0.18 at y 0.06
        Feature.DelayHousing => (PartId.Delay, NumQuaternion.Identity, .625, 0, 0, 1),             // CAT-022: 1.25 m housing at the origin
        // CAT-022: lying on its back: the r 0.55 face disc's top is 0.32 + 0.0175 = 0.3375, 3.75 cm above the housing's 0.3 face.
        // Dropped 0.4 m off the axis, clear of the hub pip and the hand.
        Feature.DelayFace => (PartId.Delay, Back, .32 + .0175, .4f, 0, 1),
        Feature.RampSlab => (PartId.Ramp, NumQuaternion.Identity, .09, 0, 0, 1),                   // CAT-054: 0.18 m slab
        Feature.BatteryTerminal => (PartId.Battery, NumQuaternion.Identity, .59 + .07, 0, 0, 1),   // CAT-005: terminal h 0.14 at y 0.59
        // CAT-002: turned over about X so the housing hangs below and the collar's top is clear: the drawn outer surface, 0.82 m.
        Feature.DetectorCollar => (PartId.BallDetector, new(1, 0, 0, 0), .82, 0, 0, .15),
        Feature.GateHousing => (PartId.PoweredGate, NumQuaternion.Identity, 1.65 + .325, 0, 0, .1), // CAT-051: housing h 0.65 at y 1.65
        // CAT-013: the Both gate on its back: the 1.45 × 1.05 × 0.04 face panel at z 0.35 tops out at 0.37, 4.5 cm above the body's
        // 0.325 face. Dropped 0.1 m off the axis: resting there (centre 0.71) the ball clears the output lamp (r 0.09 at x 0.48,
        // z 0.42) once 0.3175 = √(0.43² − 0.29²) away horizontally; it is 0.38 away.
        Feature.LogicFace => (new PartId("both_gate"), Back, .37, .1f, 0, 1),
        // CAT-025, CAT-026, CAT-027: the same face panel and drop on the NOR, OR and XOR gates.
        Feature.NorFace => (new PartId("electrical_nor"), Back, .37, .1f, 0, 1),
        Feature.OrFace => (new PartId("electrical_or"), Back, .37, .1f, 0, 1),
        Feature.XorFace => (new PartId("electrical_xor"), Back, .37, .1f, 0, 1),
        // CAT-024: the electrical NAND on its back, the ball dropped onto the top of its output lamp (r 0.09 at x 0.48, z 0.42).
        Feature.LogicLamp => (new PartId("electrical_nand"), Back, .42 + .09, .48f, 0, 1),
        // CAT-020: the Counter on its back, onto its middle dot lamp (r 0.1 at z 0.4). Resting there (centre 0.84) the ball is
        // √(0.36² + 0.44²) = 0.57 from the nearest other lamp, beyond the 0.44 at which it would touch.
        Feature.CounterLamp => (PartId.Counter, Back, .4 + .1, 0, 0, 1),
        // CAT-037: the Latch on its back, onto its indicator lamp (r 0.09 at (0, −0.33, 0.42), so at z 0.33). The ActivationOut socket
        // (r 0.075 at (0, −0.56, 0.38)) is √(0.23² + 0.47²) = 0.52 from the resting centre, beyond the 0.415 at which it would touch.
        Feature.LatchIndicator => (PartId.Latch, Back, .42 + .09, 0, .33f, 1),
        // CAT-037: the upright Latch, onto its SetIn socket (r 0.075 at (−0.45, 0.72, 0)), 17 cm above the housing's 0.625 top.
        Feature.LatchSocket => (PartId.Latch, NumQuaternion.Identity, .72 + .075, -.45f, 0, 1),
        // CAT-017: the Clock on its back: the 1.14 × 1.44 × 0.04 face at z 0.32 tops out at 0.34, 4 cm above the housing. Dropped at
        // x 0.55, beside the recess (0.84 m wide, its top 2 cm higher): resting at 0.68 the ball clears the recess's edge once
        // √(0.34² − 0.32²) = 0.115 away horizontally; it is 0.13 away.
        Feature.ClockFace => (PartId.Clock, Back, .32 + .02, .55f, 0, 1),
        // CAT-033: the Hold timer on its back: the 1.18 × 0.72 × 0.045 face at z 0.35 tops out at 0.3725, 4.75 cm above the body.
        // Dropped at x 0.4: resting there (centre 0.7125) the ball is √(0.4² + 0.22² + 0.3125²) = 0.55 from the indicator (r 0.07 at
        // (0, −0.22, 0.4)), beyond the 0.41 at which it would touch.
        Feature.HoldTimerFace => (new PartId("hold_timer"), Back, .35 + .0225, .4f, 0, 1),
        // CAT-052: the Pressure plate, onto its PowerIn socket (r 0.075 at (−1.18, −0.08, 0)), outside the base's edge at x −1.1. At
        // the socket the ball's centre is √(0.08² + 0.355²) = 0.364 from the base's top edge, beyond its 0.34 radius. The plate's
        // restitution is 0.1 (CAT-052).
        Feature.PlateSocket => (PartId.PressurePlate, NumQuaternion.Identity, -.08 + .075, -1.18f, 0, .1),
        // CAT-048: the default 3.6 m pipe lying along X; its shell's drawn top is the 0.7 m wall, dropped at the middle of its length.
        // The pipe family's documented restitution is 0.15.
        Feature.PipeShell => (PartId.Pipe, NumQuaternion.Identity, .7, 0, 0, .15),
        // CAT-048: the end collar's drawn TorusMesh (inner 0.65, outer 0.78) at x 1.8 tops out at 0.78; its top is a point apex.
        // Bend plane laid flat: its start collar is a vertical torus with apex .78.
        Feature.Bend45Collar => (PartId.PipeBend45, Back, .78, -.91844f, -.182689f, .15),
        Feature.Bend90Collar => (PartId.PipeBend90, Back, .78, -1.697056f, -.702944f, .15),
        Feature.FunnelCollar => (PartId.Funnel, NumQuaternion.Identity, .78, .9f, 0, .15),
        Feature.PipeCollar => (PartId.Pipe, NumQuaternion.Identity, .78, 1.8f, 0, .15),
        // CAT-051: the Powered gate turned over about X (housing and rails below), onto its end collar's drawn 0.78 m tube at x 0.4.
        // Clear of the blade (top at 0.72, x within ±0.06: the ball reaches x 0.06 at 0.78) and the rails (|z| 0.82, below 0.2).
        Feature.GateCollar => (PartId.PoweredGate, new(1, 0, 0, 0), .78, .4f, 0, .1),
        _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, "Unknown feature.")
    };

    /// <summary>The Basketball dropped 0.5 m onto each feature (the pressure plate's socket from <see cref="PlateFall"/>). It stops at the
    /// drawn surface: its centre comes within 5 mm of the top plus its radius, and never sinks deeper than the envelope's temporary
    /// penetration. When the feature is a sphere collider's apex (<see cref="OnSphereApex"/>), which is no stable rest, only the first
    /// impact counts, and the 120 Hz commit can miss the turn by up to v_in·T (2.6 cm): the contact height is where the falling arc
    /// through the last falling read meets the rising arc through the first rising read (both free flight under g). The impact
    /// resolves within one 480 Hz substep h, so the arcs meet at most e/(1 + e)·v_in·h below the contact (2.3 mm at 3.2 m/s, 0.8 mm
    /// for the plate's 7.7 m/s), inside the same 5 mm. Its rebound is the
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
    [InlineData(Feature.LogicFace)]
    [InlineData(Feature.LogicLamp)]
    [InlineData(Feature.CounterLamp)]
    [InlineData(Feature.LatchIndicator)]
    [InlineData(Feature.LatchSocket)]
    [InlineData(Feature.ClockFace)]
    [InlineData(Feature.HoldTimerFace)]
    [InlineData(Feature.PlateSocket)]
    [InlineData(Feature.NorFace)]
    [InlineData(Feature.OrFace)]
    [InlineData(Feature.XorFace)]
    [InlineData(Feature.PipeShell)]
    [InlineData(Feature.PipeCollar)]
    [InlineData(Feature.Bend45Collar)]
    [InlineData(Feature.Bend90Collar)]
    [InlineData(Feature.FunnelCollar)]
    [InlineData(Feature.GateCollar)]
    public async Task ABallDroppedOntoEachSolidFeatureStopsAtItsDrawnSurfaceAndReboundsAtTheProductRestitution(Feature feature)
    {
        var (part, rotation, top, x, z, staticRestitution) = Drop(feature);
        IWorkshopInstance element = part == PartId.Pipe
            ? WorkshopInput.Pipe(Element, 0, Y, 0, rotation.X, rotation.Y, rotation.Z, rotation.W)
            : Place(part, Element, new(0, (float)Y, 0), rotation);
        // A torus's top is a point apex, like a sphere's: no stable rest.
        await AssertDropOnto(element, x, z, top, staticRestitution, feature.ToString(),
            OnSphereApex(part, rotation, x, z, top) || feature is Feature.PipeCollar or Feature.Bend45Collar or Feature.Bend90Collar or Feature.FunnelCollar, feature == Feature.PlateSocket ? PlateFall : .5);
    }

    // At the rim the cap is 5 cm proud of the sphere; at x=.3 the rim's first contact
    // is .65+sqrt(.34²-.05²). A 5 mm visual clearance allowance is one tenth of that
    // exposed feature, so deleting the cap cannot satisfy this test. The 25-degree
    // rotation puts the cap's upper corner above the sphere and upper ring.
    [Theory]
    [InlineData(.2f, 0f)]
    [InlineData(.3f, 0f)]
    [InlineData(.25f, 25f)]
    public async Task TheExposedCapFaceAndRimStopAndRedirectTheBall(float x, float degrees)
    {
        var turn = NumQuaternion.CreateFromAxisAngle(NumVector3.UnitZ, degrees * MathF.PI / 180);
        var corner = NumVector3.Transform(new(x, .65f, 0), turn);
        var surface = degrees == 0 ? .65 + Math.Sqrt(BallRadius * BallRadius - Math.Pow(Math.Max(0, x - .25), 2))
            : corner.Y + BallRadius;
        await using var ring = await EventRingProducerTests.Ring.Running(Bench([
            Place(PartId.PinballBumper, Element, new(0, (float)Y, 0), turn),
            BallAt(new(corner.X, (float)(Y + surface + .2), 0))]));
        await ring.Advance(60);
        var firstRise = ring.Reads.FindIndex(read => Velocity(read).Y > 1);
        Assert.True(firstRise > 0, "the cap redirects the falling ball upwards");
        var beforeRise = ring.Reads.Take(firstRise + 1).ToArray();
        Assert.True(beforeRise.Min(read => Position(read).Y) >= Y + surface - .005,
            "the ball must rebound at the exposed cap, not sink through to the head");
        Assert.Contains(ring.Records, record => record.Kind == SimulationEventKind.Impact &&
            record.Source == Element.Value && record.Magnitude > 0);
    }

    [Fact]
    public async Task ABallOutsideTheBumperSolidsFallsPastWithoutWorkOrPulse()
    {
        // Head .65 + outermost basketball seam .35 = 1 m: 5 cm clear of all solids.
        await using var ring = await EventRingProducerTests.Ring.Running(Bench([
            Place(PartId.PinballBumper, Element, new(0, (float)Y, 0), NumQuaternion.Identity),
            BallAt(new(1.05f, (float)Y + 1.2f, 0))]));
        await ring.Advance(60);
        Assert.True(Position(ring.Reads[^1]).Y < Y, "the ball passes the bumper");
        Assert.All(ring.Reads, read => Assert.Equal(32f, read.ContactWorks[0].RemainingEnergy.Value));
        Assert.DoesNotContain(ring.Records, record => record.Kind == SimulationEventKind.Impact && record.Source == Element.Value);
    }

    /// <summary>The Bumper's upper ring (engine-hollow-shapes): the drawn TorusMesh (inner 0.472, outer 0.508) at y 0.43 is a solid
    /// tube of radius 0.018 about a 0.49 m centreline, whose outer surface stands √(0.49² + 0.43²) + 0.018 = 0.670 m from the head's
    /// centre, 2 cm proud of the 0.65 m head. With the bumper turned atan2(0.49, 0.43) about Z so that the ring's highest point is over
    /// its centre, the Basketball dropped 0.25 m onto it first stops on the ring: its lowest committed centre lies within one tick's
    /// travel (v_in·T) above the ring contact, 0.670 + 0.34 above the head's centre, and no deeper than the envelope's temporary
    /// penetration; on the head alone it would stop 0.0199 m lower, below that penetration. The rebound is the bumper's kick (its
    /// contact work), not a restitution, so only the stop is asserted.</summary>
    [Fact]
    public async Task ABallDroppedOntoTheBumpersUpperRingStopsAtItsDrawnTube()
    {
        var turn = NumQuaternion.CreateFromAxisAngle(NumVector3.UnitZ, MathF.Atan2(.49f, .43f));
        var ringTop = MathF.Sqrt(.49f * .49f + .43f * .43f) + .018;
        var rest = Y + ringTop + BallRadius;
        await using var ring = await EventRingProducerTests.Ring.Running(Bench([Place(PartId.PinballBumper, Element, new(0, (float)Y, 0), turn),
            BallAt(new(0, (float)(rest + .25), 0))]));
        await ring.Advance(60);
        double lowest = double.PositiveInfinity, fastest = 0;
        foreach (var read in ring.Reads)
        {
            var (y, vy) = (Position(read).Y, Velocity(read).Y);
            lowest = Math.Min(lowest, y);
            if (fastest > 1 && vy > 0) break; // the first rebound: the impact is over
            fastest = Math.Max(fastest, -vy);
        }
        Assert.True(lowest >= rest - Penetration && lowest <= rest + fastest * T,
            $"the ball first stopped at {lowest:F4} m, not on the ring's drawn tube at {rest:F4} m");
    }

    /// <summary>The plate's socket returns e = 0.55 · 0.1 = 0.055; the rebound bound's slack is g·T·(1 + e)/v_in, so from 3 m
    /// (v_in = √(2·g·3) = 7.7 m/s) the ratio is held to [0.044, 0.056], which tells 0.055 from the neighbouring 0.1 or 0.0.</summary>
    private const double PlateFall = 3;

    /// <summary>Whether the drop lands on a sphere collider's apex: some declared sphere, posed with the element at the origin, has its
    /// top at the drop point (x, top, z).</summary>
    private static bool OnSphereApex(PartId part, NumQuaternion rotation, float x, float z, double top) =>
        DeclaredPhysics.Colliders(part).Any(collider => collider.ShapeKind == Gpu.ColliderShapeKind.Sphere &&
            (NumVector3.Transform(new(collider.LocalPose.Translation.X, collider.LocalPose.Translation.Y, collider.LocalPose.Translation.Z),
                rotation) + new NumVector3(0, collider.Radius, 0) - new NumVector3(x, (float)top, z)).Length() < 1e-4f);

    /// <summary>The restitution control for every static that keeps the neutral 1.0: the default Wall (3 × 2 × 0.25 m, CAT-066
    /// restitution 1) returns the Basketball at its own 0.55 under the product law, by the same drop and bounds.</summary>
    [Fact]
    public async Task ABallDroppedOntoAStaticWallReboundsAtItsOwnRestitution() =>
        await AssertDropOnto(WorkshopInput.Generic(PartId.Wall, Element, 0, Y, 0, 0, 0, 0, 1, WallFixture.Default), 0, 0,
            WallFixture.Default[new("height")] * .5, 1, "Wall", apex: false, fall: .5);

    private static async Task AssertDropOnto(IWorkshopInstance element, float x, float z, double top, double staticRestitution, string feature,
        bool apex, double fall)
    {
        var rest = Y + top + BallRadius;
        await using var ring = await EventRingProducerTests.Ring.Running(Bench([element, BallAt(new(x, (float)(rest + fall), z))]));
        await ring.Advance(180);
        double vIn = 0, lowest = double.PositiveInfinity, contact = double.NaN; double? vOut = null; var landed = false;
        WorkshopRead? before = null;
        foreach (var read in ring.Reads)
        {
            var (y, vy) = (Position(read).Y, Velocity(read).Y);
            if (!landed && vIn > 1 && y - rest < .05 && vy > -.5 * vIn) landed = true;
            if (!landed && vy < 0) vIn = -vy;
            if (landed) lowest = Math.Min(lowest, y);
            if (landed && vOut is null && vy > 0)
            {
                vOut = vy;
                if (before is { } falling) contact = Meet(Position(falling).Y, Velocity(falling).Y, y, vy);
            }
            before = read;
        }
        Assert.True(landed, $"the ball never landed on the {feature}");
        var stop = apex ? contact : lowest;
        Assert.True(stop >= rest - Penetration, $"the ball sank to {stop:F4} m, below the {feature}'s surface contact {rest:F4} m");
        Assert.True(stop <= rest + .005, $"the ball stopped at {stop:F4} m, above the {feature}'s surface contact {rest:F4} m");
        var e = (double)((float)BallRestitution * (float)staticRestitution);
        Assert.True(vOut is not null, $"the ball did not rebound from the {feature}");
        var ratio = vOut!.Value / vIn;
        var low = e - G * T * (1 + e) / vIn - 2 * BallDrag * T * e - Math.Pow(2, -20);
        var high = e * (1 + G * T / vIn) + Math.Pow(2, -20);
        Assert.True(ratio >= low && ratio <= high, $"the {feature} rebound ratio {ratio:F4} lies outside [{low:F4}, {high:F4}] for e {e:F4}");
    }

    /// <summary>The height where the falling arc through (y_a, v_a) and the rising arc through (y_b, v_b), one commit T later, meet:
    /// with τ after a, y_a + v_a·τ − g·τ²/2 = y_b + v_b·(τ − T) − g·(τ − T)²/2, so τ = (y_b − y_a − v_b·T − g·T²/2)/(v_a − v_b − g·T).</summary>
    private static double Meet(double ya, double va, double yb, double vb)
    {
        var tau = (yb - ya - vb * T - G * T * T / 2) / (va - vb - G * T);
        return ya + va * tau - G * tau * tau / 2;
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

    /// <summary>An upright Both gate (CAT-013) turned so its face looks along −X stands beside a Ramp tilted 5° (downhill +X). The
    /// Basketball released at ramp-local x −1.2 rolls downhill at (5/7)·g·(sin 5° − 0.035) and reaches the gate at about 1.2 m/s. The
    /// gate is placed so that the ball's centre, where it is at ramp-local x 0.7, is level with the middle of the face panel and the
    /// panel's 0.37 front less 0.34 for the radius away. The panel stops it there: its centre comes within 5 mm of that point along x
    /// and never passes it by more than the envelope's temporary penetration. Along the ball's line the lamps (r 0.09 at part-local
    /// x ±0.48) are at least 0.48 off it, beyond the 0.43 at which they would touch; the foot's top (0.67 below the centre) and the
    /// power socket (0.75 below) are beyond its radius. Without the panel it would roll on to the body's 0.325 face, 4.5 cm
    /// further.</summary>
    [Fact]
    public async Task ABallRolledIntoAnUprightLogicPartsFaceIsStoppedAtThePanel()
    {
        var slope = NumQuaternion.CreateFromAxisAngle(NumVector3.UnitZ, -5f * MathF.PI / 180);
        var ramp = new NumVector3(0, (float)Y, 0);
        var contact = ramp + NumVector3.Transform(new(.7f, .09f + (float)BallRadius, 0), slope);
        var facing = NumQuaternion.CreateFromAxisAngle(NumVector3.UnitY, -MathF.PI / 2);
        Assert.True((NumVector3.Transform(NumVector3.UnitZ, facing) + NumVector3.UnitX).Length() < 1e-6f, "the gate's face looks along −X");
        var gate = Place(new PartId("both_gate"), Element, contact + new NumVector3(.37f + (float)BallRadius, 0, 0), facing);
        await using var ring = await EventRingProducerTests.Ring.Running(Bench([Place(PartId.Ramp, Ramp, ramp, slope), gate,
            BallAt(ramp + NumVector3.Transform(new(-1.2f, .09f + (float)BallRadius + .002f, 0), slope))]));
        await ring.Advance(480);
        var farthest = ring.Reads.Max(read =>
        {
            Assert.True(read.Bodies.TryGet(Ball, out var body));
            var q = body.Rotation;
            var rotation = new NumQuaternion(q.X, q.Y, q.Z, q.W);
            // Literal stripe mesh: centreline(.322+.35)/2=.336, tube(.35-.322)/2=.014,
            // axis tilted -35 degrees about local Z. A torus supports R sqrt(1-a_x²)+r along X.
            var axis = NumVector3.Transform(new NumVector3(MathF.Sin(35f * MathF.PI / 180), MathF.Cos(35f * MathF.PI / 180), 0), rotation);
            var support = Math.Max(BallRadius, .336 * Math.Sqrt(Math.Max(0, 1 - (double)axis.X * axis.X)) + .014);
            return Position(read).X + support;
        });
        var panel = contact.X + BallRadius;
        Assert.True(farthest <= panel + Penetration, $"ball surface reached x {farthest:F4}, past panel {panel:F4}");
        Assert.True(farthest >= panel - .005, $"ball surface stopped at x {farthest:F4}, short of panel {panel:F4}");
    }

    /// <summary>The Basketball rolls across a5-degree ramp into its downhill rail. Its real sphere/seam union
    /// remains inside the rail and rests against the drawn slab/rail surfaces within the original0.5mm envelope.</summary>
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
        Assert.True(ring.Reads[^1].Bodies.TryGet(Ball, out var body));
        var q = body.Rotation;
        var rotation = new NumQuaternion(q.X, q.Y, q.Z, q.W);
        var axis = NumVector3.Normalize(NumVector3.Transform(NumVector3.Transform(
            new NumVector3(MathF.Sin(35f * MathF.PI / 180), MathF.Cos(35f * MathF.PI / 180), 0), rotation), NumQuaternion.Inverse(slope)));
        // Literal art: sphere .34; seam centreline .336, tube .014.
        var support = Math.Max(.34, .336 * Math.Sqrt(Math.Max(0, 1 - (double)axis.Y * axis.Y)) + .014);
        var expectedY = .09 + support;
        var dy = .1375 - expectedY;
        double Gap(double separation)
        {
            var minimum = Math.Sqrt(dy * dy + separation * separation) - .34;
            // Independent dense line-distance oracle: all possible seam witnesses
            // lie within±.35 of the centre. Sampling±.36 with65536 intervals has
            // <=5.5µm distance error by the signed distance's unit Lipschitz bound.
            for (var i = 0; i <= 65536; i++)
            {
                var x = -.36 + .72 * i / 65536;
                var axial = x * axis.X + dy * axis.Y + separation * axis.Z;
                var radial = Math.Sqrt(Math.Max(0, x * x + dy * dy + separation * separation - axial * axial));
                minimum = Math.Min(minimum, Math.Sqrt((radial - .336) * (radial - .336) + axial * axial) - .014);
            }
            return minimum;
        }
        var low = 0.0; var high = .36;
        for (var i = 0; i < 32; i++)
        {
            var middle = (low + high) / 2;
            if (Gap(middle) < 0) low = middle; else high = middle;
        }
        var edge = innerFace - (low + high) / 2;
        var farthest = local.Max(p => (double)p.Z);
        Assert.True(farthest < innerFace, $"the ball's centre reached ramp-local z {farthest:F4}, over the rail's inner face {innerFace:F4}");
        var (y, z) = ((double)local[^1].Y, (double)local[^1].Z);
        Assert.True(Math.Abs(z - edge) <= resting && Math.Abs(y - expectedY) <= resting,
            $"the ball rests at ramp-local (y {y:F4}, z {z:F4}), not against the rail's edge at (y {expectedY:F4}, z {edge:F4})");
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
