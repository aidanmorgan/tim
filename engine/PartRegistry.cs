using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public sealed class PartRegistry
{
    public SortedDictionary<string, PartDefinition> Definitions { get; } = new(StringComparer.Ordinal);

    public void Discover(string directory = "res://parts/catalog")
    {
        Definitions.Clear();
        var files = ResourceLoader.ListDirectory(directory);
        Array.Sort(files, StringComparer.Ordinal);
        foreach (var file in files)
        {
            if (!file.EndsWith(".tres", StringComparison.Ordinal)) continue;
            var definition = ResourceLoader.Load<PartDefinition>(directory + "/" + file);
            if (definition?.Scene == null) throw new InvalidOperationException("Invalid part definition: " + file);
            if (!Definitions.TryAdd(definition.Id, definition))
                throw new InvalidOperationException("Duplicate part ID: " + definition.Id);
        }
    }

    public MachinePart Create(PartSpec specification)
    {
        if (!Definitions.TryGetValue(specification.Kind, out var definition))
            throw new ArgumentException("Unknown part: " + specification.Kind);
        foreach (var key in specification.Properties.Keys)
            if (!definition.Parameters.ContainsKey(key))
                throw new ArgumentException($"Unsupported property '{key}' on part '{specification.Kind}'.");
        var part = definition.Scene.Instantiate<MachinePart>();
        part.Definition = definition;
        try { part.Configure(specification); return part; }
        catch { part.Free(); throw; }
    }
}
