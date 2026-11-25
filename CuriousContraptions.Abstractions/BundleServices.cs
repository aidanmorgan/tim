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
    ElementPresentation Presentation)
{
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<MaterialParameterBinding> MaterialBindings { get; init; } = [];
}

/// <summary>Package-owned physical state and explicit payload participation.</summary>
public sealed record MechanicalPayload(KinematicMode KinematicMode, MassFrameDeclaration MassFrame, PayloadPolicy Payload,
    IReadOnlyList<AnchorPointDescriptor> AnchorPoints, IReadOnlyList<PrismaticSliderDescriptor> Sliders,
    IReadOnlyList<RevoluteHingeDescriptor> Hinges, CompliantSurfaceDescriptor? CompliantSurface,
    IReadOnlyList<DrivenSurfaceDescriptor> DrivenSurfaces,
    IReadOnlyList<MechanicalTransmissionDescriptor> Transmissions,
    ResidenceSensorDescriptor? Residence = null, PlanarGuideDescriptor? PlanarGuide = null)
{
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<ClippedExtrusionDescriptor> Extrusions { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<PrismaticRetentionDescriptor> Retentions { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<CuriousContraptions.Gpu.EnergyStoreDescriptor> EnergyStores { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<ContactWorkDescriptor> ContactWorks { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<CuriousContraptions.Gpu.ChamberRegionDescriptor> ChamberRegions { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<CuriousContraptions.Gpu.FiniteWorkActuatorDescriptor> FiniteWorkActuators { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<CuriousContraptions.Gpu.GasStoreDescriptor> GasStores { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<CuriousContraptions.Gpu.GasShapeDescriptor> GasShapes { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<CuriousContraptions.Gpu.ContactIntegrityDescriptor> IntegrityObservers { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<CuriousContraptions.Gpu.AmbientLoadDescriptor> AmbientLoads { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<CuriousContraptions.Gpu.AirflowSourceDescriptor> AirflowSources { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<CuriousContraptions.Gpu.AirflowReceiverDescriptor> AirflowReceivers { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<CuriousContraptions.Gpu.RotaryAirflowCaptureDescriptor> RotaryAirflowCaptures { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public IReadOnlyList<CuriousContraptions.Gpu.CoordinateTravelDescriptor> CoordinateTravelObservers { get; init; } = [];
}

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
    /// <summary>A scene's <c>Loc_Port_</c> locator names neither a declared socket nor a tube mouth (Story 9A.17), or a locator or tube
    /// mouth is stated twice.</summary>
    UndeclaredPortLocator = 11,
    /// <summary>A declared port has no <c>Loc_Port_&lt;socket&gt;</c> locator in its element's scene.</summary>
    MissingPortLocator = 12,
    /// <summary>A port's locator is not at the declared anchor or is not a direct child of its owning body's scene root, or a
    /// <c>Loc_Port_</c> node is a scene root. A locator pose the scene parser cannot read is refused under the parser's code, naming the
    /// locator: <see cref="MalformedScene"/> for unreadable text or a transform that cannot be decomposed, <see cref="OutOfRange"/> for a
    /// nonpositive or mirrored scale, and <see cref="NonFinite"/> for a non-finite number.</summary>
    PortLocatorDisagrees = 13,
    /// <summary>One element declares the same socket twice.</summary>
    DuplicatePort = 14,
    /// <summary>A Signal port without a signal kind, or a non-Signal port with one.</summary>
    PortSignalKind = 15,
    /// <summary>A scene node whose name carries a <c>-col_</c> infix without the <c>col_</c> prefix; the infix never names a collider
    /// proxy.</summary>
    ProxyNameInfix = 16,
    /// <summary>Scene syntax the build cannot read: a malformed section, value, number or quaternion; a line that is neither blank, a
    /// comment, a section header nor a one-line <c>key = value</c>, or a line before the first section; a section attribute or value
    /// stated twice; a sub-resource without its id or type; a node without a valid name; a root that is not a Node3D; a transform that
    /// cannot be decomposed; a cylinder cap that is neither true nor false; or a tube mouth without its bore.</summary>
    MalformedScene = 17,
    /// <summary>A collider proxy the build cannot take as authored: an unsupported node or shape kind (including <c>col_capsule</c> and
    /// <c>col_hull</c>), a shape that is not a local sub-resource, a shape value left to its default, a nonuniform scale the shape cannot
    /// take, an open cylinder without both caps open or without its wall, or a tapered cylinder mesh.</summary>
    UnsupportedColliderProxy = 18,
    /// <summary>Inconsistent collider ownership. A package's colliders are its scene's proxies, so this covers: a declaration without
    /// scene bodies (<c>ISceneGeometryElement</c>); a declaration that states data colliders on any body; an ownership that does not
    /// name the base and every moving body; or overlapping body roots.</summary>
    SceneColliderOwnership = 19
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
    IReadOnlyList<ColliderDescriptor> BodyColliders(BundleEntry entry, PackageBodyIndex body, IReadOnlyDictionary<string, float>? supplied = null);
    IReadOnlyList<TubeMouthDescriptor> Mouths(BundleEntry entry, IReadOnlyDictionary<string, float>? supplied = null);
    IReadOnlyList<RunControlLane> Lanes(BundleEntry entry, string key);
    bool IsBoundToNonLane(BundleEntry entry, string key);
}
