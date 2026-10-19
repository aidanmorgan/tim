using Godot;
using CuriousContraptions.Gpu;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>What admission decided for one catalogue resource.</summary>
public enum PartAdmission { Registered, Undelivered }

public sealed class PartRegistry
{
    private readonly Dictionary<PartId, PartDefinition> _definitions = new();
    public IReadOnlyDictionary<PartId, PartDefinition> Definitions => _definitions;

    public void Discover()
    {
        _definitions.Clear();
        var files = DirAccess.GetFilesAt("res://parts/catalog");
        Array.Sort(files, StringComparer.Ordinal);
        var seen = new HashSet<PartId>();

        foreach (var file in files)
        {
            var fileName = file;
            if (fileName.EndsWith(".remap", StringComparison.OrdinalIgnoreCase))
                fileName = fileName[..^6];
            if (!fileName.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
                continue;

            var path = "res://parts/catalog/" + fileName;

            var definition = ResourceLoader.Load<PartDefinition>(path)
                ?? throw new ArgumentException($"Canonical resource '{path}' is invalid or malformed.");
            if (Admit(definition, path, seen) == PartAdmission.Undelivered)
            {
                // The registry owns the resource it refuses and releases it here, on the main thread; a dropped wrapper's
                // finalizer would otherwise release it on the GC thread while a later Load reuses the cached resource.
                definition.Dispose();
                continue;
            }

            var id = definition.PartId;
            if (definition.Ball is not null && (id == PartId.Basketball || id == PartId.BowlingBall))
                definition.Ball.Capture(id);

            _definitions.Add(id, definition);
        }

        if (_definitions.Count == 0)
            throw new ArgumentException("No valid catalogue parts were discovered.");

        WorkshopInventoryPolicy.GenerateFree(_definitions.Keys);
    }

    /// <summary>
    /// Admission is the checked-in typed <see cref="UndeliveredParts"/> set, never the resource's contents (Story 9R.0c). Identity
    /// comes first, so a second resource claiming any identity, delivered or not, is a duplicate; an undelivered part is refused
    /// before its content is checked; a delivered part must have a scene and no untyped Parameters (the Clock alone declares its
    /// interval there).
    /// </summary>
    internal static PartAdmission Admit(PartDefinition definition, string path, ISet<PartId> seen)
    {
        if (string.IsNullOrWhiteSpace(definition.Id))
            throw new ArgumentException($"Canonical resource '{path}' has missing or empty Id.");
        var id = definition.PartId;
        if (!seen.Add(id))
            throw new ArgumentException($"Duplicate part identity '{id}' encountered in '{path}'.");
        if (UndeliveredParts.Contains(id))
            return PartAdmission.Undelivered;
        if (definition.Scene is null)
            throw new ArgumentException($"Canonical resource '{path}' is invalid or malformed.");
        if (definition.Parameters.Count != 0 && id != PartId.Clock)
            throw new ArgumentException($"Canonical resource '{path}' declares untyped Parameters.");
        return PartAdmission.Registered;
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
