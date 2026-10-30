using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Elements.ElectricalXor;

/// <summary>CAT-027: an independent energy supply passes while exactly one condition holds.</summary>
public sealed class ElectricalXorElementDeclaration : ISupplyElement, ISceneGeometryElement
{
    public ElementMetadata Metadata => new(
        Id: "electrical_xor",
        Category: ElementCategory.Control,
        DisplayName: "Exactly one gate",
        Description: "Passes the separate bottom supply when exactly one numbered input is powered. No supply means no output. The numbered inputs are conditions, not the source of output power.",
        Color: new(.4f, .72f, .79f, 1f),
        PaletteOrder: 30,
        IconPath: "res://elements/ElectricalXor/icon.svg",
        ScenePath: "res://elements/ElectricalXor/electrical_xor.tscn",
        Parameters: []);
    public ElementFootprint Footprint => new(1, 0, 0, 0);
    public SimulationDomain SupportedDomains => SimulationDomain.Electrical;
    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new(PortSocket.FirstIn,SimulationDomain.Electrical,PortDirection.Input,null,new(-.94f,.35f,0),-Vector3.UnitX),
        new(PortSocket.SecondIn,SimulationDomain.Electrical,PortDirection.Input,null,new(-.94f,-.35f,0),-Vector3.UnitX),
        new(PortSocket.PowerIn,SimulationDomain.Electrical,PortDirection.Input,null,new(0,-.75f,.55f),Vector3.UnitZ),
        new(PortSocket.Supply,SimulationDomain.Electrical,PortDirection.Output,null,new(.94f,0,0),Vector3.UnitX)
    ];
    public IReadOnlyList<ColliderDescriptor> Colliders => [];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".",new(1f,.3f,0f,.1f))];
    public IReadOnlyList<EventDescriptor> Events => [];
    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes => [new("logic",ElectricalRole.LogicGate,12f,.05f,0f,100f)];
    public SupplyCapabilityDescriptor Supply => new(SupplyCapabilityKind.LogicGate,PortSocket.PowerIn,PortSocket.Supply,LogicGateOperation.Xor);
    public ElementPresentation Presentation => new(1.1f,PresentationAxes.None,
    [
        new("Visual/FirstLamp",PresentationProperty.AlbedoRed,PresentationDriver.CommittedElectricalSocket,85f/255f,247f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,PresentationTimerPhase.None,Socket:PortSocket.FirstIn),
        new("Visual/FirstLamp",PresentationProperty.AlbedoGreen,PresentationDriver.CommittedElectricalSocket,101f/255f,203f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,PresentationTimerPhase.None,Socket:PortSocket.FirstIn),
        new("Visual/FirstLamp",PresentationProperty.AlbedoBlue,PresentationDriver.CommittedElectricalSocket,115f/255f,82f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,PresentationTimerPhase.None,Socket:PortSocket.FirstIn),
        new("Visual/SecondLamp",PresentationProperty.AlbedoRed,PresentationDriver.CommittedElectricalSocket,85f/255f,247f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,PresentationTimerPhase.None,Socket:PortSocket.SecondIn),
        new("Visual/SecondLamp",PresentationProperty.AlbedoGreen,PresentationDriver.CommittedElectricalSocket,101f/255f,203f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,PresentationTimerPhase.None,Socket:PortSocket.SecondIn),
        new("Visual/SecondLamp",PresentationProperty.AlbedoBlue,PresentationDriver.CommittedElectricalSocket,115f/255f,82f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,PresentationTimerPhase.None,Socket:PortSocket.SecondIn),
        new("Visual/OutputLamp",PresentationProperty.AlbedoRed,PresentationDriver.CommittedElectricalSocket,85f/255f,247f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,PresentationTimerPhase.None,Socket:PortSocket.Supply),
        new("Visual/OutputLamp",PresentationProperty.AlbedoGreen,PresentationDriver.CommittedElectricalSocket,101f/255f,203f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,PresentationTimerPhase.None,Socket:PortSocket.Supply),
        new("Visual/OutputLamp",PresentationProperty.AlbedoBlue,PresentationDriver.CommittedElectricalSocket,115f/255f,82f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f,PresentationTimerPhase.None,Socket:PortSocket.Supply)
    ],[],[],[]);
}
