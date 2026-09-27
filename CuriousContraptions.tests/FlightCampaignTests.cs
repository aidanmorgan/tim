using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;
using FileAccess=Godot.FileAccess;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class FlightCampaignTests(HeadlessFixture godot,ITestOutputHelper output)
{
    private const string CampaignPath="res://content/puzzles.json";
    private const string SpringPuzzle="spring_forward",PipePuzzle="clear_pipe",BendPuzzle="quarter_bend";
    private const string ReceiverId="receiver";
    [Theory]
    [InlineData(SpringPuzzle,-.5f,false)]
    [InlineData(SpringPuzzle,.3f,true)]
    [InlineData(PipePuzzle,0f,true)]
    [InlineData(BendPuzzle,0f,true)]
    public void ReceiverFlightRespectsAuthoredAssistanceRegion(string puzzleId,float offset,bool expectedCapture)
    {
        var world=new MachineWorld{Precision=0};godot.Tree.Root.AddChild(world);
        try
        {
            var puzzle=MachineCodec.ReadPuzzles(FileAccess.GetFileAsString(CampaignPath)).Single(p=>p.Id==puzzleId);
            var machine=MachineCodec.Clone(puzzle.CreateMachine());
            machine.Parts.AddRange(puzzle.Solution);machine.Connections=puzzle.SolutionConnections;
            machine.Parts.Single(p=>p.Id==ReceiverId).Position[0]+=offset;
            world.LoadMachine(machine);world.Start();
            var ball=world.Bodies.Single();var receiver=world.FindPart(ReceiverId)!;
            for(var tick=0;tick<1200&&world.Running;tick++)
            {
                world.Step();
                var local=receiver.Transform.AffineInverse()*ball.Position;
                if(tick%10==0&&local.Length()<3)
                    output.WriteLine($"tick={tick} local={local} velocity={ball.Velocity}");
            }
            output.WriteLine($"end={ball.Position} velocity={ball.Velocity}");
            Assert.Equal(expectedCapture,world.Won);
        }
        finally{world.Free();}
    }
}
