using System;
using System.Collections.Generic;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>One finite compliant surface, bound once to external dynamic bodies at Run.
/// The physics world owns contact phases, entry history and elastic work.</summary>
public sealed record SceneCompliantSurface(SceneBodyKey Frame,double HalfX,double HalfZ,
    double RestHeight,double MaximumStroke,double Stiffness,double DampingRatio,
    CompliantContactInitialState InitialState)
{
    internal void Validate(MachinePart owner)
    {
        if(Frame.Owner!=owner||Frame.Slot is null)
            throw new ArgumentException("Compliant surface requires its declaring owner's frame.");
        if(!Enum.IsDefined(InitialState)||!double.IsFinite(HalfX)||HalfX<=0||
            !double.IsFinite(HalfZ)||HalfZ<=0||!double.IsFinite(RestHeight)||
            !double.IsFinite(MaximumStroke)||MaximumStroke<=0||
            !double.IsFinite(Stiffness)||Stiffness<=0||!double.IsFinite(DampingRatio)||DampingRatio<0)
            throw new ArgumentException("Compliant surface parameters must describe a finite supported law.");
    }
}

internal static class SceneCompliantSurfaces
{
    internal static IReadOnlyList<CompliantContactLoad> Bind(IReadOnlyList<MachinePart> parts,ScenePhysicsAssembly assembly)
    {
        var frames=new HashSet<PhysicsBodyId>();
        var loads=new List<CompliantContactLoad>();
        foreach(var part in parts)
        foreach(var surface in part.PhysicsCompliantSurfaces)
        {
            ArgumentNullException.ThrowIfNull(surface);
            surface.Validate(part);
            var frame=assembly.Body(surface.Frame).Id;
            if(!frames.Add(frame))throw new ArgumentException("Duplicate compliant surface frame.");
            foreach(var declaration in assembly.Declarations)
            {
                var owner=declaration.Geometry.Owner;
                if(owner is null||owner.PhysicsOwner==part.PhysicsOwner)continue;
                var body=assembly.Body(new(owner,declaration.Geometry.Slot));
                if(body.MotionType!=PhysicsMotionType.Dynamic)continue;
                loads.Add(new(body.Id,frame,surface.HalfX,surface.HalfZ,surface.RestHeight,
                    surface.MaximumStroke,surface.Stiffness,surface.DampingRatio,surface.InitialState));
            }
        }
        return loads.AsReadOnly();
    }
}

