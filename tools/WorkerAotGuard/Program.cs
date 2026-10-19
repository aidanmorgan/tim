namespace CuriousContraptions.WorkerAotGuard;

/// <summary>The guard's checks (the CLI boundary converts the first argument to one of these).</summary>
internal enum GuardCheck { NativeModules = 1, IlStripped, Simd, HostCopies }

internal static class Program
{
    private static readonly Dictionary<string, GuardCheck> Verbs = new(StringComparer.Ordinal)
    {
        ["native-modules"] = GuardCheck.NativeModules, ["il-stripped"] = GuardCheck.IlStripped, ["simd"] = GuardCheck.Simd,
        ["host-copies"] = GuardCheck.HostCopies,
    };

    private const string Usage = """
        Usage: WorkerAotGuard <check> ...
          native-modules <dotnet.native.wasm> <assembly name>...                 (b) every engine AOT module is in the native module
          il-stripped <pair list>                                                (c) each "stripped|aot input" pair differs
          simd <llvm-objdump> <object> <symbol prefix> <report> <type>...         (d) f32x4 arithmetic in each named type
          host-copies <pair list>                                                each "worker file|host copy" pair and its .br/.gz match
        """;

    public static int Main(string[] args)
    {
        if (args.Length < 2 || !Verbs.TryGetValue(args[0], out var check))
        {
            Console.Error.WriteLine(Usage);
            return 2;
        }
        var result = check switch
        {
            GuardCheck.NativeModules => Checks.NativeModules(args[1], args[2..]),
            GuardCheck.IlStripped => Checks.IlStripped(Checks.ReadPairs(args[1])),
            GuardCheck.Simd when args.Length >= 6 => Checks.Simd(args[1], args[2], args[3], args[4], args[5..]),
            GuardCheck.HostCopies => Checks.HostCopies(Checks.ReadPairs(args[1])),
            GuardCheck.Simd => null,
            _ => throw new ArgumentOutOfRangeException(nameof(args), check, "Undefined guard check."),
        };
        if (result is null)
        {
            Console.Error.WriteLine(Usage);
            return 2;
        }
        foreach (var note in result.Notes) Console.WriteLine(note);
        foreach (var finding in result.Findings) Console.WriteLine(finding);
        return result.Passed ? 0 : 1;
    }
}
