using System.Text.Json;
using CuriousContraptions.Physics;
enum OracleCase { ParticipantCancellation, HeldIncrement, AnisotropicContraction, PrescribedDerivative, ProductResidualUnderflow, Nonfinite, Subnormal, ProductOverflow, ZeroProductUnderflow }
static class Program
{
    static PreciseScalar P(double x)=>PreciseScalar.From(x);
    static void Report(OracleCase kind,double expected,Func<double> evaluate)
    {
        try { var actual=evaluate();Console.WriteLine(JsonSerializer.Serialize(new{Case=kind,Expected=expected,Actual=actual,Equal=actual==expected})); }
        catch(InvalidOperationException ex){Console.WriteLine(JsonSerializer.Serialize(new{Case=kind,Expected=expected,Rejected=true,Reason=ex.Message}));}
    }
    static void Reject(OracleCase kind,Func<double> evaluate)
    {
        try { var actual=evaluate();Console.WriteLine(JsonSerializer.Serialize(new{Case=kind,ExpectedRejection=true,Actual=actual,Rejected=false})); }
        catch(InvalidOperationException){Console.WriteLine(JsonSerializer.Serialize(new{Case=kind,ExpectedRejection=true,Rejected=true}));}
    }
    static void Main()
    {
        Report(OracleCase.ParticipantCancellation,1,()=>((P(Math.ScaleB(1,52))+P(1))+P(-Math.ScaleB(1,52))).Value);
        Report(OracleCase.HeldIncrement,Math.ScaleB(1,-54),()=>((P(1)+P(Math.ScaleB(1,-53))*P(.5))+P(-1)).Value);
        Report(OracleCase.AnisotropicContraction,41.8125,()=>
        {
            var j=new CollisionVector(.5,-1,2);
            return (PreciseScalar.Dot(j,new(2,.5,0))*(P(3)+P(1)*P(.25))+
                PreciseScalar.Dot(j,new(.5,3,.25))*(P(-2)+P(2)*P(.25))+
                PreciseScalar.Dot(j,new(0,.25,4))*(P(5)+P(-1)*P(.25))).Value;
        });
        Report(OracleCase.PrescribedDerivative,.25,()=>(PreciseScalar.Dot(new(1,-2,3),new(.25,-.5,.75))+PreciseScalar.Dot(new(-1,.5,2),new(2,-3,.125))).Value);
        Reject(OracleCase.Nonfinite,()=>P(double.NaN).Value);
        Reject(OracleCase.Subnormal,()=>P(double.Epsilon).Value);
        Reject(OracleCase.ProductOverflow,()=>(P(double.MaxValue)*P(2)).Value);
        Reject(OracleCase.ZeroProductUnderflow,()=>(P(1e-200)*P(1e-200)).Value);
        var a=Math.ScaleB(Math.BitIncrement(1),-511);
        var rounded=a*a;
        Report(OracleCase.ProductResidualUnderflow,Math.ScaleB(1,-126),()=>((P(a)*P(a)+P(-rounded))*P(Math.ScaleB(1,1000))).Value);
    }
}
