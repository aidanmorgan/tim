using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Coupled acceleration-space solve using the shared mass/Jacobian
/// solver. Scratch bodies store accelerations, never modify physical velocities.
/// Returned total wrenches include input loads and constraint reactions.</summary>
public static class AccelerationSolver
{
    private enum SupportResidualGroup { BodyLinear,BodyAngular,SourceEngagement,JointReaction,ContactReaction,MotorReaction }
    private enum SupportResidualStatus { Unavailable,Evaluated }
    private enum SupportProbeStatus { Unavailable,Matched,Mismatched }
    /// <summary>Implicit midpoint support prediction. Forces are evaluated on
    /// the same accelerated paths that collision detection will consume.
    /// Compliant laws here are already engaged; PhysicsWorld selects them from
    /// its committed contact history before requesting a pure prediction.</summary>
    public static ForcePrediction Predict(PhysicsLoadSet declarations,IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders,IEnumerable<PhysicsBody> bodies,
        IReadOnlyList<PhysicsJoint> joints,IReadOnlyList<PersistentContactPair> contacts,IReadOnlyDictionary<PhysicsBodyId,BodyWrench> loads,
        double duration,double completeStepDuration,double positionTolerance,double velocityTolerance,double accelerationTolerance,
        IReadOnlyList<MotorPredictionSupply> motors,IReadOnlyDictionary<PhysicsBodyId,PhysicsEnergyStoreState> stores,
        IReadOnlyDictionary<PhysicsGasNodeId,AxialGasPotential> gasPotentials)
    {
        ArgumentNullException.ThrowIfNull(gasPotentials);
        ArgumentNullException.ThrowIfNull(declarations);ArgumentNullException.ThrowIfNull(motors);
        ArgumentNullException.ThrowIfNull(colliders);ArgumentNullException.ThrowIfNull(bodies); ArgumentNullException.ThrowIfNull(joints); ArgumentNullException.ThrowIfNull(contacts); ArgumentNullException.ThrowIfNull(loads);
        if(!double.IsFinite(completeStepDuration)||completeStepDuration<duration||!double.IsFinite(duration)||duration<=0||!double.IsFinite(positionTolerance)||positionTolerance<=0||
            !double.IsFinite(velocityTolerance)||velocityTolerance<=0||!double.IsFinite(accelerationTolerance)||accelerationTolerance<=0) throw new ArgumentOutOfRangeException(nameof(duration));
        var owned=bodies.ToArray(); var byId=owned.ToDictionary(b=>b.Id);
        foreach(var joint in joints)
        {
            ArgumentNullException.ThrowIfNull(joint);
            foreach(var body in joint.Bodies)
                if(!byId.TryGetValue(body.Id,out var actual)||actual!=body) throw new ArgumentException("Foreign prediction participant.");
        }
        foreach(var contact in contacts)
        {
            ArgumentNullException.ThrowIfNull(contact);
            foreach(var body in contact.Bodies)
                if(!byId.TryGetValue(body.Id,out var actual)||actual!=body)
                    throw new ArgumentException("Foreign contact prediction participant.");
        }
        foreach(var load in declarations.Drag)
        {
            ArgumentNullException.ThrowIfNull(load);load.Resolve(byId);
        }
        foreach(var load in declarations.Compliant)
        {
            ArgumentNullException.ThrowIfNull(load);load.Validate(byId);
            if(!colliders.TryGetValue(load.Body,out var collider)||collider.Body!=load.Body||collider.Geometry is null)
                throw new ArgumentException("Compliant load requires current owned collider geometry.",nameof(colliders));
        }
        foreach(var load in declarations.Guides)
        {
            ArgumentNullException.ThrowIfNull(load);load.Validate(byId,colliders);
        }
        var efforts=declarations.Efforts.Select(load=>
        {
            ArgumentNullException.ThrowIfNull(load);
            var joint=load.Resolve(joints);load.Validate(joint,byId,colliders);
            return (Load:load,Joint:joint);
        }).ToArray();
        foreach(var spring in declarations.Springs)spring.Resolve(joints);
        var elastic=declarations.Elastic.Concat(declarations.Springs.Select(spring=>spring.Elastic)).Select(load=>
        {
            ArgumentNullException.ThrowIfNull(load);
            return (Load:load,Joint:load.Resolve(joints));
        }).ToArray();
        if(declarations.Gas.Select(load=>load.Node).Distinct().Count()!=declarations.Gas.Count)
            throw new ArgumentException("A gas inventory cannot drive multiple independent chamber coordinates.");
        if(gasPotentials.Count!=declarations.Gas.Count)
            throw new ArgumentException("Prediction requires exactly the declared gas inventory bindings.");
        foreach(var chamber in declarations.Gas)
            if(!gasPotentials.TryGetValue(chamber.Node,out var potential)||potential is null||potential.Geometry!=chamber.Geometry)
                throw new ArgumentException("Prediction gas geometry does not match its declared chamber.");
        var gas=declarations.Gas.OrderBy(load=>load.Node.Index)
            .Select(load=>(Load:load,Joint:load.Resolve(joints))).ToArray();
        var damping=declarations.Damping.Select(load=>
        {
            ArgumentNullException.ThrowIfNull(load);
            return (Load:load,Joint:load.Resolve(joints));
        }).ToArray();
        IReadOnlyList<MechanicalTransferLoad> currentTransfers=declarations.PrepareRotary(byId,joints,colliders,stores).Transfers;
        MechanicalTransferSource.ValidateAll(currentTransfers,byId,joints,stores,colliders);
        var mechanicalSources=currentTransfers.Select(load=>load.Source)
            .Where(source=>source.Kind==TransferSupplyKind.Mechanical).DistinctBy(source=>source.Id)
            .OrderBy(source=>source.Id.Index).ToArray();
        var engagement=mechanicalSources.ToDictionary(source=>source.Id,
            source=>source.MechanicalPort.Bind(byId,joints).Speed>0?1.0:0.0);
        var engagementScales=mechanicalSources.Select(source=>
        {
            var gradient=source.MechanicalPort.Bind(byId,joints);
            var scale=Math.Max(1,source.Rating.MaximumForce*gradient.Coupling(gradient));
            if(!double.IsFinite(scale))throw new InvalidOperationException("Engagement scale exceeds numeric range.");
            return scale;
        }).ToArray();
        var sourceInitialSpeeds=mechanicalSources.Select(source=>source.MechanicalPort.Bind(byId,joints).Speed).ToArray();
        var sourceEndpointSpeeds=new double[mechanicalSources.Length];
        var sourceHasDemand=new bool[mechanicalSources.Length];
        var lastTransfers=Array.Empty<MechanicalTransferEvaluation>();
        IReadOnlyDictionary<PhysicsBodyId,BodyWrench> EvaluateLoads(
            IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> sample,Func<IReadOnlyDictionary<PhysicsBodyId,PhysicsBody>> sampleEnd,
            Func<IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory>> capturedPaths,double interval)
        {
            // Endpoint dependence belongs to elastic/compliant laws. Share one
            // exact endpoint sample within this evaluation, only when requested.
            IReadOnlyDictionary<PhysicsBodyId,PhysicsBody>? endpointBodies=null;
            IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> End()=>endpointBodies??=sampleEnd();
            var prepared=declarations.PrepareRotary(sample,joints,colliders,stores);
            currentTransfers=prepared.Transfers;
            var stageDamping=declarations.Rotary.Count==0?damping:prepared.Damping
                .Select(load=>(Load:load,Joint:load.Resolve(joints))).ToArray();
            var result=loads.ToDictionary(entry=>entry.Key,entry=>entry.Value);
            foreach(var (load,joint) in efforts)
            {
                var sampled=(PhysicsFrameJoint)joint.Rebind(sample);
                var effort=load.Evaluate(sampled,sample,colliders);
                if(!double.IsFinite(effort))throw new InvalidOperationException("Effort law returned a nonfinite value.");
                foreach(var term in sampled.Travel.Jacobian.Bind(sampled.A,sampled.B).Terms)
                {
                    if(term.Body.MotionType!=PhysicsMotionType.Dynamic)continue;
                    var prior=result[term.Body.Id];
                    result[term.Body.Id]=new(prior.Force+term.Linear*effort,prior.Torque+term.Angular*effort);
                }
            }
            foreach(var (load,joint) in elastic)
            {
                var sampled=(PhysicsFrameJoint)joint.Rebind(sample);
                var endpoint=(PhysicsFrameJoint)joint.Rebind(End());
                var effort=load.Potential.IntervalEffort(joint.Travel.Error,endpoint.Travel.Error);
                foreach(var term in sampled.Travel.Jacobian.Bind(sampled.A,sampled.B).Terms)
                {
                    if(term.Body.MotionType!=PhysicsMotionType.Dynamic)continue;
                    var prior=result[term.Body.Id];
                    result[term.Body.Id]=new(prior.Force+term.Linear*effort,prior.Torque+term.Angular*effort);
                }
            }
            foreach(var (load,joint) in gas)
            {
                var sampled=(PhysicsFrameJoint)joint.Rebind(sample);
                var endpoint=(PhysicsFrameJoint)joint.Rebind(End());
                var effort=gasPotentials[load.Node].IntervalEffort(joint.Travel.Error,endpoint.Travel.Error);
                foreach(var term in sampled.Travel.Jacobian.Bind(sampled.A,sampled.B).Terms)
                {
                    if(term.Body.MotionType!=PhysicsMotionType.Dynamic)continue;
                    var prior=result[term.Body.Id];
                    result[term.Body.Id]=new(prior.Force+term.Linear*effort,prior.Torque+term.Angular*effort);
                }
            }
            foreach(var (load,joint) in stageDamping)
            {
                var sampled=(PhysicsFrameJoint)joint.Rebind(sample);
                var jacobian=sampled.Travel.Jacobian.Bind(sampled.A,sampled.B);
                var effort=load.Effort(jacobian.Speed);
                foreach(var term in jacobian.Terms)
                {
                    if(term.Body.MotionType!=PhysicsMotionType.Dynamic)continue;
                    var prior=result[term.Body.Id];
                    result[term.Body.Id]=new(prior.Force+term.Linear*effort,prior.Torque+term.Angular*effort);
                }
            }
            lastTransfers=currentTransfers.Count==0?Array.Empty<MechanicalTransferEvaluation>():
                MechanicalTransferSource.EvaluateAll(currentTransfers,sample,joints,byId,capturedPaths(),interval,stores,colliders,engagement);
            foreach(var transfer in lastTransfers)
                foreach(var term in transfer.Gradient.Terms)
                {
                    if(term.Body.MotionType!=PhysicsMotionType.Dynamic)continue;
                    var prior=result[term.Body.Id];
                    result[term.Body.Id]=new(prior.Force+term.Linear*transfer.Response.Force,
                        prior.Torque+term.Angular*transfer.Response.Force);
                }
            foreach(var load in declarations.Drag)
            {
                var drag=load.Wrench(sample[load.Body]);var prior=result[load.Body];
                result[load.Body]=new(prior.Force+drag.Force,prior.Torque+drag.Torque);
            }
            foreach(var load in declarations.Compliant)
            {
                var force=load.Evaluate(colliders[load.Body].Geometry,byId,sample,End());
                void Add(PhysicsBodyId id,BodyWrench wrench)
                {
                    if(sample[id].MotionType!=PhysicsMotionType.Dynamic)return;
                    var prior=result[id];result[id]=new(prior.Force+wrench.Force,prior.Torque+wrench.Torque);
                }
                Add(load.Body,force.Body);Add(load.Frame,force.Frame);
            }
            foreach(var load in declarations.Guides)
            {
                var guide=load.Evaluate(sample,colliders);var prior=result[load.Body];
                result[load.Body]=new(prior.Force+guide.Force,prior.Torque+guide.Torque);
            }
            return result;
        }
        var motorDrives=Array.Empty<MotorPredictionDrive>();
        if(motors.Count>0)
        {
            var motorIds=new HashSet<PhysicsJointId>();
            foreach(var motor in motors)
            {
                ArgumentNullException.ThrowIfNull(motor);
                if(!motorIds.Add(motor.Command.Joint))throw new ArgumentException("Duplicate motor prediction identity.");
            }
            motorDrives=motors.OrderBy(motor=>motor.Command.Joint.Index)
                .Select(motor=>new MotorPredictionDrive(motor,joints)).ToArray();
        }
        var lastMotorRows=Array.Empty<AccelerationDrive>();
        IReadOnlyList<AccelerationDriveResult> lastMotorResults=Array.Empty<AccelerationDriveResult>();
        var lastSpringConstraints=Array.Empty<PredictedSpringConstraint>();
        var samples=new PredictionSamples(owned);
        var initialRows=joints.SelectMany(j=>j.AccelerationConstraints(byId,positionTolerance,velocityTolerance)).ToArray();
        var initialPaths=samples.Capture(loads,duration);
        var initialContacts=contacts.SelectMany(c=>c.AccelerationContacts(byId,velocityTolerance,initialPaths,0)).ToArray();
        var initialDrives=motorDrives.Select(motor=>motor.Prepare(byId,initialPaths,duration)).ToArray();
        IReadOnlyDictionary<PhysicsBodyId,BodyWrench> candidate=Solve(owned,initialRows,initialContacts,EvaluateLoads(byId,()=>byId,()=>initialPaths,duration),accelerationTolerance*.1,out var initialWork,initialDrives,out var initialDriveResults,out var initialEfforts,out var initialReactions);
        var constraintWork=default(PredictionConstraintWork).Add(initialWork);
        var sliding=contacts.SelectMany(c=>c.SlidingFeatures(velocityTolerance)).ToArray();
        var horizon=duration;
        var boundary=ForcePredictionBoundary.IntervalEnd;
        Dictionary<PhysicsBodyId,BodyTrajectory> Capture(IReadOnlyDictionary<PhysicsBodyId,BodyWrench> forces)=>
            samples.Capture(forces,horizon);
        double Limit(Dictionary<PhysicsBodyId,BodyTrajectory> paths)
        {
            var end=horizon;
            var swept=declarations.Rotary.Count==0?declarations:
                declarations.PrepareRotary(samples.Sample(paths,PredictionStage.Midpoint),joints,colliders,stores);
            foreach(var rotary in declarations.Rotary)
            {
                var hit=RotaryCaptureSweep.Cast(rotary,byId,joints,colliders,stores,paths,end,
                    positionTolerance,velocityTolerance,TransferSweepStage.Iteration);
                if(hit.Status==ScalarSweepStatus.Boundary&&hit.Time<end)
                { end=hit.Time; boundary=ForcePredictionBoundary.Field; }
            }
            foreach(var load in swept.Transfers)
            {
                var hit=load.Sweep(byId,joints,paths,end,velocityTolerance,colliders,positionTolerance,TransferSweepStage.Iteration);
                if(hit.Status==ScalarSweepStatus.Boundary&&hit.Time<end)
                { end=hit.Time; boundary=ForcePredictionBoundary.Field; }
            }
            foreach(var gap in sliding)
            {
                var hit=ContactSlipSweep.Cast(new ContactSlipPath(gap,paths),end,velocityTolerance);
                if(hit.Status==ContactSlipStatus.Boundary&&hit.Time<end)
                { end=hit.Time; boundary=ForcePredictionBoundary.Friction; }
            }
            foreach(var joint in joints)
            {
                var participants=joint.Bodies.ToArray().Select(body=>paths[body.Id]).ToArray();
                var hit=joint.Sweep(participants,end,positionTolerance,velocityTolerance);
                if(hit.Status==JointSweepStatus.Boundary&&hit.Time<end)
                { end=hit.Time; boundary=ForcePredictionBoundary.Joint; }
            }
            foreach(var (load,joint) in efforts)
            {
                var hit=load.Sweep(joint,byId,colliders,paths,end,positionTolerance);
                if(hit.Status==ScalarSweepStatus.Boundary&&hit.Time<end)
                { end=hit.Time; boundary=ForcePredictionBoundary.Field; }
            }
            foreach(var load in declarations.Guides)
            {
                var hit=load.Sweep(byId,colliders,paths,end,positionTolerance,velocityTolerance);
                if(hit.Status==ScalarSweepStatus.Boundary&&hit.Time<end)
                { end=hit.Time; boundary=ForcePredictionBoundary.Field; }
            }
            if(end<=0) throw new InvalidOperationException("Support prediction made no temporal progress.");
            return end;
        }
        var dynamicBodies=owned.Where(body=>body.MotionType==PhysicsMotionType.Dynamic).ToArray();
        var inertias=dynamicBodies.Select(body=>body.LocalInertia.Rotated(body.Pose.Rotation)).ToArray();
        var reactionOffset=checked(dynamicBodies.Length*6+mechanicalSources.Length);
        var reactionCount=checked(initialRows.Length+3*initialContacts.Length+motorDrives.Length);
        // Ephemeral per-prediction output: copy into a proposal before the next evaluation.
        var lastProjectedReactions=new double[reactionCount];
        double[] Coordinates(IReadOnlyDictionary<PhysicsBodyId,BodyWrench> forces,ReadOnlySpan<double> tailState)
        {
            var values=new double[checked(reactionOffset+reactionCount)];
            for(var i=0;i<dynamicBodies.Length;i++)
            {
                var body=dynamicBodies[i]; var force=forces[body.Id];
                var linear=force.Force*body.InverseMass; var angular=body.InverseInertia(force.Torque);
                values[6*i]=linear.X; values[6*i+1]=linear.Y; values[6*i+2]=linear.Z;
                values[6*i+3]=angular.X; values[6*i+4]=angular.Y; values[6*i+5]=angular.Z;
            }
            tailState.CopyTo(values.AsSpan(dynamicBodies.Length*6));
            return values;
        }
        void SetEngagement(double[] values)
        {
            for(var i=0;i<mechanicalSources.Length;i++)
                engagement[mechanicalSources[i].Id]=Math.Clamp(values[dynamicBodies.Length*6+i]/engagementScales[i],0,1);
        }
        double EngagementResidual(double[] values,int i)=>
            !sourceHasDemand[i]||sourceInitialSpeeds[i]<-velocityTolerance?values[dynamicBodies.Length*6+i]:
            sourceInitialSpeeds[i]>velocityTolerance?values[dynamicBodies.Length*6+i]-engagementScales[i]:
            values[dynamicBodies.Length*6+i]-Math.Clamp(values[dynamicBodies.Length*6+i]+sourceEndpointSpeeds[i]/horizon,0,engagementScales[i]);
        double ResidualError(double[] residual)
        {
            double error=0;
            for(var i=0;i<dynamicBodies.Length*6;i+=3)
                error=Math.Max(error,new CollisionVector(residual[i],residual[i+1],residual[i+2]).Length);
            for(var i=dynamicBodies.Length*6;i<residual.Length;i++)error=Math.Max(error,Math.Abs(residual[i]));
            return error;
        }
        IReadOnlyDictionary<PhysicsBodyId,BodyWrench> Forces(double[] values)
        {
            var forces=owned.ToDictionary(body=>body.Id,_=>default(BodyWrench));
            for(var i=0;i<dynamicBodies.Length;i++)
            {
                var linear=new CollisionVector(values[6*i],values[6*i+1],values[6*i+2]);
                var angular=new CollisionVector(values[6*i+3],values[6*i+4],values[6*i+5]);
                forces[dynamicBodies[i].Id]=new(linear/dynamicBodies[i].InverseMass,inertias[i].Apply(angular));
            }
            return forces;
        }
        var midpointEvaluations=0;var newtonIterations=0;
        double[] lastReactionResidual=[];double lastReactionPhysicalResidual=double.PositiveInfinity;
        ContactForce[] lastContactForces=[];
        ConstraintAcceleration[] lastRows=[];
        IReadOnlyDictionary<PhysicsBodyId,PhysicsBody>? lastMidpoint=null;
        IReadOnlyDictionary<PhysicsBodyId,BodyWrench>? lastApplied=null;
        IReadOnlyDictionary<PhysicsBodyId,BodyWrench> AtMidpoint(Dictionary<PhysicsBodyId,BodyTrajectory> paths,double[] values)
        {
            midpointEvaluations=checked(midpointEvaluations+1);
            var midpoint=samples.Sample(paths,PredictionStage.Midpoint);
            var applied=EvaluateLoads(midpoint,()=>samples.Sample(paths,PredictionStage.Endpoint),()=>paths,horizon);
            lastMidpoint=midpoint;lastApplied=applied;
            if(mechanicalSources.Length>0)
            {
                var endpoint=samples.Sample(paths,PredictionStage.Endpoint);
                for(var i=0;i<mechanicalSources.Length;i++)
                {
                    sourceEndpointSpeeds[i]=mechanicalSources[i].MechanicalPort.Bind(endpoint,joints).Speed;
                    // Without a rated branch reaction, engagement is an unused
                    // coordinate. Fix it to zero instead of imposing a stall
                    // condition that cannot influence the physical equations.
                    var rating=mechanicalSources[i].Rating;
                    sourceHasDemand[i]=rating.MaximumForce>0&&rating.MaximumPower>0&&
                        currentTransfers.Any(load=>load.Source.Id==mechanicalSources[i].Id&&load.EvaluateDemand(midpoint,joints,colliders).Response.Force>0);
                }
            }
            var identifiedRows=joints.SelectMany(j=>
                j.AccelerationConstraints(midpoint,positionTolerance,velocityTolerance).Select(row=>(Joint:j.Id,Row:row))).ToArray();
            var rows=identifiedRows.Select(identified=>identified.Row).Select(row=>
            {
                if(row.Relation!=AccelerationRelation.Equal)return row;
                // A finite support interval must satisfy its bilateral velocity
                // equation at the midpoint. At convergence this correction gives
                // J v_mid = 0, instead of only J a + J-dot v = 0 at one instant.
                double velocity=0,acceleration=0;
                foreach(var term in row.Gradient.Terms)
                {
                    var correction=paths[term.Body.Id].ConstraintVelocityTerms(term.Linear,term.Angular,horizon*.5);
                    velocity+=correction.Velocity;acceleration+=correction.Acceleration;
                }
                return new ConstraintAcceleration(row.Gradient,
                    velocity/(horizon*.5)-acceleration,row.Relation);
            }).ToArray();
            var contactForces=contacts.SelectMany(c=>c.AccelerationContacts(midpoint,velocityTolerance,paths,horizon*.5)).ToArray();
            lastContactForces=contactForces;lastRows=rows;
            // Reserve the inner constraint error inside the outer nonlinear
            // budget; a solve at the full outer tolerance can obscure decrease.
            lastMotorRows=motorDrives.Length==0?Array.Empty<AccelerationDrive>():
                motorDrives.Select(motor=>motor.Prepare(midpoint,paths,horizon)).ToArray();
            var equations=new AccelerationReactionSystem(midpoint.Values,rows,contactForces,applied,Forces(values),lastMotorRows);
            var evaluated=equations.Evaluate(values.AsSpan(reactionOffset),lastProjectedReactions);
            var result=evaluated.Forces;var jointEfforts=evaluated.ConstraintEfforts;var contactReactions=evaluated.ContactReactions;
            lastMotorResults=evaluated.DriveResults;lastReactionResidual=evaluated.Residual;
            lastReactionPhysicalResidual=evaluated.PhysicalResidual;
            var response=declarations.Springs.Count==0?null:new BilateralResponseMap(rows.Where(row=>row.Relation==AccelerationRelation.Equal).Select(row=>row.Gradient).ToArray());
            lastSpringConstraints=declarations.Springs.Select(spring=>
            {
                var (originalGuide,transmission)=spring.Resolve(joints);
                var guide=(PhysicsFrameJoint)originalGuide.Rebind(midpoint);
                var direction=guide.Travel.Jacobian.Bind(guide.A,guide.B);
                if(response!.Constrains(direction))
                    return new PredictedSpringConstraint(spring.Guide,new(SpringResponseDomain.BilaterallyLocked,0,0,0,0,0));
                var gradient=response.Project(direction,accelerationTolerance).Gradient;
                var inverseMass=gradient.Coupling(gradient);
                if(!double.IsFinite(inverseMass)||inverseMass<0)
                    throw new InvalidOperationException("A spring observation requires a finite nonnegative guide response.");
                // Additional admitted bilateral constraints can lock this coordinate.
                // Its conjugate effort is then undefined; retain that typed domain
                // instead of dividing by zero or attributing a local contact stall.
                if(inverseMass==0)
                    return new PredictedSpringConstraint(spring.Guide,new(SpringResponseDomain.BilaterallyLocked,0,0,0,0,0));
                double transmitted=0,contactEffort=0,guideLimitEffort=0,arithmeticResolution=0;
                void Accumulate(double contribution)
                {
                    var scale=Math.Abs(contribution);
                    arithmeticResolution+=Math.BitIncrement(scale)-scale;
                    contactEffort+=contribution;
                    scale=Math.Abs(contactEffort);
                    arithmeticResolution+=Math.BitIncrement(scale)-scale;
                }
                for(var i=0;i<identifiedRows.Length;i++)
                    {
                    if(identifiedRows[i].Joint==transmission.Id)transmitted+=jointEfforts[i];
                    if(rows[i].Relation!=AccelerationRelation.Equal)
                    {
                        var contribution=gradient.Coupling(rows[i].Gradient)*jointEfforts[i];
                        guideLimitEffort+=contribution;
                        arithmeticResolution+=Math.BitIncrement(Math.Abs(contribution))-Math.Abs(contribution);
                        arithmeticResolution+=Math.BitIncrement(Math.Abs(guideLimitEffort))-Math.Abs(guideLimitEffort);
                    }
                }
                for(var i=0;i<contactForces.Length;i++)
                {
                    var contact=contactForces[i].Kinematics;var reaction=contactReactions[i];
                    var local=contact.Bodies.ToArray().Any(body=>
                        body.MotionType==PhysicsMotionType.Dynamic&&(body.Id==guide.A.Id||body.Id==guide.B.Id));
                    if(!local)
                    {
                        guideLimitEffort+=gradient.Coupling(contact.NormalGradient)*reaction.Normal+
                            gradient.Coupling(contact.TangentU)*CollisionVector.Dot(reaction.Tangent,contact.U)+
                            gradient.Coupling(contact.TangentV)*CollisionVector.Dot(reaction.Tangent,contact.V);
                        continue;
                    }
                    Accumulate(gradient.Coupling(contact.NormalGradient)*reaction.Normal);
                    Accumulate(gradient.Coupling(contact.TangentU)*CollisionVector.Dot(reaction.Tangent,contact.U));
                    Accumulate(gradient.Coupling(contact.TangentV)*CollisionVector.Dot(reaction.Tangent,contact.V));
                }
                // Project the existing per-body acceleration resolution through the
                // guide Jacobian, then convert to its conjugate effort. A row
                // rescaling scales this resolution with the observed force.
                double accelerationResolution=arithmeticResolution;
                foreach(var term in gradient.Terms)
                    if(term.Body.MotionType==PhysicsMotionType.Dynamic)
                        accelerationResolution+=accelerationTolerance*(term.Linear.Length+term.Angular.Length);
                double driveAcceleration=0;
                foreach(var term in gradient.Terms)
                {
                    var wrench=applied[term.Body.Id];
                    driveAcceleration+=CollisionVector.Dot(term.Linear,wrench.Force)*term.Body.InverseMass+
                        CollisionVector.Dot(term.Angular,term.Body.InverseInertia(wrench.Torque));
                }
                for(var i=0;i<lastMotorRows.Length;i++)
                    driveAcceleration+=gradient.Coupling(lastMotorRows[i].Gradient)*lastMotorResults[i].Effort;
                var effortResolution=accelerationResolution/inverseMass;
                effortResolution+=Math.BitIncrement(Math.Abs(transmitted))-Math.Abs(transmitted);
                return new PredictedSpringConstraint(spring.Guide,new(SpringResponseDomain.Admissible,transmitted,contactEffort/inverseMass,guideLimitEffort/inverseMass,driveAcceleration/inverseMass,effortResolution));
            }).ToArray();
            return result;
        }
        double[] lastResidualState=[],lastResidualValue=[];
        double[] Residual(double[] values)
        {
            lastResidualState=values;
            lastResidualValue=[];
            SetEngagement(values);
            var evaluated=Coordinates(AtMidpoint(Capture(Forces(values)),values),[]);
            for(var i=0;i<dynamicBodies.Length*6;i++)evaluated[i]=values[i]-evaluated[i];
            for(var i=0;i<mechanicalSources.Length;i++)evaluated[dynamicBodies.Length*6+i]=EngagementResidual(values,i);
            for(var i=0;i<lastReactionResidual.Length;i++)
                evaluated[reactionOffset+i]=lastReactionResidual[i]*10;
            lastResidualValue=evaluated;
            return evaluated;
        }
        string ComputeResidualGroups(double[] state,IReadOnlyList<double> values)
        {
            if(values.Count!=reactionOffset+reactionCount)
                return $"status {SupportResidualStatus.Unavailable}, state [{string.Join(", ",state)}]";
            var groups=new List<string>();
            for(var i=0;i<dynamicBodies.Length;i++)
            {
                groups.Add($"{SupportResidualGroup.BodyLinear}, body {dynamicBodies[i].Id.Index}, residual ({values[6*i]:R},{values[6*i+1]:R},{values[6*i+2]:R})");
                groups.Add($"{SupportResidualGroup.BodyAngular}, body {dynamicBodies[i].Id.Index}, residual ({values[6*i+3]:R},{values[6*i+4]:R},{values[6*i+5]:R})");
            }
            for(var i=0;i<mechanicalSources.Length;i++)
                groups.Add($"{SupportResidualGroup.SourceEngagement}, source {mechanicalSources[i].Id}, residual {values[dynamicBodies.Length*6+i]:R}");
            var owners=joints.SelectMany(joint=>joint.AccelerationConstraints(byId,positionTolerance,velocityTolerance)
                .Select((row,index)=>(Joint:joint.Id,Row:index))).ToArray();
            for(var i=0;i<initialRows.Length;i++)
                groups.Add($"{SupportResidualGroup.JointReaction}, joint {owners[i].Joint.Index}, row {owners[i].Row}, relation {initialRows[i].Relation}, residual {values[reactionOffset+i]:R}");
            var offset=reactionOffset+initialRows.Length;
            for(var i=0;i<initialContacts.Length;i++,offset+=3)
                groups.Add($"{SupportResidualGroup.ContactReaction}, contact {i}, bodies [{string.Join(", ",initialContacts[i].Kinematics.Bodies.ToArray().Select(body=>body.Id.Index))}], regime {initialContacts[i].Regime}, residual ({values[offset]:R},{values[offset+1]:R},{values[offset+2]:R})");
            for(var i=0;i<motorDrives.Length;i++)
                groups.Add($"{SupportResidualGroup.MotorReaction}, drive {motorDrives[i].Id.Index}, residual {values[offset+i]:R}");
            return $"status {SupportResidualStatus.Evaluated}, reaction residual scale 10, state [{string.Join(", ",state)}], groups [{string.Join("; ",groups)}]";
        }
        string DescribeResidualGroups(double[] state,IReadOnlyList<double> values)
        {
            try {return ComputeResidualGroups(state,values);}
            catch(Exception unavailable)
            {return $"status {SupportResidualStatus.Unavailable}, state [{string.Join(", ",state)}], diagnostic error {unavailable.GetType().Name}: {unavailable.Message}";}
        }
        string DescribeMidpointInputs(double[] state)
        {
            if(lastMidpoint is null||lastApplied is null)throw new InvalidOperationException("Midpoint inputs are unavailable.");
            static string Terms(ConstraintGradient gradient)=>string.Join("; ",gradient.Terms.ToArray().Select(term=>
                $"body {term.Body.Id.Index}, linear {term.Linear}, angular {term.Angular}"));
            var candidateWrenches=Forces(state);
            var bodies=lastMidpoint.Values.OrderBy(body=>body.Id.Index).Select(body=>
            {
                var inertia=body.LocalInertia;
                return $"{body.Snapshot()}, inverse mass {body.InverseMass:R}, local inertia ({inertia.XX:R},{inertia.YY:R},{inertia.ZZ:R},{inertia.XY:R},{inertia.XZ:R},{inertia.YZ:R}), prescribed linear acceleration {body.PrescribedLinearAcceleration}, prescribed angular acceleration {body.PrescribedAngularAcceleration}, candidate wrench {candidateWrenches[body.Id]}, applied wrench {lastApplied[body.Id]}";
            });
            var rows=lastRows.Select((row,index)=>
                $"row {index}, relation {row.Relation}, convective acceleration {row.ConvectiveAcceleration:R}, terms [{Terms(row.Gradient)}]");
            var contactInputs=lastContactForces.Select((contact,index)=>
                $"contact {index}, regime {contact.Regime}, friction {contact.Friction:R}, normal {contact.Kinematics.Normal}, U {contact.Kinematics.U}, V {contact.Kinematics.V}, normal bias {contact.NormalBias:R}, tangent bias {contact.TangentBias}, physical slip {contact.PhysicalSlip}, normal terms [{Terms(contact.Kinematics.NormalGradient)}], U terms [{Terms(contact.Kinematics.TangentU)}], V terms [{Terms(contact.Kinematics.TangentV)}]");
            var drives=lastMotorRows.Select((drive,index)=>
                $"drive {index}, id {drive.Id.Index}, convective acceleration {drive.ConvectiveAcceleration:R}, target acceleration {drive.TargetAcceleration:R}, minimum effort {drive.MinimumEffort:R}, maximum effort {drive.MaximumEffort:R}, terms [{Terms(drive.Gradient)}]");
            return $"same-evaluation frozen midpoint inputs, horizon {horizon:R}, position tolerance {positionTolerance:R}, velocity tolerance {velocityTolerance:R}, acceleration tolerance {accelerationTolerance:R}, reaction offset {reactionOffset}, joint rows {initialRows.Length}, contacts {initialContacts.Length}, raw reactions [{string.Join(", ",state.AsSpan(reactionOffset).ToArray())}], projected reactions [{string.Join(", ",lastProjectedReactions)}], sampled bodies [{string.Join("; ",bodies)}], corrected rows [{string.Join("; ",rows)}], contacts [{string.Join("; ",contactInputs)}], prepared drives [{string.Join("; ",drives)}]";
        }
        string DescribeCandidateResidual(double[] state)
        {
            try
            {
                var evaluated=Residual(state);
                return $"physical residual maximum {lastReactionPhysicalResidual:R}, coupled [{DescribeResidualGroups(state,evaluated)}], inputs [{DescribeMidpointInputs(state)}]";
            }
            catch(Exception unavailable)
            {return $"status {SupportResidualStatus.Unavailable}, state [{string.Join(", ",state)}], diagnostic error {unavailable.GetType().Name}: {unavailable.Message}";}
        }
        string DescribeProbeReplay(NonlinearLineSearchFailure failure)
        {
            try
            {
                var state=failure.State.ToArray();
                var columns=new List<string>();
                string Contacts()=>string.Join("; ",lastContactForces.Select((contact,index)=>
                    $"contact {index}, bodies [{string.Join(", ",contact.Kinematics.Bodies.ToArray().Select(body=>body.Id.Index))}], regime {contact.Regime}, normal {contact.Kinematics.Normal}, U {contact.Kinematics.U}, V {contact.Kinematics.V}, physical slip {contact.PhysicalSlip}, normal bias {contact.NormalBias:R}, tangent bias {contact.TangentBias}, friction {contact.Friction:R}, projected normal {lastProjectedReactions[initialRows.Length+3*index]:R}, projected U {lastProjectedReactions[initialRows.Length+3*index+1]:R}, projected V {lastProjectedReactions[initialRows.Length+3*index+2]:R}, radius {contact.Friction*lastProjectedReactions[initialRows.Length+3*index]:R}"));
                var allMatched=true;
                for(var column=0;column<state.Length;column++)
                {
                    var (high,low)=NonlinearIteration.DifferenceSamples(state,column);
                    var highResidual=Residual(high);
                    // Materialize before the next evaluation reuses sampled bodies.
                    var highContacts=Contacts();
                    var lowResidual=Residual(low);
                    var lowContacts=Contacts();
                    var width=high[column]-low[column];
                    var matched=true;
                    for(var row=0;row<state.Length;row++)
                        matched&=BitConverter.DoubleToInt64Bits((highResidual[row]-lowResidual[row])/width)==
                            BitConverter.DoubleToInt64Bits(failure.Jacobian[row][column]);
                    allMatched&=matched;
                    columns.Add($"column {column}, original {state[column]:R}, high {high[column]:R}, low {low[column]:R}, width {width:R}, status {(matched?SupportProbeStatus.Matched:SupportProbeStatus.Mismatched)}, high residual [{string.Join(", ",highResidual)}], high contacts [{highContacts}], low residual [{string.Join(", ",lowResidual)}], low contacts [{lowContacts}]");
                }
                return $"recomputed physical midpoint samples, status {(allMatched?SupportProbeStatus.Matched:SupportProbeStatus.Mismatched)}, columns [{string.Join("; ",columns)}]";
            }
            catch(Exception unavailable)
            {return $"status {SupportProbeStatus.Unavailable}, diagnostic error {unavailable.GetType().Name}: {unavailable.Message}";}
        }
        var candidateState=Coordinates(candidate,mechanicalSources.Select((source,i)=>engagement[source.Id]*engagementScales[i]).ToArray());
        initialEfforts.CopyTo(candidateState,reactionOffset);
        for(var i=0;i<initialContacts.Length;i++)
        {
            var offset=reactionOffset+initialRows.Length+3*i;var reaction=initialReactions[i];
            candidateState[offset]=reaction.Normal;
            candidateState[offset+1]=CollisionVector.Dot(reaction.Tangent,initialContacts[i].Kinematics.U);
            candidateState[offset+2]=CollisionVector.Dot(reaction.Tangent,initialContacts[i].Kinematics.V);
        }
        for(var i=0;i<initialDriveResults.Count;i++)
        {
            if(initialDriveResults[i].Id!=motorDrives[i].Id)throw new InvalidOperationException("Initial motor reaction identity changed.");
            candidateState[reactionOffset+initialRows.Length+3*initialContacts.Length+i]=initialDriveResults[i].Effort;
        }
        const int maximumIterations=64,maximumRestrictions=64;
        var restrictions=0; var lastError=double.PositiveInfinity;
        for(var iteration=0;;)
        {
            // Forces already use projected engagement. Project the iterate too:
            // this preserves those forces and cannot increase the natural-map
            // residual outside either bound, while avoiding a flat force
            // Jacobian at an infeasible engagement coordinate.
            for(var i=0;i<mechanicalSources.Length;i++)
                candidateState[dynamicBodies.Length*6+i]=Math.Clamp(candidateState[dynamicBodies.Length*6+i],0,engagementScales[i]);
            SetEngagement(candidateState);
            candidate=Forces(candidateState);
            var paths=Capture(candidate);
            var limited=Limit(paths);
            if(limited<horizon)
            {
                if(++restrictions>maximumRestrictions)throw new InvalidOperationException("Prediction horizon restriction budget exceeded.");
                horizon=limited;continue;
            }
            var next=AtMidpoint(paths,candidateState);
            double error=0;
            foreach(var body in owned)
            {
                var deltaForce=next[body.Id].Force-candidate[body.Id].Force;
                var deltaTorque=next[body.Id].Torque-candidate[body.Id].Torque;
                error=Math.Max(error,Math.Max(deltaForce.Length*body.InverseMass,body.InverseInertia(deltaTorque).Length));
            }
            for(var i=0;i<mechanicalSources.Length;i++)error=Math.Max(error,Math.Abs(EngagementResidual(candidateState,i)));
            // Signed equations select steps; the original physical constraint
            // residual independently retains the stricter inner support budget.
            error=Math.Max(error,lastReactionPhysicalResidual*10);
            foreach(var value in lastReactionResidual)error=Math.Max(error,Math.Abs(value)*10);
            if(!double.IsFinite(error)) throw new InvalidOperationException("Support prediction exceeds numeric range.");
            WrenchPathWorkResult? transferResidual=null;
            var workAccepted=true;
            if(error<=accelerationTolerance&&lastTransfers.Length>0)
            {
                // Each accepted interval spends only its fraction of the complete
                // step allowance. Event cuts must never renew that allowance.
                var budget=declarations.TransferWorkTolerance*(horizon/completeStepDuration)*.125;
                var tolerance=budget/Math.Max(1,dynamicBodies.Length);
                if(tolerance<=0)throw new InvalidOperationException("Residual work tolerance is below the supported numeric range.");
                var total=default(WrenchPathWorkResult);
                foreach(var body in dynamicBodies)
                {
                    var actual=candidate[body.Id];var evaluated=next[body.Id];
                    var difference=new BodyWrench(actual.Force-evaluated.Force,actual.Torque-evaluated.Torque);
                    var work=WrenchPathWork.Measure([new(body,paths[body.Id],difference)],horizon,tolerance,0);
                    total=TransferWorkArithmetic.Merge(total,work,WorkAccumulation.Concurrent);
                }
                // Separate each body's positive/negative work before summing:
                // unrelated errors must not cancel in the acceptance budget.
                transferResidual=total;
                var upper=total.Supplied+total.Dissipated+total.SuppliedErrorBound+total.DissipatedErrorBound;
                workAccepted=double.IsFinite(upper)&&upper<=budget;
            }
            if(error<=accelerationTolerance&&workAccepted)
            {
                if(!GasPredictionWork.Accept(declarations.Gas,gasPotentials,byId,joints,paths,horizon,completeStepDuration,velocityTolerance))
                {
                    if(++restrictions>maximumRestrictions)
                        throw new InvalidOperationException("Gas work refinement budget exceeded.");
                    var reduced=horizon*.5;
                    if(reduced<=0||reduced==horizon)throw new InvalidOperationException("Gas refinement made no temporal progress.");
                    horizon=reduced;boundary=ForcePredictionBoundary.Accuracy;continue;
                }
                var acceptedEnd=horizon;
                foreach(var rotary in declarations.Rotary)
                {
                    var hit=RotaryCaptureSweep.Cast(rotary,byId,joints,colliders,stores,paths,acceptedEnd,
                        positionTolerance,velocityTolerance,TransferSweepStage.Acceptance);
                    if(hit.Status==ScalarSweepStatus.Boundary&&hit.Time<acceptedEnd)acceptedEnd=hit.Time;
                }
                foreach(var load in currentTransfers)
                {
                    var hit=load.Sweep(byId,joints,paths,acceptedEnd,velocityTolerance,colliders,
                        positionTolerance,TransferSweepStage.Acceptance);
                    if(hit.Status==ScalarSweepStatus.Boundary&&hit.Time<acceptedEnd)acceptedEnd=hit.Time;
                }
                if(acceptedEnd<horizon)
                {
                    if(++restrictions>maximumRestrictions)throw new InvalidOperationException("Prediction horizon restriction budget exceeded.");
                    horizon=acceptedEnd;boundary=ForcePredictionBoundary.Field;continue;
                }
                // Return the iterate whose residual and exact paths were checked,
                // not F(iterate): a noncontractive map can amplify even a small
                // residual when applied once more after convergence.
                var motorUse=motorDrives.Length==0?Array.Empty<PredictedMotorUse>():new PredictedMotorUse[motorDrives.Length];
                for(var i=0;i<motorDrives.Length;i++)
                    motorUse[i]=motorDrives[i].Report(lastMotorRows[i],lastMotorResults[i],paths,horizon);
                // A branch receives 1/N of the interval allowance. Certification
                // reserves 1/4 for positive paired work and 1/32 for each of six
                // signed integration errors; residual work reserves another 1/8.
                // Their sum stays strictly below the complete-step limit.
                var transferWork=lastTransfers.Length==0?Array.Empty<PredictedTransferWork>():
                    lastTransfers.Select(transfer=>transfer.Certify(byId,paths,horizon,
                        declarations.TransferWorkTolerance*(horizon/completeStepDuration)/lastTransfers.Length)).ToArray();
                return new(horizon,boundary,candidate,paths,midpointEvaluations,newtonIterations,candidateState.Length,constraintWork,motorUse,transferWork,
                    lastTransfers.SelectMany(transfer=>transfer.BodyImpulses(horizon)).ToArray(),transferResidual,lastSpringConstraints);
            }
            lastError=error;
            // The last permitted update still owns a full acceptance check.
            // Exhaustion prevents another update, not inspection of its result.
            if(iteration==maximumIterations)break;
            iteration++;
            // An exactly representable force response can lie on a unilateral
            // work boundary. Test the fixed-point proposal itself before a
            // finite-difference Newton step can overshoot that boundary.
            var fixedProposal=Coordinates(next,candidateState.AsSpan(dynamicBodies.Length*6));
            lastProjectedReactions.CopyTo(fixedProposal,reactionOffset);
            var fixedResidual=Residual(fixedProposal);
            var fixedError=Math.Max(ResidualError(fixedResidual),lastReactionPhysicalResidual*10);
            if(fixedError<=error*.5)
            {
                candidateState=fixedProposal;continue;
            }
            newtonIterations=checked(newtonIterations+1);
            try { candidateState=NonlinearIteration.Advance(candidateState,Residual, []); }
            catch(InvalidOperationException failure)
            {
                var motorStates=string.Join("; ",motors.Select(motor=>
                    $"joint {motor.Command.Joint.Index}, speed {((PhysicsFrameJoint)joints.Single(joint=>joint.Id==motor.Command.Joint)).Motion.Speed:R}, target {motor.Command.TargetSpeed:R}, work {motor.Command.AvailableWork:R}"));
                var transferStates=string.Join("; ",currentTransfers.Select(load=>
                    $"branch {load.Id.Index}, source speed {load.Source.Speed(load.Source.Bind(byId,joints)):R}, receiver speed {load.Receiver.Bind(byId,joints).Speed:R}, exposed {load.Field?.IsExposed(byId,colliders)}"));
                string ComputeSupport(double[] state)
                {
                    var diagnosticPaths=Capture(Forces(state));
                    var midpoint=samples.Sample(diagnosticPaths,PredictionStage.Midpoint);
                    var descriptions=joints.SelectMany(joint=>joint.AccelerationConstraints(midpoint,positionTolerance,velocityTolerance)
                        .Select((row,index)=>
                        {
                            var terms=row.Gradient.Terms.ToArray().Select(term=>
                            {
                                var correction=diagnosticPaths[term.Body.Id].ConstraintVelocityTerms(term.Linear,term.Angular,horizon*.5);
                                return $"body {term.Body.Id.Index}, linear {term.Linear}, angular {term.Angular}, velocity {correction.Velocity:R}, acceleration {correction.Acceleration:R}, midpoint pose {term.Body.Pose}, inverse initial momentum {term.Body.InverseInertia(byId[term.Body.Id].AngularMomentum)}";
                            });
                            return $"joint {joint.Id.Index}, row {index}, relation {row.Relation}, terms [{string.Join("; ",terms)}]";
                        }));
                    static string DescribeGradient(ConstraintGradient gradient) => string.Join("; ", gradient.Terms.ToArray().Select(term => $"body {term.Body.Id.Index}, linear {term.Linear}, angular {term.Angular}, midpoint linear velocity {term.Body.LinearVelocity}, midpoint angular velocity {term.Body.AngularVelocity}, linear product {CollisionVector.Dot(term.Linear,term.Body.LinearVelocity):R}, angular product {CollisionVector.Dot(term.Angular,term.Body.AngularVelocity):R}"));
                    var contactDescriptions=contacts.SelectMany(contact=>contact.AccelerationContacts(midpoint,velocityTolerance,diagnosticPaths,horizon*.5)).Select(contact=>
                        $"regime {contact.Regime}, normal bias {contact.NormalBias:R}, tangent bias {contact.TangentBias}, physical slip {contact.PhysicalSlip}, friction {contact.Friction:R}, normal [{DescribeGradient(contact.Kinematics.NormalGradient)}], tangent U [{DescribeGradient(contact.Kinematics.TangentU)}], tangent V [{DescribeGradient(contact.Kinematics.TangentV)}]");
                    var friction=sliding.Select((feature,index)=>
                    {
                        try
                        {
                            var path=new ContactSlipPath(feature,diagnosticPaths);
                            var start=path.At(0).Slip;
                            var direction=start/start.Length;
                            var middle=path.At(horizon*.5).Slip;
                            var end=path.At(horizon).Slip;
                            var hit=ContactSlipSweep.Cast(path,horizon,velocityTolerance);
                            return $"feature {index}, bodies [{string.Join(", ",feature.Bodies.ToArray().Select(body=>body.Id.Index))}], source physical slip {start}, midpoint physical slip {middle}, endpoint physical slip {end}, projected midpoint {CollisionVector.Dot(middle,direction):R}, projected endpoint {CollisionVector.Dot(end,direction):R}, status {hit.Status}, time {hit.Time:R}, iterations {hit.Iterations}, velocity tolerance {velocityTolerance:R}";
                        }
                        catch(Exception unavailable)
                        {return $"feature {index}, status {SupportResidualStatus.Unavailable}, diagnostic error {unavailable.GetType().Name}: {unavailable.Message}";}
                    });
                    return $"state [{string.Join(", ",state)}], rows [{string.Join("; ",descriptions)}], contacts [{string.Join("; ",contactDescriptions)}], recomputed friction sweeps [{string.Join("; ",friction)}]";
                }
                string DescribeSupport(double[] state)
                {
                    try {return ComputeSupport(state);}
                    catch(Exception unavailable)
                    {return $"status {SupportResidualStatus.Unavailable}, state [{string.Join(", ",state)}], diagnostic error {unavailable.GetType().Name}: {unavailable.Message}";}
                }
                var bodyStates=string.Join("; ",owned.Select(body=>$"{body.Snapshot()}, inverse mass {body.InverseMass:R}, local inertia ({body.LocalInertia.XX:R},{body.LocalInertia.YY:R},{body.LocalInertia.ZZ:R},{body.LocalInertia.XY:R},{body.LocalInertia.XZ:R},{body.LocalInertia.YZ:R})"));
                var trialState=lastResidualState;var trialValues=lastResidualValue;
                var trialPhysical=trialValues.Length==reactionOffset+reactionCount?(double?)lastReactionPhysicalResidual:null;
                var candidateDiagnostic=DescribeSupport(candidateState);
                var candidateResidual=DescribeCandidateResidual(candidateState);
                var trialDiagnostic=DescribeSupport(trialState);
                var trialResidual=DescribeResidualGroups(trialState,trialValues);
                if(trialPhysical is double physical)trialResidual+=$", physical residual maximum {physical:R}";
                var rejectedTrials=failure is NonlinearLineSearchFailure lineSearch
                    ? $"original Newton solve [state [{string.Join(", ",lineSearch.State)}], residual [{string.Join(", ",lineSearch.Residual)}], direction [{string.Join(", ",lineSearch.Direction)}], row scale exponents [{string.Join(", ",lineSearch.RowScaleExponents)}], column scale exponents [{string.Join(", ",lineSearch.ColumnScaleExponents)}], numerical rank {lineSearch.NumericalRank}, equilibrated norm {lineSearch.EquilibratedNorm:R}, rank resolution {lineSearch.RankResolution:R}, Jacobian [{string.Join("; ",lineSearch.Jacobian.Select(row=>$"[{string.Join(", ",row)}]"))}]], full trial [{DescribeSupport(lineSearch.FullTrial.ToArray())}], full residual [{DescribeResidualGroups(lineSearch.FullTrial.ToArray(),lineSearch.FullResidual)}], full acceptance reevaluation [{DescribeCandidateResidual(lineSearch.FullTrial.ToArray())}], best trial [{DescribeSupport(lineSearch.BestTrial.ToArray())}], best residual [{DescribeResidualGroups(lineSearch.BestTrial.ToArray(),lineSearch.BestResidual)}], best acceptance reevaluation [{DescribeCandidateResidual(lineSearch.BestTrial.ToArray())}]"
                    : string.Empty;
                if(failure is NonlinearLineSearchFailure replayFailure)
                    rejectedTrials+=$", finite-difference replay [{DescribeProbeReplay(replayFailure)}]";
                // Isolated recomputed-chain trial: retain original failure and exact captured center.
                if(failure is NonlinearLineSearchFailure diagnosticFailure)
                {
                    double[] expected = [-6.866888557235923e-17,1.2339945703994255e-16,6.945002298650626e-17,2.550519409734138e-18,-1.313515843705235e-13,4.912981345511535e-13,-2.8502911298004985e-17,1.4618037998969916e-14,3.050726843878569e-16,-1.4200808400163811e-15,1.765162492533951e-16,4.371453032299506e-16,5.648673020590101e-17,-1.545756470638103e-16,-1.9437772629238222e-16,6.497713476300846e-17,-2.6148597035537617e-19,2.4333183389927894e-13,-0.31485089496785124,-0.020899074092400036,0.06213488480530672,0.20085977236628327,0.6392691080963937,1.2329704622701234,0.1014451042883268,1.079542358887305,-0.22073751492743166,0.002636054665098101,0.00010938963916636867,85.53295809713498,-1.879339431196583e-12,-1.1905265956601867e-15,2.452500104904173,-1.086875517646723e-15,-2.4891530076720073e-16,1.799195330430755e-17,-199.99999701976773,-9.996603944717455e-16,4.905000209808351,7.953485032341185e-15,-4.52261868605114e-19,3.1887719925541173e-18,-19.999999999999986,2.6925406806899974,-0.6765409385380822,0.1354035478188438,8.77064524299242,0.6898491172075181,0.002596694142255696,-19.999999999999982];
                    double[] center = [1.6433416170209623e-27,-1.636375614624478e-17,5.659993949151266e-28,8.00174074298267e-28,-6.069104799849054e-14,2.2650206659239113e-13,5.031858666812276e-17,-1.210832269440909e-14,-3.1029640520730818e-15,4.2474336656986215e-16,-4.072821995847372e-17,4.3997439325964083e-16,4.7691701196450305e-27,-2.478586820135159e-16,1.807162215006579e-27,8.456400003230618e-29,-4.66893341410079e-28,-3.1073383101170815e-14,-0.3140336011325701,-0.020827751985877285,0.06197816874214228,0.20048968196239028,0.6461181484729195,1.2329794594214112,0.5783559107333617,-1.3167006422048209,0.4715525623016425,0.04619324213906567,-0.13707090408719294,87.74223334681633,-1.8717555705191097e-12,3.7637645464416743e-26,2.452500104904175,-1.0222713994219198e-25,8.625147370519426e-16,1.2640544583846048e-16,-199.99999701976762,1.6529736559319743e-26,4.90500020980835,-2.0289612328581302e-25,1.0751494467229027e-29,3.619061642516965e-30,-19.999999999999982,5.135694463792762,1.532706047673185,0.1568258832291448,11.21353407139141,-1.473696100494686,0.4517098001010947,-19.999999999999982];
                    double[] expectedResidual = [-1.4726389862362503e-25,-1.636375614624478e-17,4.100405585585981e-25,-2.5330447786355975e-24,-6.069104799849054e-14,2.2650206659239113e-13,1.3652227398933629e-08,-4.1231199130209466e-08,-2.4328593954051403e-08,-3.877228261165277e-08,-1.7331275780472516e-08,5.531577025991227e-10,-2.3521132879349427e-26,-2.478586820135159e-16,4.0940657100163923e-25,-2.2214701721386502e-35,1.95989075391435e-34,-3.1073383101170815e-14,3.356559652445412e-08,2.4589097247923464e-08,4.539411964377127e-09,-2.3187322562012724e-08,3.766177036901297e-08,1.3601331394497151e-08,0.0,0.0,0.0,0.0,0.0,0.0,-1.2498847794507823e-13,1.6433416170209702e-26,0.0,5.65999394915106e-27,-2.4466550631854675e-27,6.672695626869457e-28,0.0,4.769170119645029e-26,0.0,1.8071622150065985e-26,4.496964326047826e-27,1.5137237977316112e-27,0.0,0.0,-4.7944362843634716e-08,4.542239274661356e-07,7.023910519876608e-07,-5.241471936726915e-07,9.057041860534887e-08,0.0];
                    double[] correctedTrial = [-1.0551457805844646e-18,-1.6206889928258338e-17,-1.2684783682988257e-20,2.532843708429176e-20,-6.06913362809421e-14,2.2650217383386124e-13,5.0315407606233567e-17,-1.210825904218483e-14,-3.1028974173342154e-15,4.2437844402388244e-16,-4.073132060622578e-17,4.400059546719549e-16,3.1033455815717e-20,-2.481333422504896e-16,-1.9857904404918832e-20,6.628917926732023e-21,-1.2105413957775427e-21,-3.1073088817687416e-14,-0.31403361192106727,-0.02082782085567768,0.06197815260949518,0.2004898642915268,0.6461182151856505,1.2329794876080749,0.5783559153804931,-1.3167005919354593,0.4715525495245423,0.04619324174834527,-0.13707090359896887,87.74223326184051,-1.859256717058195e-12,-3.87382374775629e-19,2.452500104904175,-3.468073314945779e-20,8.625338293363085e-16,1.2639969552897864e-16,-199.99999701976762,1.1489690182320278e-18,4.90500020980835,1.964514329127299e-18,5.654725503051664e-24,1.6100806651689168e-22,-19.999999999999982,5.135694415520313,1.5327060322189987,0.1568258921992132,11.213533981971437,-1.4736960605494263,0.45170979823648,-19.999999999999982];
                    if(!diagnosticFailure.State.Select(BitConverter.DoubleToInt64Bits).SequenceEqual(expected.Select(BitConverter.DoubleToInt64Bits)))
                        rejectedTrials += ", corrected chain trial [status identity mismatch]";
                    else
                    {
                        try
                        {
                            var actualCenter=Residual(center);
                            if(!actualCenter.Select(BitConverter.DoubleToInt64Bits).SequenceEqual(expectedResidual.Select(BitConverter.DoubleToInt64Bits)))
                                rejectedTrials += $", corrected chain trial [status center residual mismatch, center [{DescribeCandidateResidual(center)}]]";
                            else
                            {
                                var response=Residual(correctedTrial);
                                var trialForces=Forces(correctedTrial);
                                var evaluatedForces=AtMidpoint(Capture(trialForces),correctedTrial);
                                var trialError=Math.Max(ResidualError(response),lastReactionPhysicalResidual*10);
                                foreach(var body in owned)
                                {
                                    var force=evaluatedForces[body.Id].Force-trialForces[body.Id].Force;
                                    var torque=evaluatedForces[body.Id].Torque-trialForces[body.Id].Torque;
                                    trialError=Math.Max(trialError,Math.Max(force.Length*body.InverseMass,body.InverseInertia(torque).Length));
                                }
                                rejectedTrials += $", corrected chain trial [center residual status {SupportProbeStatus.Matched}, error {trialError:R}, tolerance {accelerationTolerance:R}, residual [{DescribeCandidateResidual(correctedTrial)}], support [{DescribeSupport(correctedTrial)}]]";
                            }
                        }
                        catch(Exception diagnosticFailureCause)
                        {
                            rejectedTrials += $", corrected chain trial [status {SupportResidualStatus.Unavailable}, diagnostic error {diagnosticFailureCause.GetType().Name}: {diagnosticFailureCause.Message}]";
                        }
                    }
                }
                throw new InvalidOperationException($"Support iteration failed at horizon {horizon:R}, requested {duration:R}, boundary {boundary}, restrictions {restrictions}, residual {error:R}, acceleration [{string.Join(", ",candidateState)}], motors [{motorStates}], transfers [{transferStates}], initial bodies [{bodyStates}], candidate [{candidateDiagnostic}], candidate coupled residual [{candidateResidual}], last evaluated trial [{trialDiagnostic}], trial coupled residual [{trialResidual}], rejected trials [{rejectedTrials}].",failure);
            }
        }
        throw new InvalidOperationException($"Coupled support prediction did not converge: horizon {horizon:R}, restrictions {restrictions}, last acceleration residual {lastError:R}, candidate coupled residual [{DescribeCandidateResidual(candidateState)}].");
    }

