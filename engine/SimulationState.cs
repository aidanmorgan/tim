namespace CuriousContraptions;

/// <summary>A renderer-independent checkpoint cell for typed, reference-free runtime state.
/// Reference-free payloads cannot hide mutable aliases outside the checkpoint.
/// Domain owners validate values; transaction composition owns checkpoint lifetime.</summary>
public sealed class SimulationState<T>(T initial) : SimulationTransactionParticipant where T : unmanaged
{
    private T _checkpoint;
    public T Value { get; set; }=initial;
    protected override void CaptureCheckpoint()=>_checkpoint=Value;
    protected override void RestoreCheckpoint()=>Value=_checkpoint;
}
