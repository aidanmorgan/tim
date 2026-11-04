using System;
using System.Linq;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;
using Xunit;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 9A.46c: Start articulated parts in their declared joint frames.
/// Verifies generic frame composition algebra for prismatic sliders and revolute hinges,
/// zero initial constraint error and zero pose jumps on startup, and bundle validator refusals.
/// </summary>
public sealed class JointFrameTests
{
    private static WorkshopConstruction Free(params IWorkshopInstance[] instances) =>
        new(new(1), WorkshopCadenceSettings.Default(), new(instances));

    [Fact]
    public void IdentityFramesRegression_SliderAndHinge_MatchExistingBehaviors()
    {
        // 1. Springboard regression: identity frames with 0.15m compression
        var springId = new GpuBodyId(1);
        var springBench = Free(PlacedParts.Springboard(springId, 0, 1, 0, 0, 0, 0, 1, compression: 0.15f));
        var springScene = WorkshopPhysicsCompiler.Compile(springBench, new(1, 2));
        var springJoint = Assert.Single(springScene.Prismatics.ToArray());
        var springMoving = springScene.Bodies.ToArray().Single(b => b.Id == springJoint.BodyB);

        // Springboard has BaseFrame=(0, 0.14, 0), MovingFrame=(0,0,0)
        // With owner at (0, 1, 0), anchorA is (0, 1.14, 0).
        // With compression 0.15, PB is (0, 0.99, 0).
        var springMovingY = (springMoving.Cell.Y + springMoving.Local.Y) / 16f;
        Assert.InRange(MathF.Abs(springMovingY - 0.99f), 0f, 1e-5f);
        Assert.Equal(CanonicalRotation.Identity, springMoving.Rotation);

        // 2. Motor regression: identity frames with initial angle 0
        var motorId = new GpuBodyId(2);
        var motorBench = Free(WorkshopInput.Generic(new PartId("motor"), motorId, 0, 1, 0, 0, 0, 0, 1));
        var motorScene = WorkshopPhysicsCompiler.Compile(motorBench, new(1, 2));
        var motorJoint = Assert.Single(motorScene.Revolutes.ToArray());
        var motorMoving = motorScene.Bodies.ToArray().Single(b => b.Id == motorJoint.BodyB);

        // Motor has BaseFrame=(0, 0, 0.54), MovingFrame=(0,0,0)
        // With owner at (0, 1, 0), anchorA is (0, 1, 0.54).
        // PB is (0, 1, 0.54).
        var motorMovingZ = (motorMoving.Cell.Z + motorMoving.Local.Z) / 16f;
        Assert.InRange(MathF.Abs(motorMovingZ - 0.54f), 0f, 1e-5f);
        Assert.Equal(CanonicalRotation.Identity, motorMoving.Rotation);
    }

    [Fact]
    public void RotatedSliderBaseFrame_TranslatesAlongRotatedAxis_WithoutTransverseError()
    {
        var ownerPos = new Vector3(0f, 0f, 0f);
        var ownerRot = Quaternion.Identity;
        // Base frame rotated 90 degrees around Z axis: local +Y points to world -X
        var baseRot = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);
        var baseFrame = new ConstraintFrame(Vector3.Zero, baseRot);
        var movingFrame = new ConstraintFrame(Vector3.Zero, Quaternion.Identity);
        const float compression = 0.2f;

        var (posB, rotB) = WorkshopPhysicsCompiler.SliderMovingTransform(ownerPos, ownerRot, baseFrame, movingFrame, compression);

        // axisWorld = Vector3.Transform((0,1,0), rotZ(90)) = (-1, 0, 0)
        // posB = anchorA - axisWorld * compression = 0 - (-1,0,0)*0.2 = (+0.2, 0, 0)
        Assert.InRange(MathF.Abs(posB.X - 0.2f), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(posB.Y), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(posB.Z), 0f, 1e-5f);

