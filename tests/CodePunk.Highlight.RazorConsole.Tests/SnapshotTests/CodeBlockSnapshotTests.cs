using Bunit;
using CodePunk.Highlight.RazorConsole.Components;
using CodePunk.Highlight.RazorConsole.Extensions;
using Xunit;

namespace CodePunk.Highlight.RazorConsole.Tests.SnapshotTests;

public class CodeBlockSnapshotTests : TestContext
{
    public CodeBlockSnapshotTests()
    {
        // Register syntax highlighting services
        Services.AddSyntaxHighlighting();
    }

    [Fact]
    public Task CodeBlock_CSharp_RendersCorrectly()
    {
        // Arrange
        var code = @"public class Program
{
    static void Main()
    {
        Console.WriteLine(""Hello, World!"");
    }
}";

        // Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, code)
            .Add(p => p.Language, "csharp"));

        // Assert
        return Verify(component.Markup);
    }

    [Fact]
    public Task CodeBlock_Python_RendersCorrectly()
    {
        // Arrange
        var code = @"def hello():
    print('Hello, World!')

if __name__ == '__main__':
    hello()";

        // Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, code)
            .Add(p => p.Language, "python"));

        // Assert
        return Verify(component.Markup)
            .UseMethodName("CodeBlock_Python_RendersCorrectly");
    }

    [Fact]
    public Task CodeBlock_JavaScript_RendersCorrectly()
    {
        // Arrange
        var code = @"const hello = () => {
    console.log('Hello, World!');
};

hello();";

        // Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, code)
            .Add(p => p.Language, "javascript"));

        // Assert
        return Verify(component.Markup)
            .UseMethodName("CodeBlock_JavaScript_RendersCorrectly");
    }

    [Fact]
    public Task CodeBlock_Json_RendersCorrectly()
    {
        // Arrange
        var code = @"{
    ""name"": ""CodePunk.Highlight"",
    ""version"": ""1.0.0"",
    ""enabled"": true,
    ""count"": 42
}";

        // Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, code)
            .Add(p => p.Language, "json"));

        // Assert
        return Verify(component.Markup)
            .UseMethodName("CodeBlock_Json_RendersCorrectly");
    }

    [Fact]
    public Task CodeBlock_Sql_RendersCorrectly()
    {
        // Arrange
        var code = @"SELECT id, name, email
FROM users
WHERE active = 1
ORDER BY created_at DESC;";

        // Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, code)
            .Add(p => p.Language, "sql"));

        // Assert
        return Verify(component.Markup)
            .UseMethodName("CodeBlock_Sql_RendersCorrectly");
    }

    [Fact]
    public Task CodeBlock_EmptyCode_RendersNothing()
    {
        // Arrange & Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, string.Empty)
            .Add(p => p.Language, "csharp"));

        // Assert
        return Verify(component.Markup)
            .UseMethodName("CodeBlock_EmptyCode_RendersNothing");
    }

    [Fact]
    public Task CodeBlock_UnknownLanguage_RendersAsPlainText()
    {
        // Arrange
        var code = "Some plain text content";

        // Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, code)
            .Add(p => p.Language, "unknown-language"));

        // Assert
        return Verify(component.Markup)
            .UseMethodName("CodeBlock_UnknownLanguage_RendersAsPlainText");
    }

    [Theory]
    [InlineData("python", "def hello():\n    pass")]
    [InlineData("javascript", "const x = 42;")]
    [InlineData("rust", "fn main() { }")]
    [InlineData("go", "func main() { }")]
    public Task CodeBlock_MultipleLanguages_RendersCorrectly(string language, string code)
    {
        // Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, code)
            .Add(p => p.Language, language));

        // Assert
        return Verify(component.Markup)
            .UseParameters(language);
    }
}
