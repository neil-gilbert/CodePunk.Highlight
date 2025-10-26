using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// Erlang language definition for syntax highlighting.
/// Provides a tokenizer for Erlang with support for atoms, variables, pattern matching, and functional syntax.
/// </summary>
public class ErlangLanguageDefinition : ILanguageDefinition
{
    public string Name => "erlang";
    public string[] Aliases => new[] { "erl" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "after", "and", "andalso", "band", "begin", "bnot", "bor", "bsl", "bsr",
        "bxor", "case", "catch", "cond", "div", "end", "fun", "if", "let",
        "not", "of", "or", "orelse", "receive", "rem", "try", "when", "xor"
    };

    private static readonly HashSet<string> Directives = new(StringComparer.Ordinal)
    {
        "module", "export", "import", "compile", "vsn", "author", "include",
        "include_lib", "define", "undef", "ifdef", "ifndef", "else", "endif",
        "record", "type", "spec", "callback", "behaviour", "behavior"
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
            if (ch == '%')
            {
                var start = pos;
                while (pos < source.Length && source[pos] != '\n')
                    pos++;
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Preprocessor directives (-module, -export, etc.)
            if (ch == '-')
            {
                var start = pos;
                pos++;

                // Check if it's a directive
                if (pos < source.Length && char.IsLetter(source[pos]))
                {
                    var directiveStart = pos;
                    while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                        pos++;

                    var directive = source.Slice(directiveStart, pos - directiveStart).ToString();
                    if (Directives.Contains(directive))
                    {
                        tokens.Add(new Token(TokenType.Preprocessor, source.Slice(start, pos - start).ToString()));
                        continue;
                    }
                }

                // Not a directive, treat as operator
                pos = start;
                tokens.Add(new Token(TokenType.Operator, "-"));
                pos++;
                continue;
            }

            // Atoms (:atom or 'quoted atom')
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
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
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

            // Numbers (including base#value notation like 16#FF)
            if (char.IsDigit(ch))
            {
                var start = pos;

                // Integer part
                while (pos < source.Length && char.IsDigit(source[pos]))
                    pos++;

                // Check for base notation (2#101, 16#FF, etc.)
                if (pos < source.Length && source[pos] == '#')
                {
                    pos++;
                    while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                        pos++;
                    tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                    continue;
                }

                // Decimal point
                if (pos < source.Length && source[pos] == '.')
                {
                    // Check if it's actually a decimal point or end of expression
                    if (pos + 1 < source.Length && char.IsDigit(source[pos + 1]))
                    {
                        pos++;
                        while (pos < source.Length && char.IsDigit(source[pos]))
                            pos++;
                    }
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

            // Variables (start with uppercase or _)
            if (char.IsUpper(ch) || ch == '_')
            {
                var start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_' || source[pos] == '@'))
                    pos++;
                tokens.Add(new Token(TokenType.Identifier, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Atoms, keywords, and function names (start with lowercase)
            if (char.IsLower(ch))
            {
                var start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_' || source[pos] == '@'))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                TokenType type = TokenType.Type; // Atoms are Type by default
                if (Keywords.Contains(text))
                    type = TokenType.Keyword;

                tokens.Add(new Token(type, text));
                continue;
            }

            // Operators and special symbols
            if (IsOperatorStart(ch))
            {
                var start = pos;
                pos++;

                // Multi-character operators (==, /=, >=, =<, ->, etc.)
                while (pos < source.Length && IsOperatorPart(source[pos]))
                    pos++;

                tokens.Add(new Token(TokenType.Operator, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Punctuation
            if (ch == '(' || ch == ')' || ch == '[' || ch == ']' || ch == '{' || ch == '}' ||
                ch == ',' || ch == ';' || ch == '.' || ch == '|')
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

    private static bool IsOperatorStart(char ch) =>
        ch == '+' || ch == '-' || ch == '*' || ch == '/' || ch == '=' || ch == '<' || ch == '>' ||
        ch == '!' || ch == '?' || ch == ':' || ch == '#' || ch == '&';

    private static bool IsOperatorPart(char ch) =>
        ch == '+' || ch == '-' || ch == '*' || ch == '/' || ch == '=' || ch == '<' || ch == '>' ||
        ch == '!' || ch == '?' || ch == ':' || ch == '#' || ch == '&' || ch == '|';
}
