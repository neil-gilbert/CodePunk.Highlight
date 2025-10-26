using System.IO;

namespace CodePunk.Highlight.Core.SyntaxHighlighting;

/// <summary>
/// Provides helpers for inferring syntax highlighting language identifiers.
/// </summary>
public static class LanguageDetector
{
    /// <summary>
    /// Attempts to infer a language identifier from a filesystem path.
    /// </summary>
    public static string? FromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        // Check filename first for special cases like Dockerfile, Makefile
        var filename = Path.GetFileName(path).ToLowerInvariant();
        if (filename.StartsWith("dockerfile"))
            return "dockerfile";
        if (filename.StartsWith("makefile") || filename == "gnumakefile")
            return "makefile";

        var extension = Path.GetExtension(path).ToLowerInvariant();

        return extension switch
        {
            ".cs" or ".csx" or ".razor" => "csharp",
            ".c" or ".h" => "c",
            ".js" or ".jsx" or ".mjs" or ".cjs" => "javascript",
            ".ts" or ".tsx" or ".cts" or ".mts" => "typescript",
            ".py" or ".pyw" => "python",
            ".sql" => "sql",
            ".go" => "go",
            ".java" => "java",
            ".clj" or ".cljs" or ".cljc" or ".edn" => "clojure",
            ".jinja" or ".jinja2" or ".djhtml" => "django",
            ".dockerfile" => "dockerfile",
            ".ex" or ".exs" => "elixir",
            ".erl" or ".hrl" => "erlang",
            ".fs" or ".fsx" or ".fsi" => "fsharp",
            ".graphql" or ".gql" => "graphql",
            ".hbs" or ".handlebars" => "handlebars",
            ".hs" => "haskell",
            ".http" => "http",
            ".json" => "json",
            ".kt" or ".kts" => "kotlin",
            ".mk" => "makefile",
            ".md" or ".markdown" => "markdown",
            ".m" or ".mm" => "objectivec",
            ".pl" or ".pm" => "perl",
            ".php" => "php",
            ".ps1" or ".psm1" or ".psd1" => "powershell",
            ".r" => "r",
            ".rb" => "ruby",
            ".rs" => "rust",
            ".scss" => "scss",
            ".swift" => "swift",
            ".xml" or ".xaml" => "xml",
            ".yaml" or ".yml" => "yaml",
            ".html" or ".htm" => "html",
            ".css" => "css",
            ".sh" or ".bash" or ".zsh" => "bash",
            _ => null
        };
    }
}
