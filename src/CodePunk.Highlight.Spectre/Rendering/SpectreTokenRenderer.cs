using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;
using Spectre.Console;

namespace CodePunk.Highlight.Spectre.Rendering;

/// <summary>
/// Renders syntax tokens to Spectre.Console with color styling.
/// </summary>
public class SpectreTokenRenderer : ITokenRenderer
{
    private readonly IAnsiConsole _console;

    public SpectreTokenRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void RenderToken(Token token)
    {
        var color = TokenColorPalette.GetColor(token.Type);
        var escaped = Markup.Escape(token.Value);

        if (color == "default")
        {
            _console.Markup(escaped);
        }
        else
        {
            _console.Markup($"[{color}]{escaped}[/]");
        }
    }

    public void BeginRender()
    {
        // Optional: Could add a border or background here
    }

    public void EndRender()
    {
        _console.WriteLine(); // Ensure we end with a newline
    }
}
