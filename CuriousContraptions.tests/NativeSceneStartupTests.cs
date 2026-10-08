using System.Security.Cryptography;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public sealed class NativeSceneStartupTests(NativeSceneFixture godot)
{
    private const string ProbeEnvironment = "CURIOUS_NATIVE_TEST_ERROR_PROBE";
    private const string ProbeError = "Deliberate native-host error-capture qualification.";
    private enum ErrorProbe { Disabled, ReportError }
    public enum InvalidBundle { Missing, Empty, Malformed }

    [Fact]
    public void CurrentSettingsAndResourcesResolveThroughTheIsolatedProject()
    {
        var environment = godot.Environment;
        Assert.Equal(File.ReadAllBytes(environment.Source.Child(NativeTestBoundary.ProjectFile).Value),
            Godot.FileAccess.GetFileAsBytes(NativeTestBoundary.ProjectResource));
        Assert.Equal(File.ReadAllBytes(environment.Source.Child(NativeTestBoundary.CampaignRelativePath).Value),
            Godot.FileAccess.GetFileAsBytes(NativeTestBoundary.CampaignResource));
        Assert.NotNull(GD.Load<PackedScene>(NativeTestBoundary.WorkshopResource));
        Assert.Equal(120, Godot.Engine.PhysicsTicksPerSecond);
        Assert.True(File.Exists(environment.Log.Value));
        if (environment.CertificateBundle is { } bundle)
            Assert.Equal(bundle.Value, ProjectSettings.GetSetting(NativeTestBoundary.CertificateSetting).AsString());
        Assert.NotEqual(environment.Source.Value, ProjectSettings.GlobalizePath(NativeTestBoundary.ResourceRoot).TrimEnd('/'));
    }

    [Fact]
    public void NativeErrorAssertionsRemainEnabled()
    {
        var probe = System.Environment.GetEnvironmentVariable(ProbeEnvironment) switch
        {
            null => ErrorProbe.Disabled,
            "report-error" => ErrorProbe.ReportError,
            _ => throw new InvalidOperationException("Unsupported native error probe.")
        };
        if (probe == ErrorProbe.ReportError) GD.PushError(ProbeError);
        Assert.NotNull(godot.Tree);
    }

    [Theory]
    [InlineData(InvalidBundle.Missing)]
    [InlineData(InvalidBundle.Empty)]
    [InlineData(InvalidBundle.Malformed)]
    public void ExplicitInvalidCertificateBundlesAreRejected(InvalidBundle failure)
    {
        var path = new NativeTestPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        switch (failure)
        {
            case InvalidBundle.Missing:
                Assert.Throws<FileNotFoundException>(() => NativeTestEnvironment.ValidateCertificateBundle(path));
                break;
            case InvalidBundle.Empty:
                File.WriteAllText(path.Value, string.Empty);
                try { Assert.Throws<CryptographicException>(() => NativeTestEnvironment.ValidateCertificateBundle(path)); }
                finally { File.Delete(path.Value); }
                break;
            case InvalidBundle.Malformed:
                File.WriteAllText(path.Value, "-----BEGIN CERTIFICATE-----\nnot-base64\n-----END CERTIFICATE-----");
                try { Assert.Throws<CryptographicException>(() => NativeTestEnvironment.ValidateCertificateBundle(path)); }
                finally { File.Delete(path.Value); }
                break;
            default: throw new ArgumentOutOfRangeException(nameof(failure));
        }
    }

    [Fact]
    public void RelativeCertificatePathsAndUnknownArgumentsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new NativeTestPath(NativeTestBoundary.ProjectFile));
        Assert.Throws<ArgumentOutOfRangeException>(() => NativeTestBoundary.Argument((NativeStartupArgument)(-1)));
    }
}
