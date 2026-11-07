using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Abstractions.Fluid;

namespace CuriousContraptions.Bundle;

/// <summary>The element bundle: one schema, every package's declaration, ordered by element id. It carries no timestamp, so the same
/// packages always produce the same bytes.</summary>
public sealed record BundleDocument(IReadOnlyList<BundleEntry> Elements);

/// <summary>One element's complete declaration. A domain payload is present exactly when the element declares that domain's
/// contract.</summary>
public sealed record BundleEntry(
    ElementMetadata Metadata,
    IReadOnlyList<SimulationDomain> Domains,
    ElementFootprint Footprint,
    IReadOnlyList<PortDescriptor> Ports,
    IReadOnlyList<ColliderDescriptor> Colliders,
    IReadOnlyList<EventDescriptor> Events,
    MechanicalPayload? Mechanical,
    ElectricalPayload? Electrical,
    SignalPayload? Signal,
    FluidPayload? Fluid,
    IReadOnlyList<ColliderParameterBinding> ColliderBindings,
    IReadOnlyList<TubeMouthDescriptor> TubeMouths,
    IReadOnlyList<TubeMouthParameterBinding> TubeMouthBindings,
    ElementPresentation Presentation);

/// <summary>Package-owned physical state and explicit payload participation.</summary>
public sealed record MechanicalPayload(KinematicMode KinematicMode, MassFrameDeclaration MassFrame, PayloadPolicy Payload,
    IReadOnlyList<AnchorPointDescriptor> AnchorPoints, IReadOnlyList<PrismaticSliderDescriptor> Sliders,
    IReadOnlyList<RevoluteHingeDescriptor> Hinges, CompliantSurfaceDescriptor? CompliantSurface,
    IReadOnlyList<DrivenSurfaceDescriptor> DrivenSurfaces,
    ResidenceSensorDescriptor? Residence = null, PlanarGuideDescriptor? PlanarGuide = null, ContactWorkDescriptor? ContactWork = null);

public sealed record ElectricalPayload(IReadOnlyList<ElectricalNodeDescriptor> Nodes, SupplyGraphDescriptor? SupplyGraph);

/// <summary>Independent local signal nodes and their internal and public terminal bindings.</summary>
public sealed record SignalPayload(IReadOnlyList<SignalNodeDescriptor> Nodes,
    IReadOnlyList<SignalEdge> Edges, IReadOnlyList<SignalPortBinding> Ports);

public sealed record FluidPayload(FluidProperties Properties, IReadOnlyList<FluidPortDescriptor> Ports);

/// <summary>Why an entry cannot enter, or be loaded from, the bundle. The number is the error's BUNDLE code.</summary>
public enum BundleErrorCode
{
    DuplicateId = 1,
    NonFinite = 2,
    UndefinedEnum = 3,
    BodyWithoutCollider = 4,
    DeclarationCount = 5,
    OutOfRange = 6,
    UnresolvedReference = 7,
    DuplicatePaletteOrder = 8,
    MissingAsset = 9,
    EmptyText = 10,
    /// <summary>A scene's <c>Loc_Port_</c> locator names neither a declared socket nor a tube mouth (Story 9A.17).</summary>
    UndeclaredPortLocator = 11,
    /// <summary>A declared port has no <c>Loc_Port_&lt;socket&gt;</c> locator in its element's scene.</summary>
    MissingPortLocator = 12,
    /// <summary>A port's locator is not at the declared anchor, is not a direct child of its owning body's scene root, or has no
    /// readable transform.</summary>
    PortLocatorDisagrees = 13,
    /// <summary>One element declares the same socket twice.</summary>
    DuplicatePort = 14,
    /// <summary>A Signal port without a signal kind, or a non-Signal port with one.</summary>
    PortSignalKind = 15
}

/// <summary>One validation failure, naming the entry's source (its package, or its element id in a loaded bundle) and the field.</summary>
public sealed record BundleError(BundleErrorCode Code, string Detail)
{
    public string Text => $"BUNDLE{(int)Code:000}";
    public override string ToString() => $"{Text} {Detail}";
}

/// <summary>One bundle entry and the name its errors report.</summary>
public sealed record NamedEntry(string Source, BundleEntry Entry);

public interface IBundleServices
{
    BundleEntry Entry(IPuzzleElementDeclaration declaration);
    string Write(IEnumerable<IPuzzleElementDeclaration> declarations);
    string WriteEntries(IEnumerable<BundleEntry> entries);
    BundleDocument Read(string json);
    IReadOnlyList<BundleError> Validate(IReadOnlyList<NamedEntry> entries);
    IReadOnlyDictionary<string, float> Parameters(BundleEntry entry, IReadOnlyDictionary<string, float>? supplied = null);
    IReadOnlyList<ColliderDescriptor> Colliders(BundleEntry entry, IReadOnlyDictionary<string, float>? supplied = null);
    IReadOnlyList<TubeMouthDescriptor> Mouths(BundleEntry entry, IReadOnlyDictionary<string, float>? supplied = null);
    IReadOnlyList<RunControlLane> Lanes(BundleEntry entry, string key);
    bool IsBoundToNonLane(BundleEntry entry, string key);
}
