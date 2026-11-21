using System.Collections.Immutable;
using System.Linq;
using Google.FlatBuffers;
using W = CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

public static partial class WorkshopWireFlatBuffers
{
    private static Offset<W.RotaryAirflowCaptureDeclarationRecord> EncodeRotaryAirflow(
        FlatBufferBuilder b, RotaryAirflowCaptureDeclaration capture)
    {
        W.RotaryAirflowCaptureDeclarationRecord.StartSamplesVector(b, capture.Samples.Length);
        for (var i = capture.Samples.Length - 1; i >= 0; i--)
        {
            var sample = capture.Samples[i];
            W.AirflowSampleRecord.CreateAirflowSampleRecord(b, sample.Position.X, sample.Position.Y, sample.Position.Z, sample.Weight);
        }
        var samples = b.EndVector();
        var exclusions = W.RotaryAirflowCaptureDeclarationRecord.CreateInternalOccludersVector(b,
            capture.InternalOccluders.Select(id => id.Value).ToArray());
        return W.RotaryAirflowCaptureDeclarationRecord.CreateRotaryAirflowCaptureDeclarationRecord(b,
            capture.Id.Value, capture.Owner.Value, capture.Frame.Value, capture.Coordinate.Value, samples, exclusions,
            capture.MaximumPitch, capture.Response, capture.CutInForce, capture.MaximumRate);
    }

    private static RotaryAirflowCaptureDeclaration DecodeRotaryAirflow(W.RotaryAirflowCaptureDeclarationRecord row)
    {
        var samples = ImmutableArray.CreateBuilder<AirflowSample>(row.SamplesLength);
        for (var i = 0; i < row.SamplesLength; i++)
        {
            var sample = Required(row.Samples(i), "rotary airflow sample");
            samples.Add(new(new(sample.X, sample.Y, sample.Z), sample.Weight));
        }
        return new(new(row.Id), new(row.Owner), new(row.Frame), new(row.Coordinate), samples.MoveToImmutable(),
            Enumerable.Range(0, row.InternalOccludersLength).Select(i => new GpuColliderId(row.InternalOccluders(i))).ToImmutableArray(),
            row.MaximumPitch, row.Response, row.CutInForce, row.MaximumRate);
    }
}
