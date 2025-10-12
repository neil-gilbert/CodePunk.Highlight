using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.SyntaxHighlighting.Languages;

/// <summary>
/// GraphQL language definition for syntax highlighting.
/// Provides a tokenizer for GraphQL schemas and queries.
/// </summary>
public class GraphQLLanguageDefinition : ILanguageDefinition
{
    public string Name => "graphql";
    public string[] Aliases => new[] { "gql" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "type", "query", "mutation", "subscription", "schema", "interface",
        "enum", "input", "implements", "fragment", "on", "extend", "scalar",
        "union", "directive", "repeatable", "null"
    };

    private static readonly HashSet<string> BuiltInTypes = new(StringComparer.Ordinal)
    {
        "Int", "Float", "String", "Boolean", "ID"
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
            if (ch == '#')
            {
                var start = pos;
                while (pos < source.Length && source[pos] != '\n')
                    pos++;
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // String literals
            if (ch == '"')
            {
                var start = pos;
                pos++;

                // Check for triple-quoted string
                if (pos + 1 < source.Length && source[pos] == '"' && source[pos + 1] == '"')
                {
                    pos += 2;
                    while (pos < source.Length - 2)
                    {
                        if (source[pos] == '"' && source[pos + 1] == '"' && source[pos + 2] == '"')
                        {
                            pos += 3;
                            break;
                        }
                        pos++;
                    }
                }
                else
                {
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
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Numbers
            if (char.IsDigit(ch) || (ch == '-' && pos + 1 < source.Length && char.IsDigit(source[pos + 1])))
            {
                var start = pos;
                if (ch == '-') pos++;
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.' || source[pos] == 'e' || source[pos] == 'E'))
                    pos++;
                tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Identifiers and keywords
            if (char.IsLetter(ch) || ch == '_')
            {
                var start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                TokenType type = TokenType.Identifier;
                if (Keywords.Contains(text))
                    type = TokenType.Keyword;
                else if (BuiltInTypes.Contains(text))
                    type = TokenType.Type;
                else if (text == "true" || text == "false")
                    type = TokenType.Keyword;

                tokens.Add(new Token(type, text));
                continue;
            }

            // Variables (starting with $)
            if (ch == '$')
            {
                var start = pos;
                pos++;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    pos++;
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Directives (starting with @)
            if (ch == '@')
            {
                var start = pos;
                pos++;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    pos++;
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Operators
            if (ch == '!' || ch == '=' || ch == ':' || ch == '&' || ch == '|')
            {
                tokens.Add(new Token(TokenType.Operator, ch.ToString()));
                pos++;
                continue;
            }

            // Punctuation
            if (ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == '[' || ch == ']' || ch == ',' || ch == '.')
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
}
