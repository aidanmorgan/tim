using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ownership;

public static class OwnershipOracles
{
    private static readonly SourcePath Source = new("engine/presentation/AnimationBatch.cs");
    private static readonly MemberId Member = new("F:CuriousContraptions.Presentation.AnimationBatch._free");
    private static readonly CallerId Caller = new("M:CuriousContraptions.Presentation.AnimationBatch.FillFree");
    private static readonly WorkId Task = new("P0-022");
    private enum Attack
    {
        MissingMember, DuplicateMember, UnknownMember, UnknownOwner, UnknownRule,
        UnknownTask, WrongPlacement, StaleSource, MissingSource, DuplicateSource,
        StaleWriterFingerprint, StaleContentsFingerprint, UnauthorizedWriter, ReferenceContentsOmitted, ValidButWrongOwner, SceneReferenceCrossing, MissingOwnerField, UnknownField, DuplicateJsonField, NullAssignment, BindingErrors, UnauthorizedReader
    }

    public static object Run(string? root = null)
    {
        var member = new StateMember(Member, Source, 4, new("System.Double[]"), StorageForm.Field,
            StorageMutability.ReferencedStorage, false, [new(Source, 8, Caller, StateAccess.Call)]);
        var source = new SourceInput(Source, new string('a', 64));
        var snapshot = new OwnershipSnapshot([source], [member]);
        var assignment = new OwnershipAssignment(Member, AssemblyOwner.AnimationKernel, Task,
            OwnershipRule.AnimationAuthority, OwnershipAudit.Fingerprint(member));
        var contract = new OwnershipContract([source], [assignment]);
        contract = OwnershipAudit.Decode<OwnershipContract>(JsonSerializer.Serialize(contract, OwnershipAudit.Json));
        var tasks = new HashSet<WorkId> { Task };
        OwnershipAudit.Validate(snapshot, contract, tasks);
        var results = new List<object>();
        foreach (var attack in Enum.GetValues<Attack>())
        {
            var rejected = false;
            try
            {
                var candidate = contract;
                var observed = snapshot;
                switch (attack)
                {
                    case Attack.MissingMember: candidate = contract with { Assignments = [] }; break;
                    case Attack.DuplicateMember: candidate = contract with { Assignments = [assignment, assignment] }; break;
                    case Attack.UnknownMember: candidate = contract with { Assignments = [assignment with { Member = new("F:Fixture.Unknown.Values") }] }; break;
                    case Attack.UnknownOwner: candidate = contract with { Assignments = [assignment with { Owner = (AssemblyOwner)999 }] }; break;
                    case Attack.UnknownRule: candidate = contract with { Assignments = [assignment with { Rule = (OwnershipRule)999 }] }; break;
                    case Attack.UnknownTask: candidate = contract with { Assignments = [assignment with { Migration = new("P0-Unknown") }] }; break;
                    case Attack.WrongPlacement: candidate = contract with { Assignments = [assignment with { Owner = AssemblyOwner.SimulationCore }] }; break;
                    case Attack.StaleSource: candidate = contract with { Sources = [source with { Sha256 = new string('b', 64) }] }; break;
                    case Attack.MissingSource: candidate = contract with { Sources = [] }; break;
                    case Attack.DuplicateSource: candidate = contract with { Sources = [source, source] }; break;
                    case Attack.StaleWriterFingerprint:
                        observed = snapshot with { Members = [member with { Uses = [..member.Uses,
                            new(Source, 9, new("M:Fixture.Other.Mutate"), StateAccess.ReferenceEscape)] }] }; break;
                    case Attack.StaleContentsFingerprint:
                        observed = snapshot with { Members = [member with { Mutability = StorageMutability.ConstructionValue }] }; break;
                    case Attack.UnauthorizedReader:
                        var unknownReader = member with { Uses = [..member.Uses,
                            new(Source, 9, new("M:Fixture.Other.Mutate"), StateAccess.Read)] };
                        observed = snapshot with { Members = [unknownReader] };
                        candidate = contract with { Assignments = [assignment with { MemberSha256 = OwnershipAudit.Fingerprint(unknownReader) }] };
                        break;
                    case Attack.UnauthorizedWriter:
                        var changedWriter = member with { Uses = [..member.Uses,
                            new(Source, 9, new("M:Fixture.Other.Mutate"), StateAccess.ReferenceEscape)] };
                        observed = snapshot with { Members = [changedWriter] };
                        candidate = contract with { Assignments = [assignment with { MemberSha256 = OwnershipAudit.Fingerprint(changedWriter) }] };
                        break;
                    case Attack.ReferenceContentsOmitted:
                        var changedContents = member with { Mutability = StorageMutability.ConstructionValue };
                        observed = snapshot with { Members = [changedContents] };
                        candidate = contract with { Assignments = [assignment with { MemberSha256 = OwnershipAudit.Fingerprint(changedContents) }] };
                        break;
                    case Attack.ValidButWrongOwner:
                        candidate = contract with { Assignments = [assignment with {
                            Owner = AssemblyOwner.SimulationCore, Rule = OwnershipRule.SimulationAuthority }] };
                        break;
                    case Attack.SceneReferenceCrossing:
                        var scene = member with
                        {
                            Id = new("F:CuriousContraptions.MachineWorld._parts"),
                            Type = new("System.Collections.Generic.List<CuriousContraptions.MachinePart>")
                        };
                        observed = snapshot with { Members = [scene] };
                        candidate = contract with { Assignments = [assignment with
                        {
                            Member = scene.Id, Owner = AssemblyOwner.Protocol, Rule = OwnershipRule.ImmutableContract,
                            MemberSha256 = OwnershipAudit.Fingerprint(scene)
                        }] };
                        break;
                    case Attack.DuplicateJsonField:
                        var sourceJson = JsonSerializer.Serialize(source, OwnershipAudit.Json);
                        _ = OwnershipAudit.Decode<SourceInput>(sourceJson.Insert(1,
                            JsonSerializer.Serialize(nameof(SourceInput.Sha256)) + ":" + JsonSerializer.Serialize(source.Sha256) + ","));
                        break;
                    case Attack.NullAssignment:
                        candidate = contract with { Assignments = [null!] };
                        break;
                    case Attack.BindingErrors:
                        observed = snapshot with { Diagnostics = [new(InspectionContext.ProductionRelease,
                            new("CS0001"), Source, 1, "Unresolved fixture binding.")] };
                        break;
                    case Attack.MissingOwnerField:
                        var document = JsonSerializer.SerializeToNode(contract, OwnershipAudit.Json)!;
                        document[nameof(OwnershipContract.Assignments)]![0]!.AsObject().Remove(nameof(OwnershipAssignment.Owner));
                        candidate = document.Deserialize<OwnershipContract>(OwnershipAudit.Json)!; break;
                    case Attack.UnknownField:
                        candidate = JsonSerializer.Deserialize<OwnershipContract>("{\"Unsupported\":true}", OwnershipAudit.Json)!; break;
                    default: throw new InvalidOperationException("Unimplemented oracle.");
                }
                OwnershipAudit.Validate(observed, candidate, tasks);
            }
            catch (InvalidDataException) { rejected = true; }
            catch (JsonException) { rejected = true; }
            if (!rejected) throw new InvalidOperationException($"Ownership oracle accepted attack {attack}.");
            results.Add(new { Attack = attack, Rejected = rejected });
        }
        return new { Positive = true, NegativeCount = results.Count, Results = results, SemanticFlows = CheckReferenceFlows(), PolicyPlacements = CheckPolicyPlacements(), PortableAssembly = CheckPortableAssembly(), CurrentOwners = CurrentOwnerMembershipOracles.Run(root), ActualCurrentPolicy = CheckCurrentPolicy(root) };
    }



