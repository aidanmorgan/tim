using System;

namespace CuriousContraptions;

/// <summary>A prepared, typed aggregate installed by one reference assignment at configuration commit.</summary>
public abstract class PartParameterState
{
    private PartParameterState() { }
    public static PartParameterState Empty { get; } = new EmptyState();
    public static PartParameterState Create<T>(T value) where T:class
    {
        ArgumentNullException.ThrowIfNull(value);
        return new State<T>(value);
    }
    public T Read<T>() where T:class => this is State<T> state
        ? state.Value : throw new InvalidOperationException("The configured state has a different type or is not initialized.");
    private sealed class EmptyState : PartParameterState { }
    private sealed class State<T>(T value) : PartParameterState where T:class
    {
        internal T Value { get; }=value;
    }
}
