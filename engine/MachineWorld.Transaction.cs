using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

public partial class MachineWorld
{
    /// <summary>World-owned tick outputs only. Part state and scene side effects require
    /// their own enrollment before the complete gameplay tick is transactional.</summary>
    private sealed class TickObservablesCheckpoint(MachineWorld owner) : SimulationTransactionParticipant
    {
        private readonly List<KeyValuePair<MachineEvent,int>> _events=new();
        private readonly List<PhysicsImpact> _impacts=new();
        private OpticalSegment[] _paths=[];
        private PhysicsStepResult _lastStep=null!;
        private int _ticks;
        private bool _running,_won;
        protected override void CaptureCheckpoint()
        {
            _events.Clear();
            foreach(var entry in owner.Events)_events.Add(entry);
            _impacts.Clear();_impacts.AddRange(owner._tickImpacts);
            _paths=owner.OpticalPaths.ToArray();
            _lastStep=owner.LastPhysicsStep;
            _ticks=owner.Ticks;_running=owner.Running;_won=owner.Won;
        }
        protected override void RestoreCheckpoint()
        {
            owner.Events.Clear();
            foreach(var entry in _events)owner.Events.Add(entry.Key,entry.Value);
            owner._tickImpacts.Clear();owner._tickImpacts.AddRange(_impacts);
            owner.OpticalPaths=Array.AsReadOnly(_paths);
            owner.LastPhysicsStep=_lastStep;
            owner.Ticks=_ticks;owner.Running=_running;owner.Won=_won;
            // Physical poses were never submitted from this uncommitted tick.
        }
    }
}
