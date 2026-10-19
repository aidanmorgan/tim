using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public sealed class PartRegistry
{
    private readonly Dictionary<PartId, PartDefinition> _definitions = new();
    public IReadOnlyDictionary<PartId, PartDefinition> Definitions => _definitions;

    public void Discover()
    {
        _definitions.Clear();
        var files = DirAccess.GetFilesAt("res://parts/catalog");
        Array.Sort(files, StringComparer.Ordinal);
        var loadedAny = false;

        foreach (var file in files)
        {
            var fileName = file;
            if (fileName.EndsWith(".remap", StringComparison.OrdinalIgnoreCase))
                fileName = fileName[..^6];
            if (!fileName.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
                continue;

            var path = "res://parts/catalog/" + fileName;

            var definition = ResourceLoader.Load<PartDefinition>(path);
            if (definition is null || definition.Scene is null)
                throw new ArgumentException($"Canonical resource '{path}' is invalid or malformed.");

            if (string.IsNullOrWhiteSpace(definition.Id))
                throw new ArgumentException($"Canonical resource '{path}' has missing or empty Id.");

            var id = definition.PartId;
            // Admission is the checked-in typed set, never the resource's contents (Story 9R.0c). The registry owns the
            // resource it refuses and releases it here, on the main thread; a dropped wrapper's finalizer would otherwise
            // release it on the GC thread while a later Load reuses the cached resource.
            if (UndeliveredParts.Contains(id))
            {
                definition.Dispose();
                continue;
            }

            if (definition.Parameters.Count != 0 && id != PartId.Clock)
                throw new ArgumentException($"Canonical resource '{path}' declares untyped Parameters.");

            if (_definitions.ContainsKey(id))
                throw new ArgumentException($"Duplicate part identity '{id}' encountered in '{path}'.");

            if (definition.Ball is not null && (id == PartId.Basketball || id == PartId.BowlingBall))
                definition.Ball.Capture(id);

            _definitions.Add(id, definition);
            loadedAny = true;
        }

        if (!loadedAny)
            throw new ArgumentException("No valid catalogue parts were discovered.");

        Gpu.WorkshopInventoryPolicy.GenerateFree(_definitions.Keys);
    }

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
