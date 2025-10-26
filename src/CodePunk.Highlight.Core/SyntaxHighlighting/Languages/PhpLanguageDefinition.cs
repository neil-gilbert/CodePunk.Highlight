using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// PHP language definition for syntax highlighting.
/// Provides a tokenizer for PHP code with support for HTML embedding and PHP-specific syntax.
/// </summary>
public class PhpLanguageDefinition : ILanguageDefinition
{
    public string Name => "php";
    public string[] Aliases => Array.Empty<string>();

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "abstract", "and", "array", "as", "break", "callable", "case", "catch",
        "class", "clone", "const", "continue", "declare", "default", "die", "do",
        "echo", "else", "elseif", "empty", "enddeclare", "endfor", "endforeach",
        "endif", "endswitch", "endwhile", "eval", "exit", "extends", "final",
        "finally", "fn", "for", "foreach", "function", "global", "goto", "if",
        "implements", "include", "include_once", "instanceof", "insteadof",
        "interface", "isset", "list", "match", "namespace", "new", "or", "print",
        "private", "protected", "public", "readonly", "require", "require_once",
        "return", "static", "switch", "throw", "trait", "try", "unset", "use",
        "var", "while", "xor", "yield", "yield from", "true", "false", "null",
        "__halt_compiler", "__CLASS__", "__DIR__", "__FILE__", "__FUNCTION__",
        "__LINE__", "__METHOD__", "__NAMESPACE__", "__TRAIT__"
    };

    private static readonly HashSet<string> BuiltInTypes = new(StringComparer.Ordinal)
    {
        "int", "float", "bool", "string", "array", "object", "callable", "iterable",
        "void", "mixed", "never", "self", "parent", "static"
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
        var inPhp = false;

        while (pos < source.Length)
        {
            var ch = source[pos];

            // Check for PHP opening tag
            if (!inPhp && ch == '<' && pos + 4 < source.Length && source[pos + 1] == '?' &&
                source[pos + 2] == 'p' && source[pos + 3] == 'h' && source[pos + 4] == 'p')
            {
                tokens.Add(new Token(TokenType.Keyword, "<?php"));
                pos += 5;
                inPhp = true;
                continue;
            }

            // Check for short PHP opening tag
            if (!inPhp && ch == '<' && pos + 1 < source.Length && source[pos + 1] == '?')
            {
                tokens.Add(new Token(TokenType.Keyword, "<?"));
                pos += 2;
                inPhp = true;
                continue;
            }

            // Check for PHP closing tag
            if (inPhp && ch == '?' && pos + 1 < source.Length && source[pos + 1] == '>')
            {
                tokens.Add(new Token(TokenType.Keyword, "?>"));
                pos += 2;
                inPhp = false;
                continue;
            }

            // If not in PHP mode, treat as HTML/text
            if (!inPhp)
            {
                var start = pos;
                while (pos < source.Length && !(source[pos] == '<' && pos + 1 < source.Length && source[pos + 1] == '?'))
                    pos++;
                if (pos > start)
                    tokens.Add(new Token(TokenType.Text, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // PHP code parsing
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

            // Hash comment
            if (ch == '#')
            {
                var start = pos;
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

            // Variables (starting with $)
            if (ch == '$')
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
                       source[pos] == 'e' || source[pos] == 'E' || source[pos] == 'x' || source[pos] == 'X'))
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
        ch == '|' || ch == '^' || ch == '~' || ch == '?' || ch == ':' || ch == '.';

    private static bool IsOperatorPart(char ch) =>
        ch == '+' || ch == '-' || ch == '=' || ch == '&' || ch == '|' ||
        ch == '<' || ch == '>' || ch == '?' || ch == ':';

    private static bool IsPunctuation(char ch) =>
        ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == '[' || ch == ']' ||
        ch == ';' || ch == ',' || ch == '\\';
}
