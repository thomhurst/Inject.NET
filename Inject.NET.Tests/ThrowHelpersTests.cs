using Inject.NET.Attributes;

namespace Inject.NET.Tests;

public partial class ThrowHelpersTests
{
    [Test]
    public async Task ThrowGenericMethod_ShouldThrowWithMessage()
    {
        var message = "Test error message";
        
        var exception = await Assert.ThrowsAsync<Exception>(() => Task.FromResult(ThrowHelpers.Throw<string>(message)));
        
        await Assert.That(exception.Message).IsEqualTo(message);
    }

    [Test]
    public async Task ThrowGenericMethod_WithDifferentTypes_ShouldThrowCorrectly()
    {
        var intMessage = "Integer error";
        var objectMessage = "Object error";
        
        var intException = await Assert.ThrowsAsync<Exception>(() => Task.FromResult(ThrowHelpers.Throw<int>(intMessage)));
        var objectException = await Assert.ThrowsAsync<Exception>(() => Task.FromResult(ThrowHelpers.Throw<object>(objectMessage)));
        
        await Assert.That(intException.Message).IsEqualTo(intMessage);
        await Assert.That(objectException.Message).IsEqualTo(objectMessage);
    }

    [Test]
    public async Task ThrowGenericMethod_WithNullMessage_ShouldThrowWithNullMessage()
    {
        string? nullMessage = null;
        
        var exception = await Assert.ThrowsAsync<Exception>(() => Task.FromResult(ThrowHelpers.Throw<string>(nullMessage!)));
        
        // When null is passed to Exception constructor, it creates a default message
        await Assert.That(exception.Message).IsNotNull();
    }

    [Test]
    public async Task ThrowGenericMethod_WithEmptyMessage_ShouldThrowWithEmptyMessage()
    {
        var emptyMessage = string.Empty;
        
        var exception = await Assert.ThrowsAsync<Exception>(() => Task.FromResult(ThrowHelpers.Throw<string>(emptyMessage)));
        
        await Assert.That(exception.Message).IsEqualTo(emptyMessage);
    }

    [Test]
    public async Task ThrowGenericMethod_WithComplexMessage_ShouldPreserveMessage()
    {
        var complexMessage = "Error occurred in type 'MyNamespace.MyClass' with parameter 'someParameter' at line 123";
        
        var exception = await Assert.ThrowsAsync<Exception>(() => Task.FromResult(ThrowHelpers.Throw<object>(complexMessage)));
        
        await Assert.That(exception.Message).IsEqualTo(complexMessage);
    }

    [Test]
    public async Task ThrowGenericMethod_WithMultilineMessage_ShouldPreserveFormat()
    {
        var multilineMessage = @"First line of error
Second line of error
Third line with details";
        
        var exception = await Assert.ThrowsAsync<Exception>(() => Task.FromResult(ThrowHelpers.Throw<string>(multilineMessage)));
        
        await Assert.That(exception.Message).IsEqualTo(multilineMessage);
    }

    [Test]
    public async Task ThrowGenericMethod_WithSpecialCharacters_ShouldPreserveCharacters()
    {
        var specialMessage = "Error: !@#$%^&*()_+-=[]{}|;:,.<>?";
        
        var exception = await Assert.ThrowsAsync<Exception>(() => Task.FromResult(ThrowHelpers.Throw<string>(specialMessage)));
        
        await Assert.That(exception.Message).IsEqualTo(specialMessage);
    }

    [Test]
    public async Task ThrowGenericMethod_WithUnicodeCharacters_ShouldPreserveUnicode()
    {
        var unicodeMessage = "Error: 你好 世界 🌍 €£¥";
        
        var exception = await Assert.ThrowsAsync<Exception>(() => Task.FromResult(ThrowHelpers.Throw<string>(unicodeMessage)));
        
        await Assert.That(exception.Message).IsEqualTo(unicodeMessage);
    }
}