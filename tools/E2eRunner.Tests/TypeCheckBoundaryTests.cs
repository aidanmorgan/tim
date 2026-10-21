using System.Text.Json;
using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

public sealed class TypeCheckBoundaryTests
{
    [Fact]
    public void CompilerAbsenceIsARefusalNotASkip()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "tsc");
        var error = Assert.Throws<RunnerException>(() => TypeCheck.Run(missing, TestRepository.Root, TypeCheck.Project));
        Assert.Equal(RunnerError.ToolchainMissing, error.Error);
    }

    [Fact]
    public void MissingPinnedToolchainIsExplicit()
    {
        var home = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Assert.Equal(RunnerError.ToolchainMissing, Assert.Throws<RunnerException>(() => Toolchain.ResolveTypeScript(home)).Error);
        Assert.Equal(RunnerError.ToolchainMissing, Assert.Throws<RunnerException>(() => Toolchain.ResolveNodeTypes(home)).Error);
    }

    [Fact]
    public void ConfigUsesTheExistingPlaywrightExternalBoundaryAndStrictChecking()
    {
        using var config = JsonDocument.Parse(TestRepository.Read(TypeCheck.Project));
        var options = config.RootElement.GetProperty("compilerOptions");
        Assert.True(options.GetProperty("strict").GetBoolean());
        Assert.True(options.GetProperty("noEmit").GetBoolean());
        Assert.Equal(Path.Combine(Toolchain.PlaywrightMcp, "node_modules", "playwright"),
            options.GetProperty("paths").GetProperty("playwright")[0].GetString());
    }
}
