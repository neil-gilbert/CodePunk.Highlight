# CodePunk.Highlight.RazorConsole

Syntax highlighting components for [RazorConsole](https://github.com/LittleLittleCloud/RazorConsole) applications.

## Overview

CodePunk.Highlight.RazorConsole brings powerful syntax highlighting to RazorConsole applications through easy-to-use Razor components. It leverages the CodePunk.Highlight.Core tokenization engine and Spectre.Console's markup system to render beautifully highlighted code in your terminal applications.

## Features

- **Razor Component Architecture**: Use familiar `<CodeBlock>` components in your RazorConsole apps
- **40+ Languages Supported**: C#, Python, JavaScript, TypeScript, SQL, JSON, and many more
- **Spectre.Console Integration**: Leverages Spectre.Console's color system for rich terminal output
- **Dependency Injection**: Simple service registration with `AddSyntaxHighlighting()`
- **Fully Tested**: Comprehensive unit tests, snapshot tests, and component tests

## Installation

```bash
dotnet add package CodePunk.Highlight.RazorConsole
```

## Quick Start

### 1. Register Services

In your `Program.cs`:

```csharp
using CodePunk.Highlight.RazorConsole.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .UseRazorConsole<MyComponent>()
    .ConfigureServices(services =>
    {
        services.AddSyntaxHighlighting();
    })
    .Build();

await host.RunAsync();
```

### 2. Use in Components

In your Razor component:

```razor
@using CodePunk.Highlight.RazorConsole.Components

<CodeBlock Code="@myCode" Language="csharp" />

@code {
    private string myCode = @"
public class Program
{
    static void Main()
    {
        Console.WriteLine(""Hello, World!"");
    }
}";
}
```

## Component Reference

### CodeBlock

Main component for rendering syntax-highlighted code.

**Parameters:**
- `Code` (required): The source code string to highlight
- `Language` (required): Language identifier (e.g., "csharp", "python", "javascript")

**Example:**

```razor
<CodeBlock Code="@code" Language="python" />
```

## Supported Languages

C#, Python, JavaScript, TypeScript, Java, Go, Rust, Ruby, PHP, Swift, Kotlin, C, C++, Objective-C, F#, Haskell, Elixir, Erlang, Clojure, R, Perl, SQL, HTML, CSS, SCSS, XML, JSON, YAML, Markdown, Bash, PowerShell, Dockerfile, Makefile, GraphQL, HTTP, Handlebars, Django

## Architecture

```
CodePunk.Highlight.Core (tokenization)
          ↓
CodePunk.Highlight.RazorConsole (components)
          ↓
RazorConsole (Spectre.Console-based rendering)
          ↓
Terminal output
```

## Testing

The project includes comprehensive tests:

- **Unit Tests**: Test the token renderer
- **Snapshot Tests**: Verify rendered output hasn't changed
- **Component Tests**: Test component behavior and lifecycle

Run tests:

```bash
dotnet test tests/CodePunk.Highlight.RazorConsole.Tests
```

## Example Application

See `examples/CodePunk.Highlight.RazorConsole.Example` for a complete working example.

## License

MIT License - see the main repository for details.

## Contributing

Contributions are welcome! Please open issues or pull requests in the [main repository](https://github.com/neil-gilbert/CodePunk.Highlight).
