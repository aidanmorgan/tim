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

    [Theory]
    [InlineData("size = Vector3(2, 4, 6)", "size = Vector3(0, 4, 6)")]
    [InlineData("size = Vector3(2, 4, 6)", "size = Vector3(-2, 4, 6)")]
    [InlineData("size = Vector3(2, 4, 6)", "size = Vector3(NaN, 4, 6)")]
    [InlineData("position = Vector3(0, 2, 0)", "scale = Vector3(-1, 1, 1)")]
    [InlineData("position = Vector3(3, 0, 0)", "scale = Vector3(1, 0, 1)")]
    [InlineData("size = Vector3(2, 4, 6)", "")]
    [InlineData("type=\"Node3D\" parent=\".\"", "type=\"Node3D\" parent=\"missing\"")]
    [InlineData("name=\"Root\" type=\"Node3D\"", "name=\"Root\" type=\"Node\"")]
    public void MalformedGeometryIsRejected(string before, string after) =>
        Assert.ThrowsAny<ArgumentException>(() => Bake(BoxScene.Replace(before, after)));

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

        // The height scales by scale.Y, the radius by the equal radial scale.
        var scaled = Assert.Single(Bake(CylinderScene.Replace("position = Vector3(0, 0.5, 0)", "scale = Vector3(2, 3, 2)")).Colliders);
        Assert.Equal(new Vector3(.8f, .085f * 3, .8f), scaled.HalfExtents);
    }

    [Theory]
    [InlineData("radius = 0.4", "")]
    [InlineData("height = 0.17", "")]
    [InlineData("radius = 0.4", "radius = 0")]
    [InlineData("height = 0.17", "height = -1")]
    [InlineData("radius = 0.4", "radius = NaN")]
    [InlineData("position = Vector3(0, 0.5, 0)", "scale = Vector3(2, 1, 1)")]
    [InlineData("name=\"col_cylinder\"", "name=\"col_capsule\"")]
    [InlineData("name=\"col_cylinder\"", "name=\"col_hull\"")]
    [InlineData("CylinderShape3D", "CapsuleShape3D")]
    public void MalformedCylindersAreRejected(string before, string after)
    {
        Assert.Contains(before, CylinderScene, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => Bake(CylinderScene.Replace(before, after)));
    }

    [Theory]
    [InlineData("top_radius = 0.4\nbottom_radius = 0.3")]
    [InlineData("top_radius = 0.4")]
    [InlineData("bottom_radius = 0.4")]
    public void ATaperedOrUnstatedCylinderMeshIsRejected(string radii)
    {
        var mesh = CylinderScene.Replace("CylinderShape3D", "CylinderMesh").Replace("radius = 0.4", radii)
            .Replace("CollisionShape3D", "MeshInstance3D").Replace("shape =", "mesh =");
        Assert.Throws<ArgumentException>(() => Bake(mesh));
    }

    [Fact]
    public void ACylinderMeshThatIsNoProxyStaysArt()
    {
        var scene = BoxScene.Replace("[node name=\"Root\" type=\"Node3D\"]",
            "[sub_resource type=\"CylinderMesh\" id=\"Collar\"]\ntop_radius = 0.5\nbottom_radius = 0.2\n[node name=\"Root\" type=\"Node3D\"]") +
            "\n[node name=\"Collar\" type=\"MeshInstance3D\" parent=\".\"]\nmesh = SubResource(\"Collar\")";
        Assert.Equal(ColliderShapeKind.Box, Assert.Single(Bake(scene).Colliders).ShapeKind);
    }

    [Fact]
    public void DataOnlyGeometryIsUnchangedAndMixedSourcesFailAtomically()
    {
        var entry = BundleSchema.Entry(new Fixture());
        Assert.Equal(entry.Colliders, SceneGeometryExtractor.Bake("[node name=\"Root\" type=\"Node3D\"]", entry, null).Colliders);
        Assert.Throws<ArgumentException>(() => SceneGeometryExtractor.Bake(BoxScene, entry, [new(".", Surface)]));
        var mixedAcrossBodies = Empty with { Mechanical = Empty.Mechanical! with { Sliders = [Fixture.Slider()] } };
        Assert.Throws<ArgumentException>(() => SceneGeometryExtractor.Bake(BoxScene, mixedAcrossBodies,
            [new(".", Surface), new("Visual", Surface)]));
        Assert.Throws<ArgumentException>(() => Bake("[node name=\"Root\" type=\"Node3D\"]"));
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
        Assert.Throws<ArgumentException>(() => SceneGeometryExtractor.Bake(scene, entry, [new(".", Surface), new(".", Surface)]));
        Assert.Throws<ArgumentException>(() => SceneGeometryExtractor.Bake(scene, entry, [new(".", Surface), new("missing", Surface)]));
        Assert.Throws<ArgumentException>(() => SceneGeometryExtractor.Bake(BoxScene, entry, [new(".", Surface), new("Visual", Surface)]));
    }

    [Theory]
    [InlineData("0.65", true)]
    [InlineData("0", false)]
    [InlineData("-1", false)]
    [InlineData("NaN", false)]
    [InlineData("\"0.65\"", false)]
    public void MouthBoreIsBakedOrRejected(string bore, bool accepted)
    {
        var scene = BoxScene + "\n[node name=\"Loc_Port_mouth_end\" type=\"Node3D\" parent=\".\"]\nposition = Vector3(1, 0, 0)\nmetadata/bore_radius = " + bore;
        if (accepted)
        {
            var mouth = Assert.Single(Bake(scene).TubeMouths);
            Assert.Equal(TubeEnd.End, mouth.End);
            Assert.Equal(.65f, mouth.BoreRadius);
            Assert.Equal(Vector3.UnitX, mouth.Position);
        }
        else Assert.ThrowsAny<ArgumentException>(() => Bake(scene));
    }

    [Fact]
    public void MissingAndUnknownMouthsAreRejected()
    {
        Assert.ThrowsAny<Exception>(() => Bake(BoxScene + "\n[node name=\"Loc_Port_mouth_end\" type=\"Node3D\" parent=\".\"]"));
        Assert.Throws<ArgumentException>(() => Bake(BoxScene + "\n[node name=\"Loc_Port_mouth_side\" type=\"Node3D\" parent=\".\"]"));
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
}
