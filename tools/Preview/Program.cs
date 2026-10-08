using System.Globalization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.FileProviders;

if (args.Length > 3) throw new ArgumentException("Usage: Preview [directory [port [base-path/]]]");
var port = 8060;
if (args.Length > 1 && (!int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out port) || port is < 1 or > 65535))
    throw new ArgumentException("Preview port must be an integer from1 through65535.");
var prefix = args.Length > 2 ? args[2] : "/";
if (!prefix.StartsWith('/') || !prefix.EndsWith('/') ||
    (prefix != "/" && prefix[1..^1].Split('/').Any(segment =>
        segment.Length == 0 || segment.Any(value => !char.IsAsciiLetterOrDigit(value) && value is not '-' and not '_'))))
    throw new ArgumentException("Preview base path must be a rooted directory of ASCII letters, digits, '-' or '_'.");
var project = new DirectoryInfo(Directory.GetCurrentDirectory());
while (project != null && !File.Exists(Path.Combine(project.FullName, "project.godot")))
    project = project.Parent;
var directory = Path.GetFullPath(args.FirstOrDefault() ??
    Path.Combine(project?.FullName ?? Directory.GetCurrentDirectory(), "CuriousContraptions.web", "AppBundle"));
if (!File.Exists(Path.Combine(directory, "index.html")))
    throw new DirectoryNotFoundException("Publish the web host first. No index.html in " + directory);
var builder = WebApplication.CreateBuilder();
builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
builder.Services.AddResponseCompression(options =>
{
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/wasm", "application/octet-stream"]);
});
var app = builder.Build();
// app.UseResponseCompression();
var files = new PhysicalFileProvider(directory);
app.Lifetime.ApplicationStopped.Register(files.Dispose);
void Serve(IApplicationBuilder branch)
{
    branch.Use((context, next) =>
    {
        context.Request.Headers.Remove("If-None-Match");
        context.Request.Headers.Remove("If-Modified-Since");
        return next();
    });
    branch.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
    branch.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = files,
        ServeUnknownFileTypes = true,
        DefaultContentType = "application/octet-stream"
    });
}
if (prefix == "/") Serve(app);
else app.Map(prefix.TrimEnd('/'), Serve);
Console.WriteLine($"Workshop preview: root={directory}, origin=http://127.0.0.1:{port}, basePath={prefix}");
app.Run();
