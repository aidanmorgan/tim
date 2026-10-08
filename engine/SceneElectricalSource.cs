using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Bridge;

namespace CuriousContraptions;

public enum ElectricalSourceKind { BinaryInput, Condition, ScalarThreshold }

/// <summary>Binary availability only; this declaration does not model electrical energy.</summary>
public readonly record struct ElectricalSourceSignal
{
    public ElectricalSourceKind Kind { get; }
    public SceneBinaryInputKey? Input { get; private init; }
    public ElectricalContactSignal Condition { get; private init; }
    public SimulationState<float>? Scalar { get; private init; }
    public float Threshold { get; private init; }
    private ElectricalSourceSignal(ElectricalSourceKind kind)=>Kind=kind;
    public static ElectricalSourceSignal BinaryInput(SceneBinaryInputKey input)
    {
        if(input.Owner is null||input.Slot is null)throw new ArgumentException("Source input requires a complete key.");
        return new(ElectricalSourceKind.BinaryInput){Input=input};
    }
    public static ElectricalSourceSignal When(ElectricalContactSignal condition)=>
        new(ElectricalSourceKind.Condition){Condition=condition};
    public static ElectricalSourceSignal AtLeast(SimulationState<float> value,float threshold)
    {
        ArgumentNullException.ThrowIfNull(value);
        if(!float.IsFinite(threshold))throw new ArgumentOutOfRangeException(nameof(threshold));
        return new(ElectricalSourceKind.ScalarThreshold){Scalar=value,Threshold=threshold};
    }
}
public readonly record struct ElectricalSourceDeclaration(SocketId Output,ElectricalSourceSignal Signal);

/// <summary>Construction captures declared initial controls; Run replaces them with owned input cells.</summary>
internal sealed class ElectricalSourceBinding
{
    private readonly ElectricalSourceSignal _signal;
    private readonly SimulationState<BinaryInputState>? _input;
    private readonly ElectricalContactBinding? _condition;
    public ElectricalSourceBinding(MachinePart owner,ElectricalSourceSignal signal)
    {
        ArgumentNullException.ThrowIfNull(owner);_signal=signal;
        switch(signal.Kind)
        {
            case ElectricalSourceKind.BinaryInput:
                if(signal.Input is not { } key||key.Owner!=owner||key.Slot is null)
                    throw new ArgumentException("Source input must belong to its owner.");
                var declarations=owner.BinaryInputs.Where(d=>d.Key==key).ToArray();
                if(declarations.Length!=1||!Enum.IsDefined(declarations[0].Initial))
                    throw new ArgumentException("Source requires exactly one valid input declaration.");
                _input=new(declarations[0].Initial);break;
            case ElectricalSourceKind.Condition:
                _condition=new(owner,signal.Condition);break;
            case ElectricalSourceKind.ScalarThreshold:
                if(signal.Scalar is null||!owner.RuntimeState.Any(s=>ReferenceEquals(s,signal.Scalar))||
                    !float.IsFinite(signal.Threshold))
                    throw new ArgumentException("Source threshold requires an owned checkpoint cell.");
                break;
            default:throw new ArgumentException("Unsupported electrical source condition.");
        }
    }
    private ElectricalSourceBinding(ElectricalSourceBinding declaration,ElectricalRuntime runtime)
    {
        _signal=declaration._signal;
        switch(_signal.Kind)
        {
            case ElectricalSourceKind.BinaryInput:
                if(!runtime.BinaryInputs.TryGetValue(_signal.Input!.Value,out var input)||input is null)
                    throw new ArgumentException("Runtime source input binding is absent.");
                _input=input;_ = Read();break;
            case ElectricalSourceKind.Condition:
                _condition=declaration._condition!.Bind(runtime);break;
            case ElectricalSourceKind.ScalarThreshold:break;
            default:throw new ArgumentException("Unsupported electrical source condition.");
        }
    }
    public ElectricalSourceBinding Bind(ElectricalRuntime runtime)=>new(this,runtime);
    public bool Read()
    {
        switch(_signal.Kind)
        {
            case ElectricalSourceKind.BinaryInput:
                return _input!.Value switch
                {
                    BinaryInputState.Enabled=>true,BinaryInputState.Disabled=>false,
                    _=>throw new InvalidOperationException("Unsupported source input state.")
                };
            case ElectricalSourceKind.Condition:return _condition!.Read();
            case ElectricalSourceKind.ScalarThreshold:
                var value=_signal.Scalar!.Value;
                if(!float.IsFinite(value))throw new InvalidOperationException("Source reading must be finite.");
                return value>=_signal.Threshold;
            default:throw new InvalidOperationException("Unsupported electrical source condition.");
        }
    }
}
