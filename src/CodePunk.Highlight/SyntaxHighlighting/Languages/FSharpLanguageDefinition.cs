using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.SyntaxHighlighting.Languages;

/// <summary>
/// F# language definition for syntax highlighting.
/// Provides a tokenizer for F# with support for functional programming constructs, attributes, and operators.
/// </summary>
public class FSharpLanguageDefinition : ILanguageDefinition
{
    public string Name => "fsharp";
    public string[] Aliases => new[] { "fs" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "abstract", "and", "as", "assert", "base", "begin", "class", "default",
        "delegate", "do", "done", "downcast", "downto", "elif", "else", "end",
        "exception", "extern", "false", "finally", "for", "fun", "function",
        "global", "if", "in", "inherit", "inline", "interface", "internal",
        "lazy", "let", "match", "member", "module", "mutable", "namespace",
        "new", "not", "null", "of", "open", "or", "override", "private",
        "public", "rec", "return", "sig", "static", "struct", "then", "to",
        "true", "try", "type", "upcast", "use", "val", "void", "when", "while",
        "with", "yield", "atomic", "break", "checked", "component", "const",
        "constraint", "constructor", "continue", "eager", "event", "external",
        "fixed", "functor", "include", "method", "mixin", "object", "parallel",
        "process", "protected", "pure", "sealed", "tailcall", "trait", "virtual",
        "volatile"
    };

