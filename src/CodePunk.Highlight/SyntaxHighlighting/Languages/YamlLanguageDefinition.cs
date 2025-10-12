using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.SyntaxHighlighting.Languages;

/// <summary>
/// YAML language definition for syntax highlighting.
/// Provides a tokenizer for YAML configuration files with support for keys, values, and structures.
/// </summary>
public class YamlLanguageDefinition : ILanguageDefinition
{
    public string Name => "yaml";
    public string[] Aliases => new[] { "yml" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "true", "false", "yes", "no", "on", "off", "null", "~"
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
        var lineStart = true;

        while (pos < source.Length)
        {
            var ch = source[pos];

            // Handle newlines
            if (ch == '\n')
            {
                tokens.Add(new Token(TokenType.Text, "\n"));
                pos++;
                lineStart = true;
                continue;
            }

            if (ch == '\r')
            {
                tokens.Add(new Token(TokenType.Text, "\r"));
                pos++;
                continue;
            }

            // Comments
            if (ch == '#')
            {
                var start = pos;
                while (pos < source.Length && source[pos] != '\n')
                    pos++;
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // YAML directives (%, like %YAML or %TAG)
            if (lineStart && ch == '%')
            {
                var start = pos;
                while (pos < source.Length && source[pos] != '\n')
                    pos++;
                tokens.Add(new Token(TokenType.Preprocessor, source.Slice(start, pos - start).ToString()));
                lineStart = false;
                continue;
            }

            // Document markers (--- or ...)
            if (lineStart && ch == '-' && pos + 2 < source.Length &&
                source[pos + 1] == '-' && source[pos + 2] == '-')
            {
                tokens.Add(new Token(TokenType.Keyword, "---"));
                pos += 3;
                lineStart = false;
                continue;
            }

            if (lineStart && ch == '.' && pos + 2 < source.Length &&
                source[pos + 1] == '.' && source[pos + 2] == '.')
            {
                tokens.Add(new Token(TokenType.Keyword, "..."));
                pos += 3;
                lineStart = false;
                continue;
            }

            // Whitespace (preserving indentation is important in YAML)
            if (char.IsWhiteSpace(ch))
            {
                var start = pos;
                while (pos < source.Length && char.IsWhiteSpace(source[pos]) && source[pos] != '\n' && source[pos] != '\r')
                    pos++;
                tokens.Add(new Token(TokenType.Text, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // List items (-)
            if (lineStart && ch == '-' && pos + 1 < source.Length && char.IsWhiteSpace(source[pos + 1]))
            {
                tokens.Add(new Token(TokenType.Operator, "-"));
                pos++;
                lineStart = false;
                continue;
            }

            // Single-quoted strings
            if (ch == '\'')
            {
                var start = pos;
                pos++;
                while (pos < source.Length)
                {
                    if (source[pos] == '\'')
                    {
                        pos++;
                        // Check for escaped quote ('')
                        if (pos < source.Length && source[pos] == '\'')
                        {
                            pos++;
                            continue;
                        }
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                lineStart = false;
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
                lineStart = false;
                continue;
            }

            // Anchors (&anchor) and aliases (*alias)
            if (ch == '&' || ch == '*')
            {
                var start = pos;
                pos++;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_' || source[pos] == '-'))
                    pos++;
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                lineStart = false;
                continue;
            }

            // Tags (!tag)
            if (ch == '!')
            {
                var start = pos;
                pos++;
                while (pos < source.Length && !char.IsWhiteSpace(source[pos]))
                    pos++;
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                lineStart = false;
                continue;
            }

            // Key-value separator (:)
            if (ch == ':')
            {
                tokens.Add(new Token(TokenType.Operator, ":"));
                pos++;
                lineStart = false;
                continue;
            }

            // Flow collection indicators
            if (ch == '[' || ch == ']' || ch == '{' || ch == '}' || ch == ',')
            {
                tokens.Add(new Token(TokenType.Punctuation, ch.ToString()));
                pos++;
                lineStart = false;
                continue;
            }

            // Block indicators (| or >)
            if ((ch == '|' || ch == '>') && (pos + 1 >= source.Length || char.IsWhiteSpace(source[pos + 1]) || source[pos + 1] == '#'))
            {
                tokens.Add(new Token(TokenType.Operator, ch.ToString()));
                pos++;
                lineStart = false;
                continue;
            }

            // Numbers
            if (char.IsDigit(ch) || (ch == '-' && pos + 1 < source.Length && char.IsDigit(source[pos + 1])))
            {
                var start = pos;
                if (ch == '-') pos++;
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.' || source[pos] == 'e' || source[pos] == 'E' || source[pos] == '+' || source[pos] == '-'))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();
                tokens.Add(new Token(TokenType.Number, text));
                lineStart = false;
                continue;
            }

            // Keys and values (unquoted strings)
            if (char.IsLetter(ch) || ch == '_' || ch == '-')
            {
                var start = pos;
                while (pos < source.Length && !char.IsWhiteSpace(source[pos]) &&
                       source[pos] != ':' && source[pos] != '#' && source[pos] != ',' &&
                       source[pos] != '[' && source[pos] != ']' && source[pos] != '{' && source[pos] != '}')
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                // Check if it's a keyword
                if (Keywords.Contains(text))
                    tokens.Add(new Token(TokenType.Keyword, text));
                else
                    tokens.Add(new Token(TokenType.Identifier, text));

                lineStart = false;
                continue;
            }

            // Unknown character
            tokens.Add(new Token(TokenType.Text, ch.ToString()));
            pos++;
            lineStart = false;
        }

        return tokens;
    }
}
