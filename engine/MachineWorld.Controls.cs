using System;
using System.Collections.Generic;
using CuriousContraptions.Bridge;

namespace CuriousContraptions;

public partial class MachineWorld
{
    public const int ControlCommandCapacity=128;
    private readonly SimulationCommandInbox<RuntimeControlCommand> _controlCommands=new(new(1),ControlCommandCapacity);
    private Dictionary<SceneBinaryInputKey,BinaryInputId> _binaryInputIds=new();
    private SimulationState<BinaryInputState>[] _binaryInputs=[];
    private Dictionary<SceneScalarInputKey,ScalarInputId> _scalarInputIds=new();
    private SimulationState<double>[] _scalarInputs=[];
    private SceneScalarInputDeclaration[] _scalarDeclarations=[];
    public ScalarInputId ScalarInput(SceneScalarInputKey key)=>_scalarInputIds[key];
    public double ReadScalarInput(SceneScalarInputKey key)=>_scalarInputs[ScalarInput(key).Index].Value;
    public WorldGeneration ControlGeneration=>_controlCommands.Generation;
    public SimulationRevision ControlRevision=>new(Ticks);
    public BinaryInputId BinaryInput(SceneBinaryInputKey key)=>_binaryInputIds[key];
    public BinaryInputState ReadBinaryInput(SceneBinaryInputKey key)=>_binaryInputs[BinaryInput(key).Index].Value;

    public CommandAdmission AdmitControl(CommandEnvelope<RuntimeControlCommand> command)
    {
        RequireIdle();
        if(!HasPhysicsState)throw new InvalidOperationException("Runtime controls require Run.");
        switch(command.Payload.Kind)
        {
            case ControlValueKind.Binary:
                if(!Enum.IsDefined(command.Payload.State))throw new ArgumentException("Unsupported binary input state.",nameof(command));
                break;
            case ControlValueKind.Scalar:
                if(!double.IsFinite(command.Payload.Value))throw new ArgumentException("Control value must be finite.",nameof(command));
                break;
            default:throw new ArgumentException("Unsupported control value kind.",nameof(command));
        }
        return _controlCommands.Admit(command);
    }
    public CommandSequence QueueBinaryInput(SceneBinaryInputKey key,BinaryInputState state)
    {
        RequireIdle();
        var sequence=_controlCommands.NextSequence;
        var outcome=AdmitControl(new(ControlGeneration,sequence,null,new(BinaryInput(key),state)));
        if(outcome!=CommandAdmission.Accepted)throw new InvalidOperationException("Runtime control admission rejected.");
        return sequence;
    }
    public CommandSequence QueueScalarInput(SceneScalarInputKey key,double value)
    {
        RequireIdle();
        var sequence=_controlCommands.NextSequence;
        var outcome=AdmitControl(new(ControlGeneration,sequence,null,new(ScalarInput(key),value)));
        if(outcome!=CommandAdmission.Accepted)throw new InvalidOperationException("Runtime control admission rejected.");
        return sequence;
    }
    public bool TryPeekControlResult(out CommandResult result)
    {
        RequireIdle();
        return _controlCommands.TryPeekResult(out result);
    }
    public void AcknowledgeControlResult(CommandId id)
    {
        RequireIdle();
        _controlCommands.AcknowledgeResult(id);
    }
    private void ApplyControlCommands()
    {
        while(_controlCommands.TryPeek(out var command))
        {
            var outcome=CommandOutcome.Applied;
            if(command.ExpectedRevision is { } expected&&expected!=ControlRevision)
                outcome=CommandOutcome.StaleRevision;
            else switch(command.Payload.Kind)
            {
                case ControlValueKind.Binary:
                    var binary=command.Payload.BinaryTarget.Index;
                    if(binary>=_binaryInputs.Length)outcome=CommandOutcome.UnsupportedTarget;
                    else _binaryInputs[binary].Value=command.Payload.State;
                    break;
                case ControlValueKind.Scalar:
                    var scalar=command.Payload.ScalarTarget.Index;
                    if(scalar>=_scalarInputs.Length)outcome=CommandOutcome.UnsupportedTarget;
                    else if(command.Payload.Value<_scalarDeclarations[scalar].Minimum||
                        command.Payload.Value>_scalarDeclarations[scalar].Maximum)
                        outcome=CommandOutcome.OutOfRange;
                    else _scalarInputs[scalar].Value=command.Payload.Value;
                    break;
                default:throw new InvalidOperationException("Unsupported control value kind.");
            }
            _controlCommands.Complete(outcome,new(checked(Ticks+1)));
        }
    }
}
