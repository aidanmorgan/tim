using System.Xml;
using System.Xml.Linq;

namespace CuriousContraptions.E2eRunner;

/// <summary>Only understood package build changes may narrow selection; removed references participate equally with added ones.</summary>
internal static class BuildImpact
{
    public const string Solution = "CuriousContraptions.slnx";

    public static IReadOnlySet<PartId>? Parts(string path, ImpactContext context)
    {
        try
        {
            var parts = new HashSet<PartId>();
            if (path == Solution)
            {
                var before = Parse(context.ReadBase(path));
                var after = Parse(context.ReadHead(path));
                if (before is null || after is null) return null;
                var oldProjects = Projects(before);
                var newProjects = Projects(after);
                if (oldProjects is null || newProjects is null) return null;
                RemoveProjects(before);
                RemoveProjects(after);
                if (!XNode.DeepEquals(before, after)) return null;
                foreach (var project in oldProjects.SymmetricDifference(newProjects))
                {
                    if (PackageProject(project) is not { } part) return null;
                    parts.Add(part);
                }
            }
            else
            {
                if (PackageProject(path) is not { } part) return null;
                parts.Add(part);
                if (context.ReadBase(path) is null && context.ReadHead(path) is null) return null;
                foreach (var text in new[] { context.ReadBase(path), context.ReadHead(path) })
                {
                    if (text is null) continue;
                    var document = Parse(text);
                    if (document is null || !SafePackage(document)) return null;
                }
            }

            // Include both reference directions at the edit, then transitive consumers; cycles terminate in the set.
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var (directory, part) in ImpactRules.ElementPackages)
                {
                    var project = $"elements/{directory}/{directory}.csproj";
                    foreach (var text in new[] { context.ReadBase(project), context.ReadHead(project) })
                    {
                        if (text is null) continue;
                        var document = Parse(text);
                        if (document is null) return null;
                        foreach (var reference in document.Descendants("ProjectReference"))
                        {
                            var include = (string?)reference.Attribute("Include");
                            if (include is null || include.Contains('$', StringComparison.Ordinal) || include.Contains('*', StringComparison.Ordinal))
                                return null;
                            var resolved = Path.GetRelativePath(Path.GetFullPath("."), Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project)!, include))).Replace('\\', '/');
                            if (PackageProject(resolved) is not { } referenced)
                            {
                                if (!KnownSharedReference(resolved)) return null;
                                continue;
                            }
                            if (parts.Contains(referenced) && parts.Add(part)) changed = true;
                            if (project == path && parts.Add(referenced)) changed = true;
                        }
                    }
                }
            }
            return parts;
        }
        catch (XmlException) { return null; }
        catch (ArgumentException) { return null; }
    }

    internal static PartId? PackageProject(string path)
    {
        foreach (var (directory, part) in ImpactRules.ElementPackages)
            if (path == $"elements/{directory}/{directory}.csproj") return part;
        return null;
    }

    private static bool KnownSharedReference(string path) => path is
        "CuriousContraptions.Abstractions/CuriousContraptions.Abstractions.csproj" or
        "CuriousContraptions.Abstractions.Signal/CuriousContraptions.Abstractions.Signal.csproj" or
        "CuriousContraptions.Abstractions.Electrical/CuriousContraptions.Abstractions.Electrical.csproj" or
        "CuriousContraptions.Abstractions.Mechanical/CuriousContraptions.Abstractions.Mechanical.csproj" or
        "CuriousContraptions.Abstractions.Fluid/CuriousContraptions.Abstractions.Fluid.csproj";

    private static XDocument? Parse(string? text) => text is null ? null : XDocument.Parse(text);

    private static bool SafePackage(XDocument document) =>
        document.Root?.Name == "Project" &&
        document.Root.Attributes().All(attribute => attribute.Name == "Sdk" && attribute.Value == "Microsoft.NET.Sdk") &&
        document.Root.Elements().All(group =>
            (group.Name == "PropertyGroup" && !group.HasAttributes &&
                group.Elements().All(property => !property.HasAttributes && !property.HasElements &&
                    property.Name.LocalName is "TargetFramework" or "Nullable" or "ImplicitUsings" or "IsTrimmable")) ||
            (group.Name == "ItemGroup" && !group.HasAttributes &&
                group.Elements().All(item => item.Name == "ProjectReference" && !item.HasElements &&
                    item.Attributes().Count() == 1 && item.Attribute("Include") is not null)));

    private static HashSet<string>? Projects(XDocument document)
    {
        if (document.Root?.Name != "Solution") return null;
        var projects = document.Descendants("Project").ToArray();
        if (projects.Any(project => project.HasElements || project.Attributes().Count() != 1 || project.Attribute("Path") is null)) return null;
        var paths = projects.Select(project => (string)project.Attribute("Path")!).ToHashSet(StringComparer.Ordinal);
        return paths.Count == projects.Length ? paths : null;
    }

    private static void RemoveProjects(XDocument document)
    {
        document.Descendants("Project").Remove();
        foreach (var folder in document.Descendants("Folder").Where(folder => !folder.HasElements).ToArray()) folder.Remove();
    }

    private static IEnumerable<string> SymmetricDifference(this HashSet<string> before, HashSet<string> after) =>
        before.Except(after, StringComparer.Ordinal).Concat(after.Except(before, StringComparer.Ordinal));
}
