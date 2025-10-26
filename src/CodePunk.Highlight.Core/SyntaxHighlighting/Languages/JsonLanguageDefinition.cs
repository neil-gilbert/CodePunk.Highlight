using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// JSON language definition for syntax highlighting.
/// Provides a tokenizer for JSON data with support for objects, arrays, strings, numbers, and literals.
/// </summary>
public class JsonLanguageDefinition : ILanguageDefinition
{
    public string Name => "json";
    public string[] Aliases => Array.Empty<string>();

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

            // String literals (including property names)
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

            // Numbers (including negative, decimal, and scientific notation)
            if (char.IsDigit(ch) || (ch == '-' && pos + 1 < source.Length && char.IsDigit(source[pos + 1])))
            {
                var start = pos;
                if (ch == '-') pos++;

                // Integer part
                while (pos < source.Length && char.IsDigit(source[pos]))
                    pos++;

                // Decimal part
                if (pos < source.Length && source[pos] == '.')
                {
                    pos++;
                    while (pos < source.Length && char.IsDigit(source[pos]))
                        pos++;
                }

                // Exponent part
                if (pos < source.Length && (source[pos] == 'e' || source[pos] == 'E'))
                {
                    pos++;
                    if (pos < source.Length && (source[pos] == '+' || source[pos] == '-'))
                        pos++;
                    while (pos < source.Length && char.IsDigit(source[pos]))
                        pos++;
                }

                tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Boolean literals: true, false
            if (ch == 't' && pos + 3 < source.Length &&
                source[pos + 1] == 'r' && source[pos + 2] == 'u' && source[pos + 3] == 'e')
            {
                tokens.Add(new Token(TokenType.Keyword, "true"));
                pos += 4;
                continue;
            }

            if (ch == 'f' && pos + 4 < source.Length &&
                source[pos + 1] == 'a' && source[pos + 2] == 'l' && source[pos + 3] == 's' && source[pos + 4] == 'e')
            {
                tokens.Add(new Token(TokenType.Keyword, "false"));
                pos += 5;
                continue;
            }

            // Null literal
            if (ch == 'n' && pos + 3 < source.Length &&
                source[pos + 1] == 'u' && source[pos + 2] == 'l' && source[pos + 3] == 'l')
            {
                tokens.Add(new Token(TokenType.Keyword, "null"));
                pos += 4;
                continue;
            }

            // Punctuation: { } [ ] , :
            if (ch == '{' || ch == '}' || ch == '[' || ch == ']' || ch == ',' || ch == ':')
            {
                tokens.Add(new Token(TokenType.Punctuation, ch.ToString()));
                pos++;
                continue;
            }

            // Unknown character (shouldn't happen in valid JSON, but handle gracefully)
            tokens.Add(new Token(TokenType.Text, ch.ToString()));
            pos++;
        }

        return tokens;
    }
}
