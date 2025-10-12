using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.SyntaxHighlighting.Languages;

/// <summary>
/// Swift language definition for syntax highlighting.
/// Provides a tokenizer for Swift code with support for modern language features.
/// </summary>
public class SwiftLanguageDefinition : ILanguageDefinition
{
    public string Name => "swift";
    public string[] Aliases => Array.Empty<string>();

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "associatedtype", "class", "deinit", "enum", "extension", "fileprivate",
        "func", "import", "init", "inout", "internal", "let", "open", "operator",
        "private", "protocol", "public", "rethrows", "static", "struct", "subscript",
        "typealias", "var", "break", "case", "continue", "default", "defer", "do",
        "else", "fallthrough", "for", "guard", "if", "in", "repeat", "return",
        "switch", "where", "while", "as", "Any", "catch", "false", "is", "nil",
        "super", "self", "Self", "throw", "throws", "true", "try", "async", "await",
        "some", "actor", "isolated", "nonisolated"
    };

    private static readonly HashSet<string> BuiltInTypes = new(StringComparer.Ordinal)
    {
        "Int", "Int8", "Int16", "Int32", "Int64", "UInt", "UInt8", "UInt16",
        "UInt32", "UInt64", "Float", "Double", "Bool", "String", "Character",
        "Array", "Dictionary", "Set", "Optional", "Result", "Range", "ClosedRange",
        "Void", "Never", "AnyObject", "AnyHashable"
    };

    private static readonly HashSet<string> Attributes = new(StringComparer.Ordinal)
    {
        "@available", "@objc", "@nonobjc", "@NSCopying", "@NSManaged", "@UIApplicationMain",
        "@IBAction", "@IBOutlet", "@IBDesignable", "@IBInspectable", "@escaping",
        "@autoclosure", "@convention", "@discardableResult", "@dynamicCallable",
        "@dynamicMemberLookup", "@frozen", "@inlinable", "@propertyWrapper",
        "@resultBuilder", "@main", "@testable", "@_exported"
    };

    public bool Matches(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId)) return false;
        var normalized = languageId.ToLowerInvariant();
        return normalized == Name;
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

            // Multi-line comment (with nesting)
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

            // Attributes (starting with @)
            if (ch == '@')
            {
                var start = pos;
                pos++;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    pos++;

                var attr = source.Slice(start, pos - start).ToString();
                if (Attributes.Contains(attr))
                    tokens.Add(new Token(TokenType.Type, attr));
                else
                    tokens.Add(new Token(TokenType.Type, attr));
                continue;
            }

            // String literals (including multiline)
            if (ch == '"')
            {
                var start = pos;
                pos++;

                // Check for multiline string (""")
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
                        if (source[pos] == '\\' && pos + 1 < source.Length)
                        {
                            pos += 2;
                            continue;
                        }
                        pos++;
                    }
                }
                else
                {
                    // Regular string
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
            if (char.IsDigit(ch))
            {
                var start = pos;
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.' ||
                       source[pos] == '_' || source[pos] == 'e' || source[pos] == 'E' ||
                       source[pos] == 'x' || source[pos] == 'o' || source[pos] == 'b'))
                    pos++;
                tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Preprocessor directives (#if, #else, etc.)
            if (ch == '#')
            {
                var start = pos;
                pos++;
                while (pos < source.Length && (char.IsLetter(source[pos]) || source[pos] == '_'))
                    pos++;
                tokens.Add(new Token(TokenType.Preprocessor, source.Slice(start, pos - start).ToString()));
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
                else if (char.IsUpper(text[0]))
                    type = TokenType.Type; // Likely a type name

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
        ch == ';' || ch == ',' || ch == '$';
}
