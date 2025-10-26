using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// Haskell language definition for syntax highlighting.
/// Provides a tokenizer for Haskell code with support for functional programming constructs.
/// </summary>
public class HaskellLanguageDefinition : ILanguageDefinition
{
    public string Name => "haskell";
    public string[] Aliases => new[] { "hs" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "module", "import", "where", "let", "in", "case", "of", "if", "then", "else",
        "do", "data", "type", "newtype", "class", "instance", "deriving", "foreign",
        "infixl", "infixr", "infix", "qualified", "as", "hiding", "forall", "mdo",
        "rec", "proc", "family", "default", "pattern"
    };

    private static readonly HashSet<string> BuiltInTypes = new(StringComparer.Ordinal)
    {
        "Int", "Integer", "Float", "Double", "Char", "String", "Bool", "Maybe",
        "Either", "IO", "Ordering", "True", "False", "Just", "Nothing", "Left",
        "Right", "LT", "EQ", "GT"
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

            // Single-line comments
            if (ch == '-' && pos + 1 < source.Length && source[pos + 1] == '-')
            {
                var start = pos;
                pos += 2;
                while (pos < source.Length && source[pos] != '\n')
                    pos++;
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Multi-line comments {- -}
            if (ch == '{' && pos + 1 < source.Length && source[pos + 1] == '-')
            {
                var start = pos;
                pos += 2;
                var depth = 1;
                while (pos < source.Length - 1 && depth > 0)
                {
                    if (source[pos] == '{' && source[pos + 1] == '-')
                    {
                        depth++;
                        pos += 2;
                    }
                    else if (source[pos] == '-' && source[pos + 1] == '}')
                    {
                        depth--;
                        pos += 2;
                    }
                    else
                    {
                        pos++;
                    }
                }
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Char literals
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

            // String literals
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

            // Numbers
            if (char.IsDigit(ch))
            {
                var start = pos;
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.' || source[pos] == 'e' || source[pos] == 'E' || source[pos] == 'x' || source[pos] == 'o'))
                    pos++;
                tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Identifiers, keywords, and types
            if (char.IsLetter(ch) || ch == '_')
            {
                var start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_' || source[pos] == '\''))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                TokenType type = TokenType.Identifier;
                if (Keywords.Contains(text))
                    type = TokenType.Keyword;
                else if (BuiltInTypes.Contains(text))
                    type = TokenType.Type;
                else if (char.IsUpper(text[0]))
                    type = TokenType.Type;

                tokens.Add(new Token(type, text));
                continue;
            }

            // Operators (including special Haskell operators)
            if (IsOperatorChar(ch))
            {
                var start = pos;
                while (pos < source.Length && IsOperatorChar(source[pos]))
                    pos++;

                var op = source.Slice(start, pos - start).ToString();
                // Special handling for :: and -> which are common in Haskell
                tokens.Add(new Token(TokenType.Operator, op));
                continue;
            }

            // Punctuation
            if (ch == '(' || ch == ')' || ch == '[' || ch == ']' || ch == '{' || ch == '}' || ch == ',' || ch == ';' || ch == '`')
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

    private static bool IsOperatorChar(char ch) =>
        ch == '+' || ch == '-' || ch == '*' || ch == '/' || ch == '=' || ch == '<' || ch == '>' ||
        ch == '!' || ch == '&' || ch == '|' || ch == ':' || ch == '.' || ch == '$' || ch == '?' ||
        ch == '~' || ch == '^' || ch == '%' || ch == '@' || ch == '#' || ch == '\\';
}
