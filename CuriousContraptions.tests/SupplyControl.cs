using Godot;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

/// <summary>Native integration fixture built entirely from catalogue circuitry.
/// Supply changes are activation commands, never live topology edits.</summary>
internal sealed class SupplyControl
{
    private enum Role { Contact, Set, Reset }
    private readonly MachineWorld _world;
    internal LatchPart Output => Assert.IsType<LatchPart>(Find(Role.Contact));

    private static string Id(Role role)=>role switch
    {
        Role.Contact=>"supply_contact",Role.Set=>"supply_set",Role.Reset=>"supply_reset",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private MachinePart Find(Role role)=>_world.FindPart(Id(role))
        ??throw new InvalidOperationException("Supply-control construction is missing its required part.");

    private static PartSpec Spec(Role role)
    {
        var (kind,x)=role switch
        {
            Role.Contact=>("latch",0f),
            Role.Set=>("switch",-6f),
            Role.Reset=>("switch",6f),
            _=>throw new ArgumentOutOfRangeException(nameof(role))
        };
        return new(){Id=Id(role),Kind=kind,Position=[x,10,4]};
    }

    internal SupplyControl(MachineWorld world,MachinePart battery)
    {
        _world=world;
        world.AddPart(Spec(Role.Contact));
        var set=world.AddPart(Spec(Role.Set));
        var reset=world.AddPart(Spec(Role.Reset));
        Assert.True(world.Connect(battery,SocketId.Supply,Output,SocketId.PowerIn,ConnectionDomain.Electrical));
        Assert.True(world.Connect(set,SocketId.ActivationOut,Output,SocketId.SetIn,ConnectionDomain.Activation));
        Assert.True(world.Connect(reset,SocketId.ActivationOut,Output,SocketId.ResetIn,ConnectionDomain.Activation));
    }

    /// <returns>The tick at which the requested contact state first settles.</returns>
    internal int SetAndSettle(SimulationLatchPhase state)
    {
        var sender=state switch
        {
            SimulationLatchPhase.On=>Find(Role.Set),SimulationLatchPhase.Off=>Find(Role.Reset),
            _=>throw new ArgumentOutOfRangeException(nameof(state))
        };
        Assert.True(_world.Running);
        var links=_world.Connections.ToArray();
        var physics=_world.Physics;
        var effectiveTick=_world.Ticks+1;
        _world.Activate(sender);
        _world.Step();
        _world.Step();
        Assert.Equal(state,Output.State);
        Assert.Same(physics,_world.Physics);
        Assert.Equal(links,_world.Connections.ToArray());
        return effectiveTick;
    }
}
