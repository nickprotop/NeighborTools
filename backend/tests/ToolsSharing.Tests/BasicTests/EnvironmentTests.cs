namespace ToolsSharing.Tests.BasicTests;

/// <summary>
/// Basic environment and setup tests to ensure the test framework is working
/// </summary>
public class EnvironmentTests
{
    [Fact]
    public void Test_Framework_Should_Work()
    {
        // Arrange
        var expected = 2;
        
        // Act
        var result = 1 + 1;
        
        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(5, 10, 15)]
    [InlineData(-1, 1, 0)]
    [InlineData(0, 0, 0)]
    public void Add_Numbers_Should_Return_Correct_Sum(int a, int b, int expected)
    {
        // Act
        var result = a + b;
        
        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void DateTime_Now_Should_Not_Be_Default()
    {
        // Act
        var now = DateTime.Now;
        
        // Assert
        now.Should().NotBe(default(DateTime));
        now.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void String_Operations_Should_Work()
    {
        // Arrange
        var input = "Hello World";
        
        // Act
        var result = input.ToLower().Replace(" ", "_");
        
        // Assert
        result.Should().Be("hello_world");
    }

    [Fact]
    public void Collections_Should_Work()
    {
        // Arrange
        var list = new List<string> { "apple", "banana", "cherry" };
        
        // Act & Assert
        list.Should().HaveCount(3);
        list.Should().Contain("banana");
        list.Should().NotContain("orange");
        list.First().Should().Be("apple");
    }

    [Fact]
    public void Guid_Generation_Should_Work()
    {
        // Act
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        
        // Assert
        guid1.Should().NotBe(Guid.Empty);
        guid2.Should().NotBe(Guid.Empty);
        guid1.Should().NotBe(guid2);
    }

    [Fact]
    public void Exception_Handling_Should_Work()
    {
        // Arrange
        Action action = () => throw new InvalidOperationException("Test exception");
        
        // Act & Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public async Task Async_Operations_Should_Work()
    {
        // Arrange
        async Task<string> GetMessageAsync()
        {
            await Task.Delay(10);
            return "Hello Async";
        }

        // Act & Assert
        var result = await GetMessageAsync();
        result.Should().Be("Hello Async");
    }
}