using System.Text.Json.Serialization;
namespace Ownership;

public enum AssemblyOwner
{
    Geometry, SimulationCore, ConstructionCompiler, Protocol,
    SimulationHost, AnimationKernel, AnimationHost, GodotPresenter
}
public enum OwnershipRule
{
    SimulationAuthority, AnimationAuthority, SceneResource, SceneCapture,
    ImmutableContract, InvocationScratch, CompilerState, HostState
}
public enum StorageForm { Field, Property, Event, RecordProperty, PrimaryCapture }
public enum StateAccess { Read, Write, ReadWrite, ReferenceEscape, Call, Potential }
public enum StorageMutability { MutableValue, ReferencedStorage, ConstructionValue }
public readonly record struct SourcePath(string Value);
public readonly record struct MemberId(string Value);
public readonly record struct CallerId(string Value);
public readonly record struct TypeId(string Value);
public readonly record struct WorkId(string Value);
public sealed record SourceInput(SourcePath Path, string Sha256);
public sealed record UseSite(SourcePath Path, int Line, CallerId Caller, StateAccess Access);
public sealed record StateMember(MemberId Id, SourcePath Path, int Line, TypeId Type,
    StorageForm Form, StorageMutability Mutability, bool Static, UseSite[] Uses);
public sealed record OwnershipAssignment(
    [property: JsonRequired] MemberId Member,
    [property: JsonRequired] AssemblyOwner Owner,
    [property: JsonRequired] WorkId Migration,
    [property: JsonRequired] OwnershipRule Rule,
    [property: JsonRequired] string MemberSha256);
public readonly record struct DiagnosticId(string Value);
// Numeric values are the external capture boundary; retired value 4 is unsupported.
public enum InspectionContext { ProductionRelease = 0, ProductionDiagnostic = 1, TestRelease = 2, TestDiagnostic = 3, AnimationRelease = 5 }
public enum DispatchScope { NamedTarget, VirtualFamily, InterfaceFamily, ContextDelegates, External }
public sealed record CallerEdge(InspectionContext Context, SourcePath Path, int Line,
    CallerId Caller, CallerId Target, DispatchScope Scope);
public readonly record struct AssemblyId(string Value);
public sealed record CompilationContext(InspectionContext Kind, string[] Symbols, AssemblyId Assembly,
    Microsoft.CodeAnalysis.CSharp.LanguageVersion Language, Microsoft.CodeAnalysis.OutputKind Output,
    Microsoft.CodeAnalysis.NullableContextOptions Nullable, bool Unsafe, bool Checked,
    SourcePath[] CompilePaths, SourcePath[] GeneratedPaths, SourcePath[] ReferencePaths);
public sealed record BindingDiagnostic(InspectionContext Context, DiagnosticId Id, SourcePath Path, int Line, string Message);
public sealed record OwnershipSnapshot(SourceInput[] Sources, StateMember[] Members)
{
    public BindingDiagnostic[] Diagnostics { get; init; } = [];
    public SourceInput[] References { get; init; } = [];
    public CompilationContext[] Symbols { get; init; } = [];
    public CallerEdge[] Calls { get; init; } = [];
    public CallerId[] ConservativeContextCallers { get; init; } = [];
}
public sealed record OwnershipIndex([property: JsonRequired] SourcePath[] Files);
public sealed record OwnershipContract(
    [property: JsonRequired] SourceInput[] Sources,
    [property: JsonRequired] OwnershipAssignment[] Assignments);
