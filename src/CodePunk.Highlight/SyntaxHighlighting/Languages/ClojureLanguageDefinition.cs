using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.SyntaxHighlighting.Languages;

/// <summary>
/// Clojure language definition for syntax highlighting.
/// Provides a tokenizer for Clojure with support for special forms, keywords, and Lisp syntax.
/// </summary>
public class ClojureLanguageDefinition : ILanguageDefinition
{
    public string Name => "clojure";
    public string[] Aliases => new[] { "clj", "cljs", "cljc", "edn" };

    private static readonly HashSet<string> SpecialForms = new(StringComparer.Ordinal)
    {
        "def", "defn", "defn-", "defmacro", "defmethod", "defmulti", "defonce",
        "defrecord", "defstruct", "deftype", "defprotocol", "definterface",
        "fn", "let", "letfn", "loop", "recur", "do", "if", "if-not", "if-let",
        "when", "when-not", "when-let", "when-first", "cond", "condp", "case",
        "and", "or", "not", "quote", "var", "set!", "throw", "try", "catch",
        "finally", "new", ".", "..", "->", "->>", "doto", "as->", "cond->",
        "cond->>", "some->", "some->>", "import", "require", "use", "ns",
        "in-ns", "refer", "refer-clojure"
    };

    private static readonly HashSet<string> CoreFunctions = new(StringComparer.Ordinal)
    {
        "apply", "map", "reduce", "filter", "remove", "take", "drop", "concat",
        "conj", "cons", "first", "rest", "last", "butlast", "assoc", "dissoc",
        "get", "get-in", "update", "update-in", "merge", "into", "select-keys",
        "keys", "vals", "count", "empty?", "nil?", "some?", "contains?",
        "println", "print", "str", "format", "pr-str", "prn-str"
    };

    private static readonly HashSet<string> Literals = new(StringComparer.Ordinal)
    {
        "true", "false", "nil"
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
            if (ch == ';')
            {
                var start = pos;
                while (pos < source.Length && source[pos] != '\n')
                    pos++;
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

            // Character literals (\a, \newline, \space, etc.)
            if (ch == '\\' && pos + 1 < source.Length && !char.IsWhiteSpace(source[pos + 1]))
            {
                var start = pos;
                pos++;
                // Named characters like \newline, \space, \tab
                while (pos < source.Length && char.IsLetter(source[pos]))
                    pos++;
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Keywords (:keyword or ::qualified-keyword)
            if (ch == ':')
            {
                var start = pos;
                pos++;
                // Handle :: for qualified keywords
                if (pos < source.Length && source[pos] == ':')
                    pos++;
                // Read the keyword name
                while (pos < source.Length && IsSymbolChar(source[pos]))
                    pos++;
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Numbers (including ratios like 22/7)
            if (char.IsDigit(ch) || (ch == '-' && pos + 1 < source.Length && char.IsDigit(source[pos + 1])))
            {
                var start = pos;
                if (ch == '-') pos++;

                // Hex (0x), octal (0), binary (2r)
                if (source[pos] == '0' && pos + 1 < source.Length)
                {
                    if (source[pos + 1] == 'x' || source[pos + 1] == 'X')
                    {
                        pos += 2;
                        while (pos < source.Length && IsHexDigit(source[pos]))
                            pos++;
                        tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                        continue;
                    }
                }

                // Regular numbers
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.'))
                    pos++;

                // Ratio (22/7)
                if (pos < source.Length && source[pos] == '/')
                {
                    pos++;
                    while (pos < source.Length && char.IsDigit(source[pos]))
                        pos++;
                }

                // Scientific notation
                if (pos < source.Length && (source[pos] == 'e' || source[pos] == 'E'))
                {
                    pos++;
                    if (pos < source.Length && (source[pos] == '+' || source[pos] == '-'))
                        pos++;
                    while (pos < source.Length && char.IsDigit(source[pos]))
                        pos++;
                }

                // Suffixes (M for BigDecimal, N for BigInt)
                if (pos < source.Length && (source[pos] == 'M' || source[pos] == 'N'))
                    pos++;

                tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Reader macros and special characters
            if (ch == '#')
            {
                var start = pos;
                pos++;
                // #{ for sets, #( for anonymous functions, #' for var quote, etc.
                if (pos < source.Length)
                {
                    var next = source[pos];
                    if (next == '{' || next == '(' || next == '\'' || next == '_' || next == '?' || next == '"')
                    {
                        pos++;
                        tokens.Add(new Token(TokenType.Operator, source.Slice(start, pos - start).ToString()));
                        continue;
                    }
                }
                tokens.Add(new Token(TokenType.Operator, "#"));
                continue;
            }

            // Quote, deref, syntax-quote, unquote
            if (ch == '\'' || ch == '@' || ch == '`' || ch == '~')
            {
                var start = pos;
                pos++;
                // Handle ~@ (unquote-splicing)
                if (ch == '~' && pos < source.Length && source[pos] == '@')
                    pos++;
                tokens.Add(new Token(TokenType.Operator, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Punctuation (parens, brackets, braces)
            if (ch == '(' || ch == ')' || ch == '[' || ch == ']' || ch == '{' || ch == '}')
            {
                tokens.Add(new Token(TokenType.Punctuation, ch.ToString()));
                pos++;
                continue;
            }

            // Symbols and identifiers
            if (IsSymbolStart(ch))
            {
                var start = pos;
                while (pos < source.Length && IsSymbolChar(source[pos]))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                TokenType type = TokenType.Identifier;
                if (SpecialForms.Contains(text))
                    type = TokenType.Keyword;
                else if (Literals.Contains(text))
                    type = TokenType.Keyword;
                else if (CoreFunctions.Contains(text))
                    type = TokenType.Type;

                tokens.Add(new Token(type, text));
                continue;
            }

            // Everything else
            tokens.Add(new Token(TokenType.Text, ch.ToString()));
            pos++;
        }

        return tokens;
    }

    private static bool IsSymbolStart(char ch) =>
        char.IsLetter(ch) || ch == '*' || ch == '+' || ch == '!' ||
        ch == '-' || ch == '_' || ch == '?' || ch == '<' || ch == '>' ||
        ch == '=' || ch == '$' || ch == '%' || ch == '&' || ch == '/' || ch == '.';

    private static bool IsSymbolChar(char ch) =>
        char.IsLetterOrDigit(ch) || ch == '*' || ch == '+' || ch == '!' ||
        ch == '-' || ch == '_' || ch == '?' || ch == '<' || ch == '>' ||
        ch == '=' || ch == '$' || ch == '%' || ch == '&' || ch == '/' ||
        ch == '.' || ch == ':' || ch == '#';

    private static bool IsHexDigit(char ch) =>
        char.IsDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
}
