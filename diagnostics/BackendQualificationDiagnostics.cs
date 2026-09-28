using System.Diagnostics;
#if PLAYTEST
using Godot;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace CuriousContraptions;

public partial class Workshop
{
    [Conditional("PLAYTEST")]
    private void StartBackendQualification()
    {
#if PLAYTEST
        foreach(var probe in Enum.GetValues<GeneralCollisionProbe>())
            GD.Print(BackendQualificationProtocol.GeneralPrefix+JsonSerializer.Serialize(GeneralCollisionQualification.Run(probe),BackendQualificationJson.Default.GeneralCollisionReport));
        foreach(var probe in Enum.GetValues<PhysicsBackendQualification.Probe>())
        {
            var observed=PhysicsBackendQualification.Run(probe);
            var report=new BackendQueryReport(observed.Probe,observed.Collided,observed.SafeFraction,
                observed.Contacts,[observed.Normal.X,observed.Normal.Y,observed.Normal.Z],observed.SpaceAvailable);
            GD.Print(BackendQualificationProtocol.QueryPrefix+JsonSerializer.Serialize(report,BackendQualificationJson.Default.BackendQueryReport));
        }
        foreach(var experiment in Enum.GetValues<PhysicsBackendMotionProbe.Experiment>())
            AddChild(new PhysicsBackendMotionProbe
            {
                Case=experiment,
                Finished=probe=>
                {
                    var report=new BackendMotionReport(probe.Case,probe.Frames.ToArray());
                    GD.Print(BackendQualificationProtocol.MotionPrefix+JsonSerializer.Serialize(report,BackendQualificationJson.Default.BackendMotionReport));
                    probe.QueueFree();
                }
            });
#endif
    }
}

#if PLAYTEST
internal static class BackendQualificationProtocol
{
    // External diagnostic wire identifiers, never domain behaviour selectors.
    internal const string GeneralPrefix="CCGENERALCCD ";
    internal const string QueryPrefix="CCBACKENDQUERY ";
    internal const string MotionPrefix="CCBACKENDMOTION ";
}
internal sealed record BackendQueryReport(PhysicsBackendQualification.Probe Probe,bool Collided,
    float SafeFraction,int Contacts,float[] Normal,bool SpaceAvailable);
internal sealed record BackendMotionReport(PhysicsBackendMotionProbe.Experiment Experiment,
    PhysicsBackendMotionProbe.Frame[] Frames);
[JsonSourceGenerationOptions(UseStringEnumConverter=true)]
[JsonSerializable(typeof(GeneralCollisionReport))]
[JsonSerializable(typeof(BackendQueryReport))]
[JsonSerializable(typeof(BackendMotionReport))]
internal partial class BackendQualificationJson : JsonSerializerContext { }
#endif
