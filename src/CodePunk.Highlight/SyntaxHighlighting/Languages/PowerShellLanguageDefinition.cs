using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.SyntaxHighlighting.Languages;

/// <summary>
/// PowerShell language definition for syntax highlighting.
/// Provides a tokenizer for PowerShell scripts with support for cmdlets, parameters, and variables.
/// </summary>
public class PowerShellLanguageDefinition : ILanguageDefinition
{
    public string Name => "powershell";
    public string[] Aliases => new[] { "ps1" };

    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "begin", "break", "catch", "class", "continue", "data", "define", "do",
        "dynamicparam", "else", "elseif", "end", "exit", "filter", "finally",
        "for", "foreach", "from", "function", "if", "in", "inlinescript",
        "parallel", "param", "process", "return", "sequence", "switch", "throw",
        "trap", "try", "until", "using", "var", "while", "workflow"
    };

    private static readonly HashSet<string> Operators = new(StringComparer.OrdinalIgnoreCase)
    {
        "-eq", "-ne", "-gt", "-ge", "-lt", "-le", "-like", "-notlike", "-match",
        "-notmatch", "-contains", "-notcontains", "-in", "-notin", "-replace",
        "-and", "-or", "-not", "-xor", "-band", "-bor", "-bnot", "-bxor",
        "-join", "-split", "-is", "-isnot", "-as", "-f"
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

            // Multi-line comments
            if (ch == '<' && pos + 1 < source.Length && source[pos + 1] == '#')
            {
                var start = pos;
                pos += 2;
                while (pos < source.Length - 1)
                {
                    if (source[pos] == '#' && source[pos + 1] == '>')
                    {
                        pos += 2;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
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
                        // Check for doubled single quote (escape)
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
                continue;
            }

            // Double-quoted strings
            if (ch == '"')
            {
                var start = pos;
                pos++;
                while (pos < source.Length)
                {
                    if (source[pos] == '`' && pos + 1 < source.Length)
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

            // Here-strings @' '@ or @" "@
            if (ch == '@' && pos + 1 < source.Length && (source[pos + 1] == '\'' || source[pos + 1] == '"'))
            {
                var quote = source[pos + 1];
                var start = pos;
                pos += 2;
                while (pos < source.Length - 1)
                {
                    if (source[pos] == quote && source[pos + 1] == '@')
                    {
                        pos += 2;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Variables (starting with $)
            if (ch == '$')
            {
                var start = pos;
                pos++;
                if (pos < source.Length && (char.IsLetter(source[pos]) || source[pos] == '_' || source[pos] == '{'))
                {
                    if (source[pos] == '{')
                    {
                        pos++;
                        while (pos < source.Length && source[pos] != '}')
                            pos++;
                        if (pos < source.Length) pos++;
                    }
                    else
                    {
                        while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_' || source[pos] == ':'))
                            pos++;
                    }
                }
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Cmdlets and functions (starting with Get-, Set-, etc.)
            if (char.IsLetter(ch))
            {
                var start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '-'))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                TokenType type = TokenType.Identifier;
                if (Keywords.Contains(text))
                    type = TokenType.Keyword;
                else if (text.Contains('-'))
                    type = TokenType.Keyword; // Likely a cmdlet

                tokens.Add(new Token(type, text));
                continue;
            }

            // Parameters (starting with -)
            if (ch == '-' && pos + 1 < source.Length && char.IsLetter(source[pos + 1]))
            {
                var start = pos;
                pos++;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos])))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                if (Operators.Contains(text))
                    tokens.Add(new Token(TokenType.Operator, text));
                else
                    tokens.Add(new Token(TokenType.Type, text));
                continue;
            }

            // Numbers
            if (char.IsDigit(ch))
            {
                var start = pos;
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.' ||
                       source[pos] == 'e' || source[pos] == 'E' || source[pos] == 'x' || source[pos] == 'X'))
                    pos++;
                tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
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

    private static bool IsOperatorStart(char ch) =>
        ch == '+' || ch == '*' || ch == '/' || ch == '%' ||
        ch == '=' || ch == '!' || ch == '<' || ch == '>' || ch == '&' ||
        ch == '|' || ch == '^' || ch == '~' || ch == '?' || ch == ':';

    private static bool IsOperatorPart(char ch) =>
        ch == '+' || ch == '=' || ch == '&' || ch == '|' ||
        ch == '<' || ch == '>' || ch == '?';

    private static bool IsPunctuation(char ch) =>
        ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == '[' || ch == ']' ||
        ch == ';' || ch == ',' || ch == '.' || ch == '@' || ch == '`';
}
