using System;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class WorkshopWireFlatBuffersTests
{
    private static readonly RuntimeSessionId Session = new(0x1122334455667788UL, 0x8877665544332211UL);

    [Fact]
    public void Command_SimpleRunAndReset_RoundTrips()
    {
        var run = new WorkshopCommand(new(1), WorkshopCommandKind.Run, new(10), default, null,
            RevisionKind: ExpectedRevisionKind.Any, Session: Session, Cadence: new(2), Projection: new(3));
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(run);
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(encoded);
        Assert.Equal(run.Sequence, decoded.Sequence);
        Assert.Equal(run.Kind, decoded.Kind);
        Assert.Equal(run.Epoch, decoded.Epoch);
        Assert.Equal(run.Revision, decoded.Revision);
        Assert.Equal(run.RevisionKind, decoded.RevisionKind);
        Assert.Equal(run.Session, decoded.Session);
        Assert.Equal(run.Cadence, decoded.Cadence);
        Assert.Equal(run.Projection, decoded.Projection);
        Assert.Null(decoded.Construction);
        Assert.Null(decoded.Settings);
        Assert.Null(decoded.Target);
        Assert.Null(decoded.Electrical);
    }

    [Fact]
    public void Command_InitializeWithSettings_RoundTrips()
    {
        var settings = WorkshopCadenceSettings.Default();
        var init = new WorkshopCommand(new(1), WorkshopCommandKind.Initialize, new(1), new(1), null,
            Session: Session, Settings: settings);
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(init);
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(encoded);
        Assert.Equal(init.Sequence, decoded.Sequence);
        Assert.Equal(init.Kind, decoded.Kind);
        Assert.Equal(settings, decoded.Settings);
    }

    [Fact]
    public void Command_ConfigureCadence_RoundTrips()
    {
        var settings = new WorkshopCadenceSettings(
            SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz,
            AnimationCadence.Hz30, PresentationCadence.Hz60, new(60, 1));
        var cmd = new WorkshopCommand(new(2), WorkshopCommandKind.ConfigureCadence, new(1), new(5), null,
            Session: Session, Cadence: new(1), Projection: new(1), Settings: settings);
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(cmd);
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(encoded);
        Assert.Equal(cmd.Sequence, decoded.Sequence);
        Assert.Equal(cmd.Kind, decoded.Kind);
        Assert.Equal(settings, decoded.Settings);
    }

    [Fact]
    public void Command_ConfigureElectrical_RoundTrips()
    {
        var electrical = new WorkshopElectricalControl(new(42), ElectricalEnable.Enabled);
        var cmd = new WorkshopCommand(new(3), WorkshopCommandKind.ConfigureElectrical, new(1), default, null,
            RevisionKind: ExpectedRevisionKind.Any, Session: Session, Cadence: new(1), Projection: new(1),
            Electrical: electrical);
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(cmd);
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(encoded);
        Assert.Equal(cmd.Sequence, decoded.Sequence);
        Assert.Equal(cmd.Kind, decoded.Kind);
        Assert.Equal(electrical, decoded.Electrical);
    }

    [Fact]
    public void Command_CancelWithTarget_RoundTrips()
    {
        var target = new WorkshopCommandIdentity(new(5), new(1), new(2), new(3), new(4));
        var cmd = new WorkshopCommand(new(6), WorkshopCommandKind.Cancel, new(1), default, null,
            RevisionKind: ExpectedRevisionKind.Any, Session: Session, Cadence: new(1), Projection: new(1),
            Target: target);
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(cmd);
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(encoded);
        Assert.Equal(cmd.Sequence, decoded.Sequence);
        Assert.Equal(cmd.Kind, decoded.Kind);
        Assert.Equal(target, decoded.Target);
    }

    [Fact]
    public void Command_ConstructWithInstances_RoundTrips()
    {
        var ball = WorkshopInput.Basketball(new(1), 0, 3, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([ball]));
        var cmd = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), default, construction,
            Session: Session, Cadence: new(1), Projection: new(1));
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(cmd);
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(encoded);
        Assert.Equal(cmd.Sequence, decoded.Sequence);
        Assert.Equal(cmd.Kind, decoded.Kind);
        Assert.NotNull(decoded.Construction);
        Assert.Equal(construction.Revision, decoded.Construction!.Value.Revision);
        Assert.Equal(construction.Instances.Count, decoded.Construction!.Value.Instances.Count);
        Assert.Equal(ball.Id, decoded.Construction!.Value.Instances[0].Id);
    }

    [Fact]
    public void Command_InvalidInputs_Throw()
    {
        var valid = new WorkshopCommand(new(1), WorkshopCommandKind.Run, new(1), default, null,
            RevisionKind: ExpectedRevisionKind.Any, Session: Session, Cadence: new(1), Projection: new(1));
        // Sequence 0
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeCommand(valid with { Sequence = default }));
        // Initialize with cadence != 0
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeCommand(
            new WorkshopCommand(new(1), WorkshopCommandKind.Initialize, new(1), new(1), null,
                Session: Session, Cadence: new(1), Settings: WorkshopCadenceSettings.Default())));
        // Decode corrupt bytes
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.DecodeCommand(new byte[16]));
    }

    [Fact]
    public void Response_Acknowledgement_RoundTrips()
    {
        var ack = new WorkshopResponse(new(10), WorkshopResponseKind.Acknowledgement,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Building,
            default,
            Session: Session);
        var encoded = WorkshopWireFlatBuffers.EncodeResponse(ack);
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(encoded);
        Assert.Equal(ack.Sequence, decoded.Sequence);
        Assert.Equal(ack.Kind, decoded.Kind);
        Assert.Equal(ack.Result, decoded.Result);
        Assert.Equal(ack.Phase, decoded.Phase);
        Assert.Equal(ack.Session, decoded.Session);
    }

    [Fact]
    public void Response_PhysicalRead_RoundTrips()
    {
        var master = new ClockGeneration(1);
        var stamp = new WorkshopClockStamp(WorkshopClockDomain.SimulationMonotonic, master, new(1_000_000), new(100));
        var canonical = new CanonicalBody(new(1), 1, 1, default, default, default);
        var body = new PhysicsBodyRead(canonical, CanonicalRotation.Identity, default, default);
        var bodies = new PhysicsBodyReadSet(new[] { body });

        var capture = CaptureLatch.Clear(new(9));
        var captures = new PhysicsCaptureRead(new[] { capture });

        var work = new ContactWorkRead(new(40), new(1), 1, new(16f), new(4f));
        var occ = new ContactWorkOccurrence(
            new(0), new(1), new(1), 1, 8, (Half)0.5f,
            new ContactSpeed(4f), new Joules(8f), ContactWorkEffect.Paid);
        var contactWorks = new PhysicsContactWorkRead(new[] { work }, new[] { occ });

        var elec = new ElectricalSourceRead(new(50), new(1), new Joules(100f), new Joules(5f), ElectricalEnable.Enabled);
        var electrical = new PhysicsElectricalRead(new[] { elec });

        var read = new WorkshopRead(new(1), new(1), bodies, new(1), stamp, captures, null,
            ContactWorks: contactWorks, Electrical: electrical);

        var response = new WorkshopResponse(default, WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Running,
            read,
            Session: Session,
            Publication: new(1),
            Cadence: new(1),
            MasterGeneration: master,
            Projection: new(1),
            SourcePulse: new(1));

        var encoded = WorkshopWireFlatBuffers.EncodeResponse(response);
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(encoded);

        Assert.Equal(response.Sequence, decoded.Sequence);
        Assert.Equal(response.Kind, decoded.Kind);
        Assert.Equal(response.Result, decoded.Result);
        Assert.Equal(response.Phase, decoded.Phase);
        Assert.Equal(response.Session, decoded.Session);
        Assert.Equal(response.Publication, decoded.Publication);
        Assert.Equal(response.Cadence, decoded.Cadence);
        Assert.Equal(response.MasterGeneration, decoded.MasterGeneration);
        Assert.Equal(response.Projection, decoded.Projection);

        var decodedRead = decoded.Read;
        Assert.Equal(read.Epoch, decodedRead.Epoch);
        Assert.Equal(read.Tick, decodedRead.Tick);
        Assert.Equal(read.Revision, decodedRead.Revision);
        Assert.Equal(read.Capture, decodedRead.Capture);

        Assert.Equal(1, decodedRead.Bodies.Count);
        Assert.Equal(body.Body.Id, decodedRead.Bodies[0].Body.Id);

        Assert.Equal(1, decodedRead.Captures.Count);
        Assert.Equal(capture.Sensor, decodedRead.Captures[0].Sensor);

        Assert.Equal(1, decodedRead.ContactWorks.Count);
        Assert.Equal(work.RemainingEnergy, decodedRead.ContactWorks[0].RemainingEnergy);
        Assert.Equal(1, decodedRead.ContactWorks.OccurrenceCount);
        Assert.Equal(occ.Debit, decodedRead.ContactWorks.Occurrence(0).Debit);

        Assert.Equal(1, decodedRead.Electrical.Count);
        Assert.Equal(elec.Remaining, decodedRead.Electrical[0].Remaining);
        Assert.Equal(elec.Debit, decodedRead.Electrical[0].Debit);
    }
}
