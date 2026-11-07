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
        new(PortId.FirstIn,SimulationDomain.Electrical,PortDirection.Input,PortRole.FirstIn,null,new(-.94f,.35f,0),-Vector3.UnitX),
        new(PortId.SecondIn,SimulationDomain.Electrical,PortDirection.Input,PortRole.SecondIn,null,new(-.94f,-.35f,0),-Vector3.UnitX),
        new(PortId.PowerIn,SimulationDomain.Electrical,PortDirection.Input,PortRole.PowerIn,null,new(0,-.75f,.55f),Vector3.UnitZ),
        new(PortId.Supply,SimulationDomain.Electrical,PortDirection.Output,PortRole.Supply,null,new(.94f,0,0),Vector3.UnitX)
    ];
    public IReadOnlyList<ColliderDescriptor> Colliders => [];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".",new(1f,.3f,0f,.1f))];
    public IReadOnlyList<EventDescriptor> Events => [];
    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes => [new("logic",ElectricalRole.LogicGate,12f,.05f,0f,100f)];
    public SupplyGraphDescriptor SupplyGraph => new(
        [new SupplyLogicDescriptor(new("logic"), LogicGateOperation.Xor)], [],
        [
            new(PortId.PowerIn, new(new("logic"), SupplyTerminal.PowerIn)),
            new(PortId.FirstIn, new(new("logic"), SupplyTerminal.FirstIn)),
            new(PortId.SecondIn, new(new("logic"), SupplyTerminal.SecondIn)),
            new(PortId.Supply, new(new("logic"), SupplyTerminal.Output))
        ]);
    public ElementPresentation Presentation => new(1.1f,PresentationAxes.None,
    [
        new("Visual/FirstLamp",PresentationProperty.AlbedoRed,PresentationDriver.CommittedElectricalSocket, PresentationValueMapping.Identity,85f/255f,247f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f, Socket:PortId.FirstIn),
        new("Visual/FirstLamp",PresentationProperty.AlbedoGreen,PresentationDriver.CommittedElectricalSocket, PresentationValueMapping.Identity,101f/255f,203f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f, Socket:PortId.FirstIn),
        new("Visual/FirstLamp",PresentationProperty.AlbedoBlue,PresentationDriver.CommittedElectricalSocket, PresentationValueMapping.Identity,115f/255f,82f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f, Socket:PortId.FirstIn),
        new("Visual/SecondLamp",PresentationProperty.AlbedoRed,PresentationDriver.CommittedElectricalSocket, PresentationValueMapping.Identity,85f/255f,247f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f, Socket:PortId.SecondIn),
        new("Visual/SecondLamp",PresentationProperty.AlbedoGreen,PresentationDriver.CommittedElectricalSocket, PresentationValueMapping.Identity,101f/255f,203f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f, Socket:PortId.SecondIn),
        new("Visual/SecondLamp",PresentationProperty.AlbedoBlue,PresentationDriver.CommittedElectricalSocket, PresentationValueMapping.Identity,115f/255f,82f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f, Socket:PortId.SecondIn),
        new("Visual/OutputLamp",PresentationProperty.AlbedoRed,PresentationDriver.CommittedElectricalSocket, PresentationValueMapping.Identity,85f/255f,247f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f, Socket:PortId.Supply),
        new("Visual/OutputLamp",PresentationProperty.AlbedoGreen,PresentationDriver.CommittedElectricalSocket, PresentationValueMapping.Identity,101f/255f,203f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f, Socket:PortId.Supply),
        new("Visual/OutputLamp",PresentationProperty.AlbedoBlue,PresentationDriver.CommittedElectricalSocket, PresentationValueMapping.Identity,115f/255f,82f/255f,PresentationCurve.SmoothStep,.1f,PresentationOverlap.Replace,0f, Socket:PortId.Supply)
    ],[],[],[]);
}