    private static object? CheckCurrentPolicy(string? root)
    {
        if (root is null) return null;
        var snapshot = SourceInventory.Capture(root);
        if (snapshot.Diagnostics.Length != 0) throw new InvalidOperationException("Actual policy capture has binding errors.");
        var checkedMembers = new List<object>();
        foreach (var (identity, owner) in OwnershipPolicy.Expected)
        {
            var member = snapshot.Members.Single(member => member.Id == identity);
            var rule = owner switch
            {
                AssemblyOwner.AnimationKernel => OwnershipRule.AnimationAuthority,
                AssemblyOwner.SimulationHost => OwnershipRule.HostState,
                AssemblyOwner.GodotPresenter => OwnershipRule.SceneResource,
                _ => throw new InvalidOperationException("Unreviewed current guard owner.")
            };
            var assignment = new OwnershipAssignment(identity, owner, Task, rule, OwnershipAudit.Fingerprint(member));
            OwnershipPolicy.Validate(member, assignment);
            var rejected = false;
            try { OwnershipPolicy.Validate(member, assignment with { Owner = owner == AssemblyOwner.AnimationKernel ? AssemblyOwner.GodotPresenter : AssemblyOwner.AnimationKernel }); }
            catch (InvalidDataException) { rejected = true; }
            if (!rejected) throw new InvalidOperationException("Actual current guard accepted wrong ownership.");
            checkedMembers.Add(new { member.Id, member.Path, member.Type, member.Mutability,
                Fingerprint = OwnershipAudit.Fingerprint(member), Callers = member.Uses.Select(use => use.Caller).Distinct().ToArray(),
                WrongOwnerRejected = rejected });
        }
        return new { BindingErrors = snapshot.Diagnostics.Length, Members = checkedMembers };
    }

