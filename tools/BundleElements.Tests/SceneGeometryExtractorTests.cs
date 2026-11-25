using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.BundleElements.Tests;

public sealed class SceneGeometryExtractorTests
{
    private const string BoxScene = """
        [gd_scene format=3]
        [sub_resource type="BoxShape3D" id="Box"]
        size = Vector3(2, 4, 6)
        [node name="Root" type="Node3D"]
        [node name="Visual" type="Node3D" parent="."]
        position = Vector3(3, 0, 0)
        [node name="col_box" type="CollisionShape3D" parent="Visual"]
        position = Vector3(0, 2, 0)
        shape = SubResource("Box")
        """;
    private static readonly MaterialProperties Surface = new(.1f, .3f, 0f, .1f);
    private static BundleEntry Empty => BundleSchema.Entry(new Fixture { Colliders = [] });
    private static BundleEntry Bake(string text) => SceneGeometryExtractor.Bake(text, Empty, [new(".", Surface)]);

    [Fact]
    public void BakesF32GeometryAndAccumulatesParentTransforms()
    {
        var collider = Assert.Single(Bake(BoxScene).Colliders);
        Assert.Equal(new Vector3(1, 2, 3), collider.HalfExtents);
        Assert.Equal(new Vector3(3, 2, 0), collider.Offset);
        Assert.Equal(Quaternion.Identity, collider.Rotation);
        Assert.Equal(Surface, collider.Material);
    }

    [Fact]
    public void GodotTextBasisRowsBecomeTheSameOutwardMouthRotation()
    {
        var scene = BoxScene + "\n[node name=\"Loc_Port_mouth_end\" type=\"Node3D\" parent=\".\"]\n" +
            "transform = Transform3D(0, 1, 0, -1, 0, 0, 0, 0, 1, 0.702944, -1.787056, 0)\nmetadata/bore_radius = 0.65";
        var mouth = Assert.Single(Bake(scene).TubeMouths);
        Assert.True(Vector3.Distance(Vector3.Transform(Vector3.UnitX, mouth.Rotation), -Vector3.UnitY) < 1e-6f);
        Assert.Equal(new Vector3(.702944f, -1.787056f, 0), mouth.Position);
    }

    [Fact]
    public void NamedBoxAndSphereMeshesAreBuildTimeProxies()
    {
        var box = BoxScene.Replace("BoxShape3D", "BoxMesh").Replace("CollisionShape3D", "MeshInstance3D").Replace("shape =", "mesh =");
        Assert.Equal(new Vector3(1, 2, 3), Assert.Single(Bake(box).Colliders).HalfExtents);
        var sphere = box.Replace("BoxMesh", "SphereMesh").Replace("size = Vector3(2, 4, 6)", "radius = 0.34");
        Assert.Equal(ColliderShapeKind.Sphere, Assert.Single(Bake(sphere).Colliders).ShapeKind);
        Assert.Equal(new Vector3(.34f), Assert.Single(Bake(sphere).Colliders).HalfExtents);
    }

    /// <summary>A collider proxy is named only by its <c>col_</c> prefix: a box mesh named with a <c>-col_</c> infix, which an older
    /// rule baked as a proxy, is refused as <see cref="BundleErrorCode.ProxyNameInfix"/> with a reason naming the node, in either case;
    /// the same mesh named <c>col_box</c> bakes, and so does a prefixed name that also carries the infix (<c>col_box-col_b</c>).</summary>
    [Theory]
    [InlineData("Box-col_box")]
    [InlineData("Box-COL_box")]
    public void ANodeNamedWithAColInfixInsteadOfThePrefixIsRefused(string name)
    {
        var mesh = BoxScene.Replace("BoxShape3D", "BoxMesh").Replace("CollisionShape3D", "MeshInstance3D").Replace("shape =", "mesh =");
        Assert.Single(Bake(mesh).Colliders);
        Assert.Single(Bake(mesh.Replace("name=\"col_box\"", "name=\"col_box-col_b\"")).Colliders);
        var refusal = Assert.Throws<SceneGeometryRefusalException>(() => Bake(mesh.Replace("name=\"col_box\"", $"name=\"{name}\"")));
        Assert.Equal(BundleErrorCode.ProxyNameInfix, refusal.Code);
        Assert.Contains("Visual/" + name, refusal.Message, StringComparison.Ordinal);
        // The scene root (path ".") is refused under its own name.
        var root = Assert.Throws<SceneGeometryRefusalException>(() => Bake(mesh.Replace("name=\"Root\"", $"name=\"{name}\"")));
        Assert.Equal(BundleErrorCode.ProxyNameInfix, root.Code);
        Assert.EndsWith(": " + name, root.Message, StringComparison.Ordinal);
    }

