using System.Collections.Generic;

namespace CuriousContraptions;

public partial class MachinePart
{
    private BaseStateCheckpoint? _baseStateCheckpoint;
    internal SimulationTransactionParticipant BaseRuntimeCheckpoint=>_baseStateCheckpoint??=new(this);
    /// <summary>Additional owned runtime stores, registered once when Run captures the assembly.
    /// The host always registers base state separately, including internal bodies.</summary>
    public virtual IReadOnlyList<SimulationTransactionParticipant> RuntimeState=>[];

    private sealed class BaseStateCheckpoint(MachinePart owner) : SimulationTransactionParticipant
    {
        private readonly List<SocketId> _powered=new();
        private bool _active,_visible;
        protected override void CaptureCheckpoint()
        {
            _active=owner.Active;_visible=owner.Visible;
            _powered.Clear();
            foreach(var port in owner._poweredInputs)_powered.Add(port);
        }
        protected override void RestoreCheckpoint()
        {
            owner.Active=_active;owner.Visible=_visible;
            owner._poweredInputs.Clear();
            foreach(var port in _powered)owner._poweredInputs.Add(port);
        }
    }
}
