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
/// text extractor) and every solid art node must have a collider of its kind, centre, orientation and dimensions on the body that
/// owns it (the base, or the moving body whose root lies above it, each body's colliders matched only against its own art), and every
/// collider must be some solid art node (nothing reaches beyond the art). Round hollow parts (engine-hollow-shapes), a ring imported as a
/// mesh among them, match their true Tube, Frustum and Torus colliders on the body that carries them, and a bend's drawn chords its torus
/// segment; art that a span binding draws between a moving body and the base matches that body's clipped extrusion collider at the
/// authored pose; art scaled by a gas-radius binding is drawn at the radius its gas store declares. Every art node is classified
/// (<see cref="ArtSpec"/>), and no collider proxy (a <c>col_</c> mesh) is drawn or targeted by a presentation binding.</item>
/// <item>Ball drops and rolls on the production simulation (<see cref="WorkshopIntegrationHarness"/>), on the features named in
/// <see cref="Feature"/>, a switch terminal, the delay housing's face, the ramp's rail and a Wall: each stops the ball at its drawn
/// surface, and the drops return the product restitution of the documented materials where it can be told from zero. A ball that
/// passes where the old switch box reached beyond the button touches nothing and fires nothing, and a fast ball goes over a rail,
/// never through it.</item>
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
    /// <summary>The storage allowance of an imported mesh's positions (m). Godot may store an imported mesh's positions as 16-bit values
    /// across its bounds; the largest bound here is the Windmill guard's 1.63 m, so one step is 1.63 / 65535 ≈ 2.5e-5 m; two steps. It
    /// widens only the match of an imported mesh (<see cref="Solid.Allowance"/>); primitive art keeps <see cref="Tol"/>.</summary>
    private const float ImportedStep = 5e-5f;
    /// <summary>How far apart two stored positions of one drawn circle or end may lie (m): each is within <see cref="ImportedStep"/> of
    /// the true value, so two of them, a vertex and the measured extreme it is compared with, differ by at most twice that. No gameplay
    /// defect lies within it; it is used only to group vertices against a measured extreme, never to compare a reading with its
    /// literal or a collider.</summary>
    private const float ImportedSpread = 2 * ImportedStep;

    /// <summary>One element's art, every mesh classified (an entry ending "/*" names a node's whole subtree):
    /// <list type="bullet">
    /// <item><paramref name="Solid"/>: matched one to one with the colliders of the body that owns it;</item>
    /// <item><paramref name="Hollow"/>: a round hollow part (engine-hollow-shapes) matched one to one with its true hollow collider, on
    /// the body that owns it: an open <c>CylinderMesh</c> (both caps off) with the Tube (equal radii) or Frustum (tapered) whose outer
    /// radii, half-length, centre and axis are the drawn ones and whose inner radii are the element's mouth bores, a tube's inner radius
    /// also the bore its art node declares when that node states its wall (<see cref="Solid.Inner"/>); a <c>TorusMesh</c> with the full
    /// solid Torus of its drawn centreline and tube; an imported ring mesh (<see cref="Annulus"/>) with the Tube of its drawn outer
    /// radius, bore, half-length, centre and axis;</item>
    /// <item><paramref name="Arc"/>: the straight open cylinders a bend's curved shell is drawn with (Godot has no torus-segment mesh),
    /// matched together with the one partial Torus collider they draw: each chord's radius is the torus's outer radius, its centre lies
    /// in the torus's plane within the chord's sagitta inside the centreline, its axis is tangent there, and the chords together span
    /// the sweep;</item>
    /// <item><paramref name="Decorative"/>: printing flat on a face, a glow, a part inside another solid, or a part a presentation
    /// binding moves, which a static collider cannot follow;</item>
    /// <item><paramref name="MovingRoots"/>: the roots of the moving bodies, in the package's scene ownership order (the sliders' bodies,
    /// then the hinges'); the art beneath each root is matched against that body's colliders only, and every other art against the
    /// base's;</item>
    /// <item><paramref name="Spanned"/>: art a presentation span binding draws between a moving body and the base (a pusher's rod),
    /// matched with that body's clipped extrusion collider at the authored pose (<see cref="SpannedCollider"/>);</item>
    /// </list></summary>
    /// <remarks><paramref name="Fit"/> widens the dimension agreement for a stated drawing reason (m).</remarks>
    private sealed record ArtSpec(string[] Solid, string[]? Hollow, string[] Arc, string[] Decorative, string[] MovingRoots,
        float Fit = 0f, string[]? Spanned = null);

    /// <summary>One drawn primitive in its body's frame. Extents: a box's half sizes, a sphere's radius, a cylinder's (r, h, r); an open
    /// cylinder's (top radius, half-height, bottom radius) as <see cref="ColliderShapeKind.Tube"/> or
    /// <see cref="ColliderShapeKind.Frustum"/>; a torus mesh's (centreline radius, tube radius, 0) as
    /// <see cref="ColliderShapeKind.Torus"/>. <paramref name="Inner"/> is the bore an open cylinder of equal radii declares, (radius − wall)
    /// × scale, when its own art node states its <c>wall_thickness</c>; otherwise null. The tube is drawn as one open surface (the
    /// delivered convention of the pipe, the powered gate and the ball detector), so its bore is declared data, not drawn: matching it
    /// with the proxy's bore is a consistency check of the two declarations, like the mouth rule, and the bore's behaviour is proven by
    /// <c>WoundSpringContactTests.RealHeadContactLaunchesLoadsAndGuideRimStopsOversizeEntry</c>. An imported ring's bore is drawn: its
    /// inner wall's radius. <paramref name="Allowance"/> widens this mesh's own match by the storage step of an imported mesh's positions
    /// (<see cref="ImportedStep"/>); it is zero for primitive art.</summary>
    private sealed record Solid(string Path, ColliderShapeKind Kind, NumVector3 Centre, NumQuaternion Rotation, NumVector3 Extents, float MeshError,
        float? Inner, float Allowance);

    private static readonly Dictionary<PartId, ArtSpec> Specs = new()
    {
        [new PartId("lamp")] = new(["Visual/Visual_Base", "Visual/Visual_Stem", "Visual/Bulb"], null, [], [], []),
        [new PartId("switch")] = new(["Visual/Visual_Base", "Visual/Button", "Visual/Visual_Terminal1", "Visual/Visual_Terminal2",
            "Visual/Visual_Socket"], null, [], [], []),
        // The countdown hand turns with the timer (a presentation binding); a static collider cannot follow it.
        [new PartId("delay")] = new(["Visual/Visual_Body", "Visual/Visual_Face", "Visual/Visual_Base", "Visual/Visual_CenterPip",
            "Visual/Indicator", "Visual/Visual_PortIn", "Visual/Visual_PortOut"], null, [], ["Visual/CountdownHand/Visual_HandMesh"], []),
        [new PartId("ramp")] = new(["Visual/Visual_Body", "Visual/Visual_RailA", "Visual/Visual_RailB"], null, [], [], []),
        // The four charge marks (0.02 m deep boxes) and the plus symbol (0.015 m deep bars) stand proud of the band and the body; they are
        // the gauge and the label, decorative until the Battery repair redraws them as flat prints (batch-b.md:77).
        [new PartId("battery")] = new(["Visual/Visual_Body", "Visual/Visual_Stripe", "Visual/Terminal"], null, [],
            ["Visual/Mark_0", "Visual/Mark_1", "Visual/Mark_2", "Visual/Mark_3", "Visual/Visual_PlusH", "Visual/Visual_PlusV"], []),
        // The chevron is the arrow printed on the housing.
        [new PartId("ball_detector")] = new(["Visual/Visual_Housing", "Visual/Visual_Socket", "Visual/Visual_Indicator"],
            ["Visual/Visual_Collar"], [], ["Visual/Visual_Chevron/Arm_1", "Visual/Visual_Chevron/Arm_2"], []),
        [new PartId("powered_gate")] = new(
            ["Visual/Visual_Housing", "Visual/Visual_Rail_Start", "Visual/Visual_Rail_End", "Visual/Blade/Visual_Blade",
                "Visual/Visual_Socket", "Visual/Visual_Indicator"],
            ["Visual/Visual_Sleeve", "Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"], [], [], ["Visual/Blade"]),

        // CAT-042: the rotor turns on its hinge (its own body); the gold index is printed flush on the rotor face.
        [new PartId("motor")] = new(["Visual/Visual_Base", "Visual/Visual_Body", "Visual/Visual_Housing", "Visual/Visual_Stud", "Visual/Lamp",
            "Visual/Rotor/Visual_Rotor"], null, [], ["Visual/Rotor/Visual_Index"], ["Visual/Rotor"]),

        // CAT-019: the driven roller turns on its hinge; carrier box and idle roller are static. Both pulley wheels are solid: the input
        // rides the roller, and the output turns on a binding about its own axis, which a static cylinder follows exactly. Both spokes
        // are printed flat on their wheels (the output one also turned by the binding).
        [new PartId("conveyor")] = new(
            ["Visual/Visual_Carrier", "Visual/Visual_RailA", "Visual/Visual_RailB", "Visual/Visual_IdleRoller",
             "Visual/Visual_SupportA", "Visual/Visual_SupportB", "Visual/Roller/Visual_Roller", "Visual/Roller/PulleyIn/Wheel",
             "Visual/PulleyOut/Wheel"],
            null, [], ["Visual/Roller/PulleyIn/Spoke", "Visual/PulleyOut/Spoke"], ["Visual/Roller"]),

        // CAT-018: two dynamic cylinder shafts rotate on hinges; enclosed axle rod, plates and spokes are decorative.
        [new PartId("clutch")] = new(
            ["Visual/Visual_Base", "Visual/Visual_BlockLeft", "Visual/Visual_BlockRight", "Visual/Visual_Coil",
             "Visual/Visual_Stud", "Visual/Lamp", "Visual/InputShaft/Visual_InputShaft", "Visual/OutputShaft/Visual_OutputShaft"],
            null, [],
            ["Visual/Visual_Axle", "Visual/PlateLeft/*", "Visual/PlateRight/*",
             "Visual/InputShaft/Visual_InputSpoke", "Visual/OutputShaft/Visual_OutputSpoke"],
            ["Visual/InputShaft", "Visual/OutputShaft"]),

        [new PartId("reverse_transmission")] = new(
            ["Visual/Base", "Visual/Housing", "Visual/InputWheel/Disc", "Visual/OutputWheel/Disc"],
            null, [], ["Visual/InputWheel/Index", "Visual/InputWheel/Mark", "Visual/OutputWheel/Index", "Visual/OutputWheel/Mark"],
            ["Visual/InputWheel", "Visual/OutputWheel"]),

        // Every other element package, classified the same way.
        // The seam is the drawn full torus of its hollow collider on the ball's dynamic body.
        [new PartId("ball")] = new(["Visual/Visual_Sphere"], ["Visual/Visual_Stripe"], [], [], []),
        [new PartId("bowling")] = new(["Visual/Visual_Sphere"], null, [], [], []),
        // The seam is printed flush with no collider (owner decision 24i), as owner decision 11 rules for the planned Pool ball's stripe.
        [new PartId("tennis")] = new(["Visual/Felt"], null, [], ["Visual/Seam"], []),
        // The base lies inside the head; the impact ring is scaled by the impact cue. The upper ring is the
        // drawn full torus (engine-hollow-shapes).
        [new PartId("bumper")] = new(["Visual/BumperHead", "Visual/Visual_Cap"], ["Visual/Visual_Ring2"], [], ["Visual/Visual_Base", "Visual/ImpactRing"], []),
        // The pips are solid, each its own sphere collider on the domino's dynamic body.
        [new PartId("domino")] = new(["Visual/Visual_Body", "Visual/Visual_Pip1", "Visual/Visual_Pip2"], null, [], [], []),
        // The stripes are flush with the faces. The body is drawn at 0.998 of the thickness so the stripes do not z-fight with it; the
        // collider takes the full thickness, 0.001·t thicker per face, at most 2 mm at the 2 m maximum thickness.
        [new PartId("wall")] = new(["Visual/Visual_Body"], null, [], ["Visual/Visual_StripeLeft", "Visual/Visual_StripeRight"], [], Fit: .002f),
        // The body is drawn at mass m by a uniform scale 0.136734·m + 0.453070 of its 0.35166413 m radius, and the collider's radius
        // is 0.04808397·m + 0.15932827: the declared constants differ by 0.35166413·(0.136734·m + 0.453070) − (0.04808397·m +
        // 0.15932827), at most 4.0e-6 m at the 8 kg bound (the half-height by 3.2e-6 m).
        // The base is solid, and the bands and the eye are the drawn full tori of their hollow colliders, on the dynamic body.
        [new PartId("weight")] = new(["Visual/Visual_Body", "Visual/Visual_Base"], ["Visual/Visual_BandLower", "Visual/Visual_BandUpper", "Visual/Visual_Eye"], [], [], [], Fit: 4e-6f),
        // The halo is a glow.
        [new PartId("basket")] = new(["Visual/Visual_Floor", "Visual/Visual_Left", "Visual/Visual_Right", "Visual/Visual_Back", "Visual/Visual_Front"],
            null, [], ["Visual/Visual_Halo"], []),
        // The four studs are solid and the ring is the drawn full torus of its hollow collider.
        [new PartId("pressure_plate")] = new(["Visual/Visual_Base", "Visual/Visual_Top", "Visual/Visual_Socket_PowerIn", "Visual/Visual_Socket_Supply",
            "Visual/Visual_Socket_ActivationOut", "Visual/Stud_0", "Visual/Stud_1", "Visual/Stud_2", "Visual/Stud_3"], ["Visual/Visual_Ring"], [], [], []),
        // The stripe is printed on the plate; the coil is drawn by the spring's compression.
        [new PartId("spring")] = new(["Visual/Visual_Base", "Visual/Plate/Visual_Plate"], null, [], ["Visual/Plate/Visual_Stripe", "Visual/Coil/*"],
            ["Visual/Plate"]),
        // engine-hollow-shapes: the pipe family's shells, stubs and collars are its true hollow colliders; a bend's shell chords draw
        // its torus segment.
        [new PartId("pipe")] = new([], ["Visual/Visual_Shell", "Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"], [], [], []),
        [new PartId("pipe_bend_45")] = new([], ["Visual/Visual_Shell/Visual_Stub_*", "Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"],
            ["Visual/Visual_Shell/Visual_Shell_Seg_*"], [], []),
        [new PartId("pipe_bend_90")] = new([], ["Visual/Visual_Shell/Visual_Stub_*", "Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"],
            ["Visual/Visual_Shell/Visual_Shell_Seg_*"], [], []),
        [new PartId("funnel")] = new([], ["Visual/Visual_Shell", "Visual/Visual_Stub_*", "Visual/Visual_Collar_Start", "Visual/Visual_Collar_End"], [], [],
            []),
        // Static control labels are flat printing, independently checked in FaceMarkingTests.
        [new PartId("both_gate")] = Logic(["Visual/UpperPath", "Visual/LowerPath"]),
        [new PartId("electrical_nand")] = Logic(["Visual/Pictogram/*", "Visual/TruthDot*"]),
        [new PartId("electrical_nor")] = Logic(["Visual/Pictogram/*", "Visual/TruthDot*"]),
        [new PartId("electrical_or")] = Logic(["Visual/Pictogram/*", "Visual/TruthDot*"]),
        [new PartId("electrical_xor")] = Logic(["Visual/Pictogram/*", "Visual/TruthDot*"]),
        // The remaining-time bar is driven by the timer; its static track is printed.
        [new PartId("hold_timer")] = new(["Visual/Body", "Visual/Foot", "Visual/Face", "Visual/Indicator", "Visual/PortTrigger",
            "Visual/PortInput", "Visual/PortOutput"], null, [], ["Visual/Remaining/*", "Visual/Track"], []),
        // The ActivationOut socket (r 0.075 at (0, −0.72, 0.1)) lies wholly inside the foot (|y + 0.72| ≤ 0.08, z ≤ 0.425).
        [new PartId("counter")] = new(["Visual/Housing", "Visual/Foot", "Visual/Face", .. Enumerable.Range(0, 9).Select(i => $"Visual/DotLamp_{i}"),
            "Visual/Visual_Socket_ActivationIn", "Visual/Visual_Socket_PowerIn", "Visual/Visual_Socket_Supply"], null, [],
            ["Visual/Visual_Socket_ActivationOut"], []),
        // The rocker turns with state; the set, reset and output cues are printed.
        [new PartId("latch")] = new(["Visual/Housing", "Visual/Foot", "Visual/Face", "Visual/IndicatorLamp", "Visual/Visual_Socket_SetIn",
            "Visual/Visual_Socket_ResetIn", "Visual/Visual_Socket_PowerIn", "Visual/Visual_Socket_Supply", "Visual/Visual_Socket_ActivationOut"],
            null, [], ["Visual/Rocker", "Visual/SetMarker", "Visual/ResetMarker/*", "Visual/OutMarker/*"], []),
        // The pendulum swings with the clock. The recess stands 2 cm proud of the face. The pivot cap is solid.
        [new PartId("clock")] = new(["Visual/Housing", "Visual/Foot", "Visual/Face", "Visual/PivotCap", "Visual/Visual_Indicator", "Visual/Visual_Socket_PowerIn",
            "Visual/Visual_Socket_ActivationOut", "Visual/Recess"], null, [], ["Visual/PendulumPivot/*"], []),

        // Visible-solid remainder, part A. The anchor's eye is a full torus on the static body (the hidden proxy draws it).
        [new PartId("rope_anchor")] = new(["Visual_Mount"], ["Visual_Eye"], [], [], []),
        // The wheel turns on a binding about its own axis: its body, and its rim as a solid of revolution, are solid (a static collider
        // follows them exactly); the spoke and the gold marker turn off the axis, so they are drawn by the binding and decorative.
        [new PartId("pulley")] = new(["Visual/Visual_Mount", "Visual/Visual_Post", "Visual/Wheel/Visual_WheelBody"], ["Visual/Wheel/Visual_Rim"], [],
            ["Visual/Wheel/Visual_Spoke", "Visual/Wheel/Visual_Marker"], []),
        // The blades are turned by a binding and have no collider (owner decision, 21 November 2026); the flow arrow is printed on the
        // base. The hub turns on its hinge (its own body).
        [new PartId("fan")] = new(["Visual/Base", "Visual/Post", "Visual/Hub/Surface"], ["Visual/Ring"], [],
            ["Visual/BladeAxis/Blades/*", "Visual/FlowMark/*"], ["Visual/Hub"]),
        // The beam tips on its hinge (its own body); the inset and the four marks are printed flat on its top face.
        [new PartId("impact_lever")] = new(["Visual/Visual_Base", "Visual/Visual_Fulcrum", "Visual/Visual_StopLeft", "Visual/Visual_StopRight",
            "Visual/Beam/Visual_Beam"], null, [],
            ["Visual/Beam/Visual_Inset", "Visual/Beam/Visual_Mark_0", "Visual/Beam/Visual_Mark_1", "Visual/Beam/Visual_Mark_2", "Visual/Beam/Visual_Mark_3"],
            ["Visual/Beam"]),
        // The bed is drawn by its deflection binding.
        [new PartId("trampoline")] = new(["Visual/Visual_BackPlate", "Visual/Visual_SideLeft", "Visual/Visual_SideRight", "Visual/Visual_EndFront",
            "Visual/Visual_EndBack", .. Enumerable.Range(0, 4).Select(i => $"Visual/Visual_Post{i}"),
            .. Enumerable.Range(0, 4).Select(i => $"Visual/Visual_Foot{i}")], null, [], ["Visual/Visual_Bed"], []),

        // Visible-solid remainder, part B. The head slides on its own body, carrying its sphere and the full torus of its ring; the rod is
        // drawn by the head's span binding and is the head's clipped extrusion collider; the travel marks are printed on the barrel.
        [new PartId("linear_pusher")] = new(["Visual/Foot", "Visual/Barrel", "Visual/Head/Sphere", "Visual/Indicator", "Visual/Socket_PowerIn",
            "Visual/Socket_ExtendIn", "Visual/Socket_RetractIn", "Visual/Socket_RetractedOut", "Visual/Socket_ExtendedOut"], ["Visual/Head/Ring"], [],
            [.. Enumerable.Range(0, 5).Select(i => $"Visual/TravelMark{i}")], ["Visual/Head"], Spanned: ["Visual/Rod"]),
        // The plunger slides and the winding shaft turns, each on its own body; the sleeve is a tube whose art node declares its bore. The
        // stroke marks, the charge index and the shaft's spoke are printed; the latch lever is turned by the retention binding, the coil
        // drawn by compression.
        [new PartId("wound_spring")] = new(["Visual/Base", "Visual/LeftRail", "Visual/RightRail", "Visual/Head/Sphere", "Visual/Shaft/Wheel",
            "Visual/ActivationSocket"], ["Visual/Sleeve"], [],
            ["Visual/Head/ChargeIndex", "Visual/Shaft/Spoke", .. Enumerable.Range(0, 5).Select(i => $"Visual/Mark{i}"), "Visual/Latch/Lever",
             "Visual/Coil/*"], ["Visual/Head", "Visual/Shaft"]),

        // Visible-solid remainder, part C. The jacket and both collars are imported rings, each matched with its tube collider; the
        // sleeve, slid by the recoil binding, lies inside the jacket's wall over its travel; the charge bar and the ready flag are moved
        // by bindings.
        [new PartId("cannon")] = new(["Visual/Breech", "Visual/Foot", "Visual/ChargeTrack", "Visual/Indicator", "Visual/PowerIn",
            "Visual/ActivationIn"], ["Visual/Jacket", "Visual/RearCollar", "Visual/MuzzleCollar"], [],
            ["Visual/Sleeve", "Visual/ChargeBar", "Visual/ReadyFlag"], []),
        // The rotor turns on its hinge (its own body); each blade's tip lies inside its blade. The output wheel turns on a binding about
        // its own axis, which a static cylinder follows exactly; its spokes turn off the axis, so they are drawn by the binding. The guard
        // is an imported ring matched with its tube collider.
        [new PartId("windmill")] = new(["Visual/Base", "Visual/Tower", "Visual/Nacelle", "Visual/Output/Wheel", "Visual/Rotor/Hub",
            .. Enumerable.Range(0, 4).Select(i => $"Visual/Rotor/Arm{i}/Blade")], ["Visual/Guard"], [],
            [.. Enumerable.Range(0, 4).Select(i => $"Visual/Rotor/Arm{i}/Tip"), "Visual/Output/Spoke0", "Visual/Output/Spoke1"],
            ["Visual/Rotor"]),
        // The envelope is drawn at its gas radius, its collider's. The neck is moved by the gas-radius binding and the carrier lies
        // inside the envelope at every radius. The string is the thin-string exception, visible and decorative with no collider (owner,
        // 21 November 2026; owner decision 1, 22 November 2026).
        [new PartId("balloon")] = new(["Envelope"], null, [], ["Neck", "Carrier", "String"], []),
    };

    private static ArtSpec Logic(string[] printed) => new(["Visual/Body", "Visual/Foot", "Visual/Face", "Visual/FirstLamp", "Visual/SecondLamp",
            "Visual/OutputLamp", "Visual/PortFirst", "Visual/PortSecond", "Visual/PortPower", "Visual/PortOutput"], null, [],
        [.. printed, "Visual/FirstMark", "Visual/SecondMarkLeft", "Visual/SecondMarkRight"], []);

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
            var art = Art(entry, spec.MovingRoots, parameters);
            string[] entries = [.. spec.Solid, .. spec.Hollow ?? [], .. spec.Arc, .. spec.Decorative, .. spec.Spanned ?? []];
            Assert.All(art.Meshes.Keys, path => Assert.True(entries.Count(e => Names(e, path)) == 1, $"{element}: art '{path}' is not classified once"));
            Assert.All(entries, e => Assert.True(art.Meshes.Keys.Any(path => Names(e, path)), $"{element}: '{e}' names no art"));

            var colliders = BundleGeometry.Colliders(entry, parameters).ToList();
            var moving = MovingPools(element, entry, spec);
            var extrusions = new HashSet<ClippedExtrusionDescriptor>();
            // H3 runs first: it takes each extrusion collider out of its body's pool by its declared index, before another match shifts it.
            foreach (var path in spec.Spanned ?? []) SpannedCollider(element, entry, spec, art, path, moving, extrusions);
            var bores = entry.TubeMouths.Select(m => m.BoreRadius).Distinct().ToArray();
            var unmatched = new List<string>();
            foreach (var path in art.Meshes.Keys.Where(path => (spec.Hollow ?? []).Any(e => Names(e, path))))
            {
                var hollow = art.Meshes[path] ?? throw new InvalidOperationException(
                    $"{element}: hollow art '{path}' is not an open cylinder, an imported ring or a torus.");
                Assert.InRange(hollow.MeshError, 0f, .005f);
                var pool = Pool(spec, path, colliders, moving);
                var match = pool.FindIndex(collider => MatchesHollow(hollow, collider, bores, Tol + spec.Fit + hollow.Allowance));
                if (match < 0) unmatched.Add($"'{path}' ({hollow.Kind}, centre {hollow.Centre}, extents {hollow.Extents})");
                else pool.RemoveAt(match);
            }
            var chords = art.Meshes.Keys.Where(path => spec.Arc.Any(e => Names(e, path))).Select(path => art.Meshes[path]!).ToArray();
            if (chords.Length != 0)
            {
                var arc = colliders.FindIndex(collider => collider.ShapeKind == ColliderShapeKind.Torus && collider.HalfExtents.Z != ColliderDeclaration.FullSweep);
                Assert.True(arc >= 0, $"{element}: no torus segment collider for its drawn arc");
                AssertArc(chords, colliders[arc], bores);
                colliders.RemoveAt(arc);
            }
            foreach (var path in spec.Solid)
            {
                var solid = art.Meshes[path] ?? throw new InvalidOperationException($"{element}: solid art '{path}' is not a box, full sphere or straight cylinder.");
                var pool = Pool(spec, path, colliders, moving);
                var match = pool.FindIndex(collider => Matches(solid, collider, Tol + spec.Fit + solid.Allowance));
                if (match < 0) unmatched.Add($"'{path}' ({solid.Kind}, centre {solid.Centre}, half {solid.Extents})");
                else pool.RemoveAt(match);
            }
            var left = colliders.Concat(moving.SelectMany(body => body)).ToList();
            Assert.True(unmatched.Count == 0, $"{element} at {Describe(parameters)}: solid art without a matching collider: " +
                string.Join("; ", unmatched) + "; colliders left: " +
                string.Join("; ", left.Select(c => $"{c.ShapeKind} at {c.Offset} half {c.HalfExtents}")));
            Assert.True(left.Count == 0, $"{element} at {Describe(parameters)}: collider(s) reach beyond the art: " +
                string.Join("; ", left.Select(c => $"{c.ShapeKind} at {c.Offset} half {c.HalfExtents}")));
        }
    }

    private static string Describe(IReadOnlyDictionary<string, float> parameters) =>
        parameters.Count == 0 ? "no parameters" : string.Join(", ", parameters.Select(p => $"{p.Key} {p.Value}"));

    /// <summary>One collider pool per moving body, in the package's scene ownership order (the sliders' bodies, then the hinges'). The
    /// spec's moving roots must be those bodies' roots in that order, so the pool at index i belongs to the body whose root is
    /// <c>MovingRoots[i]</c>, and a reordered or missing root fails here instead of matching art against another body.</summary>
    private static List<ColliderDescriptor>[] MovingPools(PartId element, BundleEntry entry, ArtSpec spec)
    {
        var declaration = TestBundle.Packages.Single(package => package.Declaration.Metadata.Id == element.Value).Declaration;
        Assert.Equal(declaration.SceneBodies.Skip(1).Select(body => body.Root), spec.MovingRoots);
        return entry.Mechanical is { } mechanical ? [.. mechanical.MovingBodyColliders.Select(body => body.ToList())] : [];
    }

    /// <summary>The pool of the body that owns an art path: the moving body whose root lies above it (scene body roots do not nest),
    /// or the base.</summary>
    private static List<ColliderDescriptor> Pool(ArtSpec spec, string path, List<ColliderDescriptor> colliders, List<ColliderDescriptor>[] moving)
    {
        var body = Array.FindIndex(spec.MovingRoots, root => path.StartsWith(root + "/", StringComparison.Ordinal));
        return body < 0 ? colliders : moving[body];
    }

    /// <summary>H3. Art that a presentation span binding draws between a moving body and the base (the Linear pusher's rod) is that
    /// body's clipped extrusion collider: exactly one declared span draws the path; the entry has exactly one clipped extrusion, which no
    /// other spanned path has used, whose material body b is the body whose root is the span's moving node (the spec's moving roots are
    /// the scene ownership's moving bodies in order, <see cref="MovingPools"/>); that body's collider at the extrusion's index, posed by
    /// the moving root's scene transform, matches the drawn mesh, and is taken out of that body's pool. Only the authored pose is
    /// checked; extension is <c>ClippedExtrusionTests</c>' subject.</summary>
    private static void SpannedCollider(PartId element, BundleEntry entry, ArtSpec spec, ArtScene art, string path,
        List<ColliderDescriptor>[] moving, HashSet<ClippedExtrusionDescriptor> used)
    {
        var drawn = art.Meshes[path] ?? throw new InvalidOperationException($"{element}: spanned art '{path}' is not a box, full sphere or straight cylinder.");
        var span = Assert.Single(entry.Presentation.Spans, s => s.SpanNode == path);
        var mechanical = entry.Mechanical ?? throw new InvalidOperationException($"{element}: spanned art '{path}' has no moving body.");
        var extrusion = Assert.Single(mechanical.Extrusions);
        // Each extrusion's collider is taken out at most once, so a second spanned path cannot remove a shifted index.
        Assert.True(used.Add(extrusion), $"{element}: spanned art '{path}' reuses a clipped extrusion another spanned path matched");
        var body = extrusion.MaterialBody.Value;
        Assert.InRange(body, 1, spec.MovingRoots.Length);
        Assert.Equal(span.MovingNode, spec.MovingRoots[body - 1]);
        var pool = moving[body - 1];
        var collider = pool[extrusion.Collider.Value];
        var pose = art.Roots[span.MovingNode];
        var centre = pose * new Godot.Vector3(collider.Offset.X, collider.Offset.Y, collider.Offset.Z);
        var turn = pose.Basis.GetRotationQuaternion();
        var posed = collider with
        {
            Offset = new(centre.X, centre.Y, centre.Z),
            Rotation = new NumQuaternion(turn.X, turn.Y, turn.Z, turn.W) * collider.Rotation
        };
        Assert.True(Matches(drawn, posed, Tol + spec.Fit), $"{element}: the clipped extrusion collider ({posed.ShapeKind} at {posed.Offset} " +
            $"half {posed.HalfExtents}) is not the drawn '{path}' ({drawn.Kind} at {drawn.Centre} half {drawn.Extents})");
        pool.RemoveAt(extrusion.Collider.Value);
    }

    /// <summary>The drawn scene <see cref="Art"/> reads: each mesh's primitive by path, and each moving root's pose in the base
    /// frame.</summary>
    private sealed record ArtScene(Dictionary<string, Solid?> Meshes, Dictionary<string, Transform3D> Roots);

    /// <summary>Every mesh of the element's scene in the frame of the body that owns it, and each moving root's own pose in the base
    /// frame, with the declared presentation parameter scales and offsets applied to their nodes (for example the Ramp's Visual node
    /// scales X by length / 3 and Z by width / 1.3) and a gas-radius scale at the radius its gas store declares (<see cref="GasRadius"/>).
    /// A mesh that is not a box, a full sphere, a straight cylinder, an open cylinder, a torus or an imported ring has no primitive shape
    /// (null). A collider proxy (a <c>col_</c> mesh: engine-hollow-shapes' hollow proxies are mesh
    /// instances) is no art, and it must not be drawn: the scene contract's proxy is a separate node that is not rendered, so its own
    /// node is hidden and no presentation binding targets it.</summary>
    private static ArtScene Art(BundleEntry entry, string[] movingRoots, IReadOnlyDictionary<string, float> parameters)
    {
        var root = WorkshopResources.LoadScene(entry.Metadata.ScenePath).Instantiate<Node3D>();
        try
        {
            var meshes = new Dictionary<string, Solid?>(StringComparer.Ordinal);
            foreach (var mesh in Descendants(root).OfType<MeshInstance3D>())
            {
                var path = root.GetPathTo(mesh).ToString();
                if (mesh.Name.ToString().StartsWith("col_", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.False(mesh.Visible, $"{entry.Metadata.Id}: the collider proxy '{path}' is drawn");
                    continue;
                }
                meshes.Add(path, Primitive(path, mesh, BodyPose(root, mesh, entry, movingRoots, parameters)));
            }
            // Nor may a presentation binding target a proxy: a Visible binding would draw it at Run whatever its own scene flag says.
            var presentation = entry.Presentation;
            string[] bound = [.. presentation.Bindings.Select(b => b.Node), .. presentation.Bodies.Select(b => b.Node),
                .. presentation.Spans.SelectMany(s => new[] { s.SpanNode, s.MovingNode }), .. presentation.Parameters.Select(b => b.Node)];
            Assert.DoesNotContain(bound, node => node.Split('/')[^1].StartsWith("col_", StringComparison.OrdinalIgnoreCase));
            return new(meshes, movingRoots.ToDictionary(path => path, path => BodyPose(root, root.GetNode(path), entry, movingRoots, parameters),
                StringComparer.Ordinal));
        }
        finally
        {
            root.Free();
        }
    }

    /// <summary>A node's pose in the frame of the body that owns it: its transform and its ancestors', up to the scene root or to the
    /// moving root above it (so a moving root's own pose is in the base frame), each with its declared presentation parameter bindings
    /// applied.</summary>
    private static Transform3D BodyPose(Node3D root, Node start, BundleEntry entry, string[] movingRoots, IReadOnlyDictionary<string, float> parameters)
    {
        var transform = Transform3D.Identity;
        for (var node = start; node != root; node = node.GetParent())
        {
            var nodePath = root.GetPathTo(node).ToString();
            if (node != start && movingRoots.Contains(nodePath, StringComparer.Ordinal)) break;
            var local = ((Node3D)node).Transform;
            var (basis, scale, origin) = (local.Basis, new Godot.Vector3(1, 1, 1), local.Origin);
            // G2: a gas-radius scale replaces the node's authored scale, keeping its rotation; the authored scale must be the radius the
            // gas store declares at the default parameters, so the scene stays checked.
            foreach (var binding in entry.Presentation.Bindings.Where(b => b.Node == nodePath &&
                         b.Driver == PresentationDriver.CommittedGasRadius && b.Property == PresentationProperty.UniformScale))
            {
                var authored = GasRadius(entry, binding, BundleGeometry.Parameters(entry));
                Assert.True(Near(new(basis.Scale.X, basis.Scale.Y, basis.Scale.Z), new NumVector3(authored)),
                    $"{entry.Metadata.Id}: '{nodePath}' is authored at scale {basis.Scale}, not at its declared gas radius {authored} m");
                var radius = GasRadius(entry, binding, parameters);
                (basis, scale) = (new Basis(basis.GetRotationQuaternion()), new Godot.Vector3(radius, radius, radius));
            }
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
            local = new Transform3D(basis * Basis.FromScale(scale), origin);
            transform = local * transform;
        }
        return transform;
    }

    /// <summary>G2: the radius a gas-radius binding draws at the authored state in Standard atmosphere, from declared data with the
    /// production function: the outer radius of the binding's gas shape (its dry radius) holding its store's reference volume at these
    /// parameters, clamped to the binding's endpoints. It is a statement about declarations; the real part drawing it is
    /// <c>BalloonArtTests.TheEnvelopeIsDrawnAtTheColliderRadiusInBuildModeAtRunStartAndAfterReset</c>.</summary>
    private static float GasRadius(BundleEntry entry, PresentationBinding binding, IReadOnlyDictionary<string, float> parameters)
    {
        var mechanical = entry.Mechanical ?? throw new InvalidOperationException($"{entry.Metadata.Id}: gas-radius art without a mechanical payload.");
        var reference = binding.GasShape ?? throw new InvalidOperationException($"{entry.Metadata.Id}: '{binding.Node}' names no gas shape.");
        var shape = mechanical.GasShapes.Single(s => s.Body == reference.Body && s.Collider == reference.Collider);
        var store = mechanical.GasStores.Single(s => s.Id == shape.Store);
        var volume = store.ReferenceVolume.Evaluate(parameters.ToDictionary(p => new ParameterId(p.Key), p => p.Value));
        return Math.Clamp(IsobaricGas.OuterRadius(shape.DryRadius, volume), binding.From, binding.To);
    }

    /// <summary>A ring read from an imported mesh about its local Y axis (<see cref="Annulus"/>): its inner and outer radii, the least
    /// and greatest y of its two ends, and the largest angular gap between neighbouring outer vertices (rad).</summary>
    private readonly record struct Ring(float Inner, float Outer, float Low, float High, float Gap);

    /// <summary>G1: an imported mesh read as a ring from its vertices and triangles, or null. Outer is the largest radial distance
    /// √(x² + z²), inner the smallest, the ends the least and greatest y. It is a ring only when the mesh has one indexed surface of
    /// triangles (every delivered imported ring is one); inner &gt; 0
    /// and the wall exceeds two storage steps; every vertex lies within <see cref="ImportedSpread"/> of the inner or the outer radius and
    /// of one end (both stored values carry a step); each of the four circles holds at least three distinct vertices; and no triangle's
    /// centroid lies nearer the axis than inner · cos(Δ) less a step, Δ the largest angular gap between neighbouring distinct outer
    /// vertices, the wrap-around included. A ring with a gap reads with that gap's mesh error (<see cref="RingError"/>), which the
    /// theory's 5 mm bound refuses.</summary>
    private static Ring? Annulus(ArrayMesh mesh)
    {
        if (mesh.GetSurfaceCount() != 1 || mesh.SurfaceGetPrimitiveType(0) != Mesh.PrimitiveType.Triangles) return null;
        var arrays = mesh.SurfaceGetArrays(0);
        var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        var index = arrays[(int)Mesh.ArrayType.Index];
        if (vertices.Length == 0 || index.VariantType == Variant.Type.Nil) return null;
        var triangles = index.AsInt32Array();
        if (triangles.Length == 0 || triangles.Length % 3 != 0) return null;
        static float Radial(Godot.Vector3 v) => MathF.Sqrt(v.X * v.X + v.Z * v.Z);
        static bool Stored(float a, float b) => MathF.Abs(a - b) <= ImportedSpread;
        var (inner, outer, low, high) = (vertices.Min(Radial), vertices.Max(Radial), vertices.Min(v => v.Y), vertices.Max(v => v.Y));
        if (inner <= 0 || outer - inner <= 2 * ImportedStep) return null;
        if (!vertices.All(v => (Stored(Radial(v), inner) || Stored(Radial(v), outer)) && (Stored(v.Y, low) || Stored(v.Y, high)))) return null;
        (float Radius, float Y)[] circles = [(inner, low), (inner, high), (outer, low), (outer, high)];
        if (circles.Any(c => vertices.Where(v => Stored(Radial(v), c.Radius) && Stored(v.Y, c.Y)).Distinct().Count() < 3)) return null;
        var angles = vertices.Where(v => Stored(Radial(v), outer)).Select(v => MathF.Atan2(v.Z, v.X)).Distinct().Order().ToArray();
        var gap = MathF.Tau - (angles[^1] - angles[0]);
        for (var i = 1; i < angles.Length; i++) gap = MathF.Max(gap, angles[i] - angles[i - 1]);
        var nearest = inner * MathF.Cos(gap) - ImportedStep;
        for (var i = 0; i < triangles.Length; i += 3)
            if (Radial((vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3) < nearest) return null;
        return new(inner, outer, low, high, gap);
    }

    /// <summary>The largest distance of a ring's drawn outer wall inside its true circle at scale s: outer · s · (1 − cos(Δ/2)).</summary>
    private static float RingError(Ring ring, float scale) => ring.Outer * scale * (1f - MathF.Cos(ring.Gap / 2));

    /// <summary>An imported ring at uniform scale s in its body's frame: a Tube with extents (outer·s, half-length·s, outer·s), bore
    /// inner·s, centred at the mid-point of its two ends, its mesh error <see cref="RingError"/> and the allowance
    /// <see cref="ImportedStep"/>; null for any other imported mesh.</summary>
    private static Solid? Imported(string path, ArrayMesh mesh, Transform3D transform)
    {
        var s = transform.Basis.Scale;
        if (Annulus(mesh) is not { } ring || MathF.Abs(s.X - s.Y) > Tol || MathF.Abs(s.Y - s.Z) > Tol) return null;
        var q = transform.Basis.GetRotationQuaternion();
        var centre = transform * new Godot.Vector3(0, (ring.Low + ring.High) * .5f, 0);
        return new(path, ColliderShapeKind.Tube, new(centre.X, centre.Y, centre.Z), new(q.X, q.Y, q.Z, q.W),
            new NumVector3(ring.Outer * s.X, (ring.High - ring.Low) * .5f * s.X, ring.Outer * s.X), RingError(ring, s.X), ring.Inner * s.X,
            ImportedStep);
    }

    /// <summary>A mesh's primitive (<see cref="Solid"/>) at its pose in its body's frame, or null when it has none.</summary>
    private static Solid? Primitive(string path, MeshInstance3D mesh, Transform3D transform)
    {
        if (mesh.Mesh is ArrayMesh imported) return Imported(path, imported, transform);
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
        // H2: an open cylinder of equal radii whose own art node states its wall declares its bore (the tube is drawn as one surface).
        float? inner = mesh.Mesh is CylinderMesh { CapTop: false, CapBottom: false } tube && tube.TopRadius == tube.BottomRadius &&
            mesh.HasMeta("wall_thickness") ? (tube.TopRadius - mesh.GetMeta("wall_thickness").AsSingle()) * s.X : null;
        return shape is { } primitive ? new(path, primitive.Kind, new(transform.Origin.X, transform.Origin.Y, transform.Origin.Z),
            new(q.X, q.Y, q.Z, q.W), primitive.Extents, meshError, inner, 0f) : null;
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
    /// element's mouth bores, and a tube's inner radius also the bore its art node declares when that node states its wall (a tube with
    /// neither has no bore to match); for a torus mesh its drawn centreline and tube, solid, over the full turn.</summary>
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
            ColliderShapeKind.Tube => Same(collider.Radius, art.Extents.X) && Same(h.Y, art.Extents.Y) && h.Z == 0f &&
                (art.Inner is { } inner ? Same(h.X, inner) : bores.Length != 0) && (bores.Length == 0 || Bore(h.X)),
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

    /// <summary>C3, C7 and C10 (visible-solid remainder, part C): G1 reads each delivered imported ring from its own vertices and
    /// triangles as the inner radius, outer radius and half-length its source file states, within one storage allowance
    /// (<see cref="ImportedStep"/>): the Cannon's jacket, collars and sleeve and the Windmill's guard. The sleeve, at both ends of its
    /// recoil binding, lies coaxially inside the jacket's wall, so it needs no collider of its own. Controls, built here as meshes whose
    /// structural preconditions (surface count, primitive and indexing) are asserted, each shown by reading to fail the named rule of
    /// <see cref="Annulus"/>: a box centred on the axis has no wall; a capped cylinder (a centre vertex on each cap) has no bore and no
    /// inner circle, refused by those two rules together; the full ring with a second surface capping its bore is not one surface; its
    /// arrays as lines are no triangles; its triangles without indices fail the index check (an empty index list also refuses); a fan across its lower inner circle
    /// puts a triangle centroid near the axis; one more vertex at radius 0.53 m on an end lies on neither radius; one more vertex on the
    /// outer radius at mid-height lies on neither end; every outer vertex moved onto the one at angle 0 or π leaves two per outer circle;
    /// a conical shell of its lower outer and upper inner circles has two circles, not four; none is read. A half ring reads with a mesh
    /// error beyond the 5 mm the visible-solid theory admits; and the full ring every control is built from reads as its literals, so the
    /// builder is no cause of those refusals.</summary>
    [Fact]
    public void AnImportedRingIsReadFromItsMeshAndOtherMeshesAreRefused()
    {
        var (cannon, windmill) = (new PartId("cannon"), new PartId("windmill"));
        var (jacket, jacketPose) = ReadRing(cannon, "Visual/Jacket", .48f, .58f, .55f);
        ReadRing(cannon, "Visual/RearCollar", .48f, .65f, .05f);
        ReadRing(cannon, "Visual/MuzzleCollar", .48f, .65f, .05f);
        var (sleeve, sleevePose) = ReadRing(cannon, "Visual/Sleeve", .5f, .565f, .07f);
        ReadRing(windmill, "Visual/Guard", .745f, .815f, .035f);

        Assert.True(sleeve.Inner >= jacket.Inner && sleeve.Outer <= jacket.Outer,
            $"the sleeve's wall ({sleeve.Inner} to {sleeve.Outer} m) leaves the jacket's ({jacket.Inner} to {jacket.Outer} m)");
        var recoil = Assert.Single(TestBundle.Entry(cannon).Presentation.Bindings,
            b => b.Node == "Visual/Sleeve" && b.Property == PresentationProperty.LocalX);
        Godot.Vector3[] jacketEnds = [jacketPose * new Godot.Vector3(0, jacket.Low, 0), jacketPose * new Godot.Vector3(0, jacket.High, 0)];
        foreach (var x in new[] { recoil.From, recoil.To })
        {
            var pose = sleevePose with { Origin = sleevePose.Origin with { X = x } };
            foreach (var end in new[] { pose * new Godot.Vector3(0, sleeve.Low, 0), pose * new Godot.Vector3(0, sleeve.High, 0) })
                Assert.True(end.X >= jacketEnds.Min(e => e.X) && end.X <= jacketEnds.Max(e => e.X) &&
                    MathF.Abs(end.Y - jacketEnds[0].Y) <= ImportedStep && MathF.Abs(end.Z - jacketEnds[0].Z) <= ImportedStep,
                    $"with the sleeve at x {x} its end lies at {end}, outside the jacket between {jacketEnds[0]} and {jacketEnds[1]}");
        }

        const Mesh.PrimitiveType Triangles = Mesh.PrimitiveType.Triangles;
        using var box = new BoxMesh { Size = new(.6f, .2f, .6f) };
        using var boxSurface = Surface(box);
        AssertRefused(boxSurface, 1, Triangles, true, "a box centred on the axis (wall rule)");
        using var capped = new CylinderMesh { TopRadius = .3f, BottomRadius = .3f, Height = .2f, Rings = 0 };
        using var cappedSurface = Surface(capped);
        AssertRefused(cappedSurface, 1, Triangles, true, "a capped cylinder (bore and inner-circle rules)");
        var (vertices, indices, count) = RingArrays(.48f, .58f, .55f, MathF.Tau);
        using var fullRing = Built(vertices, indices, Triangles);
        var full = Annulus(fullRing);
        Assert.True(full is { } closed && MathF.Abs(closed.Inner - .48f) <= ImportedStep && MathF.Abs(closed.Outer - .58f) <= ImportedStep &&
            MathF.Abs(closed.High - .55f) <= ImportedStep && RingError(closed, 1) <= .005f, $"the built full ring reads as {full}");
        var bore = 2 * count; // the inner circle at the lower end
        List<int> fan = [.. Enumerable.Range(1, count - 2).SelectMany(k => new[] { bore, bore + k, bore + k + 1 })];
        using var twoSurfaces = Built(vertices, indices, Triangles);
        AddSurface(twoSurfaces, vertices, fan, Triangles);
        AssertRefused(twoSurfaces, 2, Triangles, true, "a second surface capping the bore (single-surface rule)");
        using var lines = Built(vertices, indices, Mesh.PrimitiveType.Lines);
        AssertRefused(lines, 1, Mesh.PrimitiveType.Lines, true, "the ring's arrays as lines (triangle rule)");
        using var unindexed = Built([.. indices.Select(i => vertices[i])], null, Triangles);
        AssertRefused(unindexed, 1, Triangles, false, "the ring's triangles unindexed (index check)");
        using var coveredBore = Built(vertices, [.. indices, .. fan], Triangles);
        AssertRefused(coveredBore, 1, Triangles, true, "a fan closing the bore (centroid rule)");
        using var midWall = Built([.. vertices, new(.53f, .55f, 0)], [.. indices, count, vertices.Count, 3 * count], Triangles);
        AssertRefused(midWall, 1, Triangles, true, "a vertex between the radii (on-circle rule)");
        using var midHeight = Built([.. vertices, new(.58f, 0, 0)], [.. indices, 0, vertices.Count, count], Triangles);
        AssertRefused(midHeight, 1, Triangles, true, "a vertex between the ends (on-end rule)");
        using var twoPerCircle = Built([.. vertices.Select((v, i) => i < 2 * count ? new Godot.Vector3(v.X >= 0 ? .58f : -.58f, v.Y, 0) : v)],
            indices, Triangles);
        AssertRefused(twoPerCircle, 1, Triangles, true, "two outer vertices per circle (three-vertices rule)");
        using var conical = Built([.. vertices.Take(count), .. vertices.Skip(3 * count)],
            [.. Enumerable.Range(0, count).SelectMany(k => new[] { k, count + k, count + (k + 1) % count, k, count + (k + 1) % count, (k + 1) % count })],
            Triangles);
        AssertRefused(conical, 1, Triangles, true, "a conical shell of two circles (four-circles rule)");
        var (halfVertices, halfIndices, _) = RingArrays(.48f, .58f, .55f, MathF.PI);
        using var halfRing = Built(halfVertices, halfIndices, Mesh.PrimitiveType.Triangles);
        var half = Annulus(halfRing);
        Assert.True(half is { } open && RingError(open, 1) > .005f, $"the built half ring reads as {half}, not as a ring with a gap");
    }

    /// <summary>The ring read from an element scene's imported mesh, asserted to be the stated literals within one storage allowance,
    /// with the mesh node's pose in its parent.</summary>
    private static (Ring Ring, Transform3D Pose) ReadRing(PartId element, string path, float inner, float outer, float half)
    {
        var root = WorkshopResources.LoadScene(TestBundle.Entry(element).Metadata.ScenePath).Instantiate<Node3D>();
        try
        {
            var node = root.GetNode<MeshInstance3D>(path);
            var mesh = Assert.IsType<ArrayMesh>(node.Mesh);
            var read = Annulus(mesh);
            Assert.True(read is { } ring && MathF.Abs(ring.Inner - inner) <= ImportedStep && MathF.Abs(ring.Outer - outer) <= ImportedStep &&
                MathF.Abs(ring.Low + half) <= ImportedStep && MathF.Abs(ring.High - half) <= ImportedStep,
                $"{element}: '{path}' reads as {read?.ToString() ?? "no ring"}, not inner {inner}, outer {outer} and half-length {half} " +
                $"({Measured(mesh)})");
            return (read!.Value, node.Transform);
        }
        finally
        {
            root.Free();
        }
    }

    /// <summary>What an imported mesh holds, for a failure message: its surfaces, and its first surface's vertex and index counts,
    /// radial range about Y and y range.</summary>
    private static string Measured(ArrayMesh mesh)
    {
        var arrays = mesh.SurfaceGetArrays(0);
        var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        var index = arrays[(int)Mesh.ArrayType.Index];
        var radial = vertices.Select(v => MathF.Sqrt(v.X * v.X + v.Z * v.Z)).ToArray();
        var (inner, outer, low, high) = (radial.Min(), radial.Max(), vertices.Min(v => v.Y), vertices.Max(v => v.Y));
        return $"{mesh.GetSurfaceCount()} surface(s), {vertices.Length} vertices, " +
            $"{(index.VariantType == Variant.Type.Nil ? 0 : index.AsInt32Array().Length)} indices, radial {inner} to {outer}, " +
            $"y {low} to {high}, farthest from the nearer radius {radial.Max(r => MathF.Min(r - inner, outer - r))}, " +
            $"from the nearer end {vertices.Max(v => MathF.Min(v.Y - low, high - v.Y))}";
    }

    private static ArrayMesh Surface(PrimitiveMesh primitive)
    {
        using var arrays = primitive.GetMeshArrays();
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    /// <summary>A ring about Y of 64 segments over <paramref name="sweep"/> rad: its vertices, four circles of <c>Count</c> each (outer
    /// lower, outer upper, inner lower, inner upper), and the indexed triangles of its outer and inner walls and its two end faces. A full
    /// turn closes on its first vertices; a part turn leaves its gap open.</summary>
    private static (List<Godot.Vector3> Vertices, List<int> Indices, int Count) RingArrays(float inner, float outer, float half, float sweep)
    {
        const int Segments = 64;
        var count = sweep >= MathF.Tau ? Segments : Segments + 1;
        var vertices = new List<Godot.Vector3>(4 * count);
        foreach (var (radius, y) in new[] { (outer, -half), (outer, half), (inner, -half), (inner, half) })
            for (var k = 0; k < count; k++)
                vertices.Add(new(radius * MathF.Cos(sweep * k / Segments), y, radius * MathF.Sin(sweep * k / Segments)));
        var indices = new List<int>(24 * Segments);
        void Band(int a, int b)
        {
            for (var k = 0; k < Segments; k++)
            {
                var next = (k + 1) % count;
                indices.AddRange([a + k, b + k, b + next, a + k, b + next, a + next]);
            }
        }
        Band(0, count);
        Band(2 * count, 3 * count);
        Band(0, 2 * count);
        Band(count, 3 * count);
        return (vertices, indices, count);
    }

    /// <summary>A mesh of one surface of <paramref name="primitive"/> from these vertices (<see cref="AddSurface"/>).</summary>
    private static ArrayMesh Built(List<Godot.Vector3> vertices, List<int>? indices, Mesh.PrimitiveType primitive)
    {
        var mesh = new ArrayMesh();
        AddSurface(mesh, vertices, indices, primitive);
        return mesh;
    }

    /// <summary>Adds a surface of <paramref name="primitive"/> from these vertices, indexed when <paramref name="indices"/> is given.</summary>
    private static void AddSurface(ArrayMesh mesh, List<Godot.Vector3> vertices, List<int>? indices, Mesh.PrimitiveType primitive)
    {
        using var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        if (indices is not null) arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();
        mesh.AddSurfaceFromArrays(primitive, arrays);
    }

    /// <summary>A control mesh has the intended structure (its surface count, every surface's primitive, and whether its first surface is
    /// indexed) and is not read as a ring; that it fails the named <paramref name="rule"/> is shown by reading the control and
    /// <see cref="Annulus"/>, not asserted here.</summary>
    private static void AssertRefused(ArrayMesh mesh, int surfaces, Mesh.PrimitiveType primitive, bool indexed, string rule)
    {
        Assert.Equal(surfaces, mesh.GetSurfaceCount());
        for (var surface = 0; surface < surfaces; surface++) Assert.Equal(primitive, mesh.SurfaceGetPrimitiveType(surface));
        Assert.Equal(indexed, mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Index].VariantType != Variant.Type.Nil);
        Assert.True(Annulus(mesh) is null, $"{rule}: the control mesh is read as a ring");
    }

    // ---------- the production simulation ----------

    private const double T = 1.0 / 120, G = 9.81;
    /// <summary>The Basketball (CAT-001): radius 0.34 m, restitution 0.55, linear drag 0.04 1/s.</summary>
    private const double BallRadius = .34, BallRestitution = .55, BallDrag = .04;
    /// <summary>The envelope's temporary visual penetration bound: 5 % of the smaller body's extent, the ball's radius here.</summary>
    private const double PenetrationShare = .05, Penetration = PenetrationShare * BallRadius;
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
        WorkshopInput.Generic(RuntimeTestServices.Bundle, part, id, (float)(at.X), (float)(at.Y), (float)(at.Z), (float)(rotation.X), (float)(rotation.Y), (float)(rotation.Z), (float)(rotation.W), parameters);
    private static WorkshopGenericInstance BallAt(NumVector3 at) => Place(new PartId("ball"), Ball, at, NumQuaternion.Identity);

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
    /// indicator and a socket; for the pressure plate, a socket at the plate's own restitution; for the Cannon, its indicator's apex;
    /// for the Windmill, its output wheel's outer face.</summary>
    public enum Feature
    {
        LampBulb, SwitchButton, DelayHousing, DelayFace, RampSlab, BatteryTerminal, DetectorCollar, GateHousing, LogicFace, LogicLamp,
        CounterLamp, LatchIndicator, LatchSocket, ClockFace, HoldTimerFace, PlateSocket, NorFace, OrFace, XorFace, PipeShell, PipeCollar,
        GateCollar, Bend45Collar, Bend90Collar, FunnelCollar, AnchorEye, PulleyPost, PulleyRim, FanRing, ConveyorOutputPulley,
        ConveyorInputPulley, PusherSocket, CannonIndicator, WindmillOutput
    }

    /// <summary>On its back: −90° about X, the face up; a part-local point (x, y, z) is then at (x, z, −y) from the origin.</summary>
    private static readonly NumQuaternion Back = NumQuaternion.CreateFromAxisAngle(NumVector3.UnitX, -MathF.PI / 2);

    /// <summary>Face down: +90° about X; a part-local point (x, y, z) is then at (x, −z, y) from the origin.</summary>
    private static readonly NumQuaternion Front = NumQuaternion.CreateFromAxisAngle(NumVector3.UnitX, MathF.PI / 2);

    /// <summary>The element, its pose, the feature's top above the element's origin, the drop's x and z offsets, and the static
    /// restitution its element doc records.</summary>
    private static (PartId Part, NumQuaternion Rotation, double Top, float X, float Z, double Restitution) Drop(Feature feature) => feature switch
    {
        Feature.LampBulb => (new PartId("lamp"), NumQuaternion.Identity, .22 + .43, 0, 0, 1),       // CAT-035: bulb r 0.43 at y 0.22
        Feature.SwitchButton => (new PartId("switch"), NumQuaternion.Identity, .06 + .09, 0, 0, 1), // CAT-063: button h 0.18 at y 0.06
        Feature.DelayHousing => (new PartId("delay"), NumQuaternion.Identity, .625, 0, 0, 1),             // CAT-022: 1.25 m housing at the origin
        // CAT-022: lying on its back: the r 0.55 face disc's top is 0.32 + 0.0175 = 0.3375, 3.75 cm above the housing's 0.3 face.
        // Dropped 0.4 m off the axis, clear of the hub pip and the hand.
        Feature.DelayFace => (new PartId("delay"), Back, .32 + .0175, .4f, 0, 1),
        Feature.RampSlab => (new PartId("ramp"), NumQuaternion.Identity, .09, 0, 0, 1),                   // CAT-054: 0.18 m slab
        Feature.BatteryTerminal => (new PartId("battery"), NumQuaternion.Identity, .59 + .07, 0, 0, 1),   // CAT-005: terminal h 0.14 at y 0.59
        // CAT-002: turned over about X so the housing hangs below and the collar's top is clear: the drawn outer surface, 0.82 m.
        Feature.DetectorCollar => (new PartId("ball_detector"), new(1, 0, 0, 0), .82, 0, 0, .15),
        Feature.GateHousing => (new PartId("powered_gate"), NumQuaternion.Identity, 1.65 + .325, 0, 0, .1), // CAT-051: housing h 0.65 at y 1.65
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
        Feature.CounterLamp => (new PartId("counter"), Back, .4 + .1, 0, 0, 1),
        // CAT-037: the Latch on its back, onto its indicator lamp (r 0.09 at (0, −0.33, 0.42), so at z 0.33). The ActivationOut socket
        // (r 0.075 at (0, −0.56, 0.38)) is √(0.23² + 0.47²) = 0.52 from the resting centre, beyond the 0.415 at which it would touch.
        Feature.LatchIndicator => (new PartId("latch"), Back, .42 + .09, 0, .33f, 1),
        // CAT-037: the upright Latch, onto its SetIn socket (r 0.075 at (−0.45, 0.72, 0)), 17 cm above the housing's 0.625 top.
        Feature.LatchSocket => (new PartId("latch"), NumQuaternion.Identity, .72 + .075, -.45f, 0, 1),
        // CAT-017: the Clock on its back: the 1.14 × 1.44 × 0.04 face at z 0.32 tops out at 0.34, 4 cm above the housing. Dropped at
        // x 0.55, beside the recess (0.84 m wide, its top 2 cm higher): resting at 0.68 the ball clears the recess's edge once
        // √(0.34² − 0.32²) = 0.115 away horizontally; it is 0.13 away.
        Feature.ClockFace => (new PartId("clock"), Back, .32 + .02, .55f, 0, 1),
        // CAT-033: the Hold timer on its back: the 1.18 × 0.72 × 0.045 face at z 0.35 tops out at 0.3725, 4.75 cm above the body.
        // Dropped at x 0.4: resting there (centre 0.7125) the ball is √(0.4² + 0.22² + 0.3125²) = 0.55 from the indicator (r 0.07 at
        // (0, −0.22, 0.4)), beyond the 0.41 at which it would touch.
        Feature.HoldTimerFace => (new PartId("hold_timer"), Back, .35 + .0225, .4f, 0, 1),
        // CAT-052: the Pressure plate, onto its PowerIn socket (r 0.075 at (−1.18, −0.08, 0)), outside the base's edge at x −1.1. At
        // the socket the ball's centre is √(0.08² + 0.355²) = 0.364 from the base's top edge, beyond its 0.34 radius. The plate's
        // restitution is 0.1 (CAT-052).
        Feature.PlateSocket => (new PartId("pressure_plate"), NumQuaternion.Identity, -.08 + .075, -1.18f, 0, .1),
        // CAT-048: the default 3.6 m pipe lying along X; its shell's drawn top is the 0.7 m wall, dropped at the middle of its length.
        // The pipe family's documented restitution is 0.15.
        Feature.PipeShell => (new PartId("pipe"), NumQuaternion.Identity, .7, 0, 0, .15),
        // CAT-048: the end collar's drawn TorusMesh (inner 0.65, outer 0.78) at x 1.8 tops out at 0.78; its top is a point apex.
        // Bend plane laid flat: its start collar is a vertical torus with apex .78.
        Feature.Bend45Collar => (new PartId("pipe_bend_45"), Back, .78, -.91844f, -.182689f, .15),
        Feature.Bend90Collar => (new PartId("pipe_bend_90"), Back, .78, -1.697056f, -.702944f, .15),
        Feature.FunnelCollar => (new PartId("funnel"), NumQuaternion.Identity, .78, .9f, 0, .15),
        Feature.PipeCollar => (new PartId("pipe"), NumQuaternion.Identity, .78, 1.8f, 0, .15),
        // CAT-051: the Powered gate turned over about X (housing and rails below), onto its end collar's drawn 0.78 m tube at x 0.4.
        // Clear of the blade (top at 0.72, x within ±0.06: the ball reaches x 0.06 at 0.78) and the rails (|z| 0.82, below 0.2).
        Feature.GateCollar => (new PartId("powered_gate"), new(1, 0, 0, 0), .78, .4f, 0, .1),
        // CAT-058: the Anchor on its back, the eye lying flat at height 0.18: the top of its tube (centreline radius 0.2, tube 0.0275)
        // at x 0.2. The mount's top is 0.04 under the origin plane; the opening is 0.1725 m, narrower than the ball.
        Feature.AnchorEye => (new PartId("rope_anchor"), Back, .18 + .0275, .2f, 0, 1),
        // CAT-053: the Pulley face down: the post's back face (local z −0.19) is 0.19 above the origin plane, 0.105 m above the wheel's
        // 0.085 face. Dropped at world z 0.15, over the post's length (0.025 to 0.575): resting there (centre 0.53) the ball clears
        // the mount's near edge (z 0.475, height 0.3) by √(0.325² + 0.23²) = 0.398 m, beyond its 0.34 radius.
        Feature.PulleyPost => (new PartId("pulley"), Front, .19, 0, .15f, 1),
        // CAT-053: the Pulley on its back: the rim's tube (centreline radius 0.4, tube 0.035) lies flat at height 0.1, its top 0.135,
        // 0.05 above the wheel's 0.085 face. Dropped at x 0.4, over the tube.
        Feature.PulleyRim => (new PartId("pulley"), Back, .1 + .035, .4f, 0, 1),
        // CAT-028: the upright Fan: the top of the ring's tube (centreline radius 0.56, tube 0.07) at the origin's x and z; the
        // hub's top is 0.49 m lower. The ball is outside the Fan's stream: its cone has no radius at the source plane.
        Feature.FanRing => (new PartId("fan"), NumQuaternion.Identity, .56 + .07, 0, 0, 1),
        // CAT-019: the Conveyor on its back, onto the outer face of its output pulley wheel (r 0.22, faces at local z 0.78 ± 0.05,
        // centre (1.35, −0.07)) on the static body. Resting there (centre 1.17) the ball is 0.53 above the rail's top (z 0.64) and the
        // roller's end (0.65), so the face is its only contact; without the wheel's collider it would stop on the idle roller's end,
        // 0.18 m lower. Restitution 0.05 (CAT-019).
        Feature.ConveyorOutputPulley => (new PartId("conveyor"), Back, .78 + .05, 1.35f, .07f, .05),
        // CAT-019: the same on the input pulley, which rides the roller's hinged body: the ball loads it along the hinge axis.
        Feature.ConveyorInputPulley => (new PartId("conveyor"), Back, .78 + .05, -1.35f, .07f, .05),
        // CAT-039: the upright Linear pusher, onto its RetractedOut socket (r 0.07 at (−0.45, 0.4, 0.45)). Resting there (centre 0.81)
        // the ball is √(0.125² + 0.485²) = 0.50 from the barrel's top front edge (y 0.325, z 0.325) and 0.62 from the indicator
        // (r 0.065 at (−0.4, 0.2, 0.34)), beyond its 0.34 radius; without the socket's collider it would stop on that edge, at
        // 0.325 + √(0.34² − 0.125²), 0.169 m lower. Restitution 0 (CAT-039).
        Feature.PusherSocket => (new PartId("linear_pusher"), NumQuaternion.Identity, .4 + .07, -.45f, .45f, 0),
        // CAT-016: the upright Cannon, onto the apex of its indicator (r 0.07 at (−0.66, 0.65, 0.4)), 0.14 m above the breech's 0.58
        // top. Resting there (centre 1.06) the ball is 0.495 m from the rear collar's rim and 0.56 m from the jacket's, beyond its 0.34
        // radius; without the indicator's collider it would stop on the breech, 0.14 m lower. Restitution 0 (CAT-016).
        Feature.CannonIndicator => (new PartId("cannon"), NumQuaternion.Identity, .65 + .07, -.66f, .4f, 0),
        // CAT-070: the Windmill on its back, onto the outer face of its output wheel (r 0.24, length 0.12, centre (0.25, −0.1, 0.44)) at
        // local z 0.5. Dropped at local (0.40, −0.25), 0.15 m off the wheel's centre along both spoke directions: resting there (centre
        // 0.84) the ball's surface passes each spoke's edge, 0.135 m away, 13 mm above its top (15 mm proud of the face); the guard is
        // 0.485 m away along the axis. Without the wheel's collider it would stop on the nacelle's face (local z 0.275), 0.225 m lower.
        // Restitution 0 (CAT-070).
        Feature.WindmillOutput => (new PartId("windmill"), Back, .44 + .06, .4f, .25f, 0),
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
    [InlineData(Feature.AnchorEye)]
    [InlineData(Feature.PulleyPost)]
    [InlineData(Feature.PulleyRim)]
    [InlineData(Feature.FanRing)]
    public async Task ABallDroppedOntoEachSolidFeatureStopsAtItsDrawnSurfaceAndReboundsAtTheProductRestitution(Feature feature)
    {
        var (part, rotation, top, x, z, staticRestitution) = Drop(feature);
        IWorkshopInstance element = part == new PartId("pipe")
            ? WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("pipe"),Element, 0, (float)(Y), 0, (float)(rotation.X), (float)(rotation.Y), (float)(rotation.Z), (float)(rotation.W))
            : Place(part, Element, new(0, (float)Y, 0), rotation);
        // A torus's top is a point apex, like a sphere's: no stable rest.
        await AssertDropOnto(element, x, z, top, staticRestitution, feature.ToString(),
            OnSphereApex(part, rotation, x, z, top) || feature is Feature.PipeCollar or Feature.Bend45Collar or Feature.Bend90Collar or Feature.FunnelCollar
                or Feature.AnchorEye or Feature.PulleyRim or Feature.FanRing, feature == Feature.PlateSocket ? PlateFall : .5);
    }

    /// <summary>The Basketball dropped 0.5 m onto a newly solid feature of a part whose static restitution is 0.05 (Conveyor) or 0
    /// (Linear pusher, Cannon and Windmill). The product with the ball's 0.55 is at most 0.0275. At the 3.13 m/s arrival and T = 1/120 s the rebound bracket's
    /// slack g·T·(1 + e)/v_in is 0.0268, so its low bound for e = 0.0275 is near zero (about 0.0006) and the bracket cannot tell 0.0275
    /// from zero; only the stop is asserted: the first landed read (landing detected as
    /// <see cref="AssertDropOnto"/> detects it) lies in [rest − the envelope's temporary penetration (17 mm), rest + 5 mm], the drop
    /// theory's two bounds. After the stop the ball rises at most e·v_in·T = 0.7 mm in a commit, so that read is the stop, on a flat
    /// face or a sphere's apex alike. Without each collider the ball would first stop at least 0.14 m lower (see <see cref="Drop"/>).</summary>
    [Theory]
    [InlineData(Feature.ConveyorOutputPulley)]
    [InlineData(Feature.ConveyorInputPulley)]
    [InlineData(Feature.PusherSocket)]
    [InlineData(Feature.CannonIndicator)]
    [InlineData(Feature.WindmillOutput)]
    public async Task ABallDroppedOntoEachLowRestitutionFeatureStopsAtItsDrawnSurface(Feature feature)
    {
        var (part, rotation, top, x, z, _) = Drop(feature);
        await AssertStopOnto(Place(part, Element, new(0, (float)Y, 0), rotation), x, z, top, feature.ToString());
    }

    private static async Task AssertStopOnto(IWorkshopInstance element, float x, float z, double top, string feature)
    {
        var rest = Y + top + BallRadius;
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([element, BallAt(new(x, (float)(rest + .5), z))]));
        await ring.Advance(180);
        double vIn = 0;
        foreach (var read in ring.Reads)
        {
            var (y, vy) = (Position(read).Y, Velocity(read).Y);
            if (Landed(vIn, y - rest, vy))
            {
                Assert.True(y >= rest - Penetration && y <= rest + .005,
                    $"the ball first stopped at {y:F4} m, not on the {feature}'s drawn surface at {rest:F4} m");
                return;
            }
            if (vy < 0) vIn = -vy;
        }
        Assert.Fail($"the ball never landed on the {feature}");
    }

    /// <summary>Whether a commit is the ball's landing: after falling faster than 1 m/s it is within 5 cm of its rest height and has
    /// lost more than half its arrival speed.</summary>
    private static bool Landed(double vIn, double height, double vy) => vIn > 1 && height < .05 && vy > -.5 * vIn;

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
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([
            Place(new PartId("bumper"), Element, new(0, (float)Y, 0), turn),
            BallAt(new(corner.X, (float)(Y + surface + .2), 0))]));
        await ring.Advance(60);
        var firstRise = ring.Reads.FindIndex(read => Velocity(read).Y > 1);
        Assert.True(firstRise > 0, "the cap redirects the falling ball upwards");
        var beforeRise = ring.Reads.Take(firstRise + 1).ToArray();
        Assert.True(beforeRise.Min(read => Position(read).Y) >= Y + surface - .005,
            "the ball must rebound at the exposed cap, not sink through to the head");
        Assert.Contains(ring.Records, record => record.Kind == SimulationEventKind.Impact &&
            record.Source.Value == Element.Value && record.Magnitude > 0);
    }

    [Fact]
    public async Task ABallOutsideTheBumperSolidsFallsPastWithoutWorkOrPulse()
    {
        // Head .65 + outermost basketball seam .35 = 1 m: 5 cm clear of all solids.
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([
            Place(new PartId("bumper"), Element, new(0, (float)Y, 0), NumQuaternion.Identity),
            BallAt(new(1.05f, (float)Y + 1.2f, 0))]));
        await ring.Advance(60);
        Assert.True(Position(ring.Reads[^1]).Y < Y, "the ball passes the bumper");
        Assert.All(ring.Reads, read => Assert.Equal(32f, read.EnergyStores[0].Energy.Value));
        Assert.DoesNotContain(ring.Records, record => record.Kind == SimulationEventKind.Impact && record.Source.Value == Element.Value);
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
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([Place(new PartId("bumper"), Element, new(0, (float)Y, 0), turn),
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
        DeclaredPhysics.Colliders(RuntimeTestServices.Bundle, part).Any(collider => collider.ShapeKind == Gpu.ColliderShapeKind.Sphere &&
            (NumVector3.Transform(new(collider.LocalPose.Translation.X, collider.LocalPose.Translation.Y, collider.LocalPose.Translation.Z),
                rotation) + new NumVector3(0, collider.Radius, 0) - new NumVector3(x, (float)top, z)).Length() < 1e-4f);

    /// <summary>The restitution control for every static that keeps the neutral 1.0: the default Wall (3 × 2 × 0.25 m, CAT-066
    /// restitution 1) returns the Basketball at its own 0.55 under the product law, by the same drop and bounds.</summary>
    [Fact]
    public async Task ABallDroppedOntoAStaticWallReboundsAtItsOwnRestitution() =>
        await AssertDropOnto(WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("wall"), Element, 0, (float)(Y), 0, 0, 0, 0, 1, WallFixture.Default), 0, 0,
            WallFixture.Default[new("height")] * .5, 1, "Wall", apex: false, fall: .5);

    /// <summary>CAT-058, the control for the eye's collider: a Tennis ball of radius 0.1 m, narrower than the eye's 0.1725 m opening
    /// by 0.0725 m, dropped 0.5 m along the axis of the Anchor on its back, passes through the eye and first stops on the mount's face,
    /// 0.04 m above the origin plane, at that height plus its radius within 5 mm, read as the drop theory reads a first impact. It
    /// rebounds at its own 0.85. A disc in place of the eye's torus would stop it 0.2475 m higher.</summary>
    [Fact]
    public async Task ASmallBallPassesThroughTheAnchorEyeOntoTheMount() =>
        await AssertDropOnto(Place(new PartId("rope_anchor"), Element, new(0, (float)Y, 0), Back), 0, 0, -.04, 1,
            "Anchor mount", apex: true, fall: .5, SmallTennis);

    /// <summary>The Impact lever's inset and four marks are printing, not raised solids (owner decision, 21 November 2026): each is a
    /// quad facing up with the X and Z extent and position the old box had, lying between 0 and 0.5 mm above the beam's top face (the
    /// bound the printed marks of FaceMarkingTests use; the old boxes stood 8 and 10 mm proud), and every mark is above the inset.</summary>
    [Fact]
    public void TheLeversBeamMarkingsArePrintedFlatAboveItsTopFace()
    {
        var root = WorkshopResources.LoadScene(TestBundle.Entry(new PartId("impact_lever")).Metadata.ScenePath).Instantiate<Node3D>();
        try
        {
            var top = VerticesInRoot(root, root.GetNode<MeshInstance3D>("Visual/Beam/Visual_Beam")).Max(v => v.Y);
            (string Name, float X, float Width, float Depth)[] printed =
                [("Visual_Inset", 0, 3.38f, .82f), ("Visual_Mark_0", -1.2f, .035f, .3f), ("Visual_Mark_1", -.6f, .035f, .3f),
                 ("Visual_Mark_2", .6f, .035f, .3f), ("Visual_Mark_3", 1.2f, .035f, .3f)];
            var heights = new Dictionary<string, (float Lowest, float Highest)>();
            const float standoff = .0005f, lateral = .005f;
            foreach (var (name, x, width, depth) in printed)
            {
                var mesh = root.GetNode<MeshInstance3D>("Visual/Beam/" + name);
                Assert.IsType<QuadMesh>(mesh.Mesh);
                var vertices = VerticesInRoot(root, mesh);
                AssertFaces(vertices, Godot.Vector3.Up, name);
                // The height above the face is the print stand-off, 0.5 mm as in FaceMarkingTests; it also keeps the quad flat to it.
                Assert.All(vertices, v => Assert.InRange(v.Y - top, 0f, standoff));
                // The lateral bound is sized to the mark, not to the stand-off: 5 mm against a 35 mm wide, 0.3 m long mark excludes a
                // mark that is moved, resized or missing, which a player would see.
                Assert.InRange(vertices.Max(v => v.X) - vertices.Min(v => v.X), width - lateral, width + lateral);
                Assert.InRange(vertices.Max(v => v.Z) - vertices.Min(v => v.Z), depth - lateral, depth + lateral);
                Assert.InRange((vertices.Max(v => v.X) + vertices.Min(v => v.X)) / 2, x - lateral, x + lateral);
                Assert.InRange((vertices.Max(v => v.Z) + vertices.Min(v => v.Z)) / 2, -lateral, lateral);
                heights[name] = (vertices.Min(v => v.Y), vertices.Max(v => v.Y));
            }
            Assert.All(printed.Skip(1), mark => Assert.True(heights[mark.Name].Lowest > heights["Visual_Inset"].Highest, mark.Name + " lies under the inset"));
        }
        finally { root.Free(); }
    }

    /// <summary>A mesh's triangle vertices in the frame of the scene root: every node transform up to the root, composed.</summary>
    internal static Godot.Vector3[] VerticesInRoot(Node3D root, MeshInstance3D mesh)
    {
        var transform = mesh.Transform;
        for (var node = mesh.GetParent(); node != root; node = node.GetParent()) transform = ((Node3D)node).Transform * transform;
        return mesh.Mesh!.GetFaces().Select(v => transform * v).ToArray();
    }

    /// <summary>Every triangle faces <paramref name="facing"/> and the surface has a readable area (FaceMarkingTests' 1 cm²). Godot's
    /// front faces wind clockwise, so the cross product of a front face points against its facing.</summary>
    internal static void AssertFaces(Godot.Vector3[] triangles, Godot.Vector3 facing, string name)
    {
        Assert.NotEmpty(triangles);
        var area = 0f;
        for (var i = 0; i < triangles.Length; i += 3)
        {
            var cross = (triangles[i + 1] - triangles[i]).Cross(triangles[i + 2] - triangles[i]);
            area += cross.Length() / 2;
            Assert.True(cross.Normalized().Dot(facing) < -.99f, $"{name} does not face {facing}");
        }
        Assert.True(area > .0001f, name + " has no readable surface area");
    }

    /// <summary>A marking printed flat on a face whose outward normal is the scene root's +Z, the face plane at <paramref name="face"/>
    /// (read by the caller from the face's own mesh): a <c>QuadMesh</c> of the authored size, every triangle facing +Z with a readable
    /// area, lying between 0 and 0.5 mm in front of the face (the bound FaceMarkingTests uses for print; the thinnest raised box these
    /// replace, 12 mm deep, fails it), drawn in the colour the scene authors for it (<paramref name="albedo"/>, its drawn material's).
    /// Returns its vertices in the scene root's frame.</summary>
    internal static Godot.Vector3[] AssertPrintedFlat(Node3D root, MeshInstance3D mesh, float face, Godot.Vector2 size, Godot.Color albedo)
    {
        var name = mesh.Name.ToString();
        Assert.Equal(size, Assert.IsType<QuadMesh>(mesh.Mesh).Size);
        Assert.Equal(albedo, Assert.IsType<StandardMaterial3D>(mesh.GetActiveMaterial(0)).AlbedoColor);
        var vertices = VerticesInRoot(root, mesh);
        AssertFaces(vertices, Godot.Vector3.Back, name);
        Assert.All(vertices, v => Assert.InRange(v.Z - face, 0f, .0005f));
        return vertices;
    }

    /// <summary>Every vertex of a print lies within the X and Y extent of a rectangular face's own vertices.</summary>
    internal static void AssertInsideRectangle(Godot.Vector3[] print, Godot.Vector3[] face, string name) =>
        Assert.All(print, v => Assert.True(v.X >= face.Min(f => f.X) && v.X <= face.Max(f => f.X) && v.Y >= face.Min(f => f.Y) &&
            v.Y <= face.Max(f => f.Y), $"{name} reaches off its face at {v}"));

    /// <summary>Every vertex of a print lies within <paramref name="radius"/>, in X and Y, of the centre of a round face's own
    /// vertices.</summary>
    internal static void AssertInsideDisc(Godot.Vector3[] print, Godot.Vector3[] face, float radius, string name)
    {
        var centre = new Godot.Vector2((face.Min(f => f.X) + face.Max(f => f.X)) / 2, (face.Min(f => f.Y) + face.Max(f => f.Y)) / 2);
        Assert.All(print, v => Assert.True(new Godot.Vector2(v.X, v.Y).DistanceTo(centre) <= radius, $"{name} reaches off its face at {v}"));
    }

    /// <summary>A ball the drop theory drops: its radius, its own restitution, its linear drag (1/s) and how it is placed. The
    /// envelope's temporary visual penetration is 5 % of the smaller body's extent, the ball's radius.</summary>
    private sealed record DroppedBall(double Radius, double Restitution, double Drag, Func<NumVector3, IWorkshopInstance> At)
    {
        public double Penetration => PenetrationShare * Radius;
    }

    private static readonly DroppedBall Basketball = new(BallRadius, BallRestitution, BallDrag, at => BallAt(at));

    /// <summary>A Tennis ball of radius 0.1 m, bounce 0.85 and drag 0.04 1/s, small enough to pass the Anchor's eye.</summary>
    private static readonly DroppedBall SmallTennis = new(.1, .85, .04, at => TennisBallTests.Ball(Ball.Value, at.X, at.Y, radius: .1f, z: at.Z));

    private static async Task AssertDropOnto(IWorkshopInstance element, float x, float z, double top, double staticRestitution, string feature,
        bool apex, double fall, DroppedBall? dropped = null)
    {
        var ball = dropped ?? Basketball;
        var rest = Y + top + ball.Radius;
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([element, ball.At(new(x, (float)(rest + fall), z))]));
        await ring.Advance(180);
        double vIn = 0, lowest = double.PositiveInfinity, contact = double.NaN; double? vOut = null; var landed = false;
        WorkshopRead? before = null;
        foreach (var read in ring.Reads)
        {
            var (y, vy) = (Position(read).Y, Velocity(read).Y);
            if (!landed && Landed(vIn, y - rest, vy)) landed = true;
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
        Assert.True(stop >= rest - ball.Penetration, $"the ball sank to {stop:F4} m, below the {feature}'s surface contact {rest:F4} m");
        Assert.True(stop <= rest + .005, $"the ball stopped at {stop:F4} m, above the {feature}'s surface contact {rest:F4} m");
        var e = (double)((float)ball.Restitution * (float)staticRestitution);
        Assert.True(vOut is not null, $"the ball did not rebound from the {feature}");
        var ratio = vOut!.Value / vIn;
        var low = e - G * T * (1 + e) / vIn - 2 * ball.Drag * T * e - Math.Pow(2, -20);
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
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([
                Place(new PartId("switch"), Element, new(0, (float)Y, 0), tip),
                Place(new PartId("lamp"), Lamp, new(6, (float)Y, 0), NumQuaternion.Identity), BallAt(new(.33f, (float)Y + 1, z))],
            new WorkshopConnection(Element, PortId.ActivationOut, Lamp, PortId.ActivationIn, WorkshopConnectionDomain.Activation)));
        await ring.Advance(90);
        // Below the tipped base's lowest corner, (0.55 + 0.5)/√2 = 0.742 m under the origin, less the radius, the ball is past it.
        var past = Y - (.55 + .5) / Math.Sqrt(2) - BallRadius;
        var near = ring.Reads.Where(read => Position(read).Y > past).ToArray();
        var struck = near.Any(read => Velocity(read).X != 0f || Velocity(read).Z != 0f);
        var fired = ring.Records.Any(record => record.Kind == SimulationEventKind.TriggerActivated && record.Source == new GpuEventSourceId(ring.Construction.Instances.Identities[Element].Signals[new("signal")].Node.Value));
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
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([
                Place(new PartId("switch"), Element, new(0, (float)Y, 0), NumQuaternion.Identity),
                Place(new PartId("lamp"), Lamp, new(6, (float)Y, 0), NumQuaternion.Identity), BallAt(new(.58f, (float)(rest + .5), 0))],
            new WorkshopConnection(Element, PortId.ActivationOut, Lamp, PortId.ActivationIn, WorkshopConnectionDomain.Activation)));
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
        Assert.Contains(ring.Records, record => record.Kind == SimulationEventKind.TriggerActivated && record.Source == new GpuEventSourceId(ring.Construction.Instances.Identities[Element].Signals[new("signal")].Node.Value));
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
        var delay = Place(new PartId("delay"), Element, World(.7f, .09f + .72f + .075f + .002f), slope * NumQuaternion.CreateFromAxisAngle(NumVector3.UnitZ, MathF.PI / 2));
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([
            Place(new PartId("ramp"), Ramp, ramp, slope), delay, BallAt(World(-1.2f, .09f + (float)BallRadius + .002f))]));
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
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([Place(new PartId("ramp"), Ramp, ramp, slope), gate,
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
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([Place(new PartId("ramp"), Ramp, ramp, slope),
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
        var test = Place(new PartId("ramp"), Ramp, new(railX + .46f * width, slabTop - .09f, 0), turn,
            new([new(new("length"), 3f), new(new("width"), width)]));
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([Place(new PartId("ramp"), new(4), chuteAt, chute), test,
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
