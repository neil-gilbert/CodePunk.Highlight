using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.SyntaxHighlighting.Languages;

/// <summary>
/// Dockerfile language definition for syntax highlighting.
/// Provides a tokenizer for Dockerfiles with support for instructions, arguments, and multi-line commands.
/// </summary>
public class DockerfileLanguageDefinition : ILanguageDefinition
{
    public string Name => "dockerfile";
    public string[] Aliases => new[] { "docker" };

    private static readonly HashSet<string> Instructions = new(StringComparer.OrdinalIgnoreCase)
    {
        "FROM", "RUN", "CMD", "LABEL", "MAINTAINER", "EXPOSE", "ENV", "ADD",
        "COPY", "ENTRYPOINT", "VOLUME", "USER", "WORKDIR", "ARG", "ONBUILD",
        "STOPSIGNAL", "HEALTHCHECK", "SHELL"
    };

    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "AS", "NONE", "CMD", "ENTRYPOINT"
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

            // Comments
            if (ch == '#')
            {
                var start = pos;
                while (pos < source.Length && source[pos] != '\n')
                    pos++;
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Whitespace
            if (char.IsWhiteSpace(ch))
            {
                var start = pos;
                while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                    pos++;
                tokens.Add(new Token(TokenType.Text, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Instructions (must be at start of line or after whitespace)
            if (char.IsLetter(ch))
            {
                var start = pos;
                while (pos < source.Length && char.IsLetter(source[pos]))
                    pos++;

                var word = source.Slice(start, pos - start).ToString();

                if (Instructions.Contains(word))
                {
                    tokens.Add(new Token(TokenType.Keyword, word));

                    // Parse the rest of the line
                    while (pos < source.Length && source[pos] != '\n')
                    {
                        var current = source[pos];

                        // Line continuation with backslash
                        if (current == '\\' && pos + 1 < source.Length && source[pos + 1] == '\n')
                        {
                            tokens.Add(new Token(TokenType.Operator, "\\"));
                            pos++;
                            tokens.Add(new Token(TokenType.Text, "\n"));
                            pos++;
                            continue;
                        }

                        // Whitespace
                        if (char.IsWhiteSpace(current))
                        {
                            var wsStart = pos;
                            while (pos < source.Length && char.IsWhiteSpace(source[pos]) && source[pos] != '\n')
                                pos++;
                            tokens.Add(new Token(TokenType.Text, source.Slice(wsStart, pos - wsStart).ToString()));
                            continue;
                        }

                        // Comments
                        if (current == '#')
                        {
                            var commentStart = pos;
                            while (pos < source.Length && source[pos] != '\n')
                                pos++;
                            tokens.Add(new Token(TokenType.Comment, source.Slice(commentStart, pos - commentStart).ToString()));
                            break;
                        }

                        // Double-quoted strings
                        if (current == '"')
                        {
                            var stringStart = pos;
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
                            tokens.Add(new Token(TokenType.String, source.Slice(stringStart, pos - stringStart).ToString()));
                            continue;
                        }

                        // Single-quoted strings
                        if (current == '\'')
                        {
                            var stringStart = pos;
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
                            tokens.Add(new Token(TokenType.String, source.Slice(stringStart, pos - stringStart).ToString()));
                            continue;
                        }

                        // Environment variables ($VAR or ${VAR})
                        if (current == '$')
                        {
                            var varStart = pos;
                            pos++;
                            if (pos < source.Length && source[pos] == '{')
                            {
                                pos++;
                                while (pos < source.Length && source[pos] != '}')
                                    pos++;
                                if (pos < source.Length) pos++; // Include closing brace
                            }
                            else
                            {
                                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                                    pos++;
                            }
                            tokens.Add(new Token(TokenType.Type, source.Slice(varStart, pos - varStart).ToString()));
                            continue;
                        }

                        // Keywords like AS
                        if (char.IsLetter(current))
                        {
                            var wordStart = pos;
                            while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_' || source[pos] == '-'))
                                pos++;

                            var identifier = source.Slice(wordStart, pos - wordStart).ToString();
                            if (Keywords.Contains(identifier))
                                tokens.Add(new Token(TokenType.Keyword, identifier));
                            else
                                tokens.Add(new Token(TokenType.Identifier, identifier));
                            continue;
                        }

                        // Operators and special characters
                        if (current == '=' || current == ':' || current == ',' || current == '[' || current == ']')
                        {
                            tokens.Add(new Token(TokenType.Operator, current.ToString()));
                            pos++;
                            continue;
                        }

                        // Numbers
                        if (char.IsDigit(current))
                        {
                            var numStart = pos;
                            while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.'))
                                pos++;
                            tokens.Add(new Token(TokenType.Number, source.Slice(numStart, pos - numStart).ToString()));
                            continue;
                        }

                        // Everything else
                        tokens.Add(new Token(TokenType.Text, current.ToString()));
                        pos++;
                    }
                    continue;
                }
                else
                {
                    // Not an instruction, treat as regular text
                    tokens.Add(new Token(TokenType.Text, word));
                }
                continue;
            }

            // Everything else (shouldn't normally reach here)
            tokens.Add(new Token(TokenType.Text, ch.ToString()));
            pos++;
        }

        return tokens;
    }
}