    private static object[] CheckPolicyPlacements()
    {
        var source = new SourceInput(Source, new string('a', 64));
        var results = new List<object>();
        foreach (var (identity, owner) in OwnershipPolicy.Expected)
        {
            // This fixture tests guard enforcement, not the semantic truth of the policy table.
            // Actual ownership remains source-derived and independently reviewed.
            var member = new StateMember(identity, Source, 1, new("System.Object"),
                StorageForm.Field, StorageMutability.ReferencedStorage, false, []);
            var rule = owner switch
            {
                AssemblyOwner.SimulationCore => OwnershipRule.SimulationAuthority,
                AssemblyOwner.SimulationHost or AssemblyOwner.AnimationHost => OwnershipRule.HostState,
                AssemblyOwner.AnimationKernel => OwnershipRule.AnimationAuthority,
                AssemblyOwner.GodotPresenter => OwnershipRule.SceneResource,
                AssemblyOwner.ConstructionCompiler => OwnershipRule.CompilerState,
                AssemblyOwner.Protocol or AssemblyOwner.Geometry => OwnershipRule.ImmutableContract,
                _ => throw new InvalidOperationException("Unsupported policy owner.")
            };
            var assignment = new OwnershipAssignment(identity, owner, Task, rule, OwnershipAudit.Fingerprint(member));
            var snapshot = new OwnershipSnapshot([source], [member]);
            var contract = new OwnershipContract([source], [assignment]);
            var tasks = new HashSet<WorkId> { Task };
            OwnershipAudit.Validate(snapshot, contract, tasks);
            var wrong = owner == AssemblyOwner.GodotPresenter ? AssemblyOwner.SimulationCore : AssemblyOwner.GodotPresenter;
            var wrongRule = wrong == AssemblyOwner.SimulationCore ? OwnershipRule.SimulationAuthority : OwnershipRule.SceneResource;
            var rejected = false;
            try { OwnershipAudit.Validate(snapshot, contract with { Assignments = [assignment with { Owner = wrong, Rule = wrongRule }] }, tasks); }
            catch (InvalidDataException) { rejected = true; }
            if (!rejected) throw new InvalidOperationException("Self-consistent wrong placement bypassed its source-derived guard.");
            results.Add(new { Member = identity, Expected = owner, Positive = true, WrongPlacementRejected = rejected });
        }
        return results.ToArray();
    }

