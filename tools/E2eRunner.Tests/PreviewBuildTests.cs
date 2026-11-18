using System.Reflection;
using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>The runner project owns Preview output freshness through its normal project dependency.</summary>
public sealed class PreviewBuildTests
{
    [Theory]
    [InlineData("Preview.dll")]
    [InlineData("Preview.runtimeconfig.json")]
    [InlineData("Preview.deps.json")]
    public void MissingMatchingPreviewOutputRefuses(string missing)
    {
        var scratch = Directory.CreateTempSubdirectory("preview-missing-");
        try
        {
            foreach (var name in new[] { "Preview.dll", "Preview.runtimeconfig.json", "Preview.deps.json" })
                if (name != missing) File.WriteAllText(Path.Combine(scratch.FullName, name), "fixture");
            var refusal = Assert.Throws<RunnerException>(() => PreviewHost.RequireExecutable(scratch.FullName));
            Assert.Equal(RunnerError.PreviewUnavailable, refusal.Error);
            Assert.Contains(missing, refusal.Message, StringComparison.Ordinal);
        }
        finally { scratch.Delete(true); }
    }

    [Fact]
    public void IncrementalRunnerBuildCopiesChangedPreviewDependency()
    {
        var scratch = Directory.CreateTempSubdirectory("preview-build-");
        try
        {
            var runner = Path.Combine(scratch.FullName, "tools", "E2eRunner");
            var preview = Path.Combine(scratch.FullName, "tools", "Preview");
            Directory.CreateDirectory(runner);
            Directory.CreateDirectory(preview);
            var project = TestRepository.Read("tools/E2eRunner/E2eRunner.csproj").Replace(
                "    <ProjectReference Include=\"../../CuriousContraptions.Abstractions/CuriousContraptions.Abstractions.csproj\" />",
                "", StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(runner, "E2eRunner.csproj"), project);
            File.WriteAllText(Path.Combine(runner, "Program.cs"), "System.Console.WriteLine(\"build fixture\");");
            File.WriteAllText(Path.Combine(preview, "Preview.csproj"), TestRepository.Read("tools/Preview/Preview.csproj"));
            File.WriteAllText(Path.Combine(preview, "Program.cs"), "System.Console.WriteLine(\"Preview dependency fixture\");");
            var input = Path.Combine(preview, "Version.cs");
            var copied = Path.Combine(runner, "bin", "Debug", "net10.0", "Preview.dll");
            foreach (var version in new[] { "1.0.0.0", "2.0.0.0" })
            {
                File.WriteAllText(input, $"[assembly: System.Reflection.AssemblyVersion(\"{version}\")]");
                var build = Processes.Run("dotnet", ["build", Path.Combine(runner, "E2eRunner.csproj"), "--nologo", "-noAutoResponse",
                    "-p:GenerateAssemblyVersionAttribute=false"], scratch.FullName);
                Assert.True(build.ExitCode == 0, build.Output + build.Error);
                Assert.Equal(Version.Parse(version), AssemblyName.GetAssemblyName(copied).Version);
                Assert.True(File.Exists(Path.ChangeExtension(copied, ".runtimeconfig.json")));
                Assert.True(File.Exists(Path.ChangeExtension(copied, ".deps.json")));
            }
        }
        finally { scratch.Delete(true); }
    }
}
