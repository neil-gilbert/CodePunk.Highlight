using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// SCSS language definition for syntax highlighting.
/// Provides a tokenizer for SCSS/Sass CSS preprocessor syntax.
/// </summary>
public class ScssLanguageDefinition : ILanguageDefinition
{
    public string Name => "scss";
    public string[] Aliases => Array.Empty<string>();

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "and", "or", "not", "only", "from", "through", "to", "in"
    };

    private static readonly HashSet<string> AtRules = new(StringComparer.Ordinal)
    {
        "@import", "@include", "@extend", "@mixin", "@function", "@return",
        "@if", "@else", "@each", "@for", "@while", "@use", "@forward",
        "@at-root", "@debug", "@warn", "@error", "@media", "@supports",
        "@keyframes", "@font-face", "@charset", "@namespace", "@page"
    };

    private static readonly HashSet<string> BuiltInFunctions = new(StringComparer.Ordinal)
    {
        "rgb", "rgba", "hsl", "hsla", "lighten", "darken", "saturate", "desaturate",
        "mix", "adjust-hue", "complement", "invert", "alpha", "opacity", "scale-color",
        "change-color", "adjust-color", "grayscale", "percentage", "round", "ceil",
        "floor", "abs", "min", "max", "random", "length", "nth", "join", "append",
        "zip", "index", "list-separator", "map-get", "map-merge", "map-remove",
        "map-keys", "map-values", "map-has-key", "selector-nest", "selector-append",
        "selector-extend", "selector-replace", "selector-unify", "is-superselector",
        "simple-selectors", "selector-parse", "quote", "unquote", "str-length",
        "str-insert", "str-index", "str-slice", "to-upper-case", "to-lower-case",
        "unique-id", "unit", "unitless", "comparable", "call", "if", "type-of",
        "variable-exists", "global-variable-exists", "function-exists", "mixin-exists"
    };

    public bool Matches(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId)) return false;
        var normalized = languageId.ToLowerInvariant();
        return normalized == Name;
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

            // Single-line comment
            if (ch == '/' && pos + 1 < source.Length && source[pos + 1] == '/')
            {
                var start = pos;
                pos += 2;
                while (pos < source.Length && source[pos] != '\n')
                    pos++;
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Multi-line comment
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

            // At-rules and directives
            if (ch == '@')
            {
                var start = pos;
                pos++;
                while (pos < source.Length && (char.IsLetter(source[pos]) || source[pos] == '-'))
                    pos++;

                var directive = source.Slice(start, pos - start).ToString();
                if (AtRules.Contains(directive))
                    tokens.Add(new Token(TokenType.Keyword, directive));
                else
                    tokens.Add(new Token(TokenType.Type, directive));
                continue;
            }

            // Variables (starting with $)
            if (ch == '$')
            {
                var start = pos;
                pos++;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_' || source[pos] == '-'))
                    pos++;
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Interpolation (#{...})
            if (ch == '#' && pos + 1 < source.Length && source[pos + 1] == '{')
            {
                tokens.Add(new Token(TokenType.Operator, "#{"));
                pos += 2;

                // Parse content inside
                var depth = 1;
                while (pos < source.Length && depth > 0)
                {
                    if (source[pos] == '{')
                        depth++;
                    else if (source[pos] == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            tokens.Add(new Token(TokenType.Operator, "}"));
                            pos++;
                            break;
                        }
                    }

                    var contentStart = pos;
                    while (pos < source.Length && source[pos] != '{' && source[pos] != '}')
                        pos++;
                    if (pos > contentStart)
                        tokens.Add(new Token(TokenType.Text, source.Slice(contentStart, pos - contentStart).ToString()));
                }
                continue;
            }

            // Single-quoted strings
            if (ch == '\'')
            {
                var start = pos;
                pos++;
                while (pos < source.Length)
                {
                    if (source[pos] == '\\' && pos + 1 < source.Length)
                    {
                        pos += 2;
                        continue;
                    }
                    if (source[pos] == '\'')
                    {
                        pos++;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Double-quoted strings
            if (ch == '"')
            {
                var start = pos;
                pos++;
                while (pos < source.Length)
                {
                    if (source[pos] == '\\' && pos + 1 < source.Length)
                    {
                        pos += 2;
                        continue;
                    }
                    if (source[pos] == '"')
                    {
                        pos++;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Colors (# followed by hex digits)
            if (ch == '#' && pos + 1 < source.Length && IsHexDigit(source[pos + 1]))
            {
                var start = pos;
                pos++;
                while (pos < source.Length && IsHexDigit(source[pos]))
                    pos++;
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Numbers (including units)
            if (char.IsDigit(ch) || (ch == '.' && pos + 1 < source.Length && char.IsDigit(source[pos + 1])))
            {
                var start = pos;
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.'))
                    pos++;
                // Units
                if (pos < source.Length && char.IsLetter(source[pos]))
                {
                    while (pos < source.Length && (char.IsLetter(source[pos]) || source[pos] == '%'))
                        pos++;
                }
                tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Identifiers, keywords, and functions
            if (char.IsLetter(ch) || ch == '_' || ch == '-')
            {
                var start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_' || source[pos] == '-'))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                // Check if it's a function call
                if (pos < source.Length && source[pos] == '(')
                {
                    if (BuiltInFunctions.Contains(text))
                        tokens.Add(new Token(TokenType.Type, text));
                    else
                        tokens.Add(new Token(TokenType.Identifier, text));
                }
                else if (Keywords.Contains(text))
                {
                    tokens.Add(new Token(TokenType.Keyword, text));
                }
                else
                {
                    tokens.Add(new Token(TokenType.Identifier, text));
                }
                continue;
            }

            // Parent selector reference
            if (ch == '&')
            {
                tokens.Add(new Token(TokenType.Operator, "&"));
                pos++;
                continue;
            }

            // Operators
            if (IsOperatorStart(ch))
            {
                var start = pos;
                pos++;
                while (pos < source.Length && IsOperatorPart(source[pos]))
                    pos++;
                tokens.Add(new Token(TokenType.Operator, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Punctuation
            if (IsPunctuation(ch))
            {
                tokens.Add(new Token(TokenType.Punctuation, ch.ToString()));
                pos++;
                continue;
            }

            // Unknown character
            tokens.Add(new Token(TokenType.Text, ch.ToString()));
            pos++;
        }

        return tokens;
    }

    private static bool IsHexDigit(char ch) =>
        (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');

    private static bool IsOperatorStart(char ch) =>
        ch == '+' || ch == '-' || ch == '*' || ch == '/' || ch == '%' ||
        ch == '=' || ch == '!' || ch == '<' || ch == '>' || ch == ':';

    private static bool IsOperatorPart(char ch) =>
        ch == '=' || ch == '<' || ch == '>';

    private static bool IsPunctuation(char ch) =>
        ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == '[' || ch == ']' ||
        ch == ';' || ch == ',' || ch == '.' || ch == ':';
}
