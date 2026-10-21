using System.Reflection;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.BundleElements;

/// <summary>The packages found and the packages refused.</summary>
public sealed record DiscoveryResult(IReadOnlyList<BundlePackage> Packages, IReadOnlyList<BundleError> Errors);

/// <summary>Finds every element package by globbing <c>elements/*/*.csproj</c> under the repository root; there is no hand list. Each
/// package's assembly is loaded by its project name, and it must declare exactly one concrete public
/// <see cref="IPuzzleElementDeclaration"/> with a public parameterless constructor.</summary>
public static class PackageDiscovery
{
    /// <summary>The project name of every <c>elements/*/*.csproj</c>, in ordinal order.</summary>
    public static IReadOnlyList<string> PackageNames(string root)
    {
        var elements = Path.Combine(root, "elements");
        if (!Directory.Exists(elements)) throw new DirectoryNotFoundException($"No elements directory under '{root}'.");
        return Directory.EnumerateDirectories(elements)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly))
            .Select(Path.GetFileNameWithoutExtension).OfType<string>().Order(StringComparer.Ordinal).ToArray();
    }

    public static DiscoveryResult Discover(string root)
    {
        var packages = new List<BundlePackage>();
        var errors = new List<BundleError>();
        foreach (var name in PackageNames(root))
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.Load(new AssemblyName(name));
            }
            catch (FileNotFoundException failure)
            {
                errors.Add(new(BundleErrorCode.DeclarationCount, $"{name}: the package assembly does not load ({failure.Message})"));
                continue;
            }
            var found = FromAssembly(name, assembly);
            packages.AddRange(found.Packages);
            errors.AddRange(found.Errors);
        }
        return new(packages, errors);
    }

    public static DiscoveryResult FromAssembly(string package, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var declarations = assembly.GetTypes()
            .Where(type => type.IsPublic && type.IsClass && !type.IsAbstract && typeof(IPuzzleElementDeclaration).IsAssignableFrom(type) &&
                type.GetConstructor(Type.EmptyTypes) is not null)
            .OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();
        if (declarations.Length != 1)
            return new([], [new(BundleErrorCode.DeclarationCount,
                $"{package}: declares {declarations.Length} elements; a package declares exactly one")]);
        return new([new(package, (IPuzzleElementDeclaration)Activator.CreateInstance(declarations[0])!)], []);
    }
}
