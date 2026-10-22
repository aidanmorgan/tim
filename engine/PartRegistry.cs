using Godot;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CuriousContraptions;

/// <summary>
/// The catalogue the game offers, loaded only from the generated element bundle (Story 9A.14). Every definition is a plain runtime
/// record built from one bundle entry; its scene and icon are the package's own files. A missing or structurally invalid bundle, or
/// an entry whose scene or icon does not load, refuses the whole load with a message naming the problem; nothing is loaded from
/// anywhere else and nothing is kept from a refused load. The typed <see cref="UndeliveredParts"/> set keeps an element out of the
/// registry and the palette even when the bundle declares it.
/// </summary>
public sealed class PartRegistry
{
    /// <summary>The generated bundle (tools/BundleElements writes it on every build and before every export).</summary>
    public const string BundlePath = "res://elements/elements.bundle.json";

    private readonly Dictionary<PartId, PartDefinition> _definitions = new();
    private IReadOnlyDictionary<PartId, PartAllowance>? _free;
    private IReadOnlySet<PartId>? _elements;
    public IReadOnlyDictionary<PartId, PartDefinition> Definitions => _definitions;

    /// <summary>The loaded bundle's element ids: every save and message element id is validated against this set at its
    /// serialisation boundary (Story 9A.15).</summary>
    public IReadOnlySet<PartId> Elements => _elements ?? throw new InvalidOperationException("The element bundle is not loaded.");

    /// <summary>The Free palette: every registered element, unlimited, in ascending declared palette order.</summary>
    public IReadOnlyDictionary<PartId, PartAllowance> Free => _free ?? throw new InvalidOperationException("The element bundle is not loaded.");

    private ElementPortCatalogue? _ports;
    private IReadOnlyList<BundleEntry>? _physics;
    public IReadOnlyList<BundleEntry> Physics => _physics ?? throw new InvalidOperationException("The element bundle is not loaded.");

    /// <summary>Every registered element's declared ports, read only from the bundle (Story 9A.17).</summary>
    public ElementPortCatalogue Ports => _ports ?? throw new InvalidOperationException("The element bundle is not loaded.");

    /// <summary>Loads <see cref="BundlePath"/>.</summary>
    public void Load() => LoadFile(BundlePath);

    /// <summary>Loads the bundle file at <paramref name="path"/>; a missing file refuses the load.</summary>
    internal void LoadFile(string path)
    {
        if (!Godot.FileAccess.FileExists(path))
            throw new InvalidDataException($"The element bundle '{path}' is missing; build tools/BundleElements to generate it.");
        Load(Godot.FileAccess.GetFileAsString(path));
    }

    /// <summary>Loads the bundle these bytes hold, or refuses them whole.</summary>
    public void Load(string json)
    {
        var definitions = new List<PartDefinition>();
        var entries = Read(json);
        foreach (var entry in entries)
        {
            var id = new PartId(entry.Metadata.Id);
            if (UndeliveredParts.Contains(id)) continue;
            definitions.Add(Definition(id, entry));
        }
        if (definitions.Count == 0) throw Invalid("it declares no deliverable element");
        var free = WorkshopInventoryPolicy.Free(definitions.Select(definition => definition.PartId));
        // A refused port declaration refuses the whole load; its typed reason is the cause.
        ElementPortCatalogue ports;
        try { ports = ElementPortCatalogue.FromBundle(entries); }
        catch (ElementPortRejectedException error) { throw new InvalidDataException($"The element bundle '{BundlePath}' is invalid: {error.Message}", error); }
        _physics = entries;
        _definitions.Clear();
        foreach (var definition in definitions) _definitions.Add(definition.PartId, definition);
        _free = free;
        _ports = ports;
        _elements = definitions.Select(definition => definition.PartId).ToFrozenSet();
    }

    /// <summary>The bundle's entries in ascending palette order, after the same structural validation the generator runs.</summary>
    public static IReadOnlyList<BundleEntry> Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        BundleDocument document;
        try
        {
            document = BundleSchema.Read(json);
        }
        catch (JsonException error)
        {
            throw new InvalidDataException($"The element bundle '{BundlePath}' is malformed: {error.Message}", error);
        }
        var errors = BundleValidator.Validate(document.Elements.Select(entry => new NamedEntry(entry.Metadata.Id, entry)).ToArray());
        if (errors.Count > 0) throw Invalid(string.Join("; ", errors));
        return document.Elements.OrderBy(entry => entry.Metadata.PaletteOrder).ToArray();
    }

    private static PartDefinition Definition(PartId id, BundleEntry entry)
    {
        var metadata = entry.Metadata;
        if (!ResourceLoader.Exists(metadata.ScenePath)) throw Invalid($"{id}.Metadata.ScenePath: '{metadata.ScenePath}' does not exist");
        if (!Godot.FileAccess.FileExists(metadata.IconPath)) throw Invalid($"{id}.Metadata.IconPath: '{metadata.IconPath}' does not exist");
        var scene = ResourceLoader.Load<PackedScene>(metadata.ScenePath)
            ?? throw Invalid($"{id}.Metadata.ScenePath: '{metadata.ScenePath}' is not a scene");
        var color = metadata.Color;
        return new PartDefinition(id, metadata.DisplayName, metadata.Description, metadata.Category, new(color.R, color.G, color.B, color.A),
            metadata.IconPath, scene, entry);
    }

    private static InvalidDataException Invalid(string detail) => new($"The element bundle '{BundlePath}' is invalid: {detail}.");

    public MachinePart Create(PartId id)
    {
        if (!_definitions.TryGetValue(id, out var definition))
            throw new ArgumentException($"This catalogue part '{id}' is not supported by the current GPU Workshop.");
        if (definition.Scene is null)
            throw new ArgumentException($"Catalogue part '{id}' has no scene.");
        var part = definition.Scene.Instantiate<MachinePart>();
        try
        {
            part.Configure(definition);
            return part;
        }
        catch
        {
            part.Free();
            throw;
        }
    }
}
