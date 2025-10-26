using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// Perl language definition for syntax highlighting.
/// Provides a tokenizer for Perl code with support for variables, operators, and common constructs.
/// </summary>
public class PerlLanguageDefinition : ILanguageDefinition
{
    public string Name => "perl";
    public string[] Aliases => new[] { "pl" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "if", "elsif", "else", "unless", "while", "until", "for", "foreach",
        "do", "continue", "next", "last", "redo", "goto", "return", "sub",
        "my", "our", "local", "state", "use", "require", "package", "BEGIN",
        "END", "CHECK", "INIT", "UNITCHECK", "no", "and", "or", "not", "xor",
        "given", "when", "default", "say", "print", "printf", "die", "warn",
        "exit", "eval", "exec", "system", "defined", "undef", "exists", "delete",
        "keys", "values", "each", "push", "pop", "shift", "unshift", "splice",
        "split", "join", "grep", "map", "sort", "reverse", "chomp", "chop",
        "length", "substr", "index", "rindex", "open", "close", "read", "write",
        "binmode", "seek", "tell", "eof", "readdir", "closedir", "opendir",
        "ref", "bless", "tie", "untie", "tied", "dbmopen", "dbmclose"
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

            // POD documentation (=pod ... =cut)
            if (pos == 0 || (pos > 0 && source[pos - 1] == '\n'))
            {
                if (ch == '=' && pos + 3 < source.Length &&
                    (source.Slice(pos, 4).ToString().StartsWith("=pod") ||
                     source.Slice(pos, 4).ToString().StartsWith("=cut") ||
                     source[pos + 1] != '='))
                {
                    var start = pos;
                    while (pos < source.Length)
                    {
                        if (source[pos] == '\n' && pos + 4 < source.Length &&
                            source.Slice(pos + 1, 4).ToString() == "=cut")
                        {
                            while (pos < source.Length && source[pos] != '\n')
                                pos++;
                            pos++;
                            break;
                        }
                        pos++;
                    }
                    tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                    continue;
                }
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

            // Regular expressions and substitutions (basic support)
            if (ch == '/' || (ch == 'm' && pos + 1 < source.Length && source[pos + 1] == '/') ||
                (ch == 's' && pos + 1 < source.Length && source[pos + 1] == '/'))
            {
                var start = pos;
                var isSubst = ch == 's';
                var isMatch = ch == 'm';

                if (isMatch || isSubst)
                    pos++;

                if (pos < source.Length && source[pos] == '/')
                {
                    pos++;
                    // First part
                    while (pos < source.Length)
                    {
                        if (source[pos] == '\\' && pos + 1 < source.Length)
                        {
                            pos += 2;
                            continue;
                        }
                        if (source[pos] == '/')
                        {
                            pos++;
                            break;
                        }
                        pos++;
                    }

                    // For substitution, parse replacement part
                    if (isSubst && pos < source.Length)
                    {
                        while (pos < source.Length)
                        {
                            if (source[pos] == '\\' && pos + 1 < source.Length)
                            {
                                pos += 2;
                                continue;
                            }
                            if (source[pos] == '/')
                            {
                                pos++;
                                break;
                            }
                            pos++;
                        }
                    }

                    // Flags
                    while (pos < source.Length && (char.IsLetter(source[pos])))
                        pos++;

                    tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                    continue;
                }
                else
                {
                    pos = start;
                }
            }

            // Scalar variables ($var)
            if (ch == '$')
            {
                var start = pos;
                pos++;
                if (pos < source.Length && (char.IsLetter(source[pos]) || source[pos] == '_'))
                {
                    while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                        pos++;
                }
                else if (pos < source.Length && (source[pos] == '#' || source[pos] == '$' ||
                         source[pos] == '@' || source[pos] == '%'))
                {
                    pos++;
                }
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Array variables (@array)
            if (ch == '@')
            {
                var start = pos;
                pos++;
                if (pos < source.Length && (char.IsLetter(source[pos]) || source[pos] == '_'))
                {
                    while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                        pos++;
                }
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Hash variables (%hash)
            if (ch == '%')
            {
                var start = pos;
                pos++;
                if (pos < source.Length && (char.IsLetter(source[pos]) || source[pos] == '_'))
                {
                    while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                        pos++;
                }
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Numbers
            if (char.IsDigit(ch))
            {
                var start = pos;
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.' ||
                       source[pos] == '_' || source[pos] == 'e' || source[pos] == 'E' ||
                       source[pos] == 'x' || source[pos] == 'b' || source[pos] == 'o'))
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

                if (Keywords.Contains(text))
                    tokens.Add(new Token(TokenType.Keyword, text));
                else
                    tokens.Add(new Token(TokenType.Identifier, text));
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
        ch == '+' || ch == '-' || ch == '*' || ch == '/' || ch == '%' ||
        ch == '=' || ch == '!' || ch == '<' || ch == '>' || ch == '&' ||
        ch == '|' || ch == '^' || ch == '~' || ch == '?' || ch == ':' ||
        ch == '.' || ch == '\\';

    private static bool IsOperatorPart(char ch) =>
        ch == '+' || ch == '-' || ch == '=' || ch == '&' || ch == '|' ||
        ch == '<' || ch == '>' || ch == '?' || ch == '!' || ch == '.' ||
        ch == '*' || ch == '/';

    private static bool IsPunctuation(char ch) =>
        ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == '[' || ch == ']' ||
        ch == ';' || ch == ',';
}