    private enum ReferenceFlow { Direct, Parenthesized, Cast, ChainedCast, RefArgument, OutArgument, Alias, Return, MutableReturn, Conditional, ObjectArgument }
    private enum CallerAuthorization { ExistingName, NewName }

    private static object[] CheckReferenceFlows()
    {
        var results = new List<object>();
        foreach (var flow in Enum.GetValues<ReferenceFlow>())
        foreach (var authorization in Enum.GetValues<CallerAuthorization>())
        {
            // These strings are C# compiler-fixture input, not behavior selectors in the inspected program.
            var body = flow switch
            {
                ReferenceFlow.Direct => "_free[0] = default;",
                ReferenceFlow.Parenthesized => "((_free))[0] = default;",
                ReferenceFlow.Cast => "((int[])_free)[0] = default;",
                ReferenceFlow.ChainedCast => "((int[])(object)(_free))[0] = default;",
                ReferenceFlow.RefArgument => "Take(ref _free[0]);",
                ReferenceFlow.OutArgument => "Give(out _free[0]);",
                ReferenceFlow.Alias => "var alias = _free; alias[0] = default;",
                ReferenceFlow.Return or ReferenceFlow.MutableReturn => "return _free;",
                ReferenceFlow.Conditional => "var alias = true ? _free : new int[1]; alias[0] = default;",
                ReferenceFlow.ObjectArgument => "Consume((object)_free);",
                _ => throw new InvalidOperationException("Unknown reference-flow fixture.")
            };
            var returnType = flow == ReferenceFlow.MutableReturn ? "int[]" : "void";
            var methodName = authorization == CallerAuthorization.ExistingName ? "FillFree" : "Mutate";
            var declaration = flow == ReferenceFlow.Return
                ? $"public System.ReadOnlySpan<int> {(authorization == CallerAuthorization.ExistingName ? "PublicationReads" : "UnreviewedReads")} {{ get {{ {body} }} }}"
                : $"public {returnType} {methodName}() {{ {body} }}";
            var source = $$"""
                namespace CuriousContraptions.Presentation;
                internal class AnimationBatch
                {
                    private readonly int[] _free = new int[1];
                    {{declaration}}
                    private static void Take(ref int value) { value = default; }
                    private static void Give(out int value) { value = default; }
                    private static void Consume(object value) { }
                }
                """;
            var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
            var compilation = CSharpCompilation.Create("OwnershipSemanticFixture", [tree],
                [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
            if (errors.Length != 0) throw new InvalidOperationException("Reference-flow fixture has binding errors.");
            var model = compilation.GetSemanticModel(tree);
            var reference = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
                .Single(name => name.Identifier.ValueText == "_free");
            var access = SourceInventory.Classify(reference, model);
            if (access != StateAccess.ReferenceEscape)
                throw new InvalidOperationException("Mutable array flow was not conservatively classified.");
            var caller = new CallerId(DocumentationCommentId.CreateDeclarationId(model.GetEnclosingSymbol(reference.SpanStart)!)!);
            var member = new StateMember(Member, Source, 4, new("System.Int32[]"),
                StorageForm.Field, StorageMutability.ReferencedStorage, false, [new(Source, 8, caller, access)]);
            var input = new SourceInput(Source, new string('a', 64));
            var assignment = new OwnershipAssignment(Member, AssemblyOwner.AnimationKernel, Task,
                OwnershipRule.AnimationAuthority, OwnershipAudit.Fingerprint(member));
            var accepted = true;
            try { OwnershipAudit.Validate(new([input], [member]), new([input], [assignment]), new HashSet<WorkId> { Task }); }
            catch (InvalidDataException) { accepted = false; }
            // A same-name method with a new mutable-array return type is a new, unapproved signature.
            if (accepted != (authorization == CallerAuthorization.ExistingName && flow is not (ReferenceFlow.MutableReturn or ReferenceFlow.Return)))
                throw new InvalidOperationException("Reference-flow authorization differs from its source-derived policy.");
            results.Add(new { Flow = flow, Authorization = authorization, Access = access, Accepted = accepted });
        }
        return results.ToArray();
    }

    private enum AssemblyAttack
    {
        MissingContext, DuplicateContext, UnknownContext, RetiredContext, MissingReference, DuplicateReference,
        MissingSource, DuplicateSource, BindingError, MissingMember, DuplicateMember,
        StaleSource, StaleConsumer, WrongOwner, StaleConfiguration
    }

    private static object CheckPortableAssembly()
    {
        // These are two separate compiler-fixture assemblies using the production capture path.
        // No game source reconstruction or alternative build route is used.
        var directory = Directory.CreateTempSubdirectory("ownership-animation-");
        try
        {
            var root = directory.FullName;
            File.WriteAllText(Path.Combine(root, "animation.cs"), """
                namespace CuriousContraptions.Presentation;
                public sealed class AnimationBatch
                {
                    private readonly double[] _slots = new double[1];
                    public double Value => _slots[0];
                    public void Advance() { _slots[0] += 1; }
                }
                """);
            File.WriteAllText(Path.Combine(root, "game.cs"), """
                using CuriousContraptions.Presentation;
                public static class Hint
                {
                    public static double Read(AnimationBatch batch) => batch.Value;
                    public static void Step(AnimationBatch batch) => batch.Advance();
                #if PLAYTEST
                    public static double DiagnosticRead(AnimationBatch batch) => batch.Value;
                #endif
                }
                """);
            File.WriteAllText(Path.Combine(root, "tests.cs"), """
                using CuriousContraptions.Presentation;
                public static class ConsumerTest
                {
                    public static double Read(AnimationBatch batch) => Hint.Read(batch);
                }
                """);
            File.WriteAllText(Path.Combine(root, "capture.props"), "<Project />");
            var core = typeof(object).Assembly.Location;
            SourceInventory.ProjectInputs Inputs(string path, string assembly, string[] references, string[] symbols) =>
                new([path], [], references, symbols, [], assembly, LanguageVersion.CSharp14,
                    NullableContextOptions.Enable, false, OutputKind.DynamicallyLinkedLibrary, false, ["capture.props"]);
            var animationInput = Inputs("animation.cs", "CuriousContraptions.Animation", [core], []);
            var animation = SourceInventory.Compile(root, animationInput);
            void Emit(CSharpCompilation compilation, string path)
            {
                using var stream = File.Create(Path.Combine(root, path));
                if (!compilation.Emit(stream).Success) throw new InvalidOperationException("Assembly fixture failed compilation.");
            }
            Emit(animation, "CuriousContraptions.Animation.dll");
            var mainInput = Inputs("game.cs", "GameFixture", [core, "CuriousContraptions.Animation.dll"], ["PLAYTEST"]);
            var releaseInput = mainInput with { Symbols = [] };
            var main = SourceInventory.Compile(root, mainInput, animation);
            var release = SourceInventory.Compile(root, releaseInput, animation);
            Emit(main, "GameFixture.dll");
            var testInput = Inputs("tests.cs", "TestFixture", [core, "CuriousContraptions.Animation.dll", "GameFixture.dll"], ["PLAYTEST"]);
            var releaseTestInput = testInput with { Symbols = [] };
            var tests = SourceInventory.Compile(root, testInput, animation, main);
            var releaseTests = SourceInventory.Compile(root, releaseTestInput, animation, release);
            (InspectionContext Kind, CSharpCompilation Compilation, SourceInventory.ProjectInputs Inputs)[] contexts =
            [
                (InspectionContext.AnimationRelease, animation, animationInput),
                (InspectionContext.ProductionDiagnostic, main, mainInput),
                (InspectionContext.ProductionRelease, release, releaseInput),
                (InspectionContext.TestDiagnostic, tests, testInput),
                (InspectionContext.TestRelease, releaseTests, releaseTestInput)
            ];
            var snapshot = SourceInventory.Inspect(root, contexts, []);
            if (snapshot.Diagnostics.Length != 0) throw new InvalidOperationException("Portable assembly fixture has binding errors.");
            var animationContext = snapshot.Symbols.Single(context => context.Kind == InspectionContext.AnimationRelease);
            var configHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
                File.ReadAllBytes(Path.Combine(root, "capture.props"))));
            if (snapshot.Sources.Count(source => source.Path == new SourcePath("animation.cs")) != 1 ||
                snapshot.Sources.Single(source => source.Path == new SourcePath("capture.props")).Sha256 != configHash ||
                animationContext.Assembly != new AssemblyId("CuriousContraptions.Animation") ||
                !animationContext.CompilePaths.SequenceEqual(new[] { new SourcePath("animation.cs") }))
                throw new InvalidOperationException("Animation source or configuration input identity differs.");
            var slotsId = new MemberId("F:CuriousContraptions.Presentation.AnimationBatch._slots");
            var valueId = new MemberId("P:CuriousContraptions.Presentation.AnimationBatch.Value");
            var slots = snapshot.Members.Single(member => member.Id == slotsId);
            var value = snapshot.Members.Single(member => member.Id == valueId);
            if (slots.Path != new SourcePath("animation.cs") || slots.Mutability != StorageMutability.ReferencedStorage ||
                !slots.Uses.Any(use => use.Caller == new CallerId("M:CuriousContraptions.Presentation.AnimationBatch.Advance")) ||
                !value.Uses.Any(use => use.Caller == new CallerId("M:Hint.Read(CuriousContraptions.Presentation.AnimationBatch)~System.Double")))
                throw new InvalidOperationException("Source-backed member or consumer identity was lost: " + JsonSerializer.Serialize(new { Slots = slots, Value = value }, OwnershipAudit.Json));
            if (!snapshot.Calls.Any(call => call.Caller == new CallerId("M:Hint.Step(CuriousContraptions.Presentation.AnimationBatch)") &&
                    call.Target == new CallerId("M:CuriousContraptions.Presentation.AnimationBatch.Advance") && call.Scope == DispatchScope.NamedTarget) ||
                !snapshot.Calls.Any(call => call.Caller == new CallerId("M:ConsumerTest.Read(CuriousContraptions.Presentation.AnimationBatch)~System.Double") &&
                    call.Target == new CallerId("M:Hint.Read(CuriousContraptions.Presentation.AnimationBatch)~System.Double") && call.Scope == DispatchScope.NamedTarget) ||
                !snapshot.Calls.Any(call => call.Context == InspectionContext.ProductionDiagnostic &&
                    call.Caller == new CallerId("M:Hint.DiagnosticRead(CuriousContraptions.Presentation.AnimationBatch)~System.Double") &&
                    call.Target == new CallerId("M:CuriousContraptions.Presentation.AnimationBatch.get_Value~System.Double") &&
                    call.Scope == DispatchScope.NamedTarget) ||
                snapshot.Calls.Any(call => call.Context == InspectionContext.ProductionRelease &&
                    call.Caller == new CallerId("M:Hint.DiagnosticRead(CuriousContraptions.Presentation.AnimationBatch)~System.Double")))
                throw new InvalidOperationException("Cross-assembly caller or configuration identity differs.");
            var assignments = snapshot.Members.Select(member => new OwnershipAssignment(member.Id,
                AssemblyOwner.AnimationKernel, Task, OwnershipRule.AnimationAuthority, OwnershipAudit.Fingerprint(member))).ToArray();
            var contract = new OwnershipContract(snapshot.Sources, assignments);
            var tasks = new HashSet<WorkId> { Task };
            OwnershipAudit.Validate(snapshot, contract, tasks);
            var results = new List<object>();
            foreach (var attack in Enum.GetValues<AssemblyAttack>())
            {
                var rejected = false;
                try
                {
                    var observed = snapshot;
                    var candidate = contract;
                    switch (attack)
                    {
                        case AssemblyAttack.MissingContext:
                            _ = SourceInventory.Inspect(root, contexts.Where(context => context.Kind != InspectionContext.AnimationRelease).ToArray(), []); break;
                        case AssemblyAttack.DuplicateContext:
                            _ = SourceInventory.Inspect(root, [.. contexts, contexts[0]], []); break;
                        case AssemblyAttack.UnknownContext:
                            var unknown = contexts.ToArray(); unknown[0].Kind = (InspectionContext)999;
                            _ = SourceInventory.Inspect(root, unknown, []); break;
                        case AssemblyAttack.RetiredContext:
                            var retired = contexts.ToArray(); retired[0].Kind = (InspectionContext)4;
                            _ = SourceInventory.Inspect(root, retired, []); break;
                        case AssemblyAttack.MissingReference:
                            _ = SourceInventory.Compile(root, mainInput with { References = [core] }, animation); break;
                        case AssemblyAttack.DuplicateReference:
                            _ = SourceInventory.Compile(root, mainInput with { References = [.. mainInput.References, "CuriousContraptions.Animation.dll"] }, animation); break;
                        case AssemblyAttack.MissingSource:
                            _ = SourceInventory.Compile(root, animationInput with { Paths = [] }); break;
                        case AssemblyAttack.DuplicateSource:
                            _ = SourceInventory.Compile(root, animationInput with { Paths = ["animation.cs", "animation.cs"] }); break;
                        case AssemblyAttack.BindingError:
                            var broken = contexts.ToArray();
                            broken[Array.FindIndex(broken, context => context.Kind == InspectionContext.ProductionDiagnostic)].Compilation = main.AddSyntaxTrees(CSharpSyntaxTree.ParseText("public class Broken { Undefined value; }", new CSharpParseOptions(LanguageVersion.CSharp14), path: "broken.cs"));
                            observed = SourceInventory.Inspect(root, broken, []); break;
                        case AssemblyAttack.MissingMember:
                            candidate = contract with { Assignments = assignments.Where(item => item.Member != slotsId).ToArray() }; break;
                        case AssemblyAttack.DuplicateMember:
                            candidate = contract with { Assignments = [.. assignments, assignments[0]] }; break;
                        case AssemblyAttack.StaleSource:
                            candidate = contract with { Sources = snapshot.Sources.Select(source => source.Path == new SourcePath("animation.cs")
                                ? source with { Sha256 = new string('b', 64) } : source).ToArray() }; break;
                        case AssemblyAttack.StaleConfiguration:
                            candidate = contract with { Sources = snapshot.Sources.Select(source => source.Path == new SourcePath("capture.props")
                                ? source with { Sha256 = new string('b', 64) } : source).ToArray() }; break;
                        case AssemblyAttack.StaleConsumer:
                            observed = snapshot with { Members = snapshot.Members.Select(member => member.Id == valueId
                                ? member with { Uses = [.. member.Uses, new(new("game.cs"), 99, new("M:Hint.Unreviewed"), StateAccess.Read)] } : member).ToArray() }; break;
                        case AssemblyAttack.WrongOwner:
                            candidate = contract with { Assignments = assignments.Select(item => item.Member == slotsId
                                ? item with { Owner = AssemblyOwner.SimulationCore, Rule = OwnershipRule.SimulationAuthority } : item).ToArray() }; break;
                        default: throw new InvalidOperationException("Unknown assembly oracle.");
                    }
                    OwnershipAudit.Validate(observed, candidate, tasks);
                }
                catch (InvalidDataException) { rejected = true; }
                if (!rejected) throw new InvalidOperationException("Portable assembly oracle accepted an invalid context or ownership record.");
                results.Add(new { Attack = attack, Rejected = rejected });
            }
            return new { Positive = true, Slots = slots.Id, Value = value.Id, Results = results };
        }
        finally { directory.Delete(recursive: true); }
    }
}
