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

            var content = FileAccess.GetFileAsString(path);
            if (content.Contains("Parameters = {") && !content.Contains("Parameters = {}") && !fileName.Equals("clock.tres", StringComparison.OrdinalIgnoreCase))
                continue; // Unmigrated legacy resource (e.g. balloon, tennis with legacy parameters)

            var definition = ResourceLoader.Load<PartDefinition>(path);
            if (definition is null || definition.Scene is null)
                throw new ArgumentException($"Canonical resource '{path}' is invalid or malformed.");

            if (definition.Parameters.Count != 0 && definition.PartId != PartId.Clock)
                continue; // Unmigrated legacy resource (e.g. balloon, tennis)

            if (string.IsNullOrWhiteSpace(definition.Id))
                throw new ArgumentException($"Canonical resource '{path}' has missing or empty Id.");

            var id = definition.PartId;
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
