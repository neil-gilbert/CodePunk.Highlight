using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.SyntaxHighlighting.Languages;

/// <summary>
/// Elixir language definition for syntax highlighting.
/// Provides a tokenizer for Elixir with support for atoms, sigils, keywords, and functional syntax.
/// </summary>
public class ElixirLanguageDefinition : ILanguageDefinition
{
    public string Name => "elixir";
    public string[] Aliases => new[] { "ex", "exs" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "def", "defp", "defmodule", "defmacro", "defmacrop", "defstruct", "defimpl",
        "defprotocol", "defdelegate", "defexception", "defoverridable",
        "do", "end", "if", "unless", "case", "cond", "when", "for", "with",
        "try", "catch", "rescue", "after", "else", "raise", "throw",
        "receive", "import", "require", "alias", "use", "quote", "unquote",
        "fn", "and", "or", "not", "in"
    };

    private static readonly HashSet<string> Literals = new(StringComparer.Ordinal)
    {
        "true", "false", "nil"
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

            // Atoms (:atom or :"atom with spaces")
            if (ch == ':')
            {
                var start = pos;
                pos++;

                // Quoted atom :"atom"
                if (pos < source.Length && source[pos] == '"')
                {
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
                }
                // Regular atom
                else if (pos < source.Length && (char.IsLetter(source[pos]) || source[pos] == '_'))
                {
                    while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_' || source[pos] == '?' || source[pos] == '!'))
                        pos++;
                }

                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Sigils (~r/regex/, ~s{string}, etc.)
            if (ch == '~')
            {
                var start = pos;
                pos++;
                if (pos < source.Length && char.IsLetter(source[pos]))
                {
                    pos++; // sigil character
                    if (pos < source.Length)
                    {
                        var delimiter = source[pos];
                        var closing = delimiter switch
                        {
                            '(' => ')',
                            '{' => '}',
                            '[' => ']',
                            '<' => '>',
                            _ => delimiter
                        };
                        pos++;
                        while (pos < source.Length && source[pos] != closing)
                        {
                            if (source[pos] == '\\' && pos + 1 < source.Length)
                                pos += 2;
                            else
                                pos++;
                        }
                        if (pos < source.Length) pos++; // closing delimiter

                        // Optional modifiers (like in ~r/regex/i)
                        while (pos < source.Length && char.IsLetter(source[pos]))
                            pos++;
                    }
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // String literals with interpolation
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

            // Character literals ('a', '\n', etc.)
            if (ch == '\'')
            {
                var start = pos;
                pos++;
                if (pos < source.Length)
                {
                    if (source[pos] == '\\' && pos + 1 < source.Length)
                        pos += 2;
                    else
                        pos++;
                }
                if (pos < source.Length && source[pos] == '\'')
                    pos++;
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Numbers (including 0x, 0o, 0b prefixes)
            if (char.IsDigit(ch))
            {
                var start = pos;

                // Hex, octal, binary
                if (ch == '0' && pos + 1 < source.Length)
                {
                    if (source[pos + 1] == 'x' || source[pos + 1] == 'X')
                    {
                        pos += 2;
                        while (pos < source.Length && IsHexDigit(source[pos]))
                            pos++;
                        tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                        continue;
                    }
                    else if (source[pos + 1] == 'o' || source[pos + 1] == 'O')
                    {
                        pos += 2;
                        while (pos < source.Length && source[pos] >= '0' && source[pos] <= '7')
                            pos++;
                        tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                        continue;
                    }
                    else if (source[pos + 1] == 'b' || source[pos + 1] == 'B')
                    {
                        pos += 2;
                        while (pos < source.Length && (source[pos] == '0' || source[pos] == '1'))
                            pos++;
                        tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                        continue;
                    }
                }

                // Regular numbers
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '_'))
                    pos++;

                // Decimal point
                if (pos < source.Length && source[pos] == '.')
                {
                    pos++;
                    while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '_'))
                        pos++;
                }

                // Exponent
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

            // Module names (start with uppercase)
            if (char.IsUpper(ch))
            {
                var start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    pos++;
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Keywords and identifiers
            if (char.IsLetter(ch) || ch == '_')
            {
                var start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    pos++;

                // Check for ? or ! suffix
                if (pos < source.Length && (source[pos] == '?' || source[pos] == '!'))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                TokenType type = TokenType.Identifier;
                if (Keywords.Contains(text))
                    type = TokenType.Keyword;
                else if (Literals.Contains(text))
                    type = TokenType.Keyword;

                tokens.Add(new Token(type, text));
                continue;
            }

            // Operators (including pipe |>, capture &, etc.)
            if (IsOperatorChar(ch))
            {
                var start = pos;
                pos++;
                // Multi-character operators
                while (pos < source.Length && IsOperatorChar(source[pos]))
                    pos++;
                tokens.Add(new Token(TokenType.Operator, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Punctuation
            if (ch == '(' || ch == ')' || ch == '[' || ch == ']' || ch == '{' || ch == '}' ||
                ch == ',' || ch == ';')
            {
                tokens.Add(new Token(TokenType.Punctuation, ch.ToString()));
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

    private static bool IsOperatorChar(char ch) =>
        ch == '+' || ch == '-' || ch == '*' || ch == '/' || ch == '=' || ch == '<' || ch == '>' ||
        ch == '!' || ch == '&' || ch == '|' || ch == '^' || ch == '~' || ch == '?' || ch == '.' ||
        ch == '@' || ch == '%' || ch == '\\';
}
