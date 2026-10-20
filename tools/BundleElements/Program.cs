using System.Text;
using CuriousContraptions.BundleElements;

// Build-time generator (Story 9A.4): BundleElements --root <repository> --out <file> [--out <file>]. Each failure is written in
// MSBuild's canonical error form, so the build that runs it fails naming the package and the field.
string? root = null;
var outputs = new List<string>();
for (var i = 0; i < args.Length; i++)
{
    var value = i + 1 < args.Length ? args[i + 1] : null;
    switch (args[i])
    {
        case "--root" when value is not null: root = Path.GetFullPath(value); i++; break;
        case "--out" when value is not null: outputs.Add(Path.GetFullPath(value)); i++; break;
        default:
            Console.Error.WriteLine($"BundleElements : error BUNDLE000 : unsupported argument '{args[i]}'; use --root <dir> --out <file>");
            return 2;
    }
}
if (root is null || outputs.Count == 0)
{
    Console.Error.WriteLine("BundleElements : error BUNDLE000 : supply --root <dir> and at least one --out <file>");
    return 2;
}

var generated = BundleGenerator.Generate(root);
if (generated.Errors.Count > 0)
{
    foreach (var error in generated.Errors) Console.Error.WriteLine($"BundleElements : error {error.Text} : {error.Detail}");
    return 1;
}
var bytes = Encoding.UTF8.GetBytes(generated.Json!);
foreach (var output in outputs)
{
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    // Unchanged bytes are not rewritten, so an incremental build sees no new input.
    if (!File.Exists(output) || !File.ReadAllBytes(output).AsSpan().SequenceEqual(bytes)) File.WriteAllBytes(output, bytes);
    Console.WriteLine($"Element bundle: {output} ({generated.Count} elements)");
}
return 0;