    private static readonly HashSet<string> BuiltInTypes = new(StringComparer.Ordinal)
    {
        "int", "int8", "int16", "int32", "int64", "uint8", "uint16", "uint32",
        "uint64", "nativeint", "unativeint", "float", "float32", "double",
        "single", "decimal", "bool", "byte", "sbyte", "char", "string",
        "unit", "obj", "exn", "list", "array", "seq", "option", "ref",
        "bigint", "nativeptr", "byref"
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

            // Single-line comment
            if (ch == '/' && pos + 1 < source.Length && source[pos + 1] == '/')
            {
                var start = pos;
                pos += 2;

                // Check for XML doc comment ///
                var isDocComment = pos < source.Length && source[pos] == '/';

                while (pos < source.Length && source[pos] != '\n')
                    pos++;
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Multi-line comment (* *)
            if (ch == '(' && pos + 1 < source.Length && source[pos + 1] == '*')
            {
                var start = pos;
                pos += 2;
                var depth = 1;

                while (pos < source.Length - 1 && depth > 0)
                {
                    if (source[pos] == '(' && source[pos + 1] == '*')
                    {
                        depth++;
                        pos += 2;
                    }
                    else if (source[pos] == '*' && source[pos + 1] == ')')
                    {
                        depth--;
                        pos += 2;
                        if (depth == 0)
                            break;
                    }
                    else
                    {
                        pos++;
                    }
                }
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Attributes [<...>]
            if (ch == '[' && pos + 1 < source.Length && source[pos + 1] == '<')
            {
                var start = pos;
                pos += 2;
                var depth = 1;

                while (pos < source.Length - 1 && depth > 0)
                {
                    if (source[pos] == '>' && source[pos + 1] == ']')
                    {
                        pos += 2;
                        depth--;
                    }
                    else
                    {
                        pos++;
                    }
                }
                tokens.Add(new Token(TokenType.Preprocessor, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Verbatim strings @"..."
            if (ch == '@' && pos + 1 < source.Length && source[pos + 1] == '"')
            {
                var start = pos;
                pos += 2;
                while (pos < source.Length)
                {
                    if (source[pos] == '"')
                    {
                        pos++;
                        // Check for escaped quote ""
                        if (pos < source.Length && source[pos] == '"')
                            pos++;
                        else
                            break;
                    }
                    else
                    {
                        pos++;
                    }
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Triple-quoted strings """..."""
            if (ch == '"' && pos + 2 < source.Length && source[pos + 1] == '"' && source[pos + 2] == '"')
            {
                var start = pos;
                pos += 3;
                while (pos < source.Length - 2)
                {
                    if (source[pos] == '"' && source[pos + 1] == '"' && source[pos + 2] == '"')
                    {
                        pos += 3;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Regular string literals
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

            // Numbers
            if (char.IsDigit(ch))
            {
                var start = pos;

                // Hex numbers
                if (ch == '0' && pos + 1 < source.Length && (source[pos + 1] == 'x' || source[pos + 1] == 'X'))
                {
                    pos += 2;
                    while (pos < source.Length && IsHexDigit(source[pos]))
                        pos++;
                }
                // Binary numbers
                else if (ch == '0' && pos + 1 < source.Length && (source[pos + 1] == 'b' || source[pos + 1] == 'B'))
                {
                    pos += 2;
                    while (pos < source.Length && (source[pos] == '0' || source[pos] == '1'))
                        pos++;
                }
                // Octal numbers
                else if (ch == '0' && pos + 1 < source.Length && (source[pos + 1] == 'o' || source[pos + 1] == 'O'))
                {
                    pos += 2;
                    while (pos < source.Length && source[pos] >= '0' && source[pos] <= '7')
                        pos++;
                }
                else
                {
                    // Regular decimal numbers
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
                }

                // Type suffixes (f, L, M, etc.)
                if (pos < source.Length && (source[pos] == 'f' || source[pos] == 'F' ||
                    source[pos] == 'L' || source[pos] == 'M' || source[pos] == 'm' ||
                    source[pos] == 'u' || source[pos] == 'U' || source[pos] == 'y' ||
                    source[pos] == 's' || source[pos] == 'l'))
                {
                    pos++;
                }

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
                    type = TokenType.Type; // F# types typically start with uppercase

                tokens.Add(new Token(type, text));
                continue;
            }

            // F# operators including |>, >>, <<, <|, ::, @, etc.
            if (IsOperatorChar(ch))
            {
                var start = pos;

                // Handle special multi-char operators
                if (ch == '|' && pos + 1 < source.Length && source[pos + 1] == '>')
                {
                    pos += 2;
                }
                else if (ch == '<' && pos + 1 < source.Length && source[pos + 1] == '|')
                {
                    pos += 2;
                }
                else if (ch == '>' && pos + 1 < source.Length && source[pos + 1] == '>')
                {
                    pos += 2;
                }
                else if (ch == '<' && pos + 1 < source.Length && source[pos + 1] == '<')
                {
                    pos += 2;
                }
                else if (ch == ':' && pos + 1 < source.Length && source[pos + 1] == ':')
                {
                    pos += 2;
                }
                else if (ch == ':' && pos + 1 < source.Length && source[pos + 1] == '>')
                {
                    pos += 2;
                }
                else if (ch == '<' && pos + 1 < source.Length && source[pos + 1] == '-')
                {
                    pos += 2;
                }
                else if (ch == '-' && pos + 1 < source.Length && source[pos + 1] == '>')
                {
                    pos += 2;
                }
                else
                {
                    pos++;
                    // Continue consuming operator characters
                    while (pos < source.Length && IsOperatorContinuation(source[pos]))
                        pos++;
                }

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

            // Unknown character - treat as text
            tokens.Add(new Token(TokenType.Text, ch.ToString()));
            pos++;
        }

        return tokens;
    }

    private static bool IsHexDigit(char ch) =>
        char.IsDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');

    private static bool IsOperatorChar(char ch) =>
        ch == '+' || ch == '-' || ch == '*' || ch == '/' || ch == '%' ||
        ch == '=' || ch == '!' || ch == '<' || ch == '>' || ch == '&' ||
        ch == '|' || ch == '^' || ch == '~' || ch == '?' || ch == ':' ||
        ch == '@' || ch == '\\' || ch == '.';

    private static bool IsOperatorContinuation(char ch) =>
        ch == '+' || ch == '-' || ch == '=' || ch == '&' || ch == '|' ||
        ch == '<' || ch == '>' || ch == '?' || ch == '!' || ch == '.';

    private static bool IsPunctuation(char ch) =>
        ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == '[' || ch == ']' ||
        ch == ';' || ch == ',';
}