    public static IReadOnlyDictionary<PhysicsBodyId,BodyWrench> Solve(IEnumerable<PhysicsBody> bodies,
        IReadOnlyList<ConstraintAcceleration> constraints,IReadOnlyList<ContactForce> contacts,
        IReadOnlyDictionary<PhysicsBodyId,BodyWrench> loads,double tolerance,out ImpulseSolveResult work,
        IReadOnlyList<AccelerationDrive> drives,out IReadOnlyList<AccelerationDriveResult> driveResults,out double[] constraintEfforts,out ContactImpulse[] contactReactions)
    {
        ArgumentNullException.ThrowIfNull(bodies); ArgumentNullException.ThrowIfNull(constraints); ArgumentNullException.ThrowIfNull(contacts); ArgumentNullException.ThrowIfNull(loads);
        if(!double.IsFinite(tolerance)||tolerance<=0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        var owned=new Dictionary<PhysicsBodyId,PhysicsBody>();
        var scratch=new Dictionary<PhysicsBodyId,PhysicsBody>();
        var before=new Dictionary<PhysicsBodyId,PhysicsBodySnapshot>();
        foreach(var body in bodies)
        {
            ArgumentNullException.ThrowIfNull(body);
            if(!owned.TryAdd(body.Id,body)) throw new ArgumentException("Duplicate acceleration body identity.");
            if(!loads.TryGetValue(body.Id,out var load)) throw new ArgumentException("Every body requires an explicit load.");
            PhysicsBody state;
            if(body.MotionType==PhysicsMotionType.Dynamic)
            {
                var freeAngular=body.InverseInertia(load.Torque-CollisionVector.Cross(body.AngularVelocity,body.AngularMomentum));
                state=new(body.Id,body.MotionType,body.Pose,load.Force*body.InverseMass,freeAngular,
                    1/body.InverseMass,body.LocalInertia);
            }
            else
            {
                if(load!=default) throw new ArgumentException("Prescribed bodies cannot receive support loads.");
                state=new(body.Id,body.MotionType,body.Pose,body.PrescribedLinearAcceleration,body.PrescribedAngularAcceleration);
            }
            scratch.Add(body.Id,state); before.Add(body.Id,state.Snapshot());
        }
        if(loads.Count!=owned.Count) throw new ArgumentException("Loads contain a foreign body.");
        var rows=new List<IImpulseConstraint>();
        var bilateral=new List<ImpulseConstraint>();
        var constraintRows=new List<ImpulseConstraint>();
        var contactRows=new List<ContactConstraint>();
        foreach(var constraint in constraints)
        {
            ArgumentNullException.ThrowIfNull(constraint);
            var terms=new List<ConstraintTerm>();
            foreach(var term in constraint.Gradient.Terms)
            {
                if(!owned.TryGetValue(term.Body.Id,out var body)||body!=term.Body)
                    throw new ArgumentException("Acceleration row refers to a foreign body state.");
                terms.Add(new(scratch[body.Id],term.Linear,term.Angular));
            }
            var (lower,upper)=constraint.Relation switch
            {
                AccelerationRelation.Equal=>(double.NegativeInfinity,double.PositiveInfinity),
                AccelerationRelation.Nonnegative=>(0.0,double.PositiveInfinity),
                AccelerationRelation.Nonpositive=>(double.NegativeInfinity,0.0),
                _=>throw new ArgumentOutOfRangeException(nameof(constraint))
            };
            var row=new ImpulseConstraint(new(terms.ToArray()),-constraint.ConvectiveAcceleration,lower,upper);
            constraintRows.Add(row);
            if(constraint.Relation==AccelerationRelation.Equal) bilateral.Add(row);
            else rows.Add(row);
        }
        if(bilateral.Count>0) rows.Add(new BilateralConstraintBlock(bilateral));
        foreach(var contact in contacts)
        {
            ArgumentNullException.ThrowIfNull(contact);
            foreach(var participant in contact.Kinematics.Bodies)
                if(!owned.TryGetValue(participant.Id,out var body)||body!=participant)
                    throw new ArgumentException("Contact force refers to a foreign physical state.");
            var row=contact.Bind(scratch);contactRows.Add(row);rows.Add(row);
        }
        ArgumentNullException.ThrowIfNull(drives);
        var driveRows=drives.Count==0?Array.Empty<(AccelerationDrive Drive,ImpulseConstraint Row)>():
            new (AccelerationDrive Drive,ImpulseConstraint Row)[drives.Count];
        if(drives.Count>0)
        {
            var driveIds=new HashSet<PhysicsDriveId>();
            foreach(var drive in drives)
            {
                ArgumentNullException.ThrowIfNull(drive);
                if(!driveIds.Add(drive.Id)) throw new ArgumentException("Duplicate acceleration drive identity.");
            }
        }
        var driveIndex=0;
        IEnumerable<AccelerationDrive> orderedDrives=drives;
        if(drives.Count>0) orderedDrives=drives.OrderBy(drive=>drive.Id.Index);
        foreach(var drive in orderedDrives)
        {
            var terms=new List<ConstraintTerm>();
            var dynamicParticipant=false;
            foreach(var term in drive.Gradient.Terms)
            {
                if(!owned.TryGetValue(term.Body.Id,out var body)||body!=term.Body)
                    throw new ArgumentException("Acceleration drive refers to a foreign body state.");
                dynamicParticipant|=body.MotionType==PhysicsMotionType.Dynamic;
                terms.Add(new(scratch[body.Id],term.Linear,term.Angular));
            }
            if(!dynamicParticipant) throw new ArgumentException("Acceleration drive requires a dynamic participant.");
            var row=new ImpulseConstraint(new(terms.ToArray()),drive.TargetAcceleration-drive.ConvectiveAcceleration,
                drive.MinimumEffort,drive.MaximumEffort);
            if(row.InverseEffectiveMass==0) throw new ArgumentException("Acceleration drive requires nonzero dynamic response.");
            driveRows[driveIndex++]=(drive,row); rows.Add(row);
        }
        try { work=ImpulseSolver.Solve(rows,tolerance:tolerance); }
        catch(InvalidOperationException failure)
        {
            static string GradientInput(ConstraintGradient gradient)=>string.Join("; ",gradient.Terms.ToArray().Select(term=>
                $"body {term.Body.Id.Index}, linear {term.Linear}, angular {term.Angular}"));
            var inputBodies=string.Join("; ",before.OrderBy(pair=>pair.Key.Index).Select(pair=>
            {
                var body=scratch[pair.Key];var inertia=body.LocalInertia;
                return $"{pair.Value}, inverse mass {body.InverseMass:R}, local inertia ({inertia.XX:R},{inertia.YY:R},{inertia.ZZ:R},{inertia.XY:R},{inertia.XZ:R},{inertia.YZ:R})";
            }));
            var inputRows=string.Join("; ",rows.Select((row,index)=>
                $"row {index}, kind {row.GetType().Name}, scalars [{string.Join("; ",row.ScalarRows.Select(scalar=>
                    $"target {scalar.TargetSpeed:R}, lower {scalar.MinimumImpulse:R}, upper {scalar.MaximumImpulse:R}, softness {scalar.Softness:R}, gradient [{GradientInput(scalar.Gradient)}]"))}]"));
            var inputContacts=string.Join("; ",contacts.Select(contact=>
                $"regime {contact.Regime}, friction {contact.Friction:R}, normal {contact.Kinematics.Normal}, bias {contact.NormalBias:R}, tangent bias {contact.TangentBias}, physical slip {contact.PhysicalSlip}, normal gradient [{GradientInput(contact.Kinematics.NormalGradient)}], tangent U [{GradientInput(contact.Kinematics.TangentU)}], tangent V [{GradientInput(contact.Kinematics.TangentV)}]"));
            throw new InvalidOperationException($"Acceleration constraint input: tolerance {tolerance:R}, bodies [{inputBodies}], rows [{inputRows}], contacts [{inputContacts}].",failure);
        }
        constraintEfforts=constraintRows.Select(row=>row.AccumulatedImpulse).ToArray();
        contactReactions=contactRows.Select(row=>row.Impulse).ToArray();
        driveResults=driveRows.Length==0?Array.Empty<AccelerationDriveResult>():
            Array.AsReadOnly(driveRows.Select(entry=>new AccelerationDriveResult(entry.Drive.Id,
                entry.Row.AccumulatedImpulse,entry.Row.Speed+entry.Drive.ConvectiveAcceleration,
                entry.Drive.MinimumEffort==0&&entry.Drive.MaximumEffort==0?DriveEffortLimit.Disabled:
                entry.Row.AccumulatedImpulse==entry.Drive.MinimumEffort?DriveEffortLimit.Lower:
                entry.Row.AccumulatedImpulse==entry.Drive.MaximumEffort?DriveEffortLimit.Upper:DriveEffortLimit.None)).ToArray());
        var result=new Dictionary<PhysicsBodyId,BodyWrench>();
        foreach(var body in owned.Values)
        {
            var load=loads[body.Id]; var state=scratch[body.Id]; var initial=before[body.Id];
            result.Add(body.Id,body.MotionType==PhysicsMotionType.Dynamic?
                new(load.Force+(state.LinearVelocity-initial.LinearVelocity)/body.InverseMass,
                    load.Torque+(state.AngularMomentum-initial.AngularMomentum)):default);
        }
        return new ReadOnlyDictionary<PhysicsBodyId,BodyWrench>(result);
    }
}
