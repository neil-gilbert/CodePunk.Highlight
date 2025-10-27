using System.Text;
using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;
using Spectre.Console;

namespace CodePunk.Highlight.RazorConsole.Rendering;

/// <summary>
/// Renders syntax tokens to Spectre.Console markup strings for use in RazorConsole components.
/// </summary>
public sealed class RazorConsoleTokenRenderer : ITokenRenderer
{
    private readonly StringBuilder _markupBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="RazorConsoleTokenRenderer"/> class.
    /// </summary>
    public RazorConsoleTokenRenderer()
    {
        _markupBuilder = new StringBuilder();
    }

    /// <summary>
    /// Renders a single token with appropriate Spectre.Console markup styling.
    /// </summary>
    /// <param name="token">The token to render.</param>
    public void RenderToken(Token token)
    {
        var color = TokenColorPalette.GetColor(token.Type);
        var escaped = Markup.Escape(token.Value);

        if (color == "default")
        {
            _markupBuilder.Append(escaped);
        }
        else
        {
            _markupBuilder.Append('[').Append(color).Append(']').Append(escaped).Append("[/]");
        }
    }

    /// <summary>
    /// Gets the rendered markup string.
    /// </summary>
    /// <returns>The complete Spectre.Console markup string.</returns>
    public string GetMarkup() => _markupBuilder.ToString();

    /// <summary>
    /// Clears the internal markup buffer.
    /// </summary>
    public void Clear() => _markupBuilder.Clear();
}