    /// <summary>The code of the refusal <paramref name="bake"/> raises; it fails unless the extractor refuses with its typed refusal.</summary>
    private static BundleErrorCode RefusalCode(Func<BundleEntry> bake) => Assert.Throws<SceneGeometryRefusalException>(() => bake()).Code;

    /// <summary>Geometry the bake cannot take is refused under the code of its kind. The shape reference <c>SubResource(")</c>, whose
    /// two delimiters overlap, is not a local sub-resource reference and is refused as one, not read past its end.</summary>
    [Theory]
    [InlineData("size = Vector3(2, 4, 6)", "size = Vector3(0, 4, 6)", BundleErrorCode.OutOfRange)]
    [InlineData("size = Vector3(2, 4, 6)", "size = Vector3(-2, 4, 6)", BundleErrorCode.OutOfRange)]
    [InlineData("size = Vector3(2, 4, 6)", "size = Vector3(NaN, 4, 6)", BundleErrorCode.NonFinite)]
    [InlineData("position = Vector3(0, 2, 0)", "scale = Vector3(-1, 1, 1)", BundleErrorCode.OutOfRange)]
    [InlineData("position = Vector3(3, 0, 0)", "scale = Vector3(1, 0, 1)", BundleErrorCode.OutOfRange)]
    [InlineData("size = Vector3(2, 4, 6)", "", BundleErrorCode.UnsupportedColliderProxy)]
    [InlineData("shape = SubResource(\"Box\")", "shape = SubResource(\")", BundleErrorCode.UnsupportedColliderProxy)]
    [InlineData("type=\"Node3D\" parent=\".\"", "type=\"Node3D\" parent=\"missing\"", BundleErrorCode.UnresolvedReference)]
    [InlineData("name=\"Root\" type=\"Node3D\"", "name=\"Root\" type=\"Node\"", BundleErrorCode.MalformedScene)]
    public void MalformedGeometryIsRejected(string before, string after, BundleErrorCode code) =>
        Assert.Equal(code, RefusalCode(() => Bake(BoxScene.Replace(before, after))));

    /// <summary>Scene text the parser cannot read is refused under its own code, one case per parser refusal: a value stated twice; a
    /// section attribute stated twice; a reused sub-resource id; a sub-resource without its id; a sub-resource without its type; a node
    /// without a name; a node name containing '/' (which would make its path ambiguous with a body root's); a line that is not a one-line
    /// <c>key = value</c> (<c>position=Vector3(…)</c>, which an older parser dropped, baking the proxy at its default pose); a value
    /// continued onto a later line (Godot's multi-line dictionary form, which no shipped scene uses); a value line before the first
    /// section (which an older parser dropped); and a section header without its closing bracket.</summary>
    [Theory]
    [InlineData("size = Vector3(2, 4, 6)", "size = Vector3(2, 4, 6)\nsize = Vector3(2, 4, 6)", BundleErrorCode.MalformedScene)]
    [InlineData("[node name=\"Visual\" type=\"Node3D\" parent=\".\"]", "[node name=\"Visual\" type=\"Node3D\" type=\"Node3D\" parent=\".\"]",
        BundleErrorCode.MalformedScene)]
    [InlineData("[node name=\"Root\" type=\"Node3D\"]", "[sub_resource type=\"BoxShape3D\" id=\"Box\"]\nsize = Vector3(1, 1, 1)\n[node name=\"Root\" type=\"Node3D\"]",
        BundleErrorCode.DuplicateId)]
    [InlineData("[sub_resource type=\"BoxShape3D\" id=\"Box\"]", "[sub_resource type=\"BoxShape3D\"]", BundleErrorCode.MalformedScene)]
    [InlineData("[sub_resource type=\"BoxShape3D\" id=\"Box\"]", "[sub_resource id=\"Box\"]", BundleErrorCode.MalformedScene)]
    [InlineData("[node name=\"Visual\" type=\"Node3D\" parent=\".\"]", "[node type=\"Node3D\" parent=\".\"]", BundleErrorCode.MalformedScene)]
    [InlineData("name=\"col_box\"", "name=\"A/col_box\"", BundleErrorCode.MalformedScene)]
    [InlineData("position = Vector3(0, 2, 0)", "position=Vector3(0, 2, 0)", BundleErrorCode.MalformedScene)]
    [InlineData("position = Vector3(3, 0, 0)", "position = Vector3(3, 0, 0)\nmetadata/rows = {\n\"a\": 1\n}", BundleErrorCode.MalformedScene)]
    [InlineData("[gd_scene format=3]", "position = Vector3(1, 2, 3)\n[gd_scene format=3]", BundleErrorCode.MalformedScene)]
    [InlineData("[node name=\"Visual\" type=\"Node3D\" parent=\".\"]", "[node name=\"Visual\" type=\"Node3D\" parent=\".\"", BundleErrorCode.MalformedScene)]
    public void UnreadableSceneTextIsRefusedUnderItsCode(string before, string after, BundleErrorCode code)
    {
        Assert.Contains(before, BoxScene, StringComparison.Ordinal);
        Assert.Equal(code, RefusalCode(() => Bake(BoxScene.Replace(before, after))));
    }

    // Story 10.1: a Godot cylinder proxy (axis local +Y) bakes as the true Cylinder (r, height / 2, r).
    private const string CylinderScene = """
        [gd_scene format=3]
        [sub_resource type="CylinderShape3D" id="Wheel"]
        radius = 0.4
        height = 0.17
        [node name="Root" type="Node3D"]
        [node name="col_cylinder" type="CollisionShape3D" parent="."]
        position = Vector3(0, 0.5, 0)
        shape = SubResource("Wheel")
        """;

    [Fact]
    public void CylinderShapesAndUntaperedMeshesBakeAsTrueCylinders()
    {
        var collider = Assert.Single(Bake(CylinderScene).Colliders);
        Assert.Equal(ColliderShapeKind.Cylinder, collider.ShapeKind);
        Assert.Equal(new Vector3(.4f, .085f, .4f), collider.HalfExtents);
        Assert.Equal(new Vector3(0, .5f, 0), collider.Offset);

        var mesh = CylinderScene.Replace("CylinderShape3D", "CylinderMesh").Replace("radius = 0.4", "top_radius = 0.4\nbottom_radius = 0.4")
            .Replace("CollisionShape3D", "MeshInstance3D").Replace("shape =", "mesh =");
        Assert.Equal(new Vector3(.4f, .085f, .4f), Assert.Single(Bake(mesh).Colliders).HalfExtents);

        var closed = Assert.Single(Bake(mesh.Replace("height = 0.17", "height = 0.17\ncap_top = true\ncap_bottom = true")).Colliders);
        Assert.Equal(ColliderShapeKind.Cylinder, closed.ShapeKind);
        Assert.Equal(new Vector3(.4f, .085f, .4f), closed.HalfExtents);
        Assert.Equal(BundleErrorCode.MalformedScene, RefusalCode(() => Bake(mesh.Replace("height = 0.17", "height = 0.17\ncap_top = invalid"))));

        // The height scales by scale.Y, the radius by the equal radial scale.
        var scaled = Assert.Single(Bake(CylinderScene.Replace("position = Vector3(0, 0.5, 0)", "scale = Vector3(2, 3, 2)")).Colliders);
        Assert.Equal(new Vector3(.8f, .085f * 3, .8f), scaled.HalfExtents);
    }

    [Theory]
    [InlineData("radius = 0.4", "", BundleErrorCode.UnsupportedColliderProxy)]
    [InlineData("height = 0.17", "", BundleErrorCode.UnsupportedColliderProxy)]
    [InlineData("radius = 0.4", "radius = 0", BundleErrorCode.OutOfRange)]
    [InlineData("height = 0.17", "height = -1", BundleErrorCode.OutOfRange)]
    [InlineData("radius = 0.4", "radius = NaN", BundleErrorCode.NonFinite)]
    [InlineData("position = Vector3(0, 0.5, 0)", "scale = Vector3(2, 1, 1)", BundleErrorCode.UnsupportedColliderProxy)]
    [InlineData("name=\"col_cylinder\"", "name=\"col_capsule\"", BundleErrorCode.UnsupportedColliderProxy)]
    [InlineData("name=\"col_cylinder\"", "name=\"col_hull\"", BundleErrorCode.UnsupportedColliderProxy)]
    [InlineData("CylinderShape3D", "CapsuleShape3D", BundleErrorCode.UnsupportedColliderProxy)]
    public void MalformedCylindersAreRejected(string before, string after, BundleErrorCode code)
    {
        Assert.Contains(before, CylinderScene, StringComparison.Ordinal);
        Assert.Equal(code, RefusalCode(() => Bake(CylinderScene.Replace(before, after))));
    }

    [Theory]
    [InlineData("top_radius = 0.4\nbottom_radius = 0.3")]
    [InlineData("top_radius = 0.4")]
    [InlineData("bottom_radius = 0.4")]
    public void ATaperedOrUnstatedCylinderMeshIsRejected(string radii)
    {
        var mesh = CylinderScene.Replace("CylinderShape3D", "CylinderMesh").Replace("radius = 0.4", radii)
            .Replace("CollisionShape3D", "MeshInstance3D").Replace("shape =", "mesh =");
        Assert.Equal(BundleErrorCode.UnsupportedColliderProxy, RefusalCode(() => Bake(mesh)));
    }

    [Fact]
    public void ACylinderMeshThatIsNoProxyStaysArt()
    {
        var scene = BoxScene.Replace("[node name=\"Root\" type=\"Node3D\"]",
            "[sub_resource type=\"CylinderMesh\" id=\"Collar\"]\ntop_radius = 0.5\nbottom_radius = 0.2\n[node name=\"Root\" type=\"Node3D\"]") +
            "\n[node name=\"Collar\" type=\"MeshInstance3D\" parent=\".\"]\nmesh = SubResource(\"Collar\")";
        Assert.Equal(ColliderShapeKind.Box, Assert.Single(Bake(scene).Colliders).ShapeKind);
    }

    /// <summary>Decision 4: a package's colliders are its scene's proxies. A declaration that states data colliders is refused as
    /// <see cref="BundleErrorCode.SceneColliderOwnership"/>: on the base, whether its scene has a proxy or is art only (which an older
    /// rule baked from the data), and on a slider's or a hinge's moving body. Controls: the box scene with no data colliders bakes its
    /// one proxy, and the art-only scene with no data colliders is refused as a body without a collider, not as an ownership
    /// refusal.</summary>
    [Fact]
    public void DeclarationDataCollidersAreRefusedOnEveryBodyWhateverTheScene()
    {
        const string artOnly = "[node name=\"Root\" type=\"Node3D\"]";
        Assert.Single(Bake(BoxScene).Colliders);
        Assert.Equal(BundleErrorCode.BodyWithoutCollider, RefusalCode(() => Bake(artOnly)));
        var data = Empty with { Colliders = [Fixture.Box(new(.5f, .5f, .5f))] };
        Assert.Equal(BundleErrorCode.SceneColliderOwnership, RefusalCode(() => SceneGeometryExtractor.Bake(BoxScene, data, [new(".", Surface)])));
        Assert.Equal(BundleErrorCode.SceneColliderOwnership, RefusalCode(() => SceneGeometryExtractor.Bake(artOnly, data, [new(".", Surface)])));
        var movingData = Empty with { Mechanical = Empty.Mechanical! with { Sliders = [Fixture.Slider()] } };
        Assert.Equal(BundleErrorCode.SceneColliderOwnership, RefusalCode(() => SceneGeometryExtractor.Bake(BoxScene, movingData,
            [new(".", Surface), new("Visual", Surface)])));
        var hinge = default(RevoluteHingeDescriptor) with { MovingBody = new(Fixture.Slider().MovingBody.MassFrame, [Fixture.Box(new(.5f, .5f, .5f))]) };
        var hingeData = Empty with { Mechanical = Empty.Mechanical! with { Hinges = [hinge] } };
        Assert.Equal(BundleErrorCode.SceneColliderOwnership, RefusalCode(() => SceneGeometryExtractor.Bake(BoxScene, hingeData,
            [new(".", Surface), new("Visual", Surface)])));
    }

    /// <summary>The scene ownership names the base at the scene root, then each moving body once, at disjoint roots; otherwise the bake
    /// is refused as <see cref="BundleErrorCode.SceneColliderOwnership"/>: a list of the wrong count (two bodies for a part with no
    /// moving body), a first root other than ".", or a moving root nested under another. Control:
    /// <see cref="MovingSubtreeBelongsOnlyToItsDeclaredBody"/>, which bakes a valid base and slider ownership.</summary>
    [Theory]
    [InlineData(0, ".", "Visual")]
    [InlineData(0, "Visual")]
    [InlineData(2, ".", "Visual", "Visual/col_box")]
    public void AnOwnershipThatDoesNotNameTheBaseAndEachMovingBodyOnceIsRefused(int sliders, params string[] roots)
    {
        var entry = Empty with { Mechanical = Empty.Mechanical! with { Sliders = Enumerable.Repeat(Fixture.Slider(moving: []), sliders).ToArray() } };
        Assert.Equal(BundleErrorCode.SceneColliderOwnership,
            RefusalCode(() => SceneGeometryExtractor.Bake(BoxScene, entry, roots.Select(root => new SceneBodyGeometry(root, Surface)).ToArray())));
    }

