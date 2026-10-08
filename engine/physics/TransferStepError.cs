using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

/// <summary>One complete Step's transfer error allowance, shared by all branches
/// and accepted subintervals. It is not a certificate for other physics families.</summary>
public readonly record struct TransferStepError
{
    public double Limit { get; }
    public double UpperBound { get; }
    internal TransferStepError(double limit,double upperBound=0)
    {
        if(!double.IsFinite(limit)||limit<0||!double.IsFinite(upperBound)||upperBound<0)
            throw new ArgumentOutOfRangeException(nameof(limit));
        Limit=limit;UpperBound=upperBound;
    }
    internal TransferStepError Add(IReadOnlyList<PredictedTransferWork> reports,WrenchPathWorkResult? residual)
    {
        ArgumentNullException.ThrowIfNull(reports);
        if(reports.Count==0)
        {
            if(residual is not null)throw new ArgumentException("Residual requires transfer reports.");
            return this;
        }
        if(residual is null)throw new ArgumentException("Transfer reports require their residual certificate.");
        var total=UpperBound;
        void Include(double value)
        {
            if(!double.IsFinite(value)||value<0)throw new ArgumentException("Transfer error must be finite and nonnegative.");
            if(value==0)return;
            total=total==0?value:Math.BitIncrement(total+value);
            if(!double.IsFinite(total))throw new InvalidOperationException("Transfer error accumulation exceeds numeric range.");
        }
        void Errors(WrenchPathWorkResult work)
        {
            Include(work.SuppliedErrorBound);Include(work.DissipatedErrorBound);
        }
        foreach(var report in reports)
        {
            Include(report.PairedWork.Supplied);
            Errors(report.SourceExtraction);Errors(report.ReceiverDelivery);Errors(report.PairedWork);
        }
        Include(residual.Value.Supplied);Include(residual.Value.Dissipated);Errors(residual.Value);
        var result=new TransferStepError(Limit,total);
        if(total>Limit)throw new TransferStepErrorException(result);
        return result;
    }
}
public sealed class TransferStepErrorException : InvalidOperationException
{
    public TransferStepError Error { get; }
    internal TransferStepErrorException(TransferStepError error):base("Accepted transfer intervals exceed the complete step's work-error budget.")
    {
        Error=error;
    }
}
