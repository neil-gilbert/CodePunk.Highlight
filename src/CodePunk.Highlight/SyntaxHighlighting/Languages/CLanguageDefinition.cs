using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.SyntaxHighlighting.Languages;

/// <summary>
/// C language definition for syntax highlighting.
/// Provides a tokenizer for C with support for keywords, types, preprocessor directives, and more.
/// </summary>
public class CLanguageDefinition : ILanguageDefinition
{
    public string Name => "c";
    public string[] Aliases => new[] { "h" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "if", "else", "for", "while", "do", "switch", "case", "default", "break",
        "continue", "return", "goto", "typedef", "struct", "union", "enum",
        "static", "extern", "auto", "register", "const", "volatile", "restrict",
        "inline", "sizeof", "typeof", "_Alignas", "_Alignof", "_Atomic", "_Bool",
        "_Complex", "_Generic", "_Imaginary", "_Noreturn", "_Static_assert",
        "_Thread_local"
    };

    private static readonly HashSet<string> Types = new(StringComparer.Ordinal)
    {
        "void", "char", "short", "int", "long", "float", "double", "signed",
        "unsigned", "size_t", "ssize_t", "ptrdiff_t", "intptr_t", "uintptr_t",
        "int8_t", "int16_t", "int32_t", "int64_t", "uint8_t", "uint16_t",
        "uint32_t", "uint64_t", "bool", "FILE", "NULL"
    };

    private static readonly HashSet<string> Literals = new(StringComparer.Ordinal)
    {
        "true", "false", "NULL"
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

            // Preprocessor directives
            if (ch == '#')
            {
                var start = pos;
                pos++;
                // Skip whitespace after #
                while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                    pos++;
                // Read directive name
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    pos++;
                // Read rest of line (including line continuations with \)
                while (pos < source.Length)
                {
                    if (source[pos] == '\\' && pos + 1 < source.Length && source[pos + 1] == '\n')
                    {
                        pos += 2;
                        continue;
                    }
                    if (source[pos] == '\n')
                        break;
                    pos++;
                }
                tokens.Add(new Token(TokenType.Preprocessor, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Single-line comments
            if (ch == '/' && pos + 1 < source.Length && source[pos + 1] == '/')
            {
                var start = pos;
                pos += 2;
                while (pos < source.Length && source[pos] != '\n')
                    pos++;
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Multi-line comments
            if (ch == '/' && pos + 1 < source.Length && source[pos + 1] == '*')
            {
                var start = pos;
                pos += 2;
                while (pos < source.Length - 1)
                {
                    if (source[pos] == '*' && source[pos + 1] == '/')
                    {
                        pos += 2;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
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

            // Character literals
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

            // Numbers
            if (char.IsDigit(ch))
            {
                var start = pos;
                var isHex = false;
                var isOctal = false;
                var isBinary = false;

                // Check for hex (0x), octal (0), or binary (0b)
                if (ch == '0' && pos + 1 < source.Length)
                {
                    if (source[pos + 1] == 'x' || source[pos + 1] == 'X')
                    {
                        isHex = true;
                        pos += 2;
                    }
                    else if (source[pos + 1] == 'b' || source[pos + 1] == 'B')
                    {
                        isBinary = true;
                        pos += 2;
                    }
                    else if (char.IsDigit(source[pos + 1]))
                    {
                        isOctal = true;
                        pos++;
                    }
                }

                while (pos < source.Length)
                {
                    var current = source[pos];

                    if (char.IsDigit(current))
                    {
                        pos++;
                        continue;
                    }

                    if (isHex && IsHexDigit(current))
                    {
                        pos++;
                        continue;
                    }

                    if (!isHex && !isBinary && !isOctal && current == '.')
                    {
                        pos++;
                        continue;
                    }

                    if (!isHex && !isBinary && !isOctal && (current == 'e' || current == 'E'))
                    {
                        pos++;
                        if (pos < source.Length && (source[pos] == '+' || source[pos] == '-'))
                            pos++;
                        continue;
                    }

                    // Suffixes: L, LL, U, UL, ULL, F, etc.
                    if (current == 'L' || current == 'U' || current == 'F' || current == 'l' || current == 'u' || current == 'f')
                    {
                        pos++;
                        // Handle LL, ULL, etc.
                        if (pos < source.Length && (source[pos] == 'L' || source[pos] == 'l' || source[pos] == 'U' || source[pos] == 'u'))
                            pos++;
                        break;
                    }

                    break;
                }

                tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Identifiers and keywords
            if (IsIdentifierStart(ch))
            {
                var start = pos;
                pos++;
                while (pos < source.Length && IsIdentifierPart(source[pos]))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                TokenType type = TokenType.Identifier;
                if (Keywords.Contains(text))
                    type = TokenType.Keyword;
                else if (Types.Contains(text))
                    type = TokenType.Type;
                else if (Literals.Contains(text))
                    type = TokenType.Keyword;

                tokens.Add(new Token(type, text));
                continue;
            }

            // Operators
            if (IsOperatorStart(ch))
            {
                var start = pos;
                pos++;
                // Multi-character operators
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

            // Everything else
            tokens.Add(new Token(TokenType.Text, ch.ToString()));
            pos++;
        }

        return tokens;
    }

    private static bool IsIdentifierStart(char ch) =>
        char.IsLetter(ch) || ch == '_';

    private static bool IsIdentifierPart(char ch) =>
        char.IsLetterOrDigit(ch) || ch == '_';

    private static bool IsOperatorStart(char ch) =>
        ch == '+' || ch == '-' || ch == '*' || ch == '/' || ch == '%' ||
        ch == '=' || ch == '!' || ch == '<' || ch == '>' || ch == '&' ||
        ch == '|' || ch == '^' || ch == '~' || ch == '?' || ch == ':' ||
        ch == '.';

    private static bool IsOperatorPart(char ch) =>
        ch == '+' || ch == '-' || ch == '=' || ch == '&' || ch == '|' ||
        ch == '<' || ch == '>' || ch == '?' || ch == '!' || ch == '*' ||
        ch == '/' || ch == '%' || ch == '.';

    private static bool IsPunctuation(char ch) =>
        ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == '[' || ch == ']' ||
        ch == ';' || ch == ',' || ch == ':';

    private static bool IsHexDigit(char ch) =>
        char.IsDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
}
