using Godot;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Pooled artwork for traced world-space segments; does not participate in physics.</summary>
public partial class OpticalPathVisual : Node3D
{
    /// <summary>Combine only co-directed paths sharing a physical origin, splitting at range endpoints.
    /// Pure presentation: crossing beams never acquire a new gameplay interaction.</summary>
    public static IReadOnlyList<OpticalSegment> Merge(IReadOnlyList<OpticalSegment> segments)
    {
        var result=new List<OpticalSegment>();
        foreach(var group in segments.GroupBy(s=>(s.OriginPart,s.From)))
        {
            var remaining=group.Where(s=>s.From.DistanceSquaredTo(s.To)>1e-8f).ToList();
            while(remaining.Count>0)
            {
                var first=remaining[0];
                var direction=(first.To-first.From).Normalized();
                var aligned=remaining.Where(s=>(s.To-s.From).Normalized().DistanceSquaredTo(direction)<1e-10f).ToArray();
                foreach(var segment in aligned)remaining.Remove(segment);
                var ends=aligned.Select(s=>s.From.DistanceTo(s.To)).Distinct().Order().ToArray();
                var start=0f;
                foreach(var end in ends)
                {
                    var middle=(start+end)*.5f;
                    var power=aligned.Where(s=>s.From.DistanceTo(s.To)>middle)
                        .Aggregate(Vector3.Zero,(sum,s)=>sum+s.Power);
                    result.Add(new(first.From+direction*start,first.From+direction*end,power,first.OriginPart));
                    start=end;
                }
            }
        }
        return result;
    }
    public bool Preview { get; set; }
    private readonly List<MeshInstance3D> _lines=new();
    public void Refresh(IReadOnlyList<OpticalSegment> segments)
    {
        while(_lines.Count<segments.Count)
        {
            var line=PartArt.Cylinder(this,Preview?.012f:.025f,1,new("#fff0a5"));
            line.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
            line.MaterialOverride=new StandardMaterial3D
            {
                ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
                AlbedoColor=new Color("#fff0a5") {A=Preview?.28f:.7f}
            };
            _lines.Add(line);
        }
        var inverse=GlobalTransform.AffineInverse();
        for(var i=0;i<_lines.Count;i++)
        {
            var line=_lines[i];
            line.Visible=i<segments.Count;
            if(!line.Visible)continue;
            var segment=segments[i];
            var from=inverse*segment.From;var to=inverse*segment.To;
            var offset=to-from;
            if(offset.LengthSquared()<1e-8f){line.Visible=false;continue;}
            line.Position=(from+to)*.5f;
            line.Quaternion=new Quaternion(Vector3.Up,offset.Normalized());
            line.Scale=new(1,offset.Length(),1);
            // Lost power reduces opacity; it never brightens a passive reflected path.
            var material=(StandardMaterial3D)line.MaterialOverride;
            var color=OpticalColours.BeamInk(segment.Power);
            color.A=(Preview?.28f:.7f)*Mathf.Clamp(Mathf.Max(segment.Power.X,Mathf.Max(segment.Power.Y,segment.Power.Z)),0,1);
            material.AlbedoColor=color;
        }
    }
}
