# CodePunk.Highlight

A powerful and extensible syntax highlighting library with a platform-agnostic core and beautiful terminal output via [Spectre.Console](https://spectreconsole.net/).

## Packages

- **CodePunk.Highlight.Core** - Core tokenization and language detection engine (no UI dependencies)
- **CodePunk.Highlight.Spectre** - Spectre.Console rendering for terminal output

## Features

- **Rich Syntax Highlighting** - Beautiful, colorized syntax highlighting for multiple programming languages
- **Built on Spectre.Console** - Leverages the power and elegance of Spectre.Console
- **Extensible Architecture** - Easy to add support for new languages
- **Zero Configuration** - Works out of the box with sensible defaults
- **Language Detection** - Automatic language detection from file extensions
- **Multiple Renderers** - Direct console output or markup string generation

## Supported Languages

- **Bash** (.sh, .bash, .zsh)
- **C** (.c, .h)
- **C#** (.cs, .csx, .razor)
- **Clojure** (.clj, .cljs, .cljc, .edn)
- **CSS** (.css)
- **Django/Jinja2** (.jinja, .jinja2, .djhtml)
- **Dockerfile** (Dockerfile, .dockerfile)
- **Elixir** (.ex, .exs)
- **Erlang** (.erl, .hrl)
- **F#** (.fs, .fsx, .fsi)
- **Go** (.go)
- **GraphQL** (.graphql, .gql)
- **Handlebars** (.hbs, .handlebars)
- **Haskell** (.hs)
- **HTML** (.html, .htm)
- **HTTP** (.http)
- **Java** (.java)
- **JavaScript** (.js, .jsx, .mjs, .cjs)
- **JSON** (.json)
- **Kotlin** (.kt, .kts)
- **Makefile** (Makefile, .mk)
- **Markdown** (.md, .markdown)
- **Objective-C** (.m, .mm)
- **Perl** (.pl, .pm)
- **PHP** (.php)
- **PowerShell** (.ps1, .psm1, .psd1)
- **Python** (.py, .pyw)
- **R** (.r)
- **Ruby** (.rb)
- **Rust** (.rs)
- **SCSS** (.scss)
- **SQL** (.sql)
- **Swift** (.swift)
- **TypeScript** (.ts, .tsx, .cts, .mts)
- **XML** (.xml, .xaml)
- **YAML** (.yaml, .yml)

## Installation

### For Spectre.Console Terminal Output

Install via NuGet:

```bash
dotnet add package CodePunk.Highlight.Spectre
```

Or via Package Manager Console:

```powershell
Install-Package CodePunk.Highlight.Spectre
```

### For Custom Renderers (Core Only)

If you want to implement your own renderer (HTML, Markdown, etc.), install just the core:

```bash
dotnet add package CodePunk.Highlight.Core
```

## Quick Start

### Basic Usage

```csharp
using CodePunk.Highlight.Core.SyntaxHighlighting;
using CodePunk.Highlight.SyntaxHighlighting.Languages;
using CodePunk.Highlight.Spectre.Rendering;
using Spectre.Console;

// Set up the highlighter with supported languages
var languages = new ILanguageDefinition[]
{
    new BashLanguageDefinition(),
    new CLanguageDefinition(),
    new CSharpLanguageDefinition(),
    new ClojureLanguageDefinition(),
    new CssLanguageDefinition(),
    new DjangoLanguageDefinition(),
    new DockerfileLanguageDefinition(),
    new ElixirLanguageDefinition(),
    new ErlangLanguageDefinition(),
    new FSharpLanguageDefinition(),
    new GoLanguageDefinition(),
    new GraphQLLanguageDefinition(),
    new HandlebarsLanguageDefinition(),
    new HaskellLanguageDefinition(),
    new HtmlLanguageDefinition(),
    new HttpLanguageDefinition(),
    new JavaLanguageDefinition(),
    new JavaScriptLanguageDefinition(),
    new JsonLanguageDefinition(),
    new KotlinLanguageDefinition(),
    new MakefileLanguageDefinition(),
    new MarkdownLanguageDefinition(),
    new ObjectiveCLanguageDefinition(),
    new PerlLanguageDefinition(),
    new PhpLanguageDefinition(),
    new PowerShellLanguageDefinition(),
    new PythonLanguageDefinition(),
    new RLanguageDefinition(),
    new RubyLanguageDefinition(),
    new RustLanguageDefinition(),
    new ScssLanguageDefinition(),
    new SqlLanguageDefinition(),
    new SwiftLanguageDefinition(),
    new TypeScriptLanguageDefinition(),
    new XmlLanguageDefinition(),
    new YamlLanguageDefinition()
};

var highlighter = new SyntaxHighlighter(languages);

// Highlight C# code
string code = """
    public class HelloWorld
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
        }
    }
    """;

var renderer = new SpectreTokenRenderer(AnsiConsole.Console);
highlighter.Highlight(code, "csharp", renderer);
```

### Language Detection from File Path

```csharp
using CodePunk.Highlight.Core.SyntaxHighlighting;

// Automatically detect language from file extension
string filePath = "MyClass.cs";
string? languageId = LanguageDetector.FromPath(filePath);

if (languageId != null)
{
    var renderer = new SpectreTokenRenderer(AnsiConsole.Console);
    highlighter.Highlight(code, languageId, renderer);
}
```

### Generate Markup Strings

If you want to generate Spectre markup strings instead of directly writing to the console:

```csharp
using System.Text;
using CodePunk.Highlight.Spectre.Rendering;

var builder = new StringBuilder();
var renderer = new MarkupTokenRenderer(builder);

highlighter.Highlight(code, "csharp", renderer);

string markup = builder.ToString();
AnsiConsole.MarkupLine(markup);
```

## Advanced Usage

### Custom Token Renderer

Implement your own renderer by implementing the `ITokenRenderer` interface:

```csharp
using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

public class CustomRenderer : ITokenRenderer
{
    public void BeginRender()
    {
        // Called before rendering starts
    }

    public void RenderToken(Token token)
    {
        // Render each token
        // token.Type: TokenType (Keyword, String, Comment, etc.)
        // token.Value: string content
    }

    public void EndRender()
    {
        // Called after rendering completes
    }
}
```

### List Available Languages

```csharp
var availableLanguages = highlighter.GetLanguages();

foreach (var language in availableLanguages)
{
    AnsiConsole.MarkupLine($"[cyan]{language.Name}[/] (aliases: {string.Join(", ", language.Aliases)})");
}
```

## Token Types

The library recognizes the following token types:

- `Keyword` - Language keywords (if, for, class, etc.)
- `Type` - Built-in types (int, string, bool, etc.)
- `String` - String literals
- `Comment` - Comments (single-line and multi-line)
- `Number` - Numeric literals
- `Operator` - Operators (+, -, *, etc.)
- `Punctuation` - Punctuation marks (;, {, }, etc.)
- `Preprocessor` - Preprocessor directives (#if, #define, etc.)
- `Identifier` - Variable and function names
- `Text` - Plain text

## Color Scheme

The default color palette:

| Token Type | Color |
|------------|-------|
| Keyword | Blue |
| Type | Cyan |
| String | Green |
| Comment | Grey |
| Number | Magenta |
| Operator | Yellow |
| Punctuation | Silver |
| Preprocessor | Purple |
| Identifier | White |
| Text | Default |

## Architecture

The library is split into two packages following the ColorCode-Universal pattern:

### CodePunk.Highlight.Core

Platform-agnostic tokenization engine with no UI dependencies:

```
CodePunk.Highlight.Core
└── SyntaxHighlighting
    ├── SyntaxHighlighter        - Main highlighter orchestrator
    ├── LanguageDetector         - File extension to language mapping
    ├── Abstractions
    │   ├── ILanguageDefinition  - Language tokenizer interface
    │   ├── ISyntaxHighlighter   - Main highlighter interface
    │   └── ITokenRenderer       - Token rendering interface
    ├── Languages
    │   ├── BashLanguageDefinition
    │   ├── CLanguageDefinition
    │   ├── CSharpLanguageDefinition
    │   ├── ... (40+ language definitions)
    │   └── YamlLanguageDefinition
    └── Tokenization
        ├── Token               - Token data structure
        └── TokenType           - Token type enumeration
```

### CodePunk.Highlight.Spectre

Spectre.Console rendering implementation:

```
CodePunk.Highlight.Spectre
└── Rendering
    ├── SpectreTokenRenderer    - Direct console output
    ├── MarkupTokenRenderer     - Generate markup strings
    └── TokenColorPalette       - Default color scheme
```

This separation allows:
- Use of the core engine for HTML, Markdown, or any custom renderer
- Smaller dependency footprint when UI framework is not needed
- Easy addition of new renderers (e.g., CodePunk.Highlight.Html)

## Contributing

Contributions are welcome! To add support for a new language:

1. Create a new class implementing `ILanguageDefinition`
2. Implement the `Tokenize` method with your language's syntax rules
3. Add the language to the `LanguageDetector.FromPath` method
4. Add unit tests in the test project

## Requirements

- .NET 9.0 or higher
- CodePunk.Highlight.Core - No dependencies
- CodePunk.Highlight.Spectre - Requires Spectre.Console 0.49.1 or higher

## Examples

### Complete Console Application

```csharp
using CodePunk.Highlight.Core.SyntaxHighlighting;
using CodePunk.Highlight.SyntaxHighlighting.Languages;
using CodePunk.Highlight.Spectre.Rendering;
using Spectre.Console;

var languages = new ILanguageDefinition[]
{
    new CSharpLanguageDefinition(),
    new PythonLanguageDefinition(),
    new JavaScriptLanguageDefinition()
};

var highlighter = new SyntaxHighlighter(languages);

// Python example
var pythonCode = """
    def fibonacci(n):
        if n <= 1:
            return n
        return fibonacci(n-1) + fibonacci(n-2)
    
    # Print first 10 fibonacci numbers
    for i in range(10):
        print(f"F({i}) = {fibonacci(i)}")
    """;

AnsiConsole.MarkupLine("[bold yellow]Python Code:[/]");
AnsiConsole.WriteLine();

var renderer = new SpectreTokenRenderer(AnsiConsole.Console);
highlighter.Highlight(pythonCode, "python", renderer);
```
