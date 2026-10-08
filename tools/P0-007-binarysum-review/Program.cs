using System.Text.Json;
using CuriousContraptions.Physics;
enum ResultKind { Value, Rejected }
static class Program
{
    static void Evaluate(double[][] products)
    {
        try
        {
            Span<ulong> storage=stackalloc ulong[BinaryProductSum.StorageLength];
            var sum=new BinaryProductSum(storage);
            foreach(var factors in products)sum.Add(factors[0],factors[1],factors[2],factors[3]);
            var result=sum.Finish();
            Console.WriteLine(JsonSerializer.Serialize(new { Kind=ResultKind.Value, Bits=BitConverter.DoubleToUInt64Bits(result) }));
        }
        catch(InvalidOperationException){Console.WriteLine(JsonSerializer.Serialize(new { Kind=ResultKind.Rejected }));}
        catch(ArgumentException){Console.WriteLine(JsonSerializer.Serialize(new { Kind=ResultKind.Rejected }));}
    }
    static void Main()
    {
        while(Console.ReadLine() is { } line)Evaluate(JsonSerializer.Deserialize<double[][]>(line)!);
    }
}
