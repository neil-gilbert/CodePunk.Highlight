using CodePunk.Highlight.RazorConsole.Extensions;

/*
 * Example RazorConsole Application with CodePunk.Highlight
 *
 * Note: This is a demonstration of how to use CodePunk.Highlight.RazorConsole
 * with the RazorConsole library. To run this example:
 *
 * 1. Install RazorConsole package when available
 * 2. Uncomment the code below
 * 3. Build and run
 */

Console.WriteLine("CodePunk.Highlight.RazorConsole Example");
Console.WriteLine("========================================");
Console.WriteLine();
Console.WriteLine("This example demonstrates syntax highlighting in RazorConsole applications.");
Console.WriteLine("To run this example, RazorConsole package must be installed.");
Console.WriteLine();
Console.WriteLine("Example usage:");
Console.WriteLine();
Console.WriteLine(@"
// In your Program.cs:
var host = Host.CreateDefaultBuilder(args)
    .UseRazorConsole<CodeViewer>()
    .ConfigureServices(services =>
    {
        services.AddSyntaxHighlighting();
    })
    .Build();

await host.RunAsync();

// In your CodeViewer.razor component:
@using CodePunk.Highlight.RazorConsole.Components

<CodeBlock Code=""@myCode"" Language=""csharp"" />
");

// Uncomment when RazorConsole is available:
/*
var host = Host.CreateDefaultBuilder(args)
    .UseRazorConsole<CodeViewer>()
    .ConfigureServices(services =>
    {
        services.AddSyntaxHighlighting();
    })
    .Build();

await host.RunAsync();
*/
