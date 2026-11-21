using System;
using System.Collections.Immutable;
using System.Linq;
using Google.FlatBuffers;
using W = CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

public static partial class WorkshopWireFlatBuffers
{
    private static VectorOffset EncodeAirflowRead(FlatBufferBuilder builder, PhysicsAirflowRead values)
    {
        W.WorkshopResponse.StartAirflowVector(builder, values.Count);
        for (var i = values.Count - 1; i >= 0; i--)
        {
            var value = values[i];
            W.AirflowReadRecord.CreateAirflowReadRecord(builder, value.Id.Value, value.Owner.Value,
                (uint)value.Input, (uint)value.Availability, value.FlowSpeed, value.TransferPower,
                value.Work.Value, value.JetLoss.Value, value.CommitWork.Value, value.CommitLoss.Value);
        }
        return builder.EndVector();
    }

    private static PhysicsAirflowRead DecodeAirflowRead(W.WorkshopResponse response)
    {
        var values = new AirflowSourceRead[response.AirflowLength];
        for (var i = 0; i < values.Length; i++)
        {
            var row = response.Airflow(i)!.Value;
            values[i] = new(new(row.Id), new(row.Owner), ObservationEnum<ContinuousWorkInputKind>(row.Input),
                ObservationEnum<AirflowAvailability>(row.Availability), row.FlowSpeed, row.Power, new(row.Work), new(row.JetLoss), new(row.CommitWork), new(row.CommitLoss));
        }
        return new(values);
    }
    private static Offset<W.AirflowSourceDeclarationRecord> EncodeAirflowSource(FlatBufferBuilder b, AirflowSourceDeclaration source)
    {
        var exclusions = W.AirflowSourceDeclarationRecord.CreateInternalOccludersVector(b,
            source.InternalOccluders.Select(id => id.Value).ToArray());
        W.AirflowSourceDeclarationRecord.StartAirflowSourceDeclarationRecord(b);
        W.AirflowSourceDeclarationRecord.AddId(b, source.Id.Value);
        W.AirflowSourceDeclarationRecord.AddOwner(b, source.Owner.Value);
        W.AirflowSourceDeclarationRecord.AddFrame(b, source.Frame.Value);
        W.AirflowSourceDeclarationRecord.AddOrigin(b, W.Vec3f.CreateVec3f(b, source.Origin.X, source.Origin.Y, source.Origin.Z));
        W.AirflowSourceDeclarationRecord.AddAxis(b, W.Vec3f.CreateVec3f(b, source.Axis.X, source.Axis.Y, source.Axis.Z));
        W.AirflowSourceDeclarationRecord.AddReach(b, source.Reach);
        W.AirflowSourceDeclarationRecord.AddTerminalRadius(b, source.TerminalRadius);
        W.AirflowSourceDeclarationRecord.AddFlowSpeed(b, source.FlowSpeed);
        W.AirflowSourceDeclarationRecord.AddMaximumForce(b, source.MaximumForce);
        W.AirflowSourceDeclarationRecord.AddMaximumPower(b, source.MaximumPower);
        W.AirflowSourceDeclarationRecord.AddPressureScale(b, source.PressureScale);
        W.AirflowSourceDeclarationRecord.AddInternalOccluders(b, exclusions);
        W.AirflowSourceDeclarationRecord.AddInputKind(b, (byte)source.Input.Kind);
        W.AirflowSourceDeclarationRecord.AddStore(b, source.Input.Store.Value);
        W.AirflowSourceDeclarationRecord.AddElectrical(b, source.Input.Electrical.Value);
        W.AirflowSourceDeclarationRecord.AddCoordinate(b, source.Input.Coordinate.Value);
        W.AirflowSourceDeclarationRecord.AddSense(b, source.Input.Sense);
        W.AirflowSourceDeclarationRecord.AddConnected(b, source.Input.Connected);
        W.AirflowSourceDeclarationRecord.AddEnable(b, source.Enable?.Value ?? 0);
        W.AirflowSourceDeclarationRecord.AddInitiallyEnabled(b, source.InitiallyEnabled);
        return W.AirflowSourceDeclarationRecord.EndAirflowSourceDeclarationRecord(b);
    }

    private static AirflowSourceDeclaration DecodeAirflowSource(W.AirflowSourceDeclarationRecord row)
    {
        var origin = Required(row.Origin, "airflow origin");
        var axis = Required(row.Axis, "airflow axis");
        return new(new(row.Id), new(row.Owner), new(row.Frame), new(origin.X, origin.Y, origin.Z),
            new(axis.X, axis.Y, axis.Z), row.Reach, row.TerminalRadius, row.FlowSpeed, row.MaximumForce,
            row.MaximumPower, row.PressureScale,
            Enumerable.Range(0, row.InternalOccludersLength).Select(i => new GpuColliderId(row.InternalOccluders(i))).ToImmutableArray(),
            new(ObservationEnum<ContinuousWorkInputKind>(row.InputKind), new(row.Store), new(row.Electrical),
                new(row.Coordinate), row.Sense, row.Connected),
            row.InitiallyEnabled, row.Enable == 0 ? null : new ActivationNodeId(row.Enable));
    }

    private static Offset<W.AirflowReceiverDeclarationRecord> EncodeAirflowReceiver(FlatBufferBuilder b, AirflowReceiverDeclaration receiver)
    {
        W.AirflowReceiverDeclarationRecord.StartSamplesVector(b, receiver.Samples.Length);
        for (var i = receiver.Samples.Length - 1; i >= 0; i--)
        {
            var sample = receiver.Samples[i];
            W.AirflowSampleRecord.CreateAirflowSampleRecord(b, sample.Position.X, sample.Position.Y, sample.Position.Z, sample.Weight);
        }
        var samples = b.EndVector();
        return W.AirflowReceiverDeclarationRecord.CreateAirflowReceiverDeclarationRecord(b,
            receiver.Id.Value, receiver.Owner.Value, receiver.Collider.Value, (byte)receiver.Sampling, samples);
    }

    private static AirflowReceiverDeclaration DecodeAirflowReceiver(W.AirflowReceiverDeclarationRecord row)
    {
        var samples = ImmutableArray.CreateBuilder<AirflowSample>(row.SamplesLength);
        for (var i = 0; i < row.SamplesLength; i++)
        {
            var sample = Required(row.Samples(i), "airflow sample");
            samples.Add(new(new(sample.X, sample.Y, sample.Z), sample.Weight));
        }
        return new(new(row.Id), new(row.Owner), new(row.Collider), ObservationEnum<AirflowSampling>(row.Sampling),
            samples.MoveToImmutable());
    }
}
