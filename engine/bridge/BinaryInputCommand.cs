using System;

namespace CuriousContraptions.Bridge;

public enum BinaryInputState { Disabled, Enabled }
public readonly record struct BinaryInputId
{
    public int Index { get; }
    public BinaryInputId(int index)
    { if(index<0)throw new ArgumentOutOfRangeException(nameof(index));Index=index; }
}
public readonly record struct ScalarInputId
{
    public int Index { get; }
    public ScalarInputId(int index)
    { if(index<0)throw new ArgumentOutOfRangeException(nameof(index));Index=index; }
}
public enum ControlValueKind { Binary, Scalar }
public readonly record struct RuntimeControlCommand
{
    public ControlValueKind Kind { get; }
    public BinaryInputId BinaryTarget { get; }
    public BinaryInputState State { get; }
    public ScalarInputId ScalarTarget { get; }
    public double Value { get; }
    public RuntimeControlCommand(BinaryInputId target,BinaryInputState state)
    { Kind=ControlValueKind.Binary;BinaryTarget=target;State=state; }
    public RuntimeControlCommand(ScalarInputId target,double value)
    { Kind=ControlValueKind.Scalar;ScalarTarget=target;Value=value; }
}
