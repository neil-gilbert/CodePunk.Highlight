using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.SyntaxHighlighting.Languages;

/// <summary>
/// Ruby language definition for syntax highlighting.
/// Provides a tokenizer for Ruby code with support for Ruby-specific syntax.
/// </summary>
public class RubyLanguageDefinition : ILanguageDefinition
{
    public string Name => "ruby";
    public string[] Aliases => new[] { "rb" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "BEGIN", "END", "alias", "and", "begin", "break", "case", "class", "def",
        "defined?", "do", "else", "elsif", "end", "ensure", "false", "for", "if",
        "in", "module", "next", "nil", "not", "or", "redo", "rescue", "retry",
        "return", "self", "super", "then", "true", "undef", "unless", "until",
        "when", "while", "yield", "__FILE__", "__LINE__", "__ENCODING__"
    };

    private static readonly HashSet<string> BuiltInClasses = new(StringComparer.Ordinal)
    {
        "Array", "Hash", "String", "Symbol", "Integer", "Float", "Range", "Regexp",
        "Class", "Module", "Object", "Proc", "Lambda", "Thread", "File", "Dir",
        "Time", "Date", "DateTime", "Numeric", "Complex", "Rational", "Set",
        "Struct", "Enumerable", "Enumerator", "Exception", "StandardError"
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

            // Multi-line comments (=begin ... =end)
            if (pos == 0 || (pos > 0 && source[pos - 1] == '\n'))
            {
                if (ch == '=' && pos + 5 < source.Length &&
                    source.Slice(pos, 6).ToString() == "=begin")
                {
                    var start = pos;
                    pos += 6;
                    while (pos < source.Length)
                    {
                        if (source[pos] == '\n' && pos + 4 < source.Length &&
                            source.Slice(pos + 1, 4).ToString() == "=end")
                        {
                            pos += 5;
                            while (pos < source.Length && source[pos] != '\n')
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

            // Regular expressions
            if (ch == '/')
            {
                var start = pos;
                pos++;
                var isRegex = false;
                while (pos < source.Length)
                {
                    if (source[pos] == '\\' && pos + 1 < source.Length)
                    {
                        pos += 2;
                        isRegex = true;
                        continue;
                    }
                    if (source[pos] == '/')
                    {
                        pos++;
                        // Check for regex flags
                        while (pos < source.Length && (source[pos] == 'i' || source[pos] == 'm' || source[pos] == 'x'))
                            pos++;
                        break;
                    }
                    if (source[pos] == '\n')
                        break;
                    pos++;
                    isRegex = true;
                }

                if (isRegex)
                {
                    tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                    continue;
                }
                else
                {
                    pos = start;
                }
            }

            // Symbols (starting with :)
            if (ch == ':' && pos + 1 < source.Length && (char.IsLetter(source[pos + 1]) || source[pos + 1] == '_'))
            {
                var start = pos;
                pos++;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_' || source[pos] == '?' || source[pos] == '!'))
                    pos++;
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Instance variables (starting with @)
            if (ch == '@')
            {
                var start = pos;
                pos++;
                if (pos < source.Length && source[pos] == '@')
                    pos++; // Class variable
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    pos++;
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Global variables (starting with $)
            if (ch == '$')
            {
                var start = pos;
                pos++;
                if (pos < source.Length && (char.IsLetter(source[pos]) || source[pos] == '_'))
                {
                    while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                        pos++;
                }
                else if (pos < source.Length)
                {
                    pos++; // Special globals like $!, $?, etc.
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
                       source[pos] == 'x' || source[pos] == 'o' || source[pos] == 'b'))
                    pos++;
                tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Identifiers and keywords
            if (char.IsLetter(ch) || ch == '_')
            {
                var start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_' || source[pos] == '?' || source[pos] == '!'))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                TokenType type = TokenType.Identifier;
                if (Keywords.Contains(text))
                    type = TokenType.Keyword;
                else if (BuiltInClasses.Contains(text))
                    type = TokenType.Type;
                else if (char.IsUpper(text[0]))
                    type = TokenType.Type; // Constants/Classes

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
        ch == '|' || ch == '^' || ch == '~' || ch == '?' || ch == '.';

    private static bool IsOperatorPart(char ch) =>
        ch == '+' || ch == '-' || ch == '=' || ch == '&' || ch == '|' ||
        ch == '<' || ch == '>' || ch == '?' || ch == '.' || ch == '*';

    private static bool IsPunctuation(char ch) =>
        ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == '[' || ch == ']' ||
        ch == ';' || ch == ',' || ch == ':' || ch == '\\';
}
