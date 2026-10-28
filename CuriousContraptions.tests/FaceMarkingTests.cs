using CuriousContraptions.Abstractions;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class FaceMarkingTests(NativeSceneFixture godot)
{
    public static TheoryData<PartId, int> Parts => new()
    {
        { new("both_gate"), 5 }, { new("electrical_nand"), 19 }, { new("electrical_nor"), 20 },
        { new("electrical_or"), 21 }, { new("electrical_xor"), 22 }, { new("hold_timer"), 1 }, { PartId.Latch, 15 }
    };

    private static IEnumerable<MeshInstance3D> Meshes(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is MeshInstance3D mesh) yield return mesh;
            foreach (var nested in Meshes(child)) yield return nested;
        }
    }

    private static bool IsPrint(string path) => path is "Visual/UpperPath" or "Visual/LowerPath" or
        "Visual/FirstMark" or "Visual/SecondMarkLeft" or "Visual/SecondMarkRight" or "Visual/Track" or "Visual/SetMarker" ||
        path.StartsWith("Visual/Pictogram/", StringComparison.Ordinal) ||
        path.StartsWith("Visual/TruthDot", StringComparison.Ordinal) ||
        path.StartsWith("Visual/ResetMarker/", StringComparison.Ordinal) ||
        path.StartsWith("Visual/OutMarker/", StringComparison.Ordinal);

    [Theory]
    [MemberData(nameof(Parts))]
    public void EveryPrintedSurfaceIsPresentFrontFacingAndAttachedToTheRotatingFace(PartId id, int count)
    {
        var registry = new PartRegistry(); registry.Load();
        var part = registry.Create(id);
        try
        {
            godot.Tree.Root.AddChild(part);
            var face = part.GetNode<MeshInstance3D>("Visual/Face");
            var faceDepth = Assert.IsType<BoxMesh>(face.Mesh).Size.Z / 2;
            var printing = Meshes(part).Where(mesh => IsPrint(part.GetPathTo(mesh).ToString())).ToArray();
            Assert.Equal(count, printing.Length);
            foreach (var rotation in new[] { Vector3.Zero, new Vector3(.7f, 1.1f, -.4f) })
            {
                part.Rotation = rotation;
                var normal = face.GlobalBasis.Z.Normalized();
                var surface = face.GlobalTransform * new Vector3(0, 0, faceDepth);
                foreach (var mesh in printing)
                {
                    Assert.True(mesh.IsVisibleInTree(), mesh.Name + " must be visible");
                    var material = Assert.IsType<StandardMaterial3D>(mesh.GetActiveMaterial(0));
                    Assert.Equal(1f, material.AlbedoColor.A);
                    Assert.Null(material.AlbedoTexture);
                    Assert.Equal(BaseMaterial3D.TransparencyEnum.Disabled, material.Transparency);
                    var vertices = mesh.Mesh!.GetFaces().Select(p => mesh.GlobalTransform * p).ToArray();
                    Assert.NotEmpty(vertices);
                    var depths = vertices.Select(p => (p - surface).Dot(normal)).ToArray();
                    // The visual contract allows 0.5 mm anti-z-fighting separation, never a buried label.
                    Assert.All(depths, d => Assert.InRange(d, 0f, .0005f));
                    // A plane has no relief; 10 micrometres covers transform roundoff at this metre-scale origin.
                    Assert.InRange(depths.Max() - depths.Min(), 0f, .00001f);
                    var area = 0f;
                    for (var i = 0; i < vertices.Length; i += 3)
                    {
                        var cross = (vertices[i + 1] - vertices[i]).Cross(vertices[i + 2] - vertices[i]);
                        if (cross.LengthSquared() < 1e-16f) continue;
                        area += cross.Length() / 2;
                        // Godot's front faces have clockwise winding. Backface culling must not hide the ink.
                        Assert.True(cross.Normalized().Dot(normal) < -.99f, mesh.Name + " faces away from its face");
                    }
                    Assert.True(area > .0001f, mesh.Name + " has no readable surface area");
                }
                // Positive control: an actual solid lamp still stands proud, and is checked against its collider separately.
                var lamp = part.GetNode<MeshInstance3D>(id == PartId.Latch ? "Visual/IndicatorLamp" :
                    id == new PartId("hold_timer") ? "Visual/Indicator" : "Visual/OutputLamp");
                Assert.True(lamp.Mesh!.GetFaces().Max(v => (lamp.GlobalTransform * v - surface).Dot(normal)) > .05f);
            }
        }
        finally { part.Free(); }
    }

    // Test the actual projected triangles, including empty space. No node-name/count check can prove a hollow symbol.
    private static bool Covers(MachinePart part, string path, Vector2 point)
    {
        var node = part.GetNode<Node3D>(path);
        var meshes = node is MeshInstance3D single ? new[] { single } : Meshes(node).ToArray();
        foreach (var mesh in meshes)
        {
            var vertices = mesh.Mesh!.GetFaces().Select(v => part.GlobalTransform.AffineInverse() * mesh.GlobalTransform * v).ToArray();
            for (var i = 0; i < vertices.Length; i += 3)
            {
                var a = new Vector2(vertices[i].X, vertices[i].Y);
                var b = new Vector2(vertices[i + 1].X, vertices[i + 1].Y);
                var c = new Vector2(vertices[i + 2].X, vertices[i + 2].Y);
                if (MathF.Abs((b - a).Cross(c - a)) < 1e-10f) continue;
                var ab = (b - a).Cross(point - a); var bc = (c - b).Cross(point - b); var ca = (a - c).Cross(point - c);
                if ((ab >= -1e-8f && bc >= -1e-8f && ca >= -1e-8f) ||
                    (ab <= 1e-8f && bc <= 1e-8f && ca <= 1e-8f)) return true;
            }
        }
        return false;
    }

    [Theory]
    [MemberData(nameof(Parts))]
    public void PrintedSymbolsRetainTheirFilledAndEmptyRegions(PartId id, int count)
    {
        Assert.True(count > 0);
        var registry = new PartRegistry(); registry.Load();
        var part = registry.Create(id);
        try
        {
            godot.Tree.Root.AddChild(part);
            void Ink(string path, float x, float y) => Assert.True(Covers(part, path, new(x, y)), path + " missing ink at " + new Vector2(x, y) + "; transform " + part.GetNode<Node3D>(path).Transform);
            void Clear(string path, float x, float y) => Assert.False(Covers(part, path, new(x, y)), path + " filled its required gap");
            if (id == PartId.Latch)
            {
                Ink("Visual/SetMarker", -.45f, .34f);
                Clear("Visual/ResetMarker", .45f, .34f);
                for (var i = 0; i < 12; i++)
                {
                    var angle = i * MathF.Tau / 12;
                    Ink("Visual/ResetMarker", .45f + .1f * MathF.Cos(angle), .34f + .1f * MathF.Sin(angle));
                }
                // CAT-037 §6: downward output cue above the lower socket, with a clear upper centre.
                Ink("Visual/OutMarker", 0, -.489f);
                Clear("Visual/OutMarker", 0, -.441f);
            }
            else if (id == new PartId("hold_timer"))
            {
                Ink("Visual/Track", -.49f, .1f); Ink("Visual/Track", .49f, .1f);
                Clear("Visual/Track", 0, .18f);
            }
            else
            {
                Ink("Visual/FirstMark", -.48f, .47f);
                Ink("Visual/SecondMarkLeft", -.58f, -.47f); Ink("Visual/SecondMarkRight", -.42f, -.47f);
                Clear("Visual/SecondMarkLeft", -.5f, -.47f); Clear("Visual/SecondMarkRight", -.5f, -.47f);
                if (id == new PartId("both_gate"))
                {
                    Ink("Visual/UpperPath", -.28f, .267f); Ink("Visual/UpperPath", .20f, .023f);
                    Ink("Visual/LowerPath", -.28f, -.267f); Ink("Visual/LowerPath", .20f, -.023f);
                    Assert.DoesNotContain(Meshes(part), m => m.Name.ToString().StartsWith("TruthDot", StringComparison.Ordinal));
                }
                else
                {
                    var nand = id == new PartId("electrical_nand");
                    var nor = id == new PartId("electrical_nor");
                    var xor = id == new PartId("electrical_xor");
                    var rows = nand ? new[] { 0, 1, 2 } : nor ? new[] { 0 } : xor ? new[] { 1, 2 } : new[] { 1, 2, 3 };
                    var dots = part.GetNode<Node3D>("Visual").GetChildren().OfType<MeshInstance3D>()
                        .Where(m => m.Name.ToString().StartsWith("TruthDot", StringComparison.Ordinal)).ToArray();
                    Assert.Equal(rows.Length, dots.Length);
                    foreach (var row in rows)
                    {
                        var path = "Visual/TruthDot" + Convert.ToString(row, 2).PadLeft(2, '0');
                        var x = -.21f + row * .14f;
                        Ink(path, x, -.4f); Ink(path, x + .03f, -.4f); Clear(path, x + .04f, -.4f);
                    }
                    // NAND has a straight back; OR/NOR/XOR a concave back. XOR adds a second input curve.
                    Ink("Visual/Pictogram", nand ? -.1625f : nor ? -.11849f : -.0914f, .054f);
                    Clear("Visual/Pictogram", 0, 0);
                    if (xor) Ink("Visual/Pictogram", -.1997f, .054f);
                    if (nand || nor)
                    {
                        var x = nand ? .21667f : .18958f;
                        Clear("Visual/Pictogram/Bubble", x, 0);
                        for (var i = 0; i < 16; i++)
                        {
                            var angle = i * MathF.Tau / 16;
                            Ink("Visual/Pictogram/Bubble", x + .0542f * MathF.Cos(angle), .0542f * MathF.Sin(angle));
                        }
                    }
                    else Assert.Null(part.GetNodeOrNull("Visual/Pictogram/Bubble"));
                }
            }
        }
        finally { part.Free(); }
    }
}
