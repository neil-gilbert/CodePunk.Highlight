using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.RazorConsole.Rendering;

/// <summary>
/// Provides color mapping for syntax token types using Spectre.Console color names.
/// </summary>
internal static class TokenColorPalette
{
    private static readonly IReadOnlyDictionary<TokenType, string> Colors = new Dictionary<TokenType, string>
    {
        [TokenType.Keyword] = "blue",
        [TokenType.Type] = "cyan",
        [TokenType.String] = "green",
        [TokenType.Comment] = "grey",
        [TokenType.Number] = "magenta",
        [TokenType.Operator] = "yellow",
        [TokenType.Punctuation] = "silver",
        [TokenType.Preprocessor] = "purple",
        [TokenType.Identifier] = "white",
        [TokenType.Text] = "default"
    };

    /// <summary>
    /// Gets the Spectre.Console color name for a given token type.
    /// </summary>
    /// <param name="tokenType">The token type.</param>
    /// <returns>A Spectre.Console color name string.</returns>
    public static string GetColor(TokenType tokenType)
        => Colors.TryGetValue(tokenType, out var color) ? color : "default";
}
