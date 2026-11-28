using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using Wire = CuriousContraptions.Wire;

namespace CuriousContraptions.Tests;

/// <summary>S14: until ENGINE-WIRE-ABI moves ownership, these domain enums must agree with their generated wire enums.</summary>
public sealed class WireEnumMirrorTests
{
    [Fact]
    public void EachDomainMirrorOfAWireEnumMatchesItsGeneratedDeclaration()
    {
        string[] differences =
        [
            .. Differences<WorkshopCommandKind, Wire.CommandKind>(),
            .. Differences<WorkshopResponseKind, Wire.ResponseKind>(),
            .. Differences<WorkshopSimulationPhase, Wire.SimulationPhase>(),
            .. Differences<WorkshopCommandOutcome, Wire.CommandOutcome>(),
            .. Differences<ElectricalEnable, Wire.ElectricalEnable>(),
            .. Differences<RunControlLane, Wire.RunControlLane>(),
            .. Differences<RunControlValueKind, Wire.RunControlValueKind>()
        ];
        Assert.True(differences.Length == 0, string.Join(Environment.NewLine, differences));
    }

    private enum Domain : byte { Enabled = 2, Disabled = 0 }
    private enum MatchingWire : uint { Disabled = 0, Enabled = 2 }
    private enum WrongValue : uint { Disabled = 0, Enabled = 1 }
    private enum WrongName : uint { Disabled = 0, Ready = 2 }

    [Fact]
    public void EnumComparisonReportsWrongValuesAndNamesAcrossStorageTypes()
    {
        Assert.Empty(Differences<Domain, MatchingWire>());
        Assert.Equal(["Domain / WrongValue: Enabled is 2 / 1"], Differences<Domain, WrongValue>());
        Assert.Equal(["Domain / WrongName: Enabled is 2 / missing", "Domain / WrongName: Ready is missing / 2"],
            Differences<Domain, WrongName>());
    }

    private static IEnumerable<string> Differences<TDomain, TWire>() where TDomain : struct, Enum where TWire : struct, Enum
    {
        var domain = MemberValues<TDomain>();
        var wire = MemberValues<TWire>();
        foreach (var name in domain.Keys.Union(wire.Keys).Order(StringComparer.Ordinal))
        {
            decimal? expected = domain.TryGetValue(name, out var domainValue) ? domainValue : null;
            decimal? actual = wire.TryGetValue(name, out var wireValue) ? wireValue : null;
            if (expected != actual)
                yield return $"{typeof(TDomain).Name} / {typeof(TWire).Name}: {name} is {Format(expected)} / {Format(actual)}";
        }
    }

    private static string Format(decimal? value) =>
        value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "missing";

    private static Dictionary<string, decimal> MemberValues<T>() where T : struct, Enum =>
        Enum.GetNames<T>().ToDictionary(name => name,
            name => Convert.ToDecimal(Enum.Parse<T>(name), System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);
}
