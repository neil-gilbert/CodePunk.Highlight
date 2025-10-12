using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.SyntaxHighlighting.Languages;

/// <summary>
/// CSS language definition for syntax highlighting.
/// Provides a tokenizer for CSS with support for selectors, properties, values, and at-rules.
/// </summary>
public class CssLanguageDefinition : ILanguageDefinition
{
    public string Name => "css";
    public string[] Aliases => new[] { "stylesheet" };

    private static readonly HashSet<string> AtRules = new(StringComparer.OrdinalIgnoreCase)
    {
        "@media", "@import", "@charset", "@namespace", "@supports", "@document",
        "@page", "@font-face", "@keyframes", "@-webkit-keyframes", "@-moz-keyframes",
        "@viewport", "@counter-style", "@font-feature-values", "@property"
    };

    private static readonly HashSet<string> CommonProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "color", "background", "background-color", "background-image", "border", "margin", "padding",
        "width", "height", "display", "position", "top", "left", "right", "bottom",
        "font-size", "font-family", "font-weight", "text-align", "flex", "grid",
        "opacity", "z-index", "overflow", "float", "clear", "content"
    };

    private static readonly HashSet<string> CommonValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "auto", "none", "inherit", "initial", "unset", "transparent", "solid",
        "block", "inline", "inline-block", "flex", "grid", "absolute", "relative",
        "fixed", "sticky", "hidden", "visible", "scroll", "center", "left", "right"
    };

    public bool Matches(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId)) return false;
        var normalized = languageId.ToLowerInvariant();
        return normalized == Name || Aliases.Contains(normalized);
    }

    public IEnumerable<Token> Tokenize(ReadOnlySpan<char> source)
    {
        var tokens = new List<Token>();
        var pos = 0;

        while (pos < source.Length)
        {
            var ch = source[pos];

            // Whitespace
            if (char.IsWhiteSpace(ch))
            {
                var start = pos;
                while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                    pos++;
                tokens.Add(new Token(TokenType.Text, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Comments
            if (ch == '/' && pos + 1 < source.Length && source[pos + 1] == '*')
            {
                var start = pos;
                pos += 2;
                while (pos < source.Length - 1)
                {
                    if (source[pos] == '*' && source[pos + 1] == '/')
                    {
                        pos += 2;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Strings (for attribute selectors, content property, etc.)
            if (ch == '"' || ch == '\'')
            {
                var start = pos;
                var quote = ch;
                pos++;
                while (pos < source.Length)
                {
                    if (source[pos] == '\\' && pos + 1 < source.Length)
                    {
                        pos += 2;
                        continue;
                    }
                    if (source[pos] == quote)
                    {
                        pos++;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // At-rules (@media, @import, etc.)
            if (ch == '@')
            {
                var start = pos;
                pos++;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '-'))
                    pos++;

                var atRule = source.Slice(start, pos - start).ToString();
                if (AtRules.Contains(atRule))
                    tokens.Add(new Token(TokenType.Preprocessor, atRule));
                else
                    tokens.Add(new Token(TokenType.Keyword, atRule));
                continue;
            }

            // Hexadecimal colors (#fff, #ffffff)
            if (ch == '#')
            {
                var start = pos;
                pos++;
                var hexCount = 0;
                while (pos < source.Length && IsHexDigit(source[pos]) && hexCount < 8)
                {
                    pos++;
                    hexCount++;
                }

                // Valid hex color: 3, 4, 6, or 8 digits
                if (hexCount == 3 || hexCount == 4 || hexCount == 6 || hexCount == 8)
                {
                    tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                    continue;
                }

                // Not a hex color, treat as ID selector
                pos = start + 1;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '-' || source[pos] == '_'))
                    pos++;
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Class selectors
            if (ch == '.')
            {
                var start = pos;
                pos++;
                if (pos < source.Length && (char.IsLetter(source[pos]) || source[pos] == '-' || source[pos] == '_'))
                {
                    while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '-' || source[pos] == '_'))
                        pos++;
                    tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                    continue;
                }
                // Just a dot (possibly decimal number)
                tokens.Add(new Token(TokenType.Punctuation, "."));
                continue;
            }

            // Numbers (including units: px, em, %, etc.)
            if (char.IsDigit(ch) || (ch == '.' && pos + 1 < source.Length && char.IsDigit(source[pos + 1])))
            {
                var start = pos;
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.'))
                    pos++;

                // Check for units
                if (pos < source.Length && char.IsLetter(source[pos]))
                {
                    while (pos < source.Length && char.IsLetter(source[pos]))
                        pos++;
                }
                // Check for percentage
                else if (pos < source.Length && source[pos] == '%')
                {
                    pos++;
                }

                tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Identifiers (properties, values, selectors)
            if (char.IsLetter(ch) || ch == '-' || ch == '_')
            {
                var start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '-' || source[pos] == '_'))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                // Skip whitespace to check what comes after
                var nextPos = pos;
                while (nextPos < source.Length && char.IsWhiteSpace(source[nextPos]))
                    nextPos++;

                TokenType type = TokenType.Identifier;

                // If followed by colon, it's likely a property
                if (nextPos < source.Length && source[nextPos] == ':')
                {
                    type = TokenType.Type;
                }
                // Check if it's a common value keyword
                else if (CommonValues.Contains(text))
                {
                    type = TokenType.Keyword;
                }
                // Important keyword
                else if (text.Equals("important", StringComparison.OrdinalIgnoreCase))
                {
                    type = TokenType.Keyword;
                }

                tokens.Add(new Token(type, text));
                continue;
            }

            // Operators and punctuation
            if (ch == ':' || ch == ';' || ch == ',' || ch == '>' || ch == '+' || ch == '~' || ch == '*')
            {
                tokens.Add(new Token(TokenType.Operator, ch.ToString()));
                pos++;
                continue;
            }

            // Brackets and braces
            if (ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == '[' || ch == ']')
            {
                tokens.Add(new Token(TokenType.Punctuation, ch.ToString()));
                pos++;
                continue;
            }

            // Exclamation mark (for !important)
            if (ch == '!')
            {
                tokens.Add(new Token(TokenType.Operator, "!"));
                pos++;
                continue;
            }

            // Everything else
            tokens.Add(new Token(TokenType.Text, ch.ToString()));
            pos++;
        }

        return tokens;
    }

    private static bool IsHexDigit(char ch) =>
        char.IsDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
}
