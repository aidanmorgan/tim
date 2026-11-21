using CuriousContraptions.WorkshopWireGeneration;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: WorkshopWireGeneration <flatc-type-directory> <output-file>");
    return 2;
}
try
{
    GeneratedAccessors.Write(args[0], args[1]);
    return 0;
}
catch (Exception error) when (error is ArgumentException or IOException)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
