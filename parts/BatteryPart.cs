using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum BatteryParameter { Enabled }

public partial class BatteryPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<BatteryParameter>(fields);
    public static readonly BinaryInputSlot EnableInput=new();
    public override IReadOnlyList<SceneBinaryInputDeclaration> BinaryInputs=>
        [new(new(this,EnableInput),ReadParameter(BatteryParameter.Enabled)>.5f
            ?Bridge.BinaryInputState.Enabled:Bridge.BinaryInputState.Disabled)];
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
        [new(SocketId.Supply, ConnectionDomain.Electrical, PortDirection.Output, new(0, .62f, 0))];
    public override IEnumerable<ElectricalSourceDeclaration> ElectricalSources=>
        [new(SocketId.Supply,ElectricalSourceSignal.BinaryInput(new(this,EnableInput)))];
    protected override void ValidateParameters(PartParameterValues parameters)=>RequireParameters<BatteryParameter>(parameters);
    protected override void Build()
    {
        PickRadius = .8f;
        AddBox(Vector3.Zero, new(.85f, 1.05f, .75f), Definition.Color);
        PartArt.Box(Visual, new(.87f, .25f, .77f), new("#fff8e9"), new(0, .3f, 0));
        PartArt.Cylinder(Visual, .16f, .14f, new("#f7cb52"), new(0, .59f, 0));
        PartArt.Box(Visual, new(.32f, .07f, .015f), new("#293954"), new(0, .02f, .385f));
        PartArt.Box(Visual, new(.07f, .32f, .015f), new("#293954"), new(0, .02f, .395f));
    }
}
