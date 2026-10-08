using System.Text.Json;
using CuriousContraptions.Physics;
enum Outcome { Direction, Rejected }
enum Mode { Direction, Acceptance }
enum AcceptanceCase { Duplicate, ZeroSatisfied, ZeroUnsatisfied, Inconsistent }

static class Program {
static void Acceptance(){
foreach(var kind in Enum.GetValues<AcceptanceCase>()){
double[] state=kind==AcceptanceCase.ZeroSatisfied||kind==AcceptanceCase.ZeroUnsatisfied?[0,0]:[0,0,0];
double[] Residual(double[] x)=>kind switch{
AcceptanceCase.Duplicate=>[x[0]-x[1]-x[2]+2,x[0],x[0]],
AcceptanceCase.ZeroSatisfied=>[x[0]-2,0],
AcceptanceCase.ZeroUnsatisfied=>[x[0]-2,Math.ScaleB(1,-20)],
AcceptanceCase.Inconsistent=>[x[0]+x[1]-1,x[0]+(1+Math.ScaleB(1,-40))*x[1]-1,2*x[0]+(2+Math.ScaleB(1,-40))*x[1]-(2+Math.ScaleB(1,-20))],
_=>throw new ArgumentOutOfRangeException()};
bool accepted=false,rejected=false;double maximum=double.PositiveInfinity;
for(int iteration=0;iteration<8;iteration++){
maximum=Residual(state).Max(Math.Abs);if(maximum<=1e-10){accepted=true;break;}
try{state=NonlinearIteration.Advance(state,Residual, []);}catch(InvalidOperationException){rejected=true;break;}}
Console.WriteLine(JsonSerializer.Serialize(new{Case=kind,Accepted=accepted,Rejected=rejected,MaximumOriginalResidual=maximum,State=state}));
}}
static void Main(string[] args){if(args.Length>0){var mode=JsonSerializer.Deserialize<Mode>(args[0]);if(!Enum.IsDefined(mode))throw new ArgumentException();if(mode==Mode.Acceptance){Acceptance();return;}}while(Console.ReadLine() is {} line){var data=JsonSerializer.Deserialize<double[][]>(line)!;int n=data.Length-1;var a=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)a[i,j]=data[i][j];try{var result=NewtonDirection.Solve(a,data[n],new int[n],new int[n],out var statistics);Console.WriteLine(JsonSerializer.Serialize(new{Outcome=Outcome.Direction,Value=result,Rank=statistics.Rank,Norm=statistics.EquilibratedNorm,Resolution=statistics.RankResolution}));}catch(InvalidOperationException e){Console.WriteLine(JsonSerializer.Serialize(new{Outcome=Outcome.Rejected,Reason=e.Message}));}}}
}
