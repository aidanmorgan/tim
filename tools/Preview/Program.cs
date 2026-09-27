using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.FileProviders;

var project = new DirectoryInfo(Directory.GetCurrentDirectory());
while (project != null && !File.Exists(Path.Combine(project.FullName, "project.godot")))
    project = project.Parent;
var directory = Path.GetFullPath(args.FirstOrDefault() ??
    Path.Combine(project?.FullName ?? Directory.GetCurrentDirectory(), "CuriousContraptions.web", "AppBundle"));
if (!File.Exists(Path.Combine(directory, "index.html")))
    throw new DirectoryNotFoundException("Publish the web host first. No index.html in " + directory);
var builder = WebApplication.CreateBuilder();
builder.WebHost.UseUrls("http://127.0.0.1:8060");
builder.Services.AddResponseCompression(options =>
{
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/wasm", "application/octet-stream"]);
});
var app = builder.Build();
app.UseResponseCompression();
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = new PhysicalFileProvider(directory) });
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(directory),
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});
app.Run();
