using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// Objective-C language definition for syntax highlighting.
/// Provides a tokenizer for Objective-C code with support for Objective-C specific syntax and C features.
/// </summary>
public class ObjectiveCLanguageDefinition : ILanguageDefinition
{
    public string Name => "objectivec";
    public string[] Aliases => new[] { "objc", "m" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        // C keywords
        "auto", "break", "case", "char", "const", "continue", "default", "do",
        "double", "else", "enum", "extern", "float", "for", "goto", "if",
        "inline", "int", "long", "register", "restrict", "return", "short",
        "signed", "sizeof", "static", "struct", "switch", "typedef", "union",
        "unsigned", "void", "volatile", "while",
        // Objective-C keywords
        "id", "Class", "SEL", "IMP", "BOOL", "nil", "Nil", "YES", "NO",
        "self", "super", "instancetype", "in", "out", "inout", "bycopy", "byref", "oneway",
        "nonatomic", "atomic", "strong", "weak", "copy", "assign", "retain", "unsafe_unretained",
        "readonly", "readwrite", "getter", "setter"
    };

    private static readonly HashSet<string> Directives = new(StringComparer.Ordinal)
    {
        "@interface", "@implementation", "@protocol", "@end", "@property", "@synthesize",
        "@dynamic", "@class", "@public", "@private", "@protected", "@package",
        "@try", "@catch", "@finally", "@throw", "@synchronized", "@autoreleasepool",
        "@selector", "@encode", "@protocol", "@optional", "@required",
        "@compatibility_alias", "@defs", "@available"
    };

    private static readonly HashSet<string> BuiltInTypes = new(StringComparer.Ordinal)
    {
        "NSString", "NSArray", "NSDictionary", "NSSet", "NSNumber", "NSObject",
        "NSInteger", "NSUInteger", "CGFloat", "NSError", "NSData", "NSDate",
        "NSURL", "UIView", "UIViewController", "NSMutableArray", "NSMutableDictionary",
        "NSMutableSet", "NSMutableString"
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

            // Multi-line comment
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

            // Objective-C string literals (@"...")
            if (ch == '@' && pos + 1 < source.Length && source[pos + 1] == '"')
            {
                var start = pos;
                pos += 2;
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

            // Objective-C directives (@interface, @property, etc.)
            if (ch == '@')
            {
                var start = pos;
                pos++;
                while (pos < source.Length && (char.IsLetter(source[pos]) || source[pos] == '_'))
                    pos++;

                var directive = source.Slice(start, pos - start).ToString();
                if (Directives.Contains(directive))
                {
                    tokens.Add(new Token(TokenType.Keyword, directive));
                    continue;
                }
                else
                {
                    // Could be @YES, @NO, etc.
                    tokens.Add(new Token(TokenType.Keyword, directive));
                    continue;
                }
            }

            // Preprocessor directives
            if (ch == '#')
            {
                var start = pos;
                while (pos < source.Length && source[pos] != '\n')
                    pos++;
                tokens.Add(new Token(TokenType.Preprocessor, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // C string literals
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
                       source[pos] == 'f' || source[pos] == 'F' || source[pos] == 'l' || source[pos] == 'L' ||
                       source[pos] == 'u' || source[pos] == 'U' || source[pos] == 'x' || source[pos] == 'X'))
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
        ch == '|' || ch == '^' || ch == '~' || ch == '?' || ch == ':';

    private static bool IsOperatorPart(char ch) =>
        ch == '+' || ch == '-' || ch == '=' || ch == '&' || ch == '|' ||
        ch == '<' || ch == '>' || ch == '?';

    private static bool IsPunctuation(char ch) =>
        ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == '[' || ch == ']' ||
        ch == ';' || ch == ',' || ch == '.';
}
