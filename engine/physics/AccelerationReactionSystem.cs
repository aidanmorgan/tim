using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>One midpoint's signed support equations. Trial reactions are
/// numerical coordinates; their bound/cone projections supply force balance.
/// Original physical residuals independently govern acceptance. This object
/// owns acceleration scratch bodies and never mutates sampled physical bodies.</summary>
internal sealed class AccelerationReactionSystem
{
    private readonly Dictionary<PhysicsBodyId,PhysicsBody> _physical=new();
    private readonly Dictionary<PhysicsBodyId,PhysicsBody> _scratch=new();
    private readonly IReadOnlyDictionary<PhysicsBodyId,BodyWrench> _loads;
    private readonly ImpulseConstraint[] _constraints;
    private readonly ContactConstraint[] _contacts;
    private readonly ContactForce[] _contactDefinitions;
    private readonly (AccelerationDrive Drive,ImpulseConstraint Row)[] _drives;
    internal int CoordinateCount=>checked(_constraints.Length+3*_contacts.Length+_drives.Length);
    internal double TangentResponseScale(int contactIndex)=>_contacts[contactIndex].TangentResponseScale;

    internal AccelerationReactionSystem(IEnumerable<PhysicsBody> bodies,
        IReadOnlyList<ConstraintAcceleration> constraints,IReadOnlyList<ContactForce> contacts,
        IReadOnlyDictionary<PhysicsBodyId,BodyWrench> loads,
        IReadOnlyDictionary<PhysicsBodyId,BodyWrench> candidate,IReadOnlyList<AccelerationDrive> drives)
    {
        ArgumentNullException.ThrowIfNull(bodies);ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(contacts);ArgumentNullException.ThrowIfNull(loads);
        ArgumentNullException.ThrowIfNull(candidate);ArgumentNullException.ThrowIfNull(drives);
        _loads=loads;
        foreach(var body in bodies)
        {
            ArgumentNullException.ThrowIfNull(body);
            if(!_physical.TryAdd(body.Id,body)||!loads.ContainsKey(body.Id)||!candidate.TryGetValue(body.Id,out var wrench))
                throw new ArgumentException("Support equations require unique bodies and complete owned wrenches.");
            PhysicsBody state;
            if(body.MotionType==PhysicsMotionType.Dynamic)
                state=new(body.Id,body.MotionType,body.Pose,wrench.Force*body.InverseMass,
                    body.InverseInertia(wrench.Torque-CollisionVector.Cross(body.AngularVelocity,body.AngularMomentum)),
                    1/body.InverseMass,body.LocalInertia);
            else
            {
                if(wrench!=default||loads[body.Id]!=default)
                    throw new ArgumentException("Prescribed bodies cannot receive support loads.");
                state=new(body.Id,body.MotionType,body.Pose,body.PrescribedLinearAcceleration,body.PrescribedAngularAcceleration);
            }
            _scratch.Add(body.Id,state);
        }
        if(loads.Count!=_physical.Count||candidate.Count!=_physical.Count)
            throw new ArgumentException("Support wrenches contain foreign bodies.");
        ConstraintGradient Bind(ConstraintGradient gradient)
        {
            var terms=new ConstraintTerm[gradient.Terms.Length];
            for(var i=0;i<terms.Length;i++)
            {
                var term=gradient.Terms[i];
                if(!_physical.TryGetValue(term.Body.Id,out var body)||body!=term.Body)
                    throw new ArgumentException("Support row refers to a foreign sampled body.");
                terms[i]=new(_scratch[body.Id],term.Linear,term.Angular);
            }
            return new(terms);
        }
        _constraints=constraints.Select(row=>
        {
            var bounds=row.Relation switch
            {
                AccelerationRelation.Equal=>(Lower:double.NegativeInfinity,Upper:double.PositiveInfinity),
                AccelerationRelation.Nonnegative=>(Lower:0.0,Upper:double.PositiveInfinity),
                AccelerationRelation.Nonpositive=>(Lower:double.NegativeInfinity,Upper:0.0),
                _=>throw new ArgumentOutOfRangeException(nameof(constraints))
            };
            return new ImpulseConstraint(Bind(row.Gradient),-row.ConvectiveAcceleration,bounds.Lower,bounds.Upper);
        }).ToArray();
        _contactDefinitions=contacts.ToArray();
        _contacts=contacts.Select(contact=>
        {
            foreach(var body in contact.Kinematics.Bodies)
                if(!_physical.TryGetValue(body.Id,out var owned)||owned!=body)
                    throw new ArgumentException("Support contact refers to a foreign sampled body.");
            return contact.Bind(_scratch);
        }).ToArray();
        var identities=new HashSet<PhysicsDriveId>();
        _drives=drives.Select(drive=>
        {
            if(!identities.Add(drive.Id))throw new ArgumentException("Duplicate support drive identity.");
            var gradient=Bind(drive.Gradient);
            var row=new ImpulseConstraint(gradient,drive.TargetAcceleration-drive.ConvectiveAcceleration,
                drive.MinimumEffort,drive.MaximumEffort);
            if(row.InverseEffectiveMass==0)throw new ArgumentException("Support drive requires dynamic response.");
            return(drive,row);
        }).ToArray();
    }

