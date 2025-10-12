using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.SyntaxHighlighting.Languages;

/// <summary>
/// R language definition for syntax highlighting.
/// Provides a tokenizer for R statistical programming language.
/// </summary>
public class RLanguageDefinition : ILanguageDefinition
{
    public string Name => "r";
    public string[] Aliases => Array.Empty<string>();

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "if", "else", "repeat", "while", "function", "for", "in", "next", "break",
        "TRUE", "FALSE", "NULL", "Inf", "NaN", "NA", "NA_integer_", "NA_real_",
        "NA_complex_", "NA_character_", "return", "switch", "invisible"
    };

    private static readonly HashSet<string> BuiltInFunctions = new(StringComparer.Ordinal)
    {
        "c", "list", "data.frame", "matrix", "array", "factor", "length", "dim",
        "nrow", "ncol", "str", "summary", "head", "tail", "names", "colnames",
        "rownames", "class", "typeof", "mode", "attributes", "attr", "print",
        "cat", "paste", "paste0", "sprintf", "format", "mean", "median", "sum",
        "min", "max", "sd", "var", "range", "seq", "rep", "which", "subset",
        "merge", "sort", "order", "unique", "table", "tapply", "apply", "lapply",
        "sapply", "mapply", "aggregate", "with", "within", "attach", "detach",
        "library", "require", "source", "setwd", "getwd", "rm", "ls", "exists",
        "read.csv", "read.table", "write.csv", "write.table", "plot", "hist",
        "boxplot", "barplot", "lines", "points", "abline", "legend", "par"
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

            // Comments
            if (ch == '#')
            {
                var start = pos;
                while (pos < source.Length && source[pos] != '\n')
                    pos++;
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

            // Numbers
            if (char.IsDigit(ch) || (ch == '.' && pos + 1 < source.Length && char.IsDigit(source[pos + 1])))
            {
                var start = pos;
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.' ||
                       source[pos] == 'e' || source[pos] == 'E' || source[pos] == 'L' ||
                       source[pos] == 'i' || source[pos] == 'x' || source[pos] == 'X'))
                    pos++;
                tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Identifiers and keywords
            if (char.IsLetter(ch) || ch == '_' || ch == '.')
            {
                var start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_' || source[pos] == '.'))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                TokenType type = TokenType.Identifier;
                if (Keywords.Contains(text))
                    type = TokenType.Keyword;
                else if (BuiltInFunctions.Contains(text))
                    type = TokenType.Type;

                tokens.Add(new Token(type, text));
                continue;
            }

            // Assignment operators (<- and <<-)
            if (ch == '<' && pos + 1 < source.Length && source[pos + 1] == '-')
            {
                if (pos + 2 < source.Length && source[pos + 2] == '<')
                {
                    tokens.Add(new Token(TokenType.Operator, "<<-"));
                    pos += 3;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Operator, "<-"));
                    pos += 2;
                }
                continue;
            }

            // Right assignment operators (-> and ->>)
            if (ch == '-' && pos + 1 < source.Length && source[pos + 1] == '>')
            {
                if (pos + 2 < source.Length && source[pos + 2] == '>')
                {
                    tokens.Add(new Token(TokenType.Operator, "->>"));
                    pos += 3;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Operator, "->"));
                    pos += 2;
                }
                continue;
            }

            // Pipe operator
            if (ch == '%')
            {
                var start = pos;
                pos++;
                while (pos < source.Length && source[pos] != '%')
                    pos++;
                if (pos < source.Length) pos++;
                tokens.Add(new Token(TokenType.Operator, source.Slice(start, pos - start).ToString()));
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
        ch == '+' || ch == '-' || ch == '*' || ch == '/' || ch == '^' ||
        ch == '=' || ch == '!' || ch == '<' || ch == '>' || ch == '&' ||
        ch == '|' || ch == '~' || ch == '?' || ch == ':';

    private static bool IsOperatorPart(char ch) =>
        ch == '=' || ch == '&' || ch == '|' || ch == '<' || ch == '>';

    private static bool IsPunctuation(char ch) =>
        ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == '[' || ch == ']' ||
        ch == ';' || ch == ',' || ch == '$' || ch == '@';
}
