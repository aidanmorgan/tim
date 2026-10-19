using CuriousContraptions;
using Godot;
using Engine = twodog.Engine;

internal static class Program
{
    // The page owns the engine for its whole lifetime: the browser loop keeps running it after Main returns, so the
    // engine is held here for the process and disposed only when startup fails.
    internal static Engine? Running { get; private set; }

    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine("CuriousContraptions (web) starting...");

        // The Godot project's assembly owns the source-generated plugins
        // initializer; register it before Start() (there is no
        // GodotPlugins.dll on web).
        Engine.RegisterWebPluginsInitializer(TwoDogWebBoot.PluginsInitializer());

        // args come from the page's GODOT_CONFIG.args plus the
        // '--main-pack godot.pck' the engine loader prepends.
        var engine = new Engine("CuriousContraptions", args: args);
        try
        {
            engine.Start();
            GD.Print("2dog is running in the browser!");
            GD.Print("Scene Root: ", engine.Tree.CurrentScene.Name);
            // The browser loop owns the lifetime after Run() returns.
            engine.Run();
            Running = engine;
        }
        catch
        {
            await engine.DisposeAsync();
            throw;
        }

        return 0;
    }
}