    /// <summary>Fills caller-owned ephemeral projection scratch. Inputs and output must not overlap.
    /// The returned residual is independently owned and may be retained across evaluations.</summary>
    internal AccelerationReactionEvaluation Evaluate(ReadOnlySpan<double> coordinates,Span<double> projected)
    {
        if(coordinates.Length!=CoordinateCount||projected.Length!=CoordinateCount)
            throw new ArgumentException("Support reaction topology changed during prediction.");
        if(coordinates.Overlaps(projected))throw new ArgumentException("Reaction input and projection scratch must not overlap.");
        var residual=new double[CoordinateCount];
        var efforts=new double[_constraints.Length];var contacts=new ContactImpulse[_contacts.Length];
        var drives=new AccelerationDriveResult[_drives.Length];
        var forces=_physical.Keys.ToDictionary(id=>id,id=>_loads[id]);
        double physicalResidual=0;
        void Apply(ConstraintGradient gradient,double effort)
        {
            foreach(var term in gradient.Terms)
            {
                if(term.Body.MotionType!=PhysicsMotionType.Dynamic)continue;
                var prior=forces[term.Body.Id];
                forces[term.Body.Id]=new(prior.Force+term.Linear*effort,prior.Torque+term.Angular*effort);
            }
        }
        var offset=0;
        for(var i=0;i<_constraints.Length;i++,offset++)
        {
            var row=_constraints[i];var raw=coordinates[offset];
            var evaluated=row.EvaluateReaction(raw);residual[offset]=evaluated.Residual;
            var effort=evaluated.Reaction;efforts[i]=effort;projected[offset]=effort;
            row.CommitCoupledImpulse(effort);
            physicalResidual=Math.Max(physicalResidual,row.Residual);
            Apply(row.Gradient,effort);
        }
        for(var i=0;i<_contacts.Length;i++,offset+=3)
        {
            var row=_contacts[i];var definition=_contactDefinitions[i].Kinematics;
            var n=coordinates[offset];var u=coordinates[offset+1];var v=coordinates[offset+2];
            var signed=row.EvaluateReaction(n,u,v);
            residual[offset]=signed.Normal;residual[offset+1]=signed.U;residual[offset+2]=signed.V;
            var reaction=signed.Reaction;contacts[i]=reaction;
            var projectedU=signed.ProjectedU;var projectedV=signed.ProjectedV;
            projected[offset]=reaction.Normal;projected[offset+1]=projectedU;projected[offset+2]=projectedV;
            row.Normal.CommitCoupledImpulse(reaction.Normal);row.CommitTangentCoordinates(projectedU,projectedV);
            physicalResidual=Math.Max(physicalResidual,row.Residual);
            Apply(definition.NormalGradient,reaction.Normal);
            Apply(definition.TangentU,projectedU);Apply(definition.TangentV,projectedV);
        }
        for(var i=0;i<_drives.Length;i++,offset++)
        {
            var (drive,row)=_drives[i];var raw=coordinates[offset];
            var evaluated=row.EvaluateReaction(raw);residual[offset]=evaluated.Residual;
            var effort=evaluated.Reaction;projected[offset]=effort;row.CommitCoupledImpulse(effort);
            physicalResidual=Math.Max(physicalResidual,row.Residual);Apply(row.Gradient,effort);
            drives[i]=new(drive.Id,effort,row.Speed+drive.ConvectiveAcceleration,
                drive.MinimumEffort==0&&drive.MaximumEffort==0?DriveEffortLimit.Disabled:
                effort==drive.MinimumEffort?DriveEffortLimit.Lower:
                effort==drive.MaximumEffort?DriveEffortLimit.Upper:DriveEffortLimit.None);
        }
        if(!double.IsFinite(physicalResidual))throw new InvalidOperationException("Support physical residual exceeds numeric range.");
        return new(new ReadOnlyDictionary<PhysicsBodyId,BodyWrench>(forces),residual,physicalResidual,efforts,contacts,drives);
    }
}

internal sealed record AccelerationReactionEvaluation(
    IReadOnlyDictionary<PhysicsBodyId,BodyWrench> Forces,double[] Residual,double PhysicalResidual,
    double[] ConstraintEfforts,ContactImpulse[] ContactReactions,AccelerationDriveResult[] DriveResults);
