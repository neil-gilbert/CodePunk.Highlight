using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// Rust language definition for syntax highlighting.
/// Provides a tokenizer for Rust code with support for modern language features.
/// </summary>
public class RustLanguageDefinition : ILanguageDefinition
{
    public string Name => "rust";
    public string[] Aliases => new[] { "rs" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "as", "async", "await", "break", "const", "continue", "crate", "dyn",
        "else", "enum", "extern", "false", "fn", "for", "if", "impl", "in",
        "let", "loop", "match", "mod", "move", "mut", "pub", "ref", "return",
        "self", "Self", "static", "struct", "super", "trait", "true", "type",
        "unsafe", "use", "where", "while", "abstract", "become", "box", "do",
        "final", "macro", "override", "priv", "typeof", "unsized", "virtual",
        "yield", "try", "union"
    };

    private static readonly HashSet<string> BuiltInTypes = new(StringComparer.Ordinal)
    {
        "i8", "i16", "i32", "i64", "i128", "isize", "u8", "u16", "u32", "u64",
        "u128", "usize", "f32", "f64", "bool", "char", "str", "String", "Vec",
        "Option", "Result", "Some", "None", "Ok", "Err", "Box", "Rc", "Arc",
        "Cell", "RefCell", "Mutex", "RwLock", "HashMap", "HashSet", "BTreeMap",
        "BTreeSet", "LinkedList", "VecDeque"
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
                while (pos < source.Length && source[pos] != '\n')
                    pos++;
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Multi-line comment (with nesting support)
            if (ch == '/' && pos + 1 < source.Length && source[pos + 1] == '*')
            {
                var start = pos;
                pos += 2;
                var depth = 1;
                while (pos < source.Length - 1 && depth > 0)
                {
                    if (source[pos] == '/' && source[pos + 1] == '*')
                    {
                        depth++;
                        pos += 2;
                    }
                    else if (source[pos] == '*' && source[pos + 1] == '/')
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

            // Raw string literals (r"..." or r#"..."#)
            if (ch == 'r' && pos + 1 < source.Length && (source[pos + 1] == '"' || source[pos + 1] == '#'))
            {
                var start = pos;
                pos++;
                var hashCount = 0;
                while (pos < source.Length && source[pos] == '#')
                {
                    hashCount++;
                    pos++;
                }
                if (pos < source.Length && source[pos] == '"')
                {
                    pos++;
                    // Find closing
                    while (pos < source.Length)
                    {
                        if (source[pos] == '"')
                        {
                            var endHashCount = 0;
                            var tempPos = pos + 1;
                            while (tempPos < source.Length && source[tempPos] == '#' && endHashCount < hashCount)
                            {
                                endHashCount++;
                                tempPos++;
                            }
                            if (endHashCount == hashCount)
                            {
                                pos = tempPos;
                                break;
                            }
                        }
                        pos++;
                    }
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
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

            // Numbers
            if (char.IsDigit(ch))
            {
                var start = pos;
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.' ||
                       source[pos] == '_' || source[pos] == 'e' || source[pos] == 'E' ||
                       source[pos] == 'x' || source[pos] == 'o' || source[pos] == 'b' ||
                       source[pos] == 'i' || source[pos] == 'u' || source[pos] == 'f'))
                    pos++;
                tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Lifetimes (starting with ')
            if (ch == '\'' && pos + 1 < source.Length && (char.IsLetter(source[pos + 1]) || source[pos + 1] == '_'))
            {
                var start = pos;
                pos++;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    pos++;
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Macros (ending with !)
            if (char.IsLetter(ch) || ch == '_')
            {
                var start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                // Check if followed by !
                if (pos < source.Length && source[pos] == '!')
                {
                    pos++;
                    tokens.Add(new Token(TokenType.Type, text + "!"));
                    continue;
                }

                TokenType type = TokenType.Identifier;
                if (Keywords.Contains(text))
                    type = TokenType.Keyword;
                else if (BuiltInTypes.Contains(text))
                    type = TokenType.Type;

                tokens.Add(new Token(type, text));
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
        ch == '|' || ch == '^' || ch == '~' || ch == '?' || ch == ':' || ch == '.';

    private static bool IsOperatorPart(char ch) =>
        ch == '+' || ch == '-' || ch == '=' || ch == '&' || ch == '|' ||
        ch == '<' || ch == '>' || ch == '?' || ch == ':' || ch == '.' || ch == '*';

    private static bool IsPunctuation(char ch) =>
        ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == '[' || ch == ']' ||
        ch == ';' || ch == ',' || ch == '#' || ch == '@';
}