    [Fact]
    public void MovingSubtreeBelongsOnlyToItsDeclaredBody()
    {
        var scene = BoxScene + """

            [node name="BaseCollider" type="CollisionShape3D" parent="."]
            shape = SubResource("Box")
            """;
        var entry = Empty with { Mechanical = Empty.Mechanical! with { Sliders = [Fixture.Slider(moving: [])] } };
        var baked = SceneGeometryExtractor.Bake(scene, entry, [new(".", Surface), new("Visual", Surface)]);
        Assert.Single(baked.Colliders);
        var moving = Assert.Single(Assert.Single(baked.Mechanical!.Sliders).MovingBody.Colliders);
        Assert.Equal(new Vector3(0, 2, 0), moving.Offset);
        Assert.Equal(BundleErrorCode.SceneColliderOwnership,
            RefusalCode(() => SceneGeometryExtractor.Bake(scene, entry, [new(".", Surface), new(".", Surface)])));
        Assert.Equal(BundleErrorCode.UnresolvedReference,
            RefusalCode(() => SceneGeometryExtractor.Bake(scene, entry, [new(".", Surface), new("missing", Surface)])));
        Assert.Equal(BundleErrorCode.BodyWithoutCollider,
            RefusalCode(() => SceneGeometryExtractor.Bake(BoxScene, entry, [new(".", Surface), new("Visual", Surface)])));
    }

    [Theory]
    [InlineData("0.65", null)]
    [InlineData("0", BundleErrorCode.OutOfRange)]
    [InlineData("-1", BundleErrorCode.OutOfRange)]
    [InlineData("NaN", BundleErrorCode.NonFinite)]
    [InlineData("\"0.65\"", BundleErrorCode.MalformedScene)]
    public void MouthBoreIsBakedOrRejected(string bore, BundleErrorCode? refused)
    {
        var scene = BoxScene + "\n[node name=\"Loc_Port_mouth_end\" type=\"Node3D\" parent=\".\"]\nposition = Vector3(1, 0, 0)\nmetadata/bore_radius = " + bore;
        if (refused is { } code)
        {
            Assert.Equal(code, RefusalCode(() => Bake(scene)));
            return;
        }
        var mouth = Assert.Single(Bake(scene).TubeMouths);
        Assert.Equal(TubeEnd.End, mouth.End);
        Assert.Equal(.65f, mouth.BoreRadius);
        Assert.Equal(Vector3.UnitX, mouth.Position);
    }

    [Fact]
    public void MissingAndUnknownMouthsAreRejected()
    {
        Assert.Equal(BundleErrorCode.MalformedScene, Assert.Throws<SceneGeometryRefusalException>(
            () => Bake(BoxScene + "\n[node name=\"Loc_Port_mouth_end\" type=\"Node3D\" parent=\".\"]")).Code);
        // The generator's locator check refuses this locator first under the same code; the extractor refuses it on its own.
        Assert.Equal(BundleErrorCode.UndeclaredPortLocator, Assert.Throws<SceneGeometryRefusalException>(
            () => Bake(BoxScene + "\n[node name=\"Loc_Port_mouth_side\" type=\"Node3D\" parent=\".\"]")).Code);
        // A second End mouth (under another parent, so its path differs) is a duplicate mouth.
        static string EndMouth(string parent) => $"\n[node name=\"Loc_Port_mouth_end\" type=\"Node3D\" parent=\"{parent}\"]\nmetadata/bore_radius = 0.65";
        Assert.Equal(BundleErrorCode.UndeclaredPortLocator, RefusalCode(() => Bake(BoxScene + EndMouth(".") + EndMouth("Visual"))));
    }

    /// <summary>FO5-2: the locator check and the extractor read tube-mouth names by one rule (<see cref="PackageValidator.TubeMouth"/>):
    /// a name with the <c>mouth_</c> prefix names a tube mouth, never a declared socket. A declared port so named is refused on its id
    /// (<see cref="BundleErrorCode.OutOfRange"/>). Its locator <c>mouth_x</c> names neither end and is refused by both checks as
    /// <see cref="BundleErrorCode.UndeclaredPortLocator"/>; its locator <c>mouth_start</c> is a tube mouth to both, and the extractor
    /// bakes it. Control: the same port and locator named <c>socket_x</c> are accepted by both, and the extractor bakes no tube
    /// mouth.</summary>
    [Theory]
    [InlineData("mouth_x", BundleErrorCode.UndeclaredPortLocator, 0, BundleErrorCode.UndeclaredPortLocator, BundleErrorCode.OutOfRange)]
    [InlineData("mouth_start", null, 1, BundleErrorCode.OutOfRange)]
    [InlineData("socket_x", null, 0)]
    public void ADeclaredPortNamedLikeATubeMouthIsTreatedAlikeByTheLocatorCheckAndTheExtractor(string name, BundleErrorCode? baked, int mouths,
        params BundleErrorCode[] located)
    {
        var port = new PortDescriptor(new PortId(name), SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn,
            new(name, SocketGlyph.Text), null, Vector3.Zero, -Vector3.UnitX);
        var scene = BoxScene + $"\n[node name=\"Loc_Port_{name}\" type=\"Node3D\" parent=\".\"]\nmetadata/bore_radius = 0.65";
        var errors = InPackage(scene, root => PackageValidator.Locators(root, [new BundlePackage("Fixture", new SceneOwnedFixture { Ports = [port] })]));
        Assert.Equal(located, errors.Select(error => error.Code));
        if (baked is { } code)
        {
            Assert.Equal(code, RefusalCode(() => Bake(scene)));
            return;
        }
        Assert.Equal(mouths, Bake(scene).TubeMouths.Count);
    }

