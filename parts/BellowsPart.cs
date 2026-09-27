using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public enum BellowsPhase { Ready, Compressing, Held, Refilling }
public static class BellowsParameters
{
    public const string Force="force";
    public const string Reach="reach";
    public const string Width="width";
}

/// <summary>Impact-operated finite-stroke air pump, using the game's ideal jet model.</summary>
public partial class BellowsPart : MachinePart
{
    public const float RestHeight=.45f;
    public const float MaximumStroke=.4f;
    public const float CompressionSpeed=.8f;
    public const float RefillSpeed=.4f;
    public const float MinimumImpactSpeed=.8f;
    private static readonly Vector3 PlateHalf=new(.7f,.06f,.55f);
    private static readonly Vector3 NozzleAt=new(.96f,-.12f,0);
    public BellowsPhase Phase { get; private set; }
    public float Compression { get; private set; }
    public float EmissionForce { get; private set; }
    public int StrokeCount { get; private set; }
    private float _pendingEnergy;
    private float _targetCompression;
    private Node3D _plate=null!;
    private Node3D _folds=null!;
    public override float SurfaceBounce=>0;
    public override AirflowEmitter? AirflowSource=>EmissionForce>0
        ?new(NozzleAt,Vector3.Right,Properties[BellowsParameters.Reach],Properties[BellowsParameters.Width],EmissionForce):null;

    public override void ValidateParameters()
    {
        foreach(var key in new[]{BellowsParameters.Force,BellowsParameters.Reach,BellowsParameters.Width})
            if(!float.IsFinite(Properties[key]))throw new ArgumentException("Bellows parameters must be finite.");
        if(Properties[BellowsParameters.Force]<=0||Properties[BellowsParameters.Force]>40||
            Properties[BellowsParameters.Reach]<=0||Properties[BellowsParameters.Reach]>12||
            Properties[BellowsParameters.Width]<=0||Properties[BellowsParameters.Width]>4)
            throw new ArgumentException("Bellows airflow parameters are outside supported bounds.");
    }
    public override void OnContact(MachinePart body,float speed,MachineWorld world)
    {
        if(Phase!=BellowsPhase.Ready||!body.Dynamic||!body.Visible||!float.IsFinite(speed)||speed<MinimumImpactSpeed)return;
        var at=Transform.AffineInverse()*body.Position;
        // Accept the top face only, not an impact on the nozzle, base, side or underside.
        if(Mathf.Abs(at.X)>PlateHalf.X||Mathf.Abs(at.Z)>PlateHalf.Z||
            at.Y<RestHeight+PlateHalf.Y+body.Radius-.025f)return;
        _pendingEnergy=Mathf.Max(_pendingEnergy,.5f*body.Mass*speed*speed);
        // Coalesce same-tick contacts before moving; no render or body ordering controls the stroke.
    }
    public override void BeforeNetworks(MachineWorld world)
    {
        if(Phase!=BellowsPhase.Ready||_pendingEnergy<=0)return;
        _targetCompression=MaximumStroke*Mathf.Clamp(_pendingEnergy/12,0,1);
        _pendingEnergy=0;
        Phase=BellowsPhase.Compressing;StrokeCount++;
        world.Events.TryAdd(new(MachineEventKind.Activated,Uid),world.Ticks);
    }
    private bool PlateOccupied(MachineWorld world)
    {
        // Include the whole refill sweep, so recovery never pushes through an overhanging load.
        return world.Bodies.Any(body=>
        {
            if(!body.Visible)return false;
            var at=Transform.AffineInverse()*body.Position;
            return Mathf.Abs(at.X)<PlateHalf.X+body.Radius+.025f&&Mathf.Abs(at.Z)<PlateHalf.Z+body.Radius+.025f&&
                at.Y>RestHeight-Compression-PlateHalf.Y-body.Radius-.025f&&at.Y<RestHeight+PlateHalf.Y+body.Radius+.04f;
        });
    }
    public override void BeforeStep(MachineWorld world,float delta)
    {
        EmissionForce=0;
        switch(Phase)
        {
            case BellowsPhase.Compressing:
                var previous=Compression;
                Compression=Mathf.MoveToward(Compression,_targetCompression,CompressionSpeed*delta);
                EmissionForce=Properties[BellowsParameters.Force]*(Compression-previous)/(CompressionSpeed*delta);
                if(Compression>=_targetCompression)Phase=BellowsPhase.Held;
                break;
            case BellowsPhase.Held:
                if(!PlateOccupied(world))Phase=BellowsPhase.Refilling;
                break;
            case BellowsPhase.Refilling:
                if(PlateOccupied(world)){Phase=BellowsPhase.Held;break;}
                Compression=Mathf.MoveToward(Compression,0,RefillSpeed*delta);
                if(Compression==0)Phase=BellowsPhase.Ready;
                break;
        }
        Active=EmissionForce>0;
        SyncGeometry();
    }
    private void SyncGeometry()
    {
        var height=RestHeight-Compression;
        Boxes[0]=new(new(0,height,0),PlateHalf);
        _plate.Position=new(0,height,0);
        _folds.Scale=new(1,(height+.3f)/.75f,1);
    }
    protected override void Build()
    {
        PickRadius=.95f;
        AddBox(new(0,RestHeight,0),PlateHalf*2,new("#fff8e9"),false);
        AddBox(new(0,-.4f,0),new(1.5f,.16f,1.2f),new("#293954"));
        _plate=new Node3D {Name="PressPlate"};Visual.AddChild(_plate);
        PartArt.Box(_plate,PlateHalf*2,new("#fff8e9"));
        PartArt.Box(_plate,new(.7f,.025f,.35f),new("#e8b764"),new(0,.07f,0));
        _folds=new Node3D {Name="Accordion",Position=new(0,-.3f,0)};Visual.AddChild(_folds);
        for(var i=0;i<5;i++)
        {
            var y=(i+.5f)*.15f;
            PartArt.Box(_folds,new(1.2f,.1f,.92f),new("#66b8c9"),new(0,y,0));
            PartArt.Box(_folds,new(1.28f,.035f,1),new("#fff8e9"),new(0,y+.05f,0));
        }
        AddBox(new(.73f,-.12f,0),new(.45f,.19f,.24f),new("#e8b764"));
        var mouth=PartArt.Ring(Visual,.12f,.025f,new("#293954"),NozzleAt);
        mouth.RotationDegrees=new(0,0,90);
        SyncGeometry();
    }
}
