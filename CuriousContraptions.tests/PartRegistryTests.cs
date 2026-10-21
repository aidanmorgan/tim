using System;
using System.IO;
using System.Linq;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.14: the registry loads only the generated element bundle. Every definition is a plain runtime record built from
/// one bundle entry, its scene loads from its package, and a missing or structurally invalid bundle refuses the whole load with a
/// message naming the problem.</summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class PartRegistryTests(NativeSceneFixture godot)
{
    /// <summary>The Free palette rows the player saw before the cutover (Story 9.4b baseline, minus the undelivered parts).</summary>
    private static readonly PartId[] PaletteRows =
    [
        PartId.Basketball, PartId.Receiver, PartId.ImpactSwitch, PartId.SignalLamp, PartId.Wall, PartId.Delay, PartId.PinballBumper,
        PartId.Ramp, PartId.Domino, PartId.BowlingBall, PartId.Battery, PartId.Springboard, PartId.Pipe, PartId.BallDetector,
        PartId.PipeBend45, PartId.PipeBend90, PartId.Funnel, PartId.PoweredGate, PartId.PressurePlate, PartId.Counter, PartId.Clock,
        PartId.Latch, new("hold_timer")
    ];

    private static string Bundle() => Godot.FileAccess.GetFileAsString(PartRegistry.BundlePath);

    private static PartRegistry Loaded()
    {
        var registry = new PartRegistry();
        registry.Load();
        return registry;
    }

    [Fact]
    public void Load_RegistersEveryBundleElementFromItsPackageInPaletteOrder()
    {
        _ = godot;
        var registry = Loaded();
        Assert.Equal(23, registry.Definitions.Count);
        Assert.Equal(PaletteRows, registry.Free.Keys);
        Assert.All(registry.Free.Values, allowance => Assert.Equal(PartAllowance.Unlimited, allowance));
        var entries = PartRegistry.Read(Bundle()).ToDictionary(entry => new PartId(entry.Metadata.Id));
        foreach (var (id, definition) in registry.Definitions)
        {
            var metadata = entries[id].Metadata;
            Assert.Equal(id, definition.PartId);
            Assert.Equal(metadata.DisplayName, definition.Title);
            Assert.Equal(metadata.Description, definition.Description);
            Assert.Equal(metadata.Category, definition.Category);
            Assert.Equal(new Color(metadata.Color.R, metadata.Color.G, metadata.Color.B, metadata.Color.A), definition.Color);
            Assert.Equal(metadata.IconPath, definition.IconPath);
            Assert.StartsWith("res://elements/", metadata.ScenePath, StringComparison.Ordinal);
            Assert.NotNull(definition.Scene);
            Assert.Equal(metadata.ScenePath, definition.Scene.ResourcePath);
            Assert.Equal(id == PartId.Basketball || id == PartId.BowlingBall, definition.Ball is not null);
        }
        foreach (var part in UndeliveredParts.All)
        {
            Assert.False(registry.Definitions.ContainsKey(UndeliveredParts.Id(part)), $"Undelivered part '{part}' must not be registered.");
            Assert.False(registry.Free.ContainsKey(UndeliveredParts.Id(part)), $"Undelivered part '{part}' must not be offered.");
        }
    }

    [Fact]
    public void Create_InstantiatesAndConfiguresEachLoadedPart()
    {
        var registry = Loaded();
        foreach (var (id, definition) in registry.Definitions)
        {
            var part = registry.Create(id);
            try
            {
                godot.Tree.Root.AddChild(part);
                Assert.Same(definition, part.Definition);
                Assert.Equal(id, part.PartId);
            }
            finally
            {
                part.GetParent()?.RemoveChild(part);
                part.Free();
            }
        }
    }

    /// <summary>The typed undelivered set is the admission authority: a bundle that declares a Tennis ball (here the bowling ball's
    /// entry renamed) still never registers or offers it.</summary>
    [Fact]
    public void AnUndeliveredElementInTheBundleNeverReachesTheRegistryOrThePalette()
    {
        var bundle = Bundle();
        Assert.Contains("\"Id\": \"bowling\"", bundle, StringComparison.Ordinal);
        var registry = new PartRegistry();
        registry.Load(bundle.Replace("\"Id\": \"bowling\"", "\"Id\": \"tennis\"", StringComparison.Ordinal));
        var tennis = UndeliveredParts.Id(UndeliveredPart.Tennis);
        Assert.False(registry.Definitions.ContainsKey(tennis));
        Assert.False(registry.Free.ContainsKey(tennis));
        Assert.Equal(PaletteRows.Where(id => id != PartId.BowlingBall), registry.Free.Keys);
    }

    [Theory]
    [InlineData("malformed", "is malformed")]
    [InlineData("\"Elements\": [|\"Elements\": [null, ", "is malformed: Elements[0] is null")]
    [InlineData("\"Options\": []|\"Options\": [null]", "is malformed: Elements[")]
    [InlineData("\"ScenePath\":|\"Unknown\": 1, \"ScenePath\":", "is malformed")]
    [InlineData("\"Category\": \"Motion\"|\"Category\": \"Sideways\"", "is malformed")]
    [InlineData("\"Id\": \"bowling\"|\"Id\": \"ball\"", "BUNDLE001 duplicate id 'ball'")]
    [InlineData("\"PaletteOrder\": 10,|\"PaletteOrder\": 1,", "BUNDLE008 duplicate palette order 1")]
    [InlineData("\"DisplayName\": \"Wall\"|\"DisplayName\": \"\"", "BUNDLE010 wall.Metadata.DisplayName")]
    [InlineData("res://elements/Wall/wall.tscn|res://parts/scenes/wall.tscn", "BUNDLE007 wall.Metadata.ScenePath")]
    [InlineData("res://elements/Wall/wall.tscn|res://elements/Wall/missing.tscn", "wall.Metadata.ScenePath: 'res://elements/Wall/missing.tscn' does not exist")]
    [InlineData("res://elements/Wall/icon.svg|res://elements/Wall/missing.svg", "wall.Metadata.IconPath: 'res://elements/Wall/missing.svg' does not exist")]
    public void AnInvalidBundleRefusesTheWholeLoadNamingTheProblem(string edit, string expected)
    {
        var bundle = Bundle();
        string text;
        if (edit == "malformed") text = bundle[..(bundle.Length / 2)];
        else
        {
            var (original, replacement) = (edit.Split('|')[0], edit.Split('|')[1]);
            var at = bundle.IndexOf(original, StringComparison.Ordinal);
            Assert.True(at >= 0, $"The bundle has no '{original}'.");
            text = string.Concat(bundle.AsSpan(0, at), replacement, bundle.AsSpan(at + original.Length));
        }
        var registry = Loaded();
        var error = Assert.Throws<InvalidDataException>(() => registry.Load(text));
        Assert.Contains(PartRegistry.BundlePath, error.Message, StringComparison.Ordinal);
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
        // Nothing is kept from a refused load: the registry still holds the last good bundle.
        Assert.Equal(PaletteRows, registry.Free.Keys);
        Assert.Equal(23, registry.Definitions.Count);
    }

    [Fact]
    public void AMissingOrEmptyBundleRefusesTheLoad()
    {
        var missing = Assert.Throws<InvalidDataException>(() => new PartRegistry().LoadFile("res://elements/absent.bundle.json"));
        Assert.Contains("'res://elements/absent.bundle.json' is missing", missing.Message, StringComparison.Ordinal);
        var empty = Assert.Throws<InvalidDataException>(() => new PartRegistry().Load("{\"Elements\": []}"));
        Assert.Contains("declares no deliverable element", empty.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => new PartRegistry().Free);
    }

    [Fact]
    public void Create_ThrowsArgumentException_OnUnknownOrUnregisteredPartId()
    {
        var registry = Loaded();
        Assert.Throws<ArgumentException>(() => registry.Create(new PartId("unknown-part")));
        Assert.Throws<ArgumentException>(() => registry.Create(UndeliveredParts.Id(UndeliveredPart.Balloon)));
    }

    [Fact]
    public void PartId_ValidatesValuesAndSupportsEqualityAndComparison()
    {
        Assert.Throws<ArgumentException>(() => new PartId(null!));
        Assert.Throws<ArgumentException>(() => new PartId(""));
        Assert.Throws<ArgumentException>(() => new PartId("   "));

        var a = new PartId("ball");
        var b = new PartId("ball");
        var c = new PartId("ramp");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.True(a == b);
        Assert.True(a != c);
        Assert.Equal("ball", a.ToString());
        Assert.Equal("ball", a.Value);
        Assert.True(a.CompareTo(c) < 0);
        Assert.True(c.CompareTo(a) > 0);
        Assert.Equal(0, a.CompareTo(b));
    }

    /// <summary>Story 9A.15: the registry's element set is exactly the loaded bundle's deliverable elements, the set every save is
    /// validated against.</summary>
    [Fact]
    public void TheRegistryElementsAreTheLoadedBundlesDeliverableElements()
    {
        Assert.Throws<InvalidOperationException>(() => new PartRegistry().Elements);
        var registry = Loaded();
        Assert.Equal(PaletteRows.ToHashSet(), registry.Elements.ToHashSet());
        Assert.Equal(registry.Definitions.Keys.ToHashSet(), registry.Elements.ToHashSet());
        foreach (var part in UndeliveredParts.All) Assert.DoesNotContain(UndeliveredParts.Id(part), registry.Elements);
    }

    private static string Root => twodog.Engine.ResolveProjectDir();

    private static readonly string[] GameSourceDirectories = ["engine", "ui"];

    /// <summary>The UI-control and socket-choice pictograms Story 9A.18 keeps, in ordinal order.</summary>
    private static readonly string[] UiPictograms = ["cube", "extend_input", "extended_output", "first_input", "front", "move", "reset_input",
        "resize", "retract_input", "retracted_output", "second_input", "set_input", "socket_reset", "socket_set"];

    private static string[] GameSources() =>
        GameSourceDirectories.SelectMany(directory => Directory.GetFiles(Path.Combine(Root, directory), "*.cs", SearchOption.AllDirectories)).ToArray();

    /// <summary>Story 9A.14 deletions: the catalogue resources and their scenes directory are gone, and no game source scans or loads
    /// a catalogue resource again (a re-added .tres scan, DirAccess listing or res://parts path fails here).</summary>
    [Fact]
    public void NoCatalogueResourceOrResourceLoaderRemains()
    {
        Assert.False(Directory.Exists(Path.Combine(Root, "parts")), "the emptied parts/ directory is deleted");
        Assert.Empty(Directory.GetFiles(Root, "*.tres", SearchOption.TopDirectoryOnly));
        foreach (var directory in new[] { "elements", "engine", "ui", "scenes" })
            Assert.Empty(Directory.GetFiles(Path.Combine(Root, directory), "*.tres", SearchOption.AllDirectories));
        var hits = GameSources().Where(path => File.ReadAllText(path) is var text &&
            (text.Contains(".tres", StringComparison.Ordinal) || text.Contains("res://parts/", StringComparison.Ordinal) ||
             text.Contains("DirAccess", StringComparison.Ordinal))).ToArray();
        Assert.Empty(hits);
        Assert.Null(typeof(PartRegistry).GetMethod("Discover"));
        Assert.False(typeof(PartDefinition).IsSubclassOf(typeof(Resource)), "PartDefinition is a plain runtime record, not a Godot Resource");
        // The resource form of the ball material is gone: no source file, no compile item and no type in the game assembly.
        Assert.False(File.Exists(Path.Combine(Root, "engine", "BallMaterialResource.cs")));
        Assert.DoesNotContain("BallMaterialResource", File.ReadAllText(Path.Combine(Root, "CuriousContraptions.csproj")), StringComparison.Ordinal);
        Assert.DoesNotContain(typeof(MachineWorld).Assembly.GetTypes(), type => type.Name == "BallMaterialResource");
        Assert.DoesNotContain(GameSources(), path => File.ReadAllText(path).Contains("BallMaterialResource", StringComparison.Ordinal));
    }

    /// <summary>Story 9A.14 deletion: the undelivered parts have no PartId constant until Story 12.1 delivers their packages (a
    /// re-added PartId.Tennis fails here), and a save naming one is refused naming it (Story 9A.15, F16-4).</summary>
    [Fact]
    public void TheUndeliveredPartsHaveNoPartIdConstantOrWireIdentity()
    {
        var constants = typeof(PartId).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.FieldType == typeof(PartId)).Select(field => ((PartId)field.GetValue(null)!).Value).ToArray();
        Assert.NotEmpty(constants);
        foreach (var part in UndeliveredParts.All)
        {
            var id = UndeliveredParts.Id(part);
            Assert.DoesNotContain(id.Value, constants);
            Assert.DoesNotContain(part.ToString(), typeof(PartId).GetFields().Select(field => field.Name));
            var save = SaveFixture.Forged(new Gpu.WorkshopConstruction(new(1), Gpu.WorkshopCadenceSettings.Default(),
                new([new Gpu.WorkshopGenericInstance(id, new(1), default, default, Gpu.CanonicalRotation.Identity)])), 2);
            var refusal = SaveFixture.Refused(Gpu.WorkshopSaveRejection.UnknownElement,
                () => Gpu.WorkshopSaveCodec.Decode(Loaded().Elements, save));
            Assert.Contains($"'{id}'", refusal.Message, StringComparison.Ordinal);
        }
        var source = File.ReadAllText(Path.Combine(Root, "engine", "PartId.cs"));
        Assert.DoesNotMatch(@"\b(Tennis|Balloon)\b|""tennis""|""balloon""", source);
    }

    /// <summary>Story 9A.14 deletion: no static Free palette list remains; the Free rows exist only as the registry's bundle order.</summary>
    [Fact]
    public void NoStaticFreePaletteListRemains()
    {
        var members = typeof(WorkshopInventoryPolicy).GetMembers(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
        Assert.DoesNotContain(members, member => member is System.Reflection.FieldInfo or System.Reflection.PropertyInfo);
        var source = File.ReadAllText(Path.Combine(Root, "engine", "gpu", "WorkshopInventory.cs"));
        Assert.DoesNotMatch(@"PartId\[\]|IReadOnlyList<PartId>", source);
    }

    /// <summary>Story 9A.18 deletion: WorkshopIcons keeps no element arm. Every bundle element's icon is its package icon.svg, and
    /// asking the pictogram table for an element id is refused (a re-added element arm fails here).</summary>
    [Fact]
    public void WorkshopIconsKeepsNoElementArm()
    {
        _ = godot;
        var source = File.ReadAllText(Path.Combine(Root, "ui", "WorkshopIcons.cs"));
        var ids = PartRegistry.Read(Bundle()).Select(entry => entry.Metadata.Id)
            .Concat(UndeliveredParts.All.Select(part => UndeliveredParts.Id(part).Value)).ToArray();
        Assert.Equal(25, ids.Length);
        foreach (var id in ids)
        {
            Assert.DoesNotContain($"\"{id}\"", source, StringComparison.Ordinal);
            Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopIcons.Pictogram(id));
        }
        // Ratchet: the pictogram table holds exactly the UI-control and socket-choice arms Story 9A.18 keeps, so any re-added
        // element arm (delivered, undelivered or future, such as "motor") fails here.
        var pictogram = source[source.IndexOf("public static Texture2D Pictogram", StringComparison.Ordinal)..source.IndexOf("private static ImageTexture Rasterise", StringComparison.Ordinal)];
        var arms = System.Text.RegularExpressions.Regex.Matches(pictogram, "\"([a-z_]+)\"(?: or \"([a-z_]+)\")* =>")
            .SelectMany(match => match.Groups.Values.Skip(1).SelectMany(group => group.Captures.Select(capture => capture.Value)))
            .Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(UiPictograms, arms);
        // Control: the UI pictograms stay.
        Assert.NotNull(WorkshopIcons.Pictogram("socket_set"));
        Assert.NotNull(WorkshopIcons.Pictogram("resize"));
        foreach (var definition in Loaded().Definitions.Values)
            Assert.NotNull(WorkshopIcons.Element(definition.IconPath));
        Assert.Throws<InvalidOperationException>(() => WorkshopIcons.Element("res://elements/Wall/missing.svg"));
    }

    [Fact]
    public void TheFreeInventoryKeepsTheGivenPaletteOrderMinusTheUndeliveredSet()
    {
        var tennis = UndeliveredParts.Id(UndeliveredPart.Tennis);
        Assert.Equal([PartId.Ramp, PartId.Basketball], WorkshopInventoryPolicy.Free([PartId.Ramp, tennis, PartId.Basketball]).Keys);
        Assert.Throws<ArgumentException>(() => WorkshopInventoryPolicy.Free([]));
        Assert.Throws<ArgumentException>(() => WorkshopInventoryPolicy.Free([tennis]));
    }
}
