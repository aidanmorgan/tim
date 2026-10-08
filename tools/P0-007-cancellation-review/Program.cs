using System.Text.Json;
using CuriousContraptions.Geometry;
enum ProbeCase { AxisAligned, RotatedFifteen, RotatedCompound }
static class Program
{
    static void Main()
    {
        const double horizon=3.5896331742143114e-10;
        foreach(var kind in Enum.GetValues<ProbeCase>())
        {
            var rotation=kind switch
            {
                ProbeCase.AxisAligned=>RigidRotation.Identity,
                ProbeCase.RotatedFifteen=>RigidRotation.FromRotationVector(new(Math.PI/12,0,0)),
                ProbeCase.RotatedCompound=>RigidRotation.FromRotationVector(new(.3,.4,.5)),
                _=>throw new ArgumentOutOfRangeException()
            };
            var normal=rotation.Apply(new(0,0,1));
            var tangent=rotation.Apply(new(0,1,0));
            foreach(var speed in new[]{.1,.3,.6,1.0,3.0,6.099570687201605,48.75297577968597})
            {
                var velocity=tangent*speed;
                var dot=CollisionVector.Dot(normal,velocity);
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    Case=kind,Speed=speed,Horizon=horizon,Normal=normal,Velocity=velocity,
                    Dot=dot,Correction=dot/(horizon*.5),
                    HalfHorizonCorrection=dot/(horizon*.25),
                    DoubleHorizonCorrection=dot/horizon
                }));
            }
        }
    }
}
