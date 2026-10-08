namespace CuriousContraptions.Physics;

/// <summary>Accepted transfer-prediction residual, separate from physical work.
/// Integrals accumulate through time; peak bounds use the sequential maximum.</summary>
public readonly record struct TransferResidualTotals(ulong Intervals,WrenchPathWorkResult Work)
{
    internal TransferResidualTotals Add(TransferResidualTotals other)=>new(
        checked(Intervals+other.Intervals),TransferWorkArithmetic.Merge(Work,other.Work,WorkAccumulation.Sequential));
}
