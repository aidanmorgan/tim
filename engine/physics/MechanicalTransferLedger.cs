using System;

namespace CuriousContraptions.Physics;

/// <summary>Owned accumulated work for one stable transfer/source identity.
/// Work integrals, scalar transfer impulse (N s) and error bounds add; peak bounds take the maximum over time.
/// Impulse is conjugate to the declared port coordinate, not an unprojected body-vector impulse.</summary>
public readonly record struct MechanicalTransferTotals(MechanicalTransferId Transfer,MechanicalSourceId Source,
    ulong Intervals,WrenchPathWorkResult SourceExtraction,WrenchPathWorkResult ReceiverDelivery,WrenchPathWorkResult PairedWork,double Impulse)
{
    internal MechanicalTransferTotals Add(PredictedTransferWork work)=>Add(new MechanicalTransferTotals(work.Transfer,work.Source,1,
        work.SourceExtraction,work.ReceiverDelivery,work.PairedWork,work.Impulse));

    internal MechanicalTransferTotals Add(MechanicalTransferTotals other)
    {
        if(Transfer!=other.Transfer||Source!=other.Source)
            throw new ArgumentException("Transfer accounting identity cannot change.");
        return new(Transfer,Source,checked(Intervals+other.Intervals),
            TransferWorkArithmetic.Merge(SourceExtraction,other.SourceExtraction,WorkAccumulation.Sequential),TransferWorkArithmetic.Merge(ReceiverDelivery,other.ReceiverDelivery,WorkAccumulation.Sequential),TransferWorkArithmetic.Merge(PairedWork,other.PairedWork,WorkAccumulation.Sequential),TransferWorkArithmetic.Sum(Impulse,other.Impulse));
    }
}

internal enum WorkAccumulation { Sequential, Concurrent }

internal static class TransferWorkArithmetic
{
    internal static double Sum(double a,double b)
    {
        if(!double.IsFinite(a)||a<0||!double.IsFinite(b)||b<0||!double.IsFinite(a+b))
        throw new InvalidOperationException("Transfer accounting exceeds numeric range.");
        return a+b;
    }
    static double Peak(double a,double b)
    {
        if(!double.IsFinite(a)||a<0||!double.IsFinite(b)||b<0)
        throw new InvalidOperationException("Transfer peak accounting exceeds numeric range.");
        return Math.Max(a,b);
    }
    internal static WrenchPathWorkResult Merge(WrenchPathWorkResult a,WrenchPathWorkResult b,WorkAccumulation accumulation)=>new(
        Sum(a.Supplied,b.Supplied),Sum(a.Dissipated,b.Dissipated),
        Sum(a.SuppliedErrorBound,b.SuppliedErrorBound),Sum(a.DissipatedErrorBound,b.DissipatedErrorBound),
        CombinePeak(a.SuppliedPowerUpperBound,b.SuppliedPowerUpperBound,accumulation),
        CombinePeak(a.DissipatedPowerUpperBound,b.DissipatedPowerUpperBound,accumulation));

    private static double CombinePeak(double a,double b,WorkAccumulation accumulation)=>accumulation switch
    {
        WorkAccumulation.Sequential=>Peak(a,b),
        WorkAccumulation.Concurrent=>Sum(a,b),
        _=>throw new ArgumentOutOfRangeException(nameof(accumulation))
    };
}

/// <summary>Source extraction summed over concurrent branches before time accumulation.</summary>
public readonly record struct MechanicalSourceTotals(MechanicalSourceId Source,ulong Intervals,WrenchPathWorkResult Extraction,double Impulse)
{
    internal MechanicalSourceTotals Add(MechanicalSourceTotals other)
    {
        if(Source!=other.Source)throw new ArgumentException("Source accounting identity cannot change.");
        return new(Source,checked(Intervals+other.Intervals),
            TransferWorkArithmetic.Merge(Extraction,other.Extraction,WorkAccumulation.Sequential),TransferWorkArithmetic.Sum(Impulse,other.Impulse));
    }
    internal static MechanicalSourceTotals Interval(MechanicalSourceId source,
        System.Collections.Generic.IEnumerable<PredictedTransferWork> reports)
    {
        var extraction=default(WrenchPathWorkResult);
        var count=0;var impulse=0d;
        foreach(var report in reports)
        {
            if(report.Source!=source)throw new ArgumentException("Foreign source work report.");
            extraction=TransferWorkArithmetic.Merge(extraction,report.SourceExtraction,WorkAccumulation.Concurrent);
            impulse=TransferWorkArithmetic.Sum(impulse,report.Impulse);
            count++;
        }
        if(count==0)throw new ArgumentException("Source interval requires branch reports.");
        return new(source,1,extraction,impulse);
    }
}