    /// <summary>Tube-mouth names are matched with their case: <c>Loc_Port_Mouth_end</c> does not take the <c>mouth_</c> prefix, so the
    /// locator check refuses it as naming no declared socket (<see cref="BundleErrorCode.UndeclaredPortLocator"/>) and the extractor
    /// bakes no tube mouth for it. Control: <c>Loc_Port_mouth_end</c> is accepted by the locator check and bakes the End mouth.</summary>
    [Theory]
    [InlineData("Mouth_end", true)]
    [InlineData("mouth_end", false)]
    public void ATubeMouthNameIsMatchedWithItsCase(string name, bool refused)
    {
        var scene = BoxScene + $"\n[node name=\"Loc_Port_{name}\" type=\"Node3D\" parent=\".\"]\nmetadata/bore_radius = 0.65";
        var errors = InPackage(scene, root => PackageValidator.Locators(root, [new BundlePackage("Fixture", new SceneOwnedFixture())]));
        BundleErrorCode[] expected = refused ? [BundleErrorCode.UndeclaredPortLocator] : [];
        Assert.Equal(expected, errors.Select(error => error.Code));
        Assert.Equal(refused ? 0 : 1, Bake(scene).TubeMouths.Count);
    }

    [Fact]
    public void GeometryBindingsRejectUnknownOutOfRangeAndDuplicateTargets()
    {
        var entry = BundleSchema.Entry(new Fixture()) with
        {
            ColliderBindings = [new(0, GeometryComponent.HalfExtentX, "missing", .5f, 0f)]
        };
        Assert.NotEmpty(BundleValidator.Validate([new("Fixture", entry)]));
        entry = entry with { ColliderBindings = [new(50, (GeometryComponent)255, "missing", float.NaN, 0f)] };
        Assert.NotEmpty(BundleValidator.Validate([new("Fixture", entry)]));
        var binding = new ColliderParameterBinding(0, GeometryComponent.OffsetX, "stiffness", 0f, 0f);
        entry = BundleSchema.Entry(new Fixture { Parameters = Fixture.SpringParameters }) with { ColliderBindings = [binding, binding] };
        Assert.NotEmpty(BundleValidator.Validate([new("Fixture", entry)]));
    }

    [Theory]
    [InlineData(0f, false)]
    [InlineData(2f, false)]
    [InlineData(1f, true)]
    public void BundleMouthOrientationMustBeUnitLength(float w, bool accepted)
    {
        var entry = BundleSchema.Entry(new Fixture()) with
        { TubeMouths = [new(TubeEnd.End, Vector3.UnitX, new Quaternion(0, 0, 0, w), .65f)] };
        Assert.Equal(accepted, BundleValidator.Validate([new("Fixture", entry)]).Count == 0);
    }

    [Fact]
    public void FixedAndParameterMechanicalValuesRejectAmbiguousOrMissingInputs()
    {
        Assert.Equal(14f, MechanicalValue.Fixed(14f).Evaluate(new Dictionary<string, float>()));
        Assert.Equal(400f, MechanicalValue.FromParameter("stiffness").Evaluate(new Dictionary<string, float> { ["stiffness"] = 400f }));
        Assert.Throws<ArgumentException>(() => MechanicalValue.FromParameter("missing").Evaluate(new Dictionary<string, float>()));
        Assert.Throws<ArgumentException>(() => new MechanicalValue(MechanicalValueSource.Constant, 14f, "stiffness").Evaluate(new Dictionary<string, float>()));
    }

    // engine-hollow-shapes: an open cylinder mesh proxy with its wall bakes as a Tube or Frustum, a torus mesh proxy as a Torus.
    private const string OpenScene = """
        [gd_scene format=3]
        [sub_resource type="CylinderMesh" id="Shell"]
        top_radius = 0.7
        bottom_radius = 0.7
        height = 3.6
        cap_top = false
        cap_bottom = false
        [node name="Root" type="Node3D"]
        [node name="col_shell" type="MeshInstance3D" parent="."]
        position = Vector3(0, 0.5, 0)
        mesh = SubResource("Shell")
        metadata/wall_thickness = 0.05
        """;

    private const string TorusScene = """
        [gd_scene format=3]
        [sub_resource type="TorusMesh" id="Collar"]
        inner_radius = 0.65
        outer_radius = 0.78
        [node name="Root" type="Node3D"]
        [node name="col_collar" type="MeshInstance3D" parent="."]
        position = Vector3(1.8, 0, 0)
        mesh = SubResource("Collar")
        """;

