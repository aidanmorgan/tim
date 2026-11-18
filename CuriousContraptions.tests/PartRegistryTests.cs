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
    /// <summary>The current delivered Free palette rows in declared palette order.</summary>
    private static readonly PartId[] PaletteRows =
    [
        new PartId("ball"), new PartId("basket"), new PartId("switch"), new PartId("lamp"), new PartId("wall"), new PartId("delay"), new PartId("bumper"),
        new PartId("ramp"), new PartId("domino"), new PartId("bowling"), new PartId("battery"), new PartId("spring"), new PartId("pipe"), new PartId("ball_detector"),
        new PartId("pipe_bend_45"), new PartId("pipe_bend_90"), new PartId("funnel"), new PartId("powered_gate"), new PartId("pressure_plate"), new PartId("counter"), new PartId("clock"),
        new PartId("latch"), new("hold_timer"), new("both_gate"), new("electrical_nand"), new("electrical_nor"), new("electrical_or"), new("electrical_xor"),
        new("weight"), new("rope_anchor"), new("pulley"), new("impact_lever"), new("trampoline"), new("motor"),
        new("conveyor"), new("clutch"), new("reverse_transmission"), new("linear_pusher"), new("wound_spring")
    ];

    private static string Bundle() => Godot.FileAccess.GetFileAsString(PartRegistry.BundlePath);

    private static PartRegistry Loaded()
    {
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();
        return registry;
    }

    [Fact]
    public void Load_RegistersEveryBundleElementFromItsPackageInPaletteOrder()
    {
        _ = godot;
        var registry = Loaded();
        Assert.Equal(PaletteRows.Length, registry.Definitions.Count);
        Assert.Equal(PaletteRows, registry.Free.Keys);
        Assert.All(registry.Free.Values, allowance => Assert.Equal(PartAllowance.Unlimited, allowance));
        var entries = PartRegistry.Read(RuntimeTestServices.Bundle, Bundle()).ToDictionary(entry => new PartId(entry.Metadata.Id));
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
            Assert.Equal(metadata.ScenePath, definition.ScenePath);
            // The payload predicate is derived from each admitted declaration: exactly the two ball packages.
            Assert.Equal(id == new PartId("ball") || id == new PartId("bowling"), DeclaredPhysics.IsPayload(id));
        }
        foreach (var part in new PartId[] { new("tennis"), new("balloon") })
        {
            Assert.False(registry.Definitions.ContainsKey(part), $"Undelivered part '{part}' must not be registered.");
            Assert.False(registry.Free.ContainsKey(part), $"Undelivered part '{part}' must not be offered.");
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

    /// <summary>A fully declared package is admitted without an engine-owned name whitelist.</summary>
    [Fact]
    public void AValidNewPackageIdentityNeedsNoEngineAdmissionChange()
    {
        var bundle = Bundle();
        Assert.Contains("\"Id\": \"bowling\"", bundle, StringComparison.Ordinal);
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load(bundle.Replace("\"Id\": \"bowling\"", "\"Id\": \"tennis\"", StringComparison.Ordinal));
        var tennis = new PartId("tennis");
        Assert.True(registry.Definitions.ContainsKey(tennis));
        Assert.True(registry.Free.ContainsKey(tennis));
        Assert.Equal(PaletteRows.Select(id => id == new PartId("bowling") ? tennis : id), registry.Free.Keys);
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
        Assert.Equal(PaletteRows.Length, registry.Definitions.Count);
    }

    [Fact]
    public void AMissingOrEmptyBundleRefusesTheLoad()
    {
        var missing = Assert.Throws<InvalidDataException>(() => new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle).LoadFile("res://elements/absent.bundle.json"));
        Assert.Contains("'res://elements/absent.bundle.json' is missing", missing.Message, StringComparison.Ordinal);
        var empty = Assert.Throws<InvalidDataException>(() => new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle).Load("{\"Elements\": []}"));
        Assert.Contains("declares no deliverable element", empty.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle).Free);
    }

    [Fact]
    public void Create_ThrowsArgumentException_OnUnknownOrUnregisteredPartId()
    {
        var registry = Loaded();
        Assert.Throws<ArgumentException>(() => registry.Create(new PartId("unknown-part")));
        Assert.Throws<ArgumentException>(() => registry.Create(new PartId("balloon")));
    }

    /// <summary>Private registry loads do not resurrect art wrappers while old finalizers are pending.</summary>
    [Fact]
    public async Task PrivateRegistryReloadsWhilePreviousSceneFinalizersArePending()
    {
        AssertNoNativeErrors("before private lifecycle check");
        var gate = new ResourceFinalizerGate();
        var scenes = new System.Collections.Generic.List<WeakReference>();
        try
        {
            StartFinalizerBlocker(gate);
            GC.Collect();
            await gate.Started.Task.WaitAsync(ResourceFinalizerDeadline, TestContext.Current.CancellationToken);
            var first = LoadCreateAndDropPrivateRegistry();
            scenes.AddRange(first);
            GC.Collect();
            Assert.All(first, scene => Assert.False(scene.IsAlive, "The old scene wrapper must be collected before reload."));
            AssertNoNativeErrors("old scene wrappers collected");

            // Two real reloads cover the second reuse that failed in the baseline dependency diagnostic.
            scenes.AddRange(LoadCreateAndDropPrivateRegistry());
            AssertNoNativeErrors("first private reload");
            scenes.AddRange(LoadCreateAndDropPrivateRegistry());
            AssertNoNativeErrors("second private reload");
        }
        finally
        {
            gate.Release.TrySetResult();
            await CompleteResourceFinalizers();
            Assert.False(gate.TimedOut, "The diagnostic finalizer blocker exceeded its finite deadline.");
        }
        Assert.All(scenes, scene => Assert.False(scene.IsAlive, "Released registry scenes must retire."));
        AssertNoNativeErrors("private registry cleanup");
    }

    /// <summary>Reload and refused reload preserve live nodes; private artwork retires after both instances are freed.</summary>
    [Fact]
    public async Task PrivateRegistryReloadKeepsLiveArtworkIndependentAndRefusalAtomic()
    {
        var resources = ExerciseLiveRegistryReload();
        await CompleteResourceFinalizers();
        Assert.All(resources, resource => Assert.False(resource.IsAlive, "Freed private artwork must not be retained."));
        Assert.False(ResourceLoader.HasCached("res://elements/Ball/ball.tscn"));
        Assert.False(ResourceLoader.HasCached("res://elements/Ball/ball.tscn::Mesh_Ball"));
        Assert.False(ResourceLoader.HasCached("res://elements/Ball/ball.tscn::Mat_Ball"));
        AssertNoNativeErrors("live registry and artwork cleanup");
    }

    [Fact]
    public void PrivateImportedIconKeepsItsReadableImageAndApplicationOwner()
    {
        var icon = WorkshopIcons.Load("play");
        Assert.True(icon.GetWidth() > 0);
        Assert.True(icon.GetHeight() > 0);
        using var image = icon.GetImage();
        Assert.False(image.IsEmpty());
        Assert.Same(icon, WorkshopIcons.Load("play"));
        Assert.False(ResourceLoader.HasCached("res://assets/icons/play.svg"));
        AssertNoNativeErrors("imported private icon");
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private WeakReference[] LoadCreateAndDropPrivateRegistry()
    {
        var registry = Loaded();
        Assert.Equal(PaletteRows, registry.Free.Keys);
        var scenes = new System.Collections.Generic.List<WeakReference>();
        foreach (var definition in registry.Definitions.Values)
        {
            Assert.NotNull(definition.Scene);
            Assert.False(ResourceLoader.HasCached(definition.ScenePath));
            scenes.Add(new WeakReference(definition.Scene));
            var part = registry.Create(definition.PartId);
            try
            {
                godot.Tree.Root.AddChild(part);
                Assert.Equal(definition.PartId, part.PartId);
                Assert.Same(definition, part.Definition);
                Assert.NotEmpty(part.GetChildren());
            }
            finally
            {
                part.GetParent()?.RemoveChild(part);
                part.Free();
            }
        }
        Assert.False(ResourceLoader.HasCached("res://elements/Ball/ball.tscn::Mesh_Ball"));
        Assert.False(ResourceLoader.HasCached("res://elements/Ball/ball.tscn::Mat_Ball"));
        Assert.False(ResourceLoader.HasCached("res://elements/ElectricalNor/printed-ring.obj"));
        return scenes.ToArray();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private WeakReference[] ExerciseLiveRegistryReload()
    {
        var registry = Loaded();
        var id = new PartId("ball");
        var originalDefinition = registry.Definitions[id];
        var first = registry.Create(id);
        MachinePart? second = null;
        try
        {
            godot.Tree.Root.AddChild(first);
            var firstMesh = first.GetNode<MeshInstance3D>("Visual/Visual_Sphere").Mesh;
            var firstMaterial = Assert.IsType<StandardMaterial3D>(firstMesh.SurfaceGetMaterial(0));
            registry.Load();
            var replacementDefinition = registry.Definitions[id];
            Assert.NotSame(originalDefinition.Scene, replacementDefinition.Scene);
            Assert.Same(originalDefinition, first.Definition);
            second = registry.Create(id);
            godot.Tree.Root.AddChild(second);
            var secondMesh = second.GetNode<MeshInstance3D>("Visual/Visual_Sphere").Mesh;
            var secondMaterial = Assert.IsType<StandardMaterial3D>(secondMesh.SurfaceGetMaterial(0));
            Assert.NotSame(firstMesh, secondMesh);
            Assert.NotSame(firstMaterial, secondMaterial);
            var secondColor = secondMaterial.AlbedoColor;
            firstMaterial.AlbedoColor = Colors.Green;
            Assert.Equal(Colors.Green, firstMaterial.AlbedoColor);
            Assert.Equal(secondColor, secondMaterial.AlbedoColor);

            Assert.Throws<InvalidDataException>(() => registry.Load("malformed"));
            Assert.Same(replacementDefinition, registry.Definitions[id]);
            Assert.Equal(PaletteRows, registry.Free.Keys);
            Assert.Equal(id, first.PartId);
            Assert.Equal(id, second.PartId);

            first.GetParent()?.RemoveChild(first);
            first.Free();
            Assert.True(GodotObject.IsInstanceValid(second));
            Assert.Equal(secondColor, secondMaterial.AlbedoColor);
            Assert.Equal(id, second.PartId);
            AssertNoNativeErrors("live reload and refused reload");
            return [new(originalDefinition.Scene!), new(replacementDefinition.Scene!), new(firstMesh),
                new(secondMesh), new(firstMaterial), new(secondMaterial)];
        }
        finally
        {
            if (GodotObject.IsInstanceValid(first))
            {
                first.GetParent()?.RemoveChild(first);
                first.Free();
            }
            if (second is not null)
            {
                second.GetParent()?.RemoveChild(second);
                second.Free();
            }
        }
    }


    [Fact]
    public void NativeArtworkTransfersEveryCatalogueHierarchyWithoutChangingLocalProperties()
    {
        foreach (var definition in Loaded().Definitions.Values)
        {
            var artwork = definition.Scene!.Instantiate<Node3D>();
            var part = MachinePart.InstantiateArtwork(definition.Scene);
            try { AssertArtwork(artwork, part, artwork, part); }
            finally { part.Free(); artwork.Free(); }
        }
    }

    private static void AssertArtwork(Node expected, Node actual, Node expectedRoot, Node actualRoot)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.GetClass(), actual.GetClass());
        Assert.Equal(expected.GetPathTo(expectedRoot), actual.GetPathTo(actualRoot));
        if (expected.Owner == expectedRoot) Assert.Same(actualRoot, actual.Owner);
        if (expected is Node3D expected3D)
        {
            var actual3D = Assert.IsAssignableFrom<Node3D>(actual);
            // Exact authored property transfer excludes lost transforms/visibility; no simulation comparison.
            Assert.Equal(expected3D.Transform, actual3D.Transform);
            Assert.Equal(expected3D.Visible, actual3D.Visible);
        }
        if (expected is MeshInstance3D mesh)
        {
            var actualMesh = Assert.IsType<MeshInstance3D>(actual);
            Assert.Same(mesh.Mesh, actualMesh.Mesh);
            Assert.Same(mesh.MaterialOverride, actualMesh.MaterialOverride);
        }
        Assert.Equal(expected.GetChildCount(), actual.GetChildCount());
        for (var index = 0; index < expected.GetChildCount(); index++)
            AssertArtwork(expected.GetChild(index), actual.GetChild(index), expectedRoot, actualRoot);
    }

    [Fact]
    public void NativeArtworkTransfersNonDefaultRootAndNestedOwnership()
    {
        var root = new Node3D { Name = "authored", Position = new(2, 3, 4), Rotation = new(0, .5f, 0), Visible = false };
        var child = new Node3D { Name = "Visual", Position = new(1, 0, 0) };
        var nested = new Node3D { Name = "Marker", Position = new(0, 2, 0) };
        root.AddChild(child); child.Owner = root;
        child.AddChild(nested); nested.Owner = root;
        root.AddToGroup("authored_group", persistent: true);
        using var packed = new PackedScene();
        try
        {
            Assert.Equal(Error.Ok, packed.Pack(root));
            WorkshopResources.ValidateArtwork(packed);
            var part = MachinePart.InstantiateArtwork(packed);
            try
            {
                AssertArtwork(root, part, root, part);
                Assert.True(part.IsInGroup("authored_group"));
            }
            finally { part.Free(); }
        }
        finally { root.Free(); }
        AssertNoNativeErrors("non-default native artwork transfer");
    }

    [Fact]
    public void ScriptBearingArtworkIsRefusedAndItsPackedOwnerDisposed()
    {
        var scripted = new MachinePart { Name = "unsupported" };
        var packed = new PackedScene();
        try
        {
            Assert.Equal(Error.Ok, packed.Pack(scripted));
            var error = Assert.Throws<InvalidDataException>(() => WorkshopResources.ValidateArtwork(packed));
            Assert.Contains("cannot contain scripts", error.Message, StringComparison.Ordinal);
            Assert.False(GodotObject.IsInstanceValid(packed));
        }
        finally
        {
            packed.Dispose();
            scripted.Free();
        }
        AssertNoNativeErrors("script-bearing artwork refusal");
    }

    private static readonly TimeSpan ResourceFinalizerDeadline = TimeSpan.FromSeconds(30);

    private static async Task CompleteResourceFinalizers()
    {
        // PackedScene owns mesh resources, which own materials. Drain these three ownership levels.
        for (var level = 0; level < 3; level++)
        {
            GC.Collect();
            await Task.Run(GC.WaitForPendingFinalizers, TestContext.Current.CancellationToken)
                .WaitAsync(ResourceFinalizerDeadline, TestContext.Current.CancellationToken);
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void StartFinalizerBlocker(ResourceFinalizerGate gate) => _ = new ResourceFinalizerBlocker(gate);

    private sealed class ResourceFinalizerGate
    {
        internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal volatile bool TimedOut;
    }

    private sealed class ResourceFinalizerBlocker(ResourceFinalizerGate gate)
    {
        ~ResourceFinalizerBlocker()
        {
            gate.Started.TrySetResult();
            if (!gate.Release.Task.Wait(ResourceFinalizerDeadline)) gate.TimedOut = true;
        }
    }

    private void AssertNoNativeErrors(string phase)
    {
        var errors = godot.Errors.Drain();
        Assert.True(errors.Length == 0, $"{phase}: {string.Join(System.Environment.NewLine, errors.Select(error => error.ToString()))}");
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
        Assert.Throws<InvalidOperationException>(() => new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle).Elements);
        var registry = Loaded();
        Assert.Equal(PaletteRows.ToHashSet(), registry.Elements.ToHashSet());
        Assert.Equal(registry.Definitions.Keys.ToHashSet(), registry.Elements.ToHashSet());
        foreach (var part in new PartId[] { new("tennis"), new("balloon") }) Assert.DoesNotContain(part, registry.Elements);
    }

    private static string Root => twodog.Engine.ResolveProjectDir();

    private static readonly string[] GameSourceDirectories = ["engine", "ui"];

    /// <summary>The UI-control and socket-choice pictograms Story 9A.18 keeps, in ordinal order.</summary>
    private static readonly string[] UiPictograms = ["cube", "extend_input", "extended_output", "first_input", "front", "move", "reset_input",
        "resize", "retract_input", "retracted_output", "second_input", "set_input", "socket_power", "socket_reset", "socket_set"];

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

    /// <summary>PartId has no named constants. Saves naming absent packages are refused with the unknown identity.</summary>
    [Fact]
    public void TheUndeliveredPartsHaveNoPartIdConstantOrWireIdentity()
    {
        var constants = typeof(PartId).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.FieldType == typeof(PartId)).Select(field => ((PartId)field.GetValue(null)!).Value).ToArray();
        Assert.Empty(constants);
        foreach (var part in new PartId[] { new("tennis"), new("balloon") })
        {
            var id = part;
            Assert.DoesNotContain(id.Value, constants);
            Assert.DoesNotContain(part.ToString(), typeof(PartId).GetFields().Select(field => field.Name));
            var save = SaveFixture.Forged(new Gpu.WorkshopConstruction(new(1), Gpu.WorkshopCadenceSettings.Default(),
                new([new Gpu.WorkshopGenericInstance(id, new(1), default, default, Gpu.CanonicalRotation.Identity)])), 2);
            var refusal = SaveFixture.Refused(Gpu.WorkshopSaveRejection.UnknownElement,
                () => Gpu.WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, Loaded().Elements, save));
            Assert.Contains($"'{id}'", refusal.Message, StringComparison.Ordinal);
        }
        var source = File.ReadAllText(Path.Combine(Root, "CuriousContraptions.Abstractions", "PartId.cs"));
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
        var ids = PartRegistry.Read(RuntimeTestServices.Bundle, Bundle()).Select(entry => entry.Metadata.Id)
            .Concat(new PartId[] { new("tennis"), new("balloon") }.Select(part => part.Value)).ToArray();
        Assert.Equal(PaletteRows.Length + 2, ids.Length);
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
    public void TheFreeInventoryKeepsEveryAdmittedIdentityInPaletteOrder()
    {
        var tennis = new PartId("tennis");
        Assert.Equal([new PartId("ramp"), tennis, new PartId("ball")], WorkshopInventoryPolicy.Free([new PartId("ramp"), tennis, new PartId("ball")]).Keys);
        Assert.Throws<ArgumentException>(() => WorkshopInventoryPolicy.Free([]));
        Assert.Equal([tennis], WorkshopInventoryPolicy.Free([tennis]).Keys);
    }
}
