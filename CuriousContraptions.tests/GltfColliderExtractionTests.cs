using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CuriousContraptions;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class GltfColliderExtractionTests
{
    public GltfColliderExtractionTests(NativeSceneFixture _) { }
    [Fact]
    public void ExtractsBoxFromCollisionShape3D()
    {
        var root = new Node3D();
        try
        {
            var shape = new CollisionShape3D
            {
                Name = "col_box_main",
                Position = new Vector3(1f, 2f, 3f),
                Shape = new BoxShape3D { Size = new Vector3(2f, 4f, 6f) }
            };
            root.AddChild(shape);

            var (colliders, _) = GltfPhysicsBodyExtractor.Extract(root);

            var collider = Assert.Single(colliders);
            Assert.Equal(ColliderShapeKind.Box, collider.ShapeKind);
            Assert.Equal((Half)1f, collider.HalfExtents.X);
            Assert.Equal((Half)2f, collider.HalfExtents.Y);
            Assert.Equal((Half)3f, collider.HalfExtents.Z);
            Assert.Equal((Half)0, collider.Radius);
            Assert.Equal((Half)1f, collider.LocalPose.Translation.X);
            Assert.Equal((Half)2f, collider.LocalPose.Translation.Y);
            Assert.Equal((Half)3f, collider.LocalPose.Translation.Z);
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void ExtractsBoxFromMeshInstance3D()
    {
        var root = new Node3D();
        try
        {
            var mi = new MeshInstance3D
            {
                Name = "col_box",
                Position = new Vector3(0.5f, 0f, -1f),
                Mesh = new BoxMesh { Size = new Vector3(1f, 2f, 0.5f) }
            };
            root.AddChild(mi);

            var (colliders, _) = GltfPhysicsBodyExtractor.Extract(root);

            var collider = Assert.Single(colliders);
            Assert.Equal(ColliderShapeKind.Box, collider.ShapeKind);
            Assert.Equal((Half)0.5f, collider.HalfExtents.X);
            Assert.Equal((Half)1f, collider.HalfExtents.Y);
            Assert.Equal((Half)0.25f, collider.HalfExtents.Z);
            Assert.Equal((Half)0.5f, collider.LocalPose.Translation.X);
            Assert.Equal((Half)0f, collider.LocalPose.Translation.Y);
            Assert.Equal((Half)(-1f), collider.LocalPose.Translation.Z);
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void ExtractsSphereFromCollisionShape3D()
    {
        var root = new Node3D();
        try
        {
            var shape = new CollisionShape3D
            {
                Name = "col_sphere_core",
                Position = new Vector3(-2f, 1.5f, 0f),
                Shape = new SphereShape3D { Radius = 0.75f }
            };
            root.AddChild(shape);

            var (colliders, _) = GltfPhysicsBodyExtractor.Extract(root);

            var collider = Assert.Single(colliders);
            Assert.Equal(ColliderShapeKind.Sphere, collider.ShapeKind);
            Assert.Equal((Half)0.75f, collider.Radius);
            Assert.Equal((Half)0, collider.HalfExtents.X);
            Assert.Equal((Half)(-2f), collider.LocalPose.Translation.X);
            Assert.Equal((Half)1.5f, collider.LocalPose.Translation.Y);
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void ExtractsSphereFromMeshInstance3D()
    {
        var root = new Node3D();
        try
        {
            var mi = new MeshInstance3D
            {
                Name = "col_sphere",
                Mesh = new SphereMesh { Radius = 1.25f }
            };
            root.AddChild(mi);

            var (colliders, _) = GltfPhysicsBodyExtractor.Extract(root);

            var collider = Assert.Single(colliders);
            Assert.Equal(ColliderShapeKind.Sphere, collider.ShapeKind);
            Assert.Equal((Half)1.25f, collider.Radius);
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void IgnoresVisualMeshesWithoutProxyNaming()
    {
        var root = new Node3D();
        try
        {
            var visual = new MeshInstance3D
            {
                Name = "VisualPlasterMesh",
                Mesh = new BoxMesh { Size = new Vector3(2f, 2f, 2f) }
            };
            root.AddChild(visual);

            var (colliders, ports) = GltfPhysicsBodyExtractor.Extract(root);

            Assert.Empty(colliders);
            Assert.Empty(ports);
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void ComputesAccumulatedTransformsInDeepHierarchy()
    {
        var root = new Node3D { Name = "Root" };
        try
        {
            var parent = new Node3D
            {
                Name = "Parent",
                Position = new Vector3(2f, 1f, 0f),
                Rotation = new Vector3(0f, Mathf.DegToRad(90f), 0f)
            };
            var child = new Node3D
            {
                Name = "Child",
                Position = new Vector3(0f, 3f, 0f)
            };
            var box = new CollisionShape3D
            {
                Name = "col_box",
                Position = new Vector3(1f, 0f, 0f),
                Shape = new BoxShape3D { Size = new Vector3(1f, 1f, 1f) }
            };
            var port = new Node3D
            {
                Name = "Loc_Port_output",
                Position = new Vector3(0.5f, 0f, 0f)
            };

            child.AddChild(box);
            child.AddChild(port);
            parent.AddChild(child);
            root.AddChild(parent);

            var (colliders, ports) = GltfPhysicsBodyExtractor.Extract(root);

            var collider = Assert.Single(colliders);
            Assert.Equal(ColliderShapeKind.Box, collider.ShapeKind);

            Assert.True(Math.Abs((float)collider.LocalPose.Translation.X - 2f) < 0.05f);
            Assert.True(Math.Abs((float)collider.LocalPose.Translation.Y - 4f) < 0.05f);
            Assert.True(Math.Abs((float)collider.LocalPose.Translation.Z - (-1f)) < 0.05f);

            Assert.True(ports.TryGetValue("output", out var portLocator));
            Assert.Equal("output", portLocator.PortId);
            Assert.True(Math.Abs(portLocator.Position.X - 2f) < 0.05f);
            Assert.True(Math.Abs(portLocator.Position.Y - 4f) < 0.05f);
            Assert.True(Math.Abs(portLocator.Position.Z - (-0.5f)) < 0.05f);
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void ExtractsMultiplePortLocators()
    {
        var root = new Node3D();
        try
        {
            var portA = new Node3D { Name = "Loc_Port_power_in", Position = new Vector3(0.1f, 0.2f, 0.3f) };
            var portB = new Node3D { Name = "Loc_Port_signal_out", Position = new Vector3(-0.4f, 0.5f, -0.6f) };
            root.AddChild(portA);
            root.AddChild(portB);

            var (_, ports) = GltfPhysicsBodyExtractor.Extract(root);

            Assert.Equal(2, ports.Count);
            Assert.True(ports.ContainsKey("power_in"));
            Assert.True(ports.ContainsKey("signal_out"));
            Assert.Equal(new Vector3(0.1f, 0.2f, 0.3f), ports["power_in"].Position);
            Assert.Equal(new Vector3(-0.4f, 0.5f, -0.6f), ports["signal_out"].Position);
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void NonNode3DPortLocatorIsIgnored()
    {
        var root = new Node3D();
        try
        {
            var non3D = new Node { Name = "Loc_Port_ignored" };
            root.AddChild(non3D);

            var (_, ports) = GltfPhysicsBodyExtractor.Extract(root);

            Assert.Empty(ports);
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void DuplicatePortLocatorThrowsArgumentException()
    {
        var root = new Node3D();
        try
        {
            var branchA = new Node3D { Name = "BranchA" };
            var branchB = new Node3D { Name = "BranchB" };
            root.AddChild(branchA);
            root.AddChild(branchB);
            branchA.AddChild(new Node3D { Name = "Loc_Port_dup" });
            branchB.AddChild(new Node3D { Name = "Loc_Port_dup" });

            Assert.Throws<ArgumentException>(() => GltfPhysicsBodyExtractor.Extract(root));
        }
        finally
        {
            root.Free();
        }
    }

    [Theory]
    [InlineData(0f, 1f, 1f)]
    [InlineData(1f, 0f, 1f)]
    [InlineData(1f, 1f, 0f)]
    public void NonPositiveBoxExtentsThrowsArgumentException(float x, float y, float z)
    {
        var root = new Node3D();
        try
        {
            root.AddChild(new CollisionShape3D
            {
                Name = "col_box",
                Shape = new BoxShape3D { Size = new Vector3(x, y, z) }
            });

            Assert.Throws<ArgumentException>(() => GltfPhysicsBodyExtractor.Extract(root));
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void NegativeBoxScaleThrowsArgumentException()
    {
        var root = new Node3D();
        try
        {
            root.AddChild(new CollisionShape3D
            {
                Name = "col_box",
                Scale = new Vector3(1f, -1f, 1f),
                Shape = new BoxShape3D { Size = new Vector3(1f, 1f, 1f) }
            });

            Assert.Throws<ArgumentException>(() => GltfPhysicsBodyExtractor.Extract(root));
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void ZeroSphereRadiusThrowsArgumentException()
    {
        var root = new Node3D();
        try
        {
            root.AddChild(new CollisionShape3D
            {
                Name = "col_sphere",
                Shape = new SphereShape3D { Radius = 0f }
            });

            Assert.Throws<ArgumentException>(() => GltfPhysicsBodyExtractor.Extract(root));
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void NegativeSphereScaleThrowsArgumentException()
    {
        var root = new Node3D();
        try
        {
            root.AddChild(new CollisionShape3D
            {
                Name = "col_sphere",
                Scale = new Vector3(-1f, 1f, 1f),
                Shape = new SphereShape3D { Radius = 1f }
            });

            Assert.Throws<ArgumentException>(() => GltfPhysicsBodyExtractor.Extract(root));
        }
        finally
        {
            root.Free();
        }
    }

    [Theory]
    [InlineData("col_cylinder")]
    [InlineData("col_capsule")]
    [InlineData("col_hull")]
    [InlineData("wheel-convcol")]
    public void UnsupportedProxyNamesThrowNotSupportedException(string proxyName)
    {
        var root = new Node3D();
        try
        {
            root.AddChild(new Node3D { Name = proxyName });

            Assert.Throws<NotSupportedException>(() => GltfPhysicsBodyExtractor.Extract(root));
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void UnsupportedCollisionShapesThrowNotSupportedException()
    {
        var root = new Node3D();
        try
        {
            root.AddChild(new CollisionShape3D
            {
                Name = "CollisionShape3D",
                Shape = new CylinderShape3D()
            });

            Assert.Throws<NotSupportedException>(() => GltfPhysicsBodyExtractor.Extract(root));
        }
        finally
        {
            root.Free();
        }
    }

    /// <summary>A scene-less definition, as a test fixture builds one; the registry builds real ones from the bundle.</summary>
    private static PartDefinition Fixture(string id) => new(new PartId(id), id, id, CuriousContraptions.Abstractions.ElementCategory.Structure,
        Colors.White, "res://elements/Wall/icon.svg", null, null);

    [Fact]
    public void PartDefinitionExposesCollidersAndPortLocators()
    {
        var def = Fixture("test_fixture");
        Assert.Empty(def.Colliders);
        Assert.Empty(def.PortLocators);

        var collider = new ExtractedCollider(ColliderShapeKind.Box, RigidLocalPose.Identity, (Half)0, new MetreVector((Half)1, (Half)1, (Half)1));
        var port = new ExtractedPortLocator("mouth_start", new Vector3(1, 2, 3), Quaternion.Identity);
        def.SetExtractedData(new[] { collider }, new Dictionary<string, ExtractedPortLocator> { ["mouth_start"] = port });

        Assert.Single(def.Colliders);
        Assert.Equal(ColliderShapeKind.Box, def.Colliders[0].ShapeKind);
        Assert.True(def.PortLocators.ContainsKey("mouth_start"));
    }

    /// <summary>Story 9A.17: locator names are matched exactly, so a differently cased name finds no locator.</summary>
    [Fact]
    public void PortLocatorNamesAreMatchedExactly()
    {
        var root = new Node3D { Name = "Root" };
        try
        {
            root.AddChild(new Node3D { Name = "Loc_Port_PowerIn", Position = new Vector3(-0.93f, 0f, 0f) });
            root.AddChild(new Node3D { Name = "loc_port_Supply" });
            var (_, ports) = GltfPhysicsBodyExtractor.Extract(root);
            Assert.True(ports.ContainsKey("PowerIn"));
            Assert.False(ports.ContainsKey("powerin"));
            Assert.False(ports.ContainsKey("POWERIN"));
            Assert.Single(ports);
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void WorkshopPhysicsCompilerCompilesGenericInstanceWithExtractedColliders()
    {
        var poseA = RigidLocalPose.Identity;
        var poseB = new RigidLocalPose(new MetreVector((Half)0, (Half)1, (Half)0), CanonicalRotation.Identity);
        var colliders = new List<ExtractedCollider>
        {
            new(ColliderShapeKind.Box, poseA, (Half)0, new MetreVector((Half)0.5, (Half)0.2, (Half)0.3)),
            new(ColliderShapeKind.Sphere, poseB, (Half)0.4, default)
        };

        var genericInstance = WorkshopInput.Generic(new GpuBodyId(10), 0, 1, 0, 0, 0, 0, 1, colliders);

        var construction = new WorkshopConstruction(
            new ConstructionRevision(1),
            WorkshopCadenceSettings.Default(),
            new WorkshopInstances(genericInstance));

        var scene = WorkshopPhysicsCompiler.Compile(construction, new PhysicsDocumentId(1, 1));

        Assert.Equal(2, scene.Bodies.Length);
        Assert.Equal(3, scene.Colliders.Length);

        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(10));
        Assert.Equal(RigidMotionKind.Static, body.Motion);

        var compiledColliders = scene.Colliders.ToArray().Where(c => c.Body == new GpuBodyId(10)).ToList();
        Assert.Equal(2, compiledColliders.Count);

        Assert.Equal(ColliderShapeKind.Box, compiledColliders[0].Shape);
        Assert.Equal((Half)0.5, compiledColliders[0].HalfExtents.X);
        Assert.Equal((Half)0.2, compiledColliders[0].HalfExtents.Y);
        Assert.Equal((Half)0.3, compiledColliders[0].HalfExtents.Z);

        Assert.Equal(ColliderShapeKind.Sphere, compiledColliders[1].Shape);
        Assert.Equal((Half)0.4, compiledColliders[1].Radius.Value);
    }

    [Fact]
    public void VisualMeshEntityIgnoredAndSeparateFromColliderEntity()
    {
        var root = new Node3D();
        try
        {
            var visualMesh = new MeshInstance3D
            {
                Name = "Visual_Mesh",
                Mesh = new BoxMesh { Size = new Vector3(5f, 5f, 5f) },
                Visible = true
            };
            var colliderMesh = new MeshInstance3D
            {
                Name = "col_box",
                Position = new Vector3(1f, 0f, 0f),
                Mesh = new BoxMesh { Size = new Vector3(1f, 2f, 3f) },
                Visible = true
            };
            root.AddChild(visualMesh);
            root.AddChild(colliderMesh);

            var (colliders, _) = GltfPhysicsBodyExtractor.Extract(root);

            var collider = Assert.Single(colliders);
            Assert.Equal(ColliderShapeKind.Box, collider.ShapeKind);
            Assert.Equal((Half)0.5f, collider.HalfExtents.X);
            Assert.Equal((Half)1.0f, collider.HalfExtents.Y);
            Assert.Equal((Half)1.5f, collider.HalfExtents.Z);

            Assert.True(visualMesh.Visible);
            Assert.False(colliderMesh.Visible);
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void CollisionShapeColliderVisibilitySuppressedAlongsideVisualMesh()
    {
        var root = new Node3D();
        try
        {
            var visualBody = new MeshInstance3D
            {
                Name = "Visual_Body",
                Mesh = new SphereMesh { Radius = 3f },
                Visible = true
            };
            var collisionShape = new CollisionShape3D
            {
                Name = "col_sphere_phys",
                Position = new Vector3(0f, 2f, 0f),
                Shape = new SphereShape3D { Radius = 0.5f },
                Visible = true
            };
            root.AddChild(visualBody);
            root.AddChild(collisionShape);

            var (colliders, _) = GltfPhysicsBodyExtractor.Extract(root);

            var collider = Assert.Single(colliders);
            Assert.Equal(ColliderShapeKind.Sphere, collider.ShapeKind);
            Assert.Equal((Half)0.5f, collider.Radius);

            Assert.True(visualBody.Visible);
            Assert.False(collisionShape.Visible);
        }
        finally
        {
            root.Free();
        }
    }

    [Fact]
    public void SceneWithOnlyVisualMeshExtractsZeroColliders()
    {
        var root = new Node3D();
        try
        {
            var visualMesh = new MeshInstance3D
            {
                Name = "Visual_Lamp",
                Mesh = new BoxMesh { Size = new Vector3(1f, 1f, 1f) },
                Visible = true
            };
            root.AddChild(visualMesh);

            var (colliders, _) = GltfPhysicsBodyExtractor.Extract(root);

            Assert.Empty(colliders);
            Assert.True(visualMesh.Visible);
        }
        finally
        {
            root.Free();
        }
    }
}