    [Fact]
    public void AnOpenCylinderProxyWithItsWallBakesAsATubeAndATaperedOneAsAFrustum()
    {
        // Tube: radius lane the outer 0.7, lanes (inner 0.7 − 0.05, half-length 1.8, 0).
        var tube = Assert.Single(Bake(OpenScene).Colliders);
        Assert.Equal((ColliderShapeKind.Tube, .7f, new Vector3(.7f - .05f, 1.8f, 0f), new Vector3(0, .5f, 0)),
            (tube.ShapeKind, tube.Radius, tube.HalfExtents, tube.Offset));
        // Frustum: radius lane the outer 1.35 at −Y (the bottom), lanes (wall 0.05, half-length 1.8, outer 0.7 at +Y).
        var frustum = Assert.Single(Bake(OpenScene.Replace("bottom_radius = 0.7", "bottom_radius = 1.35")).Colliders);
        Assert.Equal((ColliderShapeKind.Frustum, 1.35f, new Vector3(.05f, 1.8f, .7f)), (frustum.ShapeKind, frustum.Radius, frustum.HalfExtents));
        // A uniform scale scales every lane.
        var scaled = Assert.Single(Bake(OpenScene.Replace("position = Vector3(0, 0.5, 0)", "scale = Vector3(2, 2, 2)")).Colliders);
        Assert.Equal((1.4f, new Vector3((.7f - .05f) * 2f, 3.6f, 0f)), (scaled.Radius, scaled.HalfExtents));
    }

    [Fact]
    public void ATorusProxyBakesAsAFullSolidTorusOrItsDeclaredSegmentAndWall()
    {
        // Centreline (0.65 + 0.78) / 2, tube (0.78 − 0.65) / 2, solid, the full turn.
        var full = Assert.Single(Bake(TorusScene).Colliders);
        Assert.Equal((ColliderShapeKind.Torus, (.65f + .78f) * .5f, new Vector3(0f, (.78f - .65f) * .5f, MathF.Tau), new Vector3(1.8f, 0, 0)),
            (full.ShapeKind, full.Radius, full.HalfExtents, full.Offset));
        var segment = Assert.Single(Bake(TorusScene.Replace("inner_radius = 0.65\nouter_radius = 0.78", "inner_radius = 1.7\nouter_radius = 3.1")
            .Replace("mesh = SubResource(\"Collar\")", "mesh = SubResource(\"Collar\")\nmetadata/sweep_degrees = 45.0\nmetadata/wall_thickness = 0.05")).Colliders);
        Assert.Equal((2.4f, new Vector3((3.1f - 1.7f) * .5f - .05f, (3.1f - 1.7f) * .5f, 45f * (MathF.PI / 180f))),
            (segment.Radius, segment.HalfExtents));
    }

    [Theory]
    [InlineData("cap_top = false\n", "", BundleErrorCode.UnsupportedColliderProxy)] // one cap open
    [InlineData("cap_bottom = false\n", "", BundleErrorCode.UnsupportedColliderProxy)]
    [InlineData("cap_top = false", "cap_top = true", BundleErrorCode.UnsupportedColliderProxy)]
    [InlineData("metadata/wall_thickness = 0.05", "", BundleErrorCode.UnsupportedColliderProxy)] // an open cylinder states its wall
    [InlineData("metadata/wall_thickness = 0.05", "metadata/wall_thickness = NaN", BundleErrorCode.NonFinite)]
    [InlineData("position = Vector3(0, 0.5, 0)", "scale = Vector3(1, 2, 1)", BundleErrorCode.UnsupportedColliderProxy)] // nonuniform scale
    public void AMalformedOpenCylinderProxyIsRejected(string before, string after, BundleErrorCode code)
    {
        Assert.Contains(before, OpenScene, StringComparison.Ordinal);
        Assert.Equal(code, RefusalCode(() => Bake(OpenScene.Replace(before, after))));
    }

    [Theory]
    [InlineData("inner_radius = 0.65\n", "")] // a torus states both radii
    [InlineData("outer_radius = 0.78\n", "")]
    [InlineData("position = Vector3(1.8, 0, 0)", "scale = Vector3(1, 1, 2)")] // nonuniform scale
    public void AMalformedTorusProxyIsRejected(string before, string after)
    {
        Assert.Contains(before, TorusScene, StringComparison.Ordinal);
        Assert.Equal(BundleErrorCode.UnsupportedColliderProxy, RefusalCode(() => Bake(TorusScene.Replace(before, after))));
    }