        // Verify rotation B equals baseRot
        Assert.InRange(MathF.Abs(MathF.Abs(Quaternion.Dot(rotB, baseRot)) - 1f), 0f, 1e-5f);

        // Verify Jacobian constraint row evaluation
        Span<PrismaticRow> rows = stackalloc PrismaticRow[PrismaticRows.Count];
        var bodyA = new ConstraintBodyFrame(ownerPos, ownerRot, Vector3.Zero);
        var bodyB = new ConstraintBodyFrame(posB, rotB, Vector3.Zero);
        PrismaticRows.Prepare(bodyA, bodyB, baseFrame, movingFrame, -0.5f, 0.5f, rows);

        // Zero transverse errors and zero angular errors
        Assert.InRange(MathF.Abs(rows[(int)PrismaticRowKind.TransverseX].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)PrismaticRowKind.TransverseZ].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)PrismaticRowKind.AngularX].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)PrismaticRowKind.AngularY].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)PrismaticRowKind.AngularZ].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)PrismaticRowKind.Spring].Error - (-compression)), 0f, 1e-5f);

        // Negative control: unrotated placement PB = (0, -0.2, 0) gives non-zero transverse error
        var unrotatedBodyB = new ConstraintBodyFrame(new Vector3(0f, -0.2f, 0f), rotB, Vector3.Zero);
        PrismaticRows.Prepare(bodyA, unrotatedBodyB, baseFrame, movingFrame, -0.5f, 0.5f, rows);
        Assert.True(MathF.Abs(rows[(int)PrismaticRowKind.TransverseX].Error) > 0.1f, "Unrotated placement causes transverse error");
    }

    [Fact]
    public void RotatedSliderMovingFrame_ChildOrientationSatisfiesInvariant()
    {
        var ownerPos = new Vector3(1f, 2f, 3f);
        var ownerRot = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 6f);
        var baseRot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 3f);
        var movingRot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 4f);
        var baseFrame = new ConstraintFrame(new Vector3(0.2f, 0.4f, 0.1f), baseRot);
        var movingFrame = new ConstraintFrame(new Vector3(-0.1f, 0.2f, 0.3f), movingRot);
        const float compression = 0.25f;

        var (posB, rotB) = WorkshopPhysicsCompiler.SliderMovingTransform(ownerPos, ownerRot, baseFrame, movingFrame, compression);

        // Invariant: R_B * R_moving == R_A * R_base
        var jointRotB = Quaternion.Multiply(rotB, movingRot);
        var jointRotA = Quaternion.Multiply(ownerRot, baseRot);
        Assert.InRange(MathF.Abs(MathF.Abs(Quaternion.Dot(jointRotB, jointRotA)) - 1f), 0f, 1e-5f);

        // Prepare rows and assert zero transverse and angular errors
        Span<PrismaticRow> rows = stackalloc PrismaticRow[PrismaticRows.Count];
        var bodyA = new ConstraintBodyFrame(ownerPos, ownerRot, Vector3.Zero);
        var bodyB = new ConstraintBodyFrame(posB, rotB, Vector3.Zero);
        PrismaticRows.Prepare(bodyA, bodyB, baseFrame, movingFrame, -0.5f, 0.5f, rows);

        Assert.InRange(MathF.Abs(rows[(int)PrismaticRowKind.TransverseX].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)PrismaticRowKind.TransverseZ].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)PrismaticRowKind.AngularX].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)PrismaticRowKind.AngularY].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)PrismaticRowKind.AngularZ].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)PrismaticRowKind.Spring].Error - (-compression)), 0f, 1e-5f);
    }

    [Fact]
    public void RotatedHingeMovingFrame_ChildOrientationMatchesInverseMovingFrame()
    {
        var ownerPos = new Vector3(2f, 1f, 0f);
        var ownerRot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4f);
        var baseRot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 6f);
        var movingRot = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 4f);
        var baseFrame = new ConstraintFrame(new Vector3(0.1f, 0.3f, -0.2f), baseRot);
        var movingFrame = new ConstraintFrame(new Vector3(-0.2f, 0.1f, 0.4f), movingRot);
        var initialAngleRad = MathF.PI / 6f; // 30 degrees

        var (posB, rotB) = WorkshopPhysicsCompiler.HingeMovingTransform(ownerPos, ownerRot, baseFrame, movingFrame, initialAngleRad);

        // Expected: R_B = R_A * R_base * R(theta) * R_moving^-1
        var rJointA = Quaternion.Multiply(ownerRot, baseRot);
        var rAngle = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, initialAngleRad);
        var expectedRotB = Quaternion.Multiply(Quaternion.Multiply(rJointA, rAngle), Quaternion.Conjugate(movingRot));
        Assert.InRange(MathF.Abs(MathF.Abs(Quaternion.Dot(rotB, expectedRotB)) - 1f), 0f, 1e-5f);

        // Prepare rows and assert zero linear and angular transverse errors
        Span<RevoluteRow> rows = stackalloc RevoluteRow[RevoluteRows.Count];
        var bodyA = new ConstraintBodyFrame(ownerPos, ownerRot, Vector3.Zero);
        var bodyB = new ConstraintBodyFrame(posB, rotB, Vector3.Zero);
        RevoluteRows.Prepare(bodyA, bodyB, baseFrame, movingFrame, -MathF.PI, MathF.PI, rows);

        Assert.InRange(MathF.Abs(rows[(int)RevoluteRowKind.LinearX].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)RevoluteRowKind.LinearY].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)RevoluteRowKind.LinearZ].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)RevoluteRowKind.AngularX].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)RevoluteRowKind.AngularY].Error), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(rows[(int)RevoluteRowKind.Axial].Error - initialAngleRad), 0f, 1e-5f);
    }

    [Fact]
    public void RotatedOwnerBody_ChildRigidlyFollowsOwner()
    {
        // Compound 3D owner rotation: 45 degrees around Y, then 30 degrees around X
        var qY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4f);
        var qX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 6f);
        var ownerRot = Quaternion.Normalize(Quaternion.Multiply(qY, qX));
        var springId = new GpuBodyId(1);
        const float compression = 0.12f;

        // 1. Slider (springboard) with compound rotated owner
        var construction = Free(PlacedParts.Springboard(
            springId, 2f, 3f, 4f, ownerRot.X, ownerRot.Y, ownerRot.Z, ownerRot.W, compression: compression));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var joint = Assert.Single(scene.Prismatics.ToArray());
        var moving = scene.Bodies.ToArray().Single(b => b.Id == joint.BodyB);

        // Independently derived expectation for springboard:
        // BaseFrame=(0, 0.14, 0), MovingFrame=(0, 0, 0), Compression=0.12
        // Total local offset from owner: (0, 0.14 - 0.12, 0) = (0, 0.02, 0)
        var expectedSpringPos = new Vector3(2f, 3f, 4f) + Vector3.Transform(new Vector3(0f, 0.14f - compression, 0f), ownerRot);
        var expectedSpringRot = ownerRot;

        var actualPos = new Vector3(
            (moving.Cell.X + moving.Local.X) / 16f,
            (moving.Cell.Y + moving.Local.Y) / 16f,
            (moving.Cell.Z + moving.Local.Z) / 16f);
        var actualRot = new Quaternion(moving.Rotation.X, moving.Rotation.Y, moving.Rotation.Z, moving.Rotation.W);

        Assert.InRange(MathF.Abs(actualPos.X - expectedSpringPos.X), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(actualPos.Y - expectedSpringPos.Y), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(actualPos.Z - expectedSpringPos.Z), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(MathF.Abs(Quaternion.Dot(actualRot, expectedSpringRot)) - 1f), 0f, 1e-5f);

        // 2. Hinge (motor) with compound rotated owner
        var motorId = new GpuBodyId(2);
        var motorPos = new Vector3(1f, 5f, 2f);
        var motorConstruction = Free(WorkshopInput.Generic(
            new PartId("motor"), motorId,
            motorPos.X, motorPos.Y, motorPos.Z,
            ownerRot.X, ownerRot.Y, ownerRot.Z, ownerRot.W));
        var motorScene = WorkshopPhysicsCompiler.Compile(motorConstruction, new(1, 2));
        var motorJoint = Assert.Single(motorScene.Revolutes.ToArray());
        var motorMoving = motorScene.Bodies.ToArray().Single(b => b.Id == motorJoint.BodyB);

        // Independently derived expectation for motor:
        // BaseFrame=(0, 0, 0.54), MovingFrame=(0, 0, 0), InitialAngle=0
        var expectedMotorPos = motorPos + Vector3.Transform(new Vector3(0f, 0f, 0.54f), ownerRot);
        var expectedMotorRot = ownerRot;

        var actualMotorPos = new Vector3(
            (motorMoving.Cell.X + motorMoving.Local.X) / 16f,
            (motorMoving.Cell.Y + motorMoving.Local.Y) / 16f,
            (motorMoving.Cell.Z + motorMoving.Local.Z) / 16f);
        var actualMotorRot = new Quaternion(motorMoving.Rotation.X, motorMoving.Rotation.Y, motorMoving.Rotation.Z, motorMoving.Rotation.W);

        Assert.InRange(MathF.Abs(actualMotorPos.X - expectedMotorPos.X), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(actualMotorPos.Y - expectedMotorPos.Y), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(actualMotorPos.Z - expectedMotorPos.Z), 0f, 1e-5f);
        Assert.InRange(MathF.Abs(MathF.Abs(Quaternion.Dot(actualMotorRot, expectedMotorRot)) - 1f), 0f, 1e-5f);
    }

    [Fact]
    public void ZeroPoseJump_UnforcedMechanism_RemainsStableOnFirstTick()
    {
        // 1. Setup a zero-gravity law scene with non-identity joint frames
        var scene = new LawScene(bench: false);
        var ownerPos = new Vector3(0f, 5f, 0f);
        var ownerRot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 3f);
        var frame = scene.StaticBox(ownerPos, new Vector3(0.5f, 0.5f, 0.5f), rotation: ownerRot);

        // Slider with compound frames
        var sliderBaseFrame = new ConstraintFrame(new Vector3(0.1f, 0.2f, 0f), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 4f));
        var sliderMovingFrame = new ConstraintFrame(new Vector3(0f, 0.05f, 0.1f), Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 6f));
        const float compression = 0f;
        var (sliderPos, sliderRot) = WorkshopPhysicsCompiler.SliderMovingTransform(ownerPos, ownerRot, sliderBaseFrame, sliderMovingFrame, compression);

        var sliderBody = scene.Box(sliderPos, new Vector3(0.1f, 0.1f, 0.1f), mass: 1f, LawMaterial.Domino,
            rotation: sliderRot, gravity: 0f);
        scene.Prismatic(frame, sliderBody, sliderBaseFrame, sliderMovingFrame, -0.5f, 0.5f, stiffness: 400f, damping: 0.2f, ConnectedCollision.Disabled);

        // Hinge with compound frames
        var hingeBaseFrame = new ConstraintFrame(new Vector3(0f, 0.3f, 0.2f), Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 4f));
        var hingeMovingFrame = new ConstraintFrame(new Vector3(0.05f, 0f, -0.1f), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 3f));
        const float angleRad = MathF.PI / 4f;
        var (hingePos, hingeRot) = WorkshopPhysicsCompiler.HingeMovingTransform(ownerPos, ownerRot, hingeBaseFrame, hingeMovingFrame, angleRad);

        var hingeBody = scene.Box(hingePos, new Vector3(0.1f, 0.1f, 0.1f), mass: 1f, LawMaterial.Domino,
            rotation: hingeRot, gravity: 0f);
        scene.Revolute(frame, hingeBody, hingeBaseFrame, hingeMovingFrame, -MathF.PI, MathF.PI, frictionTorque: 0f, ConnectedCollision.Disabled);

        var world = scene.Admit();
        var initialSlider = world.Initial(sliderBody);
        var initialHinge = world.Initial(hingeBody);

        // Advance 1 tick
        world.Step(1, (tick, state) =>
        {
            var bSlider = state[sliderBody];
            var bHinge = state[hingeBody];

            // Velocity must remain essentially zero (no solver impulse correction from misalignment)
            Assert.InRange(bSlider.Speed, 0f, 1e-4f);
            Assert.InRange(bSlider.Angular.Length(), 0f, 1e-4f);
            Assert.InRange((bSlider.Position - initialSlider.Position).Length(), 0f, 1e-4f);

            Assert.InRange(bHinge.Speed, 0f, 1e-4f);
            Assert.InRange(bHinge.Angular.Length(), 0f, 1e-4f);
            Assert.InRange((bHinge.Position - initialHinge.Position).Length(), 0f, 1e-4f);
        });

        // Control: misaligned placement causes immediate artificial pose jump / impulse
        var controlScene = new LawScene(bench: false);
        var controlFrame = controlScene.StaticBox(ownerPos, new Vector3(0.5f, 0.5f, 0.5f), rotation: ownerRot);
        // Misaligned: place child at owner rotation without inverse moving frame
        var misalignedBody = controlScene.Box(sliderPos, new Vector3(0.1f, 0.1f, 0.1f), mass: 1f, LawMaterial.Domino,
            rotation: ownerRot, gravity: 0f);
        controlScene.Prismatic(controlFrame, misalignedBody, sliderBaseFrame, sliderMovingFrame, -0.5f, 0.5f, stiffness: 400f, damping: 0.2f, ConnectedCollision.Disabled);

        var controlWorld = controlScene.Admit();
        controlWorld.Step(1, (tick, state) =>
        {
            var bMisaligned = state[misalignedBody];
            // Misalignment causes solver to fire corrective impulse, giving nonzero velocity
            Assert.True(bMisaligned.Angular.Length() > 0.01f || bMisaligned.Speed > 0.01f,
                "Misaligned frame causes artificial solver velocity jump");
        });
    }

    [Fact]
    public void BundleValidatorRefusals_InvalidQuaternionsAndNonFinite_Rejected()
    {
        var springEntry = TestBundle.Entry(new PartId("spring"));
        var motorEntry = TestBundle.Entry(new PartId("motor"));

        // 1. Slider: non-unit BaseFrame.Rotation triggers OutOfRange
        var sliderNonUnitBase = springEntry with
        {
            Mechanical = springEntry.Mechanical! with
            {
                Sliders = [springEntry.Mechanical.Sliders[0] with
                {
                    BaseFrame = springEntry.Mechanical.Sliders[0].BaseFrame with
                    {
                        Rotation = new Quaternion(0f, 0f, 0f, 2f)
                    }
                }]
            }
        };
        var errors1 = BundleValidator.Validate([new("spring", sliderNonUnitBase)]);
        Assert.Contains(errors1, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("BaseFrame.Rotation"));

        // 2. Slider: non-unit MovingFrame.Rotation triggers OutOfRange
        var sliderNonUnitMoving = springEntry with
        {
            Mechanical = springEntry.Mechanical! with
            {
                Sliders = [springEntry.Mechanical.Sliders[0] with
                {
                    MovingFrame = springEntry.Mechanical.Sliders[0].MovingFrame with
                    {
                        Rotation = new Quaternion(0f, 0f, 0f, 0.5f)
                    }
                }]
            }
        };
        var errors2 = BundleValidator.Validate([new("spring", sliderNonUnitMoving)]);
        Assert.Contains(errors2, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("MovingFrame.Rotation"));

        // 3. Slider: non-finite BaseFrame.Rotation triggers NonFinite (and not OutOfRange)
        var sliderNonFiniteBase = springEntry with
        {
            Mechanical = springEntry.Mechanical! with
            {
                Sliders = [springEntry.Mechanical.Sliders[0] with
                {
                    BaseFrame = springEntry.Mechanical.Sliders[0].BaseFrame with
                    {
                        Rotation = new Quaternion(float.PositiveInfinity, 0f, 0f, 1f)
                    }
                }]
            }
        };
        var errors3 = BundleValidator.Validate([new("spring", sliderNonFiniteBase)]);
        Assert.Contains(errors3, e => e.Code == BundleErrorCode.NonFinite && e.Detail.Contains("BaseFrame.Rotation"));
        Assert.DoesNotContain(errors3, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("BaseFrame.Rotation"));

        // 4. Slider: non-finite MovingFrame.Position triggers NonFinite
        var sliderNonFiniteMoving = springEntry with
        {
            Mechanical = springEntry.Mechanical! with
            {
                Sliders = [springEntry.Mechanical.Sliders[0] with
                {
                    MovingFrame = springEntry.Mechanical.Sliders[0].MovingFrame with
                    {
                        Position = new Vector3(float.PositiveInfinity, 0f, 0f)
                    }
                }]
            }
        };
        var errors4 = BundleValidator.Validate([new("spring", sliderNonFiniteMoving)]);
        Assert.Contains(errors4, e => e.Code == BundleErrorCode.NonFinite && e.Detail.Contains("MovingFrame.Position"));

        // 5. Hinge: non-unit BaseFrame.Rotation triggers OutOfRange
        var hingeNonUnitBase = motorEntry with
        {
            Mechanical = motorEntry.Mechanical! with
            {
                Hinges = [motorEntry.Mechanical.Hinges[0] with
                {
                    BaseFrame = motorEntry.Mechanical.Hinges[0].BaseFrame with
                    {
                        Rotation = new Quaternion(0f, 0f, 0f, 1.5f)
                    }
                }]
            }
        };
        var errors5 = BundleValidator.Validate([new("motor", hingeNonUnitBase)]);
        Assert.Contains(errors5, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("BaseFrame.Rotation"));

        // 6. Hinge: non-unit MovingFrame.Rotation triggers OutOfRange
        var hingeNonUnitMoving = motorEntry with
        {
            Mechanical = motorEntry.Mechanical! with
            {
                Hinges = [motorEntry.Mechanical.Hinges[0] with
                {
                    MovingFrame = motorEntry.Mechanical.Hinges[0].MovingFrame with
                    {
                        Rotation = new Quaternion(0f, 0f, 0f, 0.7f)
                    }
                }]
            }
        };
        var errors6 = BundleValidator.Validate([new("motor", hingeNonUnitMoving)]);
        Assert.Contains(errors6, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("MovingFrame.Rotation"));

        // 7. Hinge: non-finite MovingFrame.Rotation triggers NonFinite (and not OutOfRange)
        var hingeNonFiniteMoving = motorEntry with
        {
            Mechanical = motorEntry.Mechanical! with
            {
                Hinges = [motorEntry.Mechanical.Hinges[0] with
                {
                    MovingFrame = motorEntry.Mechanical.Hinges[0].MovingFrame with
                    {
                        Rotation = new Quaternion(0f, float.PositiveInfinity, 0f, 1f)
                    }
                }]
            }
        };
        var errors7 = BundleValidator.Validate([new("motor", hingeNonFiniteMoving)]);
        Assert.Contains(errors7, e => e.Code == BundleErrorCode.NonFinite && e.Detail.Contains("MovingFrame.Rotation"));
        Assert.DoesNotContain(errors7, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("MovingFrame.Rotation"));
    }
}
