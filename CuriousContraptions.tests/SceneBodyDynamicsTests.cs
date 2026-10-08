using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SceneBodyDynamicsTests(NativeSceneFixture godot)
{
    public enum Component { Ball, Weight, Spring, Lever, Gate, Shutter }
    private static string Catalogue(Component kind)=>kind switch
    {
        Component.Ball=>"ball",Component.Weight=>"weight",Component.Spring=>WoundSpringPart.CatalogId,
        Component.Lever=>ImpactLeverPart.CatalogId,Component.Gate=>"powered_gate",Component.Shutter=>"beam_shutter",
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0}; godot.Tree.Root.AddChild(world); return world;
    }
    private static MachinePart Add(MachineWorld world,Component kind)
    {
        var name=Catalogue(kind);
        return world.AddPart(new(){Id=name,Kind=name,Position=[0,8,0]});
    }

    [Theory]
    [InlineData(Component.Ball)]
    [InlineData(Component.Weight)]
    [InlineData(Component.Spring)]
    public void MassEnvelopesDeclareDynamicsEvenWhenGuidedOrLatched(Component kind)
    {
        var world=World();
        try
        {
            var part=Add(world,kind);
            var moving=kind==Component.Spring?Assert.Single(part.InternalBodies):part;
            moving.InitialVelocity=new(.2f,.3f,.4f);
            var captured=WorldGeometry.CapturePhysicsBodies(world);
            var declaration=Assert.Single(captured,d=>d.Geometry.Owner==moving&&d.Geometry.Slot==MachinePart.RootBody);
            Assert.Equal(PhysicsMotionType.Dynamic,declaration.Dynamics.Motion);
            Assert.Equal(moving.InitialContactMaterial,declaration.Material);
            Assert.Equal(moving.Mass,declaration.Dynamics.Mass);
            var inertia=.4*(double)moving.Mass*moving.Radius*moving.Radius;
            Assert.Equal(new InertiaTensor(inertia,inertia,inertia),declaration.Dynamics.Inertia);
            var owned=declaration.CreateBody(new(17));
            Assert.Equal(declaration.Geometry.Pose,owned.Pose);
            Assert.Equal(SceneGeometryAdapter.CaptureVector(moving.InitialVelocity),owned.LinearVelocity);
            Assert.Equal(PhysicsMotionType.Static,Assert.Single(captured,d=>d.Geometry.Owner is null).Dynamics.Motion);
            moving.InitialVelocity=Vector3.Zero;
            Assert.Equal(new CollisionVector(.2f,.3f,.4f),owned.LinearVelocity);
            Assert.Equal(owned.LinearVelocity,declaration.Dynamics.LinearVelocity);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(Component.Ball)]
    [InlineData(Component.Weight)]
    [InlineData(Component.Spring)]
    public void InitialConditionsCannotMutateCapturedBodiesEvenWhilePaused(Component kind)
    {
        var world=World();
        try
        {
            var part=Add(world,kind);
            var moving=kind==Component.Spring?Assert.Single(part.InternalBodies):part;
            moving.InitialVelocity=Vector3.Zero;
            world.Start();
            var body=world.PhysicsAssembly.Body(new(moving,MachinePart.RootBody));
            var before=body.Snapshot();
            Assert.Throws<InvalidOperationException>(()=>moving.InitialVelocity=Vector3.Right);
            world.Running=false;
            Assert.Throws<InvalidOperationException>(()=>moving.InitialVelocity=Vector3.Right);
            Assert.Equal(before,body.Snapshot());
            world.Restore();
            var reset=world.FindPart(part.Uid)!;
            var restored=kind==Component.Spring?Assert.Single(reset.InternalBodies):reset;
            restored.InitialVelocity=Vector3.Up;
            Assert.Equal(SceneGeometryAdapter.CaptureVector(Vector3.Up),restored.InitialBodyDynamics.LinearVelocity);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(Component.Ball)]
    [InlineData(Component.Weight)]
    [InlineData(Component.Spring)]
    public void ResetRestoresInitialMotionInsteadOfLastRuntimeVelocity(Component kind)
    {
        var world=World();
        try
        {
            var part=Add(world,kind);
            var moving=kind==Component.Spring?Assert.Single(part.InternalBodies):part;
            var initial=new Vector3(.2f,-.3f,.4f);
            moving.InitialVelocity=initial;
            world.Start();
            var body=world.PhysicsAssembly.Body(new(moving,MachinePart.RootBody));
            Assert.Equal(SceneGeometryAdapter.CaptureVector(initial),body.LinearVelocity);
            world.Physics.ApplyImpulse(body.Id,new(.5,1,.2),body.Center);
            FixtureParts.PresentCaptured(world);
            Assert.NotEqual(SceneGeometryAdapter.CaptureVector(initial),body.LinearVelocity);
            world.Running=false;
            world.Restore();
            var restored=world.FindPart(Catalogue(kind))!;
            moving=kind==Component.Spring?Assert.Single(restored.InternalBodies):restored;
            Assert.Equal(initial,moving.InitialVelocity);
            Assert.Throws<InvalidOperationException>(()=>world.Physics);
            world.Start();
            Assert.Equal(SceneGeometryAdapter.CaptureVector(initial),world.PhysicsAssembly.Body(new(moving,MachinePart.RootBody)).LinearVelocity);
            world.Restore();
            restored=world.FindPart(Catalogue(kind))!;
            moving=kind==Component.Spring?Assert.Single(restored.InternalBodies):restored;
            Assert.Equal(initial,moving.InitialVelocity);
            // A different construction must not inherit the previous machine's native inputs.
            world.LoadMachine(new());
            var fresh=Add(world,kind);
            var freshBody=kind==Component.Spring?Assert.Single(fresh.InternalBodies):fresh;
            Assert.Equal(Vector3.Zero,freshBody.InitialVelocity);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(Component.Ball)]
    [InlineData(Component.Weight)]
    [InlineData(Component.Spring)]
    public void SavedConstructionPreservesAuthoredMotionThroughLoadAndReset(Component kind)
    {
        var world=World();
        try
        {
            var part=Add(world,kind);
            var initial=new Vector3(.2f,-.3f,.4f);
            MachinePart Moving(MachinePart owner)=>kind==Component.Spring?owner.InternalBody(InternalBodyRole.Plunger):owner;
            float[] SavedVelocity(MachineData data)
            {
                var specification=Assert.Single(data.Parts);
                if(kind!=Component.Spring) return specification.InitialVelocity;
                var bodySpec=Assert.Single(specification.InternalBodies);
                Assert.Equal(InternalBodyRole.Plunger,bodySpec.Role);
                return bodySpec.InitialVelocity;
            }
            Moving(part).InitialVelocity=initial;
            var saved=MachineCodec.Clone(world.Snapshot());
            Assert.Equal(new[]{initial.X,initial.Y,initial.Z},SavedVelocity(saved));
            world.Start();
            var body=world.PhysicsAssembly.Body(new(Moving(part),MachinePart.RootBody));
            world.Physics.ApplyImpulse(body.Id,new(.5,1,.2),body.Center);
            Assert.Equal(SavedVelocity(saved),SavedVelocity(world.Snapshot()));
            world.LoadMachine(saved);
            part=Assert.Single(world.Parts);
            Assert.Equal(initial,Moving(part).InitialVelocity);
            world.Start();
            Assert.Equal(SceneGeometryAdapter.CaptureVector(initial),world.PhysicsAssembly.Body(new(Moving(part),MachinePart.RootBody)).LinearVelocity);
            world.Step();
            world.Restore();
            Assert.Equal(initial,Moving(Assert.Single(world.Parts)).InitialVelocity);
            Assert.Equal(SavedVelocity(saved),SavedVelocity(world.Snapshot()));
        }
        finally {world.Free();}
    }

    [Fact]
    public void InvalidSerializedInitialMotionCannotAddAPart()
    {
        var world=World();
        try
        {
            foreach(var value in new float[][]{null!,[],[1,2],[1,2,3,4],[float.NaN,0,0],[0,float.PositiveInfinity,0],[0,0,float.NegativeInfinity]})
            {
                Assert.Throws<ArgumentException>(()=>world.AddPart(new(){
                    Id=Catalogue(Component.Ball),Kind=Catalogue(Component.Ball),InitialVelocity=value}));
                Assert.Empty(world.Parts);
            }
        }
        finally {world.Free();}
    }

    [Fact]
    public void InternalBodyWireRolesHaveOnlyCanonicalNames()
    {
        var options=new JsonSerializerOptions();
        options.Converters.Add(new InternalBodyRoleJsonConverter());
        Assert.Equal("\"plunger\"",JsonSerializer.Serialize(InternalBodyRole.Plunger,options));
        Assert.Equal(InternalBodyRole.Plunger,JsonSerializer.Deserialize<InternalBodyRole>("\"plunger\"",options));
        foreach(var json in new[]{"\"Plunger\"","\"unknown\"","0","null","\"0\""})
            Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<InternalBodyRole>(json,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((InternalBodyRole)99,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<InternalBodySpec>("{}",options));
    }

    [Fact]
    public void InvalidInternalMotionRejectsLoadWithoutReplacingConstruction()
    {
        var world=World();
        try
        {
            var original=Add(world,Component.Ball);
            void Reject(Component kind,List<InternalBodySpec> bodies)
            {
                var specification=new PartSpec {Id=Catalogue(kind),Kind=Catalogue(kind),InternalBodies=bodies};
                Assert.ThrowsAny<ArgumentException>(()=>world.AddPart(specification));
                Assert.Same(original,Assert.Single(world.Parts));
                var failure=Record.Exception(()=>world.LoadMachine(new(){Parts=[specification]}));
                Assert.True(failure is ArgumentException or JsonException);
                Assert.Same(original,Assert.Single(world.Parts));
            }
            Reject(Component.Ball,[new(){Role=InternalBodyRole.Plunger}]);
            Reject(Component.Spring,[new(){Role=(InternalBodyRole)99}]);
            Reject(Component.Spring,[new(){Role=InternalBodyRole.Plunger},new(){Role=InternalBodyRole.Plunger}]);
            Reject(Component.Spring,null!);
            Reject(Component.Spring,[null!]);
            foreach(var value in new float[][]{null!,[],[1,2],[1,2,3,4],[float.NaN,0,0],[0,float.PositiveInfinity,0]})
                Reject(Component.Spring,[new(){Role=InternalBodyRole.Plunger,InitialVelocity=value}]);
        }
        finally {world.Free();}
    }

    [Fact]
    public void SharedVelocityChangesWithoutPresentationAndNeverRewritesInitialMotion()
    {
        var world=World();
        try
        {
            var ball=Add(world,Component.Ball);
            ball.InitialVelocity=Vector3.Right;
            Assert.Throws<InvalidOperationException>(()=>world.Physics);
            world.Start();
            var body=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            Assert.Equal(new CollisionVector(1,0,0),body.LinearVelocity);
            world.Physics.ApplyImpulse(body.Id,new(0,ball.Mass,0),body.Center);
            Assert.Equal(new CollisionVector(1,1,0),body.LinearVelocity); // Presentation has not run.
            FixtureParts.PresentCaptured(world);
            Assert.Equal(new CollisionVector(1,1,0),body.LinearVelocity);
            Assert.Equal(Vector3.Right,ball.InitialVelocity);
            Assert.Equal(new CollisionVector(1,0,0),ball.InitialBodyDynamics.LinearVelocity);
            Assert.Equal(new CollisionVector(1,1,0),body.LinearVelocity);
        }
        finally {world.Free();}
    }

    [Fact]
    public void NonFiniteInitialVelocityIsRejectedBeforeCapture()
    {
        var world=World();
        try
        {
            var ball=Add(world,Component.Ball);
            foreach(var invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
                Assert.Throws<ArgumentOutOfRangeException>(()=>ball.InitialVelocity=new(invalid,0,0));
            Assert.Equal(Vector3.Zero,ball.InitialVelocity);
        }
        finally {world.Free();}
    }

    [Fact]
    public void RotatedBeamDeclaresFullBodyLocalInertiaAndStartsAtRest()
    {
        var world=World();
        try
        {
            var lever=(ImpactLeverPart)Add(world,Component.Lever);
            lever.Rotation=new(.3f,.4f,.5f);
            var declared=WorldGeometry.CapturePhysicsBodies(world);
            var beam=Assert.Single(declared,d=>d.Geometry.Slot==ImpactLeverPart.BeamBody);
            var mass=(double)lever.ReadParameter(ImpactLeverParameter.BeamMass);
            var half=ImpactLeverPart.BeamHalf;
            Assert.Equal(PhysicsMotionType.Dynamic,beam.Dynamics.Motion);
            Assert.Equal(mass,beam.Dynamics.Mass);
            Assert.Equal(mass*((double)half.Y*half.Y+(double)half.Z*half.Z)/3,beam.Dynamics.Inertia.XX);
            Assert.Equal(mass*((double)half.X*half.X+(double)half.Z*half.Z)/3,beam.Dynamics.Inertia.YY);
            Assert.Equal(mass*((double)half.X*half.X+(double)half.Y*half.Y)/3,beam.Dynamics.Inertia.ZZ);
            Assert.Equal(default,beam.Dynamics.AngularVelocity);
            var body=beam.CreateBody(new(2));
            Assert.InRange((body.AngularVelocity-beam.Dynamics.AngularVelocity).Length,0,1e-12);
            Assert.Equal(PhysicsMotionType.Static,Assert.Single(declared,
                d=>d.Geometry.Owner==lever&&d.Geometry.Slot==MachinePart.RootBody).Dynamics.Motion);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(Component.Gate)]
    [InlineData(Component.Shutter)]
    public void BladeUsesFiniteMassAndSharedMotorVelocityAndResetRestoresIt(Component kind)
    {
        var world=World();
        try
        {
            var part=Add(world,kind); part.Rotation=new(.3f,.4f,.5f);
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-6,2,4]});
            Assert.True(world.Connect(battery,part));
            var slot=kind==Component.Gate?PoweredGatePart.BladeBody:BeamShutterPart.BladeBody;
            var before=WorldGeometry.CapturePhysicsBodies(world).Single(d=>d.Geometry.Owner==part&&d.Geometry.Slot==slot);
            world.Start();
            for(var i=0;i<12;i++)world.Step();
            var current=world.PhysicsAssembly.Body(new(part,slot));
            var speed=part switch
            {
                PoweredGatePart gate=>gate.BladeSpeed,BeamShutterPart shutter=>shutter.BladeSpeed,
                _=>throw new InvalidOperationException("Unexpected test actuator.")
            };
            Assert.True(speed>0);
            Assert.Equal(PhysicsMotionType.Dynamic,current.MotionType);
            Assert.Equal(SlidingBlade.Mass,1/current.InverseMass);
            Assert.True(before.Dynamics.Inertia.IsPositiveDefinite);
            Assert.InRange((SceneGeometryAdapter.CaptureVector(part.Basis.Y*speed)-current.LinearVelocity).Length,0,1e-6);
            Assert.NotEqual(before.Geometry.Pose,current.Pose);
            Assert.InRange(current.AngularVelocity.Length,0,1e-8);
            Assert.Equal(default,before.Dynamics.LinearVelocity);
            world.Restore(); part=world.FindPart(Catalogue(kind))!;
            var restored=WorldGeometry.CapturePhysicsBodies(world).Single(d=>d.Geometry.Owner==part&&d.Geometry.Slot==slot);
            Assert.Equal(before.Geometry.Pose,restored.Geometry.Pose);
            Assert.Equal(before.Dynamics,restored.Dynamics);
        }
        finally {world.Free();}
    }

    private partial class UndeclaredDynamicPart : MachinePart
    {
        public UndeclaredDynamicPart()
        {
            Dynamic=true;
            Boxes.Add(new(default,Vector3.One,RootBody));
        }
    }

    [Fact]
    public void MissingMassDeclarationAndNullProviderResultReject()
    {
        using var scene=new GeometryQueryScene();
        var undeclared=new UndeclaredDynamicPart();
        try
        {
            FixtureParts.Attach(scene.World,undeclared,FixturePartId.Second);
            Assert.Throws<InvalidOperationException>(()=>WorldGeometry.CapturePhysicsBodies(scene.World));
            scene.World.RemovePart(undeclared);
            var slot=new BodySlot(p=>global::CuriousContraptions.Geometry.RigidPose.Identity,_=>null!,BodyQueryPolicy.Include,_=>new(0,0,0),_=>[]);
            scene.Part.Boxes.Add(new(default,Vector3.One,slot));
            Assert.Throws<InvalidOperationException>(()=>WorldGeometry.CapturePhysicsBodies(scene.World));
        }
        finally {if(GodotObject.IsInstanceValid(undeclared) && undeclared.GetParent() is null) undeclared.Free();}
    }

    [Fact]
    public void SlotMaterialIsCapturedOnceWithoutOwnerWideSubstitution()
    {
        using var scene=new GeometryQueryScene();
        scene.Part.Boxes.Add(new(default, Vector3.One * .1f, MachinePart.RootBody));
        var material=new ContactMaterial(.23,.17,.41);
        var slot=new BodySlot(p=>global::CuriousContraptions.Geometry.RigidPose.Identity,
            _=>new(PhysicsMotionType.Static,0,default,default,default),
            BodyQueryPolicy.Include,_=>material,_=>[]);
        scene.Part.Boxes.Add(new(new(0,4,0),Vector3.One*.2f,slot));
        var assembly=WorldGeometry.CapturePhysicsAssembly(scene.World,RopeNetwork.Build(scene.World.Parts,scene.World.Connections));
        var body=assembly.Body(new(scene.Part,slot));
        var obj=Assert.Single(assembly.Objects.ToArray(),o=>o.Body==body);
        Assert.Equal(material,obj.Material);
        material=new(.91,.28,.02);
        Assert.NotEqual(material,obj.Material);
        var recaptured=WorldGeometry.CapturePhysicsAssembly(scene.World,RopeNetwork.Build(scene.World.Parts,scene.World.Connections));
        var next=recaptured.Body(new(scene.Part,slot));
        Assert.Equal(material,Assert.Single(recaptured.Objects.ToArray(),o=>o.Body==next).Material);
    }

    [Fact]
    public void CapturedBallAndWorkbenchRunInOnePersistentSharedWorldAndReplayExactly()
    {
        var scene=World();
        try
        {
            var part=Add(scene,Component.Ball); part.Position=new(0,2,0);
            var assembly=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            var objects=assembly.Objects.ToArray();
            for(var i=0;i<objects.Length;i++)
            {
                Assert.Same(assembly.Bodies[i],objects[i].Body);
                Assert.Same(assembly.Declarations[i].Geometry.Geometry,objects[i].Geometry);
                Assert.Equal(assembly.Declarations[i].Material,objects[i].Material);
            }
            Assert.Equal(part.InitialContactMaterial,objects.Single(o=>o.Body.MotionType==PhysicsMotionType.Dynamic).Material);
            var ball=objects.Single(o=>o.Body.MotionType==PhysicsMotionType.Dynamic).Body;
            var physics=new PhysicsWorld([],objects,[],new(new(0,-9.81,0)));
            var initial=physics.Capture();
            var impacted=false;
            void Run()
            {
                for(var i=0;i<180;i++)
                {
                    physics.Step([],[],1.0/120);
                    impacted|=physics.Impacts.Length>0;
                    Assert.True(ball.Center.Y>=-.46+part.Radius-.00001);
                }
            }
            Run();
            Assert.True(impacted);
            var final=physics.Capture();
            Assert.Equal((ulong)180,physics.StepIndex);
            physics.Restore(initial);
            Run();
            Assert.Equal(final.BodyStates.ToArray(),physics.Capture().BodyStates.ToArray());
            Assert.Equal(new Vector3(0,2,0),part.Position); // This is a declaration test, not the production cutover.
        }
        finally {scene.Free();}
    }

    [Fact]
    public void PoseCaptureValuesStayIsolatedUntilExplicitRecapture()
    {
        using var scene = new GeometryQueryScene();
        scene.Part.Boxes.Add(new(default, Vector3.One * .1f, MachinePart.RootBody));
        BodySlot Slot() => new(_ => RigidPose.Identity,
            _ => new(PhysicsMotionType.Static, 0, default, default, default),
            BodyQueryPolicy.Include, _ => new(0, 0, 0), _ => []);
        var first = Slot(); var second = Slot();
        scene.Part.Boxes.Add(new(new(0, 4, 0), Vector3.One * .2f, first));
        scene.Part.Boxes.Add(new(new(0, 6, 0), Vector3.One * .2f, second));
        var assembly = WorldGeometry.CapturePhysicsAssembly(scene.World,
            RopeNetwork.Build(scene.World.Parts, scene.World.Connections));
        var body = assembly.Body(new(scene.Part, second));
        var reads = assembly.CapturePresentationReads();
        var retained = reads[body.Id.Index];
        body.Restore(body.Snapshot() with { Pose = RigidPose.At(new(10, 11, 12)) });
        Assert.Equal(retained.Pose, reads[body.Id.Index].Pose);
        Assert.NotEqual(body.Pose, retained.Pose);
        var next = assembly.CapturePresentationReads();
        Assert.Equal(body.Pose, next[body.Id.Index].Pose);
        Assert.NotEqual(next[body.Id.Index].Pose, retained.Pose);
    }

    [Fact]
    public void FailedPoseMappingLeavesSceneUntouchedAndAllowsRetry()
    {
        using var scene = new GeometryQueryScene();
        scene.Part.Boxes.Add(new(default, Vector3.One * .1f, MachinePart.RootBody));
        var target = new Node3D(); scene.Part.AddChild(target);
        var map = Presentation.ScenePoseMap.AxisAffine(Presentation.PoseReadSpace.World,
            Presentation.PoseMapAxis.X, RigidRotation.Identity,
            default, default, new(0, 1, 1), new(1, 0, 0));
        var slot = new BodySlot(_ => RigidPose.Identity,
            _ => new(PhysicsMotionType.Static, 0, default, default, default),
            BodyQueryPolicy.Include, _ => new(0, 0, 0), _ => [new(target, new(new(scene.Part,MachinePart.RootBody),RigidPose.Identity), map)]);
        scene.Part.Boxes.Add(new(new(0, 4, 0), Vector3.One * .2f, slot));
        var baseline = scene.Part.Transform;
        var assembly = WorldGeometry.CapturePhysicsAssembly(scene.World,
            RopeNetwork.Build(scene.World.Parts, scene.World.Connections));
        scene.Part.Position = new(9, 10, 11);
        var altered = scene.Part.Transform;
        Assert.Throws<ArgumentException>(() => FixtureParts.PresentUnowned(assembly));
        Assert.Equal(altered, scene.Part.Transform);
        var body = assembly.Body(new(scene.Part, slot));
        body.Restore(body.Snapshot() with { Pose = RigidPose.At(new(1, 4, 0)) });
        FixtureParts.PresentUnowned(assembly);
        Assert.Equal(baseline, scene.Part.Transform);
        Assert.Equal(0, assembly.LastPresentation.ColourWrites);
        assembly.RemovePresentation();
        Assert.Throws<InvalidOperationException>(() => FixtureParts.PresentUnowned(assembly));
    }

    private partial class OffsetReferencePart : MachinePart
    {
        public override Vector3 LocalCenterOfMass => new(.25f, -.5f, .75f);
    }

    [Fact]
    public void CapturedReferenceUsesRootPhysicsAndAuthoredCenterOffset()
    {
        using var scene = new GeometryQueryScene();
        var part = new OffsetReferencePart();
        FixtureParts.Attach(scene.World, part, FixturePartId.Second);
        part.Transform = new(new Basis(Vector3.Up, .4f), new(3, 5, 7));
        part.Boxes.Add(new(default, Vector3.One * .1f, MachinePart.RootBody));
        var local = RigidPose.At(new(1, 2, 3));
        var child = new BodySlot(_ => local,
            _ => new(PhysicsMotionType.Static, 0, default, default, default),
            BodyQueryPolicy.Include, _ => new(0, 0, 0), _ => []);
        part.Boxes.Add(new(default, Vector3.One * .1f, child));
        var expected = SceneGeometryAdapter.CaptureRigidPose(part.Transform);
        var assembly = WorldGeometry.CapturePhysicsAssembly(scene.World,
            RopeNetwork.Build(scene.World.Parts, scene.World.Connections));
        part.Transform = new(new Basis(Vector3.Right, .7f), new(-10, -12, 14));
        FixtureParts.PresentUnowned(assembly);
        var read = assembly.CapturePresentationReads()[assembly.Body(new(part,child)).Id.Index];
        Assert.InRange((read.ReferencePose.Center - expected.Center).Length, 0, 1e-12);
        Assert.InRange((read.RelativePose.Center - local.Center).Length, 0, 1e-12);
        Assert.InRange((part.Position - expected.ToScene().Origin).Length(), 0, 1e-6f);
        var body = assembly.Body(new(part, MachinePart.RootBody));
        body.Restore(body.Snapshot() with { Pose = new(body.Pose.Center + new CollisionVector(2, 0, 0),
            body.Pose.Rotation) });
        var retained = read;
        FixtureParts.PresentUnowned(assembly);
        read = assembly.CapturePresentationReads()[assembly.Body(new(part,child)).Id.Index];
        Assert.InRange((read.ReferencePose.Center - expected.Center - new CollisionVector(2, 0, 0)).Length, 0, 1e-12);
        Assert.Equal(expected.Center, retained.ReferencePose.Center);
    }

    [Fact]
    public void MissingOwnerRootReferenceRejectsExplicitly()
    {
        using var scene = new GeometryQueryScene();
        var child = new BodySlot(_ => RigidPose.Identity,
            _ => new(PhysicsMotionType.Static, 0, default, default, default),
            BodyQueryPolicy.Include, _ => new(0, 0, 0), _=>[]);
        scene.Part.Boxes.Add(new(default, Vector3.One * .1f, child));
        Assert.Throws<ArgumentException>(() => WorldGeometry.CapturePhysicsAssembly(scene.World,
            RopeNetwork.Build(scene.World.Parts, scene.World.Connections)));
    }
}
