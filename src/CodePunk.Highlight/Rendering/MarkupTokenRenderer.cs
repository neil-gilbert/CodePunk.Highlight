using System.Text;
using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;
using Spectre.Console;

namespace CodePunk.Highlight.Rendering;

/// <summary>
/// Builds Spectre markup strings from syntax tokens.
/// </summary>
public sealed class MarkupTokenRenderer : ITokenRenderer
{
    private readonly StringBuilder _builder;

    public MarkupTokenRenderer(StringBuilder builder)
    {
        _builder = builder;
    }

    public void RenderToken(Token token)
    {
        var color = TokenColorPalette.GetColor(token.Type);
        var escaped = Markup.Escape(token.Value);

        if (color == "default")
        {
            _builder.Append(escaped);
        }
        else
        {
            _builder.Append('[').Append(color).Append(']').Append(escaped).Append("[/]");
        }
    }
}