    /// <summary>The result of <paramref name="act"/> on a temporary repository root holding one package, <c>Fixture</c>, whose scene is
    /// <paramref name="scene"/>, with its icon.</summary>
    private static T InPackage<T>(string scene, Func<string, T> act)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var package = Path.Combine(root, "elements", "Fixture");
        Directory.CreateDirectory(package);
        try
        {
            File.WriteAllText(Path.Combine(package, "fixture.tscn"), scene);
            File.WriteAllText(Path.Combine(package, "icon.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
            return act(root);
        }
        finally { Directory.Delete(root, true); }
    }

    /// <summary>The generator's result for the one package <paramref name="declaration"/>, named <c>Fixture</c>, whose scene is
    /// <paramref name="scene"/>, in a temporary repository root.</summary>
    private static GeneratedBundle Generate(IPuzzleElementDeclaration declaration, string scene) =>
        InPackage(scene, root => BundleGenerator.Generate(new CuriousContraptions.Bundle.BundleServices(), root,
            new DiscoveryResult([new BundlePackage("Fixture", declaration)], [])));

    /// <summary>Coordinator decision 3: an extractor refusal reaches the build under the code of its kind, reported against the package
    /// (the detail starts "Fixture: ", which the validator's own errors, naming a field as "Fixture.Colliders", never do) and naming the
    /// refused node or value where the refusal has one, and no bundle is written; one case for each code the extractor raises through
    /// the generator. Control: the unedited scene generates a bundle with no error.</summary>
    [Theory]
    [InlineData("name=\"col_box\"", "name=\"Box-col_box\"", BundleErrorCode.ProxyNameInfix, "Visual/Box-col_box")]
    [InlineData("name=\"col_box\" type=\"CollisionShape3D\"", "name=\"Box\" type=\"Node3D\"", BundleErrorCode.BodyWithoutCollider, null)]
    [InlineData("[node name=\"Visual\" type=\"Node3D\" parent=\".\"]",
        "[node name=\"Visual\" type=\"Node3D\" parent=\".\"]\n[node name=\"Visual\" type=\"Node3D\" parent=\".\"]", BundleErrorCode.DuplicateId, "Visual")]
    [InlineData("size = Vector3(2, 4, 6)", "size = Vector3(NaN, 4, 6)", BundleErrorCode.NonFinite, null)]
    [InlineData("size = Vector3(2, 4, 6)", "size = Vector3(0, 4, 6)", BundleErrorCode.OutOfRange, null)]
    [InlineData("shape = SubResource(\"Box\")", "shape = SubResource(\"Missing\")", BundleErrorCode.UnresolvedReference, "Visual/col_box")]
    [InlineData("position = Vector3(3, 0, 0)", "position = Vector3(3, 0)", BundleErrorCode.MalformedScene, null)]
    [InlineData("BoxShape3D", "CapsuleShape3D", BundleErrorCode.UnsupportedColliderProxy, "CapsuleShape3D")]
    public void AnExtractorRefusalReachesTheBuildUnderTheCodeOfItsKind(string before, string after, BundleErrorCode code, string? names)
    {
        var control = Generate(new SceneOwnedFixture(), BoxScene);
        Assert.Empty(control.Errors);
        Assert.NotNull(control.Json);
        Assert.Contains(before, BoxScene, StringComparison.Ordinal);
        var refused = Generate(new SceneOwnedFixture(), BoxScene.Replace(before, after));
        Assert.Null(refused.Json);
        var error = Assert.Single(refused.Errors);
        Assert.Equal(code, error.Code);
        Assert.StartsWith("Fixture: ", error.Detail, StringComparison.Ordinal);
        if (names is not null) Assert.Contains(names, error.Detail, StringComparison.Ordinal);
    }

    /// <summary>Decision 4: the build takes a package's colliders from its scene only. A scene-owned declaration that also states a
    /// data collider, or a declaration that names no scene bodies (not <see cref="ISceneGeometryElement"/>), is refused with exactly
    /// <see cref="BundleErrorCode.SceneColliderOwnership"/> reported against the package, and no bundle is written. Control: the same
    /// scene with the scene-owned fixture and no data collider generates.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void APackageWhoseCollidersAreNotItsScenesIsRefusedAndWritesNoBundle(bool statesDataColliders)
    {
        var control = Generate(new SceneOwnedFixture(), BoxScene);
        Assert.Empty(control.Errors);
        Assert.NotNull(control.Json);
        IPuzzleElementDeclaration declaration = statesDataColliders
            ? new SceneOwnedFixture { Colliders = [Fixture.Box(new(.5f, .5f, .5f))] }
            : new Fixture { Colliders = [] };
        var refused = Generate(declaration, BoxScene);
        Assert.Null(refused.Json);
        var error = Assert.Single(refused.Errors);
        Assert.Equal(BundleErrorCode.SceneColliderOwnership, error.Code);
        Assert.StartsWith("Fixture: ", error.Detail, StringComparison.Ordinal);
    }
}
