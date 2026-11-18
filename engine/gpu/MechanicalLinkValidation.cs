using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

public static class MechanicalLinkValidation
{
    public static void Validate(IEnumerable<CompiledMechanicalLink> links, IPhysicsSceneDeclaration physics,
        IEnumerable<CompiledPresentationTrack> tracks, PackageBodyMembership membership)
    {
        var targets = tracks.Select(track => track.Target).ToHashSet();
        foreach (var link in links)
        {
            if (link.Target.Value < MechanicalLinkGeometry.TargetBase || !targets.Add(link.Target))
                throw new ArgumentException("Invalid mechanical presentation target.");
            Rim(link.Source, physics, membership); Rim(link.TargetRim, physics, membership);
            if (!physics.Transmissions.ToArray().Any(row => row.Driving == new ConstraintCoordinateReference(link.Source.Joint) && row.Driven == new ConstraintCoordinateReference(link.TargetRim.Joint)))
                throw new ArgumentException("Mechanical presentation has no declared physical relation.");
        }
    }
    private static void Rim(CompiledMechanicalRim rim, IPhysicsSceneDeclaration physics, PackageBodyMembership membership)
    {
        new MechanicalRimDescriptor(new(0), rim.Centre, rim.Frame, rim.Radius, rim.AnglePerCoordinate).Validate();
        if (membership.Root(rim.Support) != membership.Root(rim.CoordinateSupport) ||
            !physics.Revolutes.ToArray().Any(joint => joint.Id == rim.Joint && joint.BodyA == rim.CoordinateSupport))
            throw new ArgumentException("Mechanical presentation support or coordinate is absent.");
    }
}
