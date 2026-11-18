using Godot;
using System.IO;

namespace CuriousContraptions;

/// <summary>Loads private native artwork and imported icons without retrieving globally cached wrappers.</summary>
public static class WorkshopResources
{
    public static PackedScene LoadScene(string path)
    {
        var scene = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.IgnoreDeep)
            ?? throw new InvalidDataException($"Cannot load native artwork scene '{path}'.");
        ValidateArtwork(scene);
        return scene;
    }

    /// <summary>Refuses script-bearing artwork without materializing script wrappers. A refused scene is disposed here.</summary>
    internal static void ValidateArtwork(PackedScene scene)
    {
        try
        {
            using var state = scene.GetState();
            if (state.GetNodeCount() == 0 || state.GetNodeType(0) != "Node3D")
                throw new InvalidDataException("Native artwork must have a Node3D root.");
            for (var node = 0; node < state.GetNodeCount(); node++)
                for (var property = 0; property < state.GetNodePropertyCount(node); property++)
                    if (state.GetNodePropertyName(node, property) == "script")
                        throw new InvalidDataException("Native artwork cannot contain scripts.");
        }
        catch
        {
            scene.Dispose();
            throw;
        }
    }

    public static Texture2D LoadIcon(string path) =>
        ResourceLoader.Load<Texture2D>(path, cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
}
