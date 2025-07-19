using ToolsSharing.Core.Common.Models;

namespace ToolsSharing.Tests.Unit.Models;

/// <summary>
/// Tests for API response models and their behavior
/// </summary>
public class ApiResponseTests
{
    [Fact]
    public void ApiResponse_Should_Initialize_With_Default_Values()
    {
        // Act
        var response = new ApiResponse<string>();

        // Assert
        response.Success.Should().BeFalse();
        response.Message.Should().BeNull();
        response.Data.Should().BeNull();
        response.Errors.Should().NotBeNull();
        response.Errors.Should().BeEmpty();
    }

    [Fact]
    public void SuccessResult_Should_Return_Successful_Response()
    {
        // Arrange
        var data = "test data";
        var message = "Operation successful";

        // Act
        var response = ApiResponse<string>.SuccessResult(data, message);

        // Assert
        response.Success.Should().BeTrue();
        response.Message.Should().Be(message);
        response.Data.Should().Be(data);
        response.Errors.Should().BeEmpty();
    }

    [Fact]
    public void SuccessResult_Should_Work_With_Default_Message()
    {
        // Arrange
        var data = 42;

        // Act
        var response = ApiResponse<int>.SuccessResult(data);

        // Assert
        response.Success.Should().BeTrue();
        response.Message.Should().BeNull();
        response.Data.Should().Be(data);
        response.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ErrorResult_Should_Return_Failed_Response()
    {
        // Arrange
        var errorMessage = "Something went wrong";

        // Act
        var response = ApiResponse<string>.ErrorResult(errorMessage);

        // Assert
        response.Success.Should().BeFalse();
        response.Message.Should().Be(errorMessage);
        response.Data.Should().BeNull();
        response.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ErrorResult_Should_Accept_Multiple_Errors()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2", "Error 3" };
        var message = "Multiple errors occurred";

        // Act
        var response = ApiResponse<string>.ErrorResult(message, errors);

        // Assert
        response.Success.Should().BeFalse();
        response.Message.Should().Be(message);
        response.Data.Should().BeNull();
        response.Errors.Should().HaveCount(3);
        response.Errors.Should().Contain("Error 1");
        response.Errors.Should().Contain("Error 2");
        response.Errors.Should().Contain("Error 3");
    }

    [Theory]
    [InlineData(true, "Success message")]
    [InlineData(false, "Failure message")]
    public void ApiResponse_Should_Handle_Different_Success_States(bool success, string message)
    {
        // Act
        var response = new ApiResponse<int>
        {
            Success = success,
            Message = message,
            Data = success ? 100 : 0
        };

        // Assert
        response.Success.Should().Be(success);
        response.Message.Should().Be(message);
        if (success)
        {
            response.Data.Should().Be(100);
        }
        else
        {
            response.Data.Should().Be(0);
        }
    }

    [Fact]
    public void ApiResponse_Should_Handle_Complex_Data_Types()
    {
        // Arrange
        var complexData = new
        {
            Id = Guid.NewGuid(),
            Name = "Test Object",
            Properties = new List<string> { "prop1", "prop2" },
            Metadata = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = 42,
                ["key3"] = true
            }
        };

        // Act
        var response = ApiResponse<object>.SuccessResult(complexData, "Complex data retrieved");

        // Assert
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Complex data retrieved");
        response.Data.Should().NotBeNull();
        response.Data.Should().BeEquivalentTo(complexData);
    }

    [Fact]
    public void ApiResponse_Should_Handle_Null_Data_Gracefully()
    {
        // Act
        var response = ApiResponse<string?>.SuccessResult(null, "Null data is valid");

        // Assert
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Null data is valid");
        response.Data.Should().BeNull();
        response.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ApiResponse_Should_Support_Generic_Collections()
    {
        // Arrange
        var dataList = new List<string> { "item1", "item2", "item3" };

        // Act
        var response = ApiResponse<List<string>>.SuccessResult(dataList, "List retrieved");

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data.Should().HaveCount(3);
        response.Data.Should().Contain("item1");
        response.Data.Should().Contain("item2");
        response.Data.Should().Contain("item3");
    }

    [Fact]
    public void PagedResult_Should_Initialize_With_Default_Values()
    {
        // Act
        var pagedResult = new PagedResult<string>();

        // Assert
        pagedResult.Items.Should().NotBeNull();
        pagedResult.Items.Should().BeEmpty();
        pagedResult.TotalCount.Should().Be(0);
        pagedResult.PageSize.Should().Be(0);
        pagedResult.HasPreviousPage.Should().BeFalse();
        pagedResult.HasNextPage.Should().BeFalse();
    }
}