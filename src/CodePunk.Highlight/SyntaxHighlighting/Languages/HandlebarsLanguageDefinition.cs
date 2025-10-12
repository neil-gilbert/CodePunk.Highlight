using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.SyntaxHighlighting.Languages;

/// <summary>
/// Handlebars language definition for syntax highlighting.
/// Provides a tokenizer for Handlebars templates with support for expressions, blocks, and comments.
/// </summary>
public class HandlebarsLanguageDefinition : ILanguageDefinition
{
    public string Name => "handlebars";
    public string[] Aliases => new[] { "hbs" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "if", "else", "unless", "each", "with", "as", "this", "lookup", "log"
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

            // Handlebars comments {{!-- --}} or {{! }}
            if (ch == '{' && pos + 2 < source.Length && source[pos + 1] == '{' && source[pos + 2] == '!')
            {
                var start = pos;
                pos += 3;

                // Check for {{!-- extended comment --}}
                if (pos + 1 < source.Length && source[pos] == '-' && source[pos + 1] == '-')
                {
                    pos += 2;
                    while (pos < source.Length - 4)
                    {
                        if (source[pos] == '-' && source[pos + 1] == '-' && source[pos + 2] == '}' && source[pos + 3] == '}')
                        {
                            pos += 4;
                            break;
                        }
                        pos++;
                    }
                }
                else
                {
                    // Regular comment {{! }}
                    while (pos < source.Length - 1)
                    {
                        if (source[pos] == '}' && source[pos + 1] == '}')
                        {
                            pos += 2;
                            break;
                        }
                        pos++;
                    }
                }
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Handlebars expressions {{...}}, {{#...}}, {{/...}}, {{{...}}}
            if (ch == '{' && pos + 1 < source.Length && source[pos + 1] == '{')
            {
                var start = pos;
                pos += 2;

                // Check for unescaped {{{ or triple braces
                var isUnescaped = pos < source.Length && source[pos] == '{';
                if (isUnescaped) pos++;

                // Add opening braces
                tokens.Add(new Token(TokenType.Punctuation, source.Slice(start, pos - start).ToString()));

                // Check for block helpers (# or /)
                if (pos < source.Length && (source[pos] == '#' || source[pos] == '/'))
                {
                    tokens.Add(new Token(TokenType.Operator, source[pos].ToString()));
                    pos++;
                }

                // Parse content inside handlebars
                while (pos < source.Length)
                {
                    var current = source[pos];

                    // Check for closing braces
                    if (current == '}' && pos + 1 < source.Length && source[pos + 1] == '}')
                    {
                        var closeStart = pos;
                        pos += 2;
                        if (isUnescaped && pos < source.Length && source[pos] == '}')
                            pos++;
                        tokens.Add(new Token(TokenType.Punctuation, source.Slice(closeStart, pos - closeStart).ToString()));
                        break;
                    }

                    // Whitespace
                    if (char.IsWhiteSpace(current))
                    {
                        var wsStart = pos;
                        while (pos < source.Length && char.IsWhiteSpace(source[pos]) && source[pos] != '}')
                            pos++;
                        tokens.Add(new Token(TokenType.Text, source.Slice(wsStart, pos - wsStart).ToString()));
                        continue;
                    }

                    // String literals
                    if (current == '"' || current == '\'')
                    {
                        var quote = current;
                        var stringStart = pos;
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
                        tokens.Add(new Token(TokenType.String, source.Slice(stringStart, pos - stringStart).ToString()));
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

                    // Identifiers and keywords
                    if (char.IsLetter(current) || current == '_' || current == '@' || current == '.')
                    {
                        var identStart = pos;
                        while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_' || source[pos] == '.' || source[pos] == '@' || source[pos] == '-'))
                            pos++;

                        var text = source.Slice(identStart, pos - identStart).ToString();
                        var word = text.TrimStart('@', '.');

                        if (Keywords.Contains(word))
                            tokens.Add(new Token(TokenType.Keyword, text));
                        else
                            tokens.Add(new Token(TokenType.Identifier, text));
                        continue;
                    }

                    // Operators
                    if (current == '=' || current == '!' || current == '>' || current == '<')
                    {
                        tokens.Add(new Token(TokenType.Operator, current.ToString()));
                        pos++;
                        continue;
                    }

                    // Punctuation
                    if (current == '(' || current == ')' || current == '[' || current == ']' || current == ',' || current == '|')
                    {
                        tokens.Add(new Token(TokenType.Punctuation, current.ToString()));
                        pos++;
                        continue;
                    }

                    // Unknown character
                    tokens.Add(new Token(TokenType.Text, current.ToString()));
                    pos++;
                }
                continue;
            }

            // Regular HTML/text content outside handlebars
            var textStart = pos;
            while (pos < source.Length && source[pos] != '{')
                pos++;

            if (pos > textStart)
            {
                tokens.Add(new Token(TokenType.Text, source.Slice(textStart, pos - textStart).ToString()));
            }
        }

        return tokens;
    }
}
