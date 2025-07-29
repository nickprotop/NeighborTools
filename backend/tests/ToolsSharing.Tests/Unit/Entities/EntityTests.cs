using ToolsSharing.Core.Entities;

namespace ToolsSharing.Tests.Unit.Entities;

/// <summary>
/// Tests for entity behavior and validation
/// </summary>
public class EntityTests
{
    [Fact]
    public void User_Should_Initialize_With_Default_Values()
    {
        // Act
        var user = new User();

        // Assert
        user.FirstName.Should().Be(string.Empty);
        user.LastName.Should().Be(string.Empty);
        user.IsDeleted.Should().BeFalse();
        user.DataProcessingConsent.Should().BeFalse();
        user.MarketingConsent.Should().BeFalse();
        user.TermsOfServiceAccepted.Should().BeFalse();
        user.GDPROptOut.Should().BeFalse();
        user.DataPortabilityRequested.Should().BeFalse();
        user.OwnedTools.Should().NotBeNull();
        user.RentalsAsOwner.Should().NotBeNull();
        user.RentalsAsRenter.Should().NotBeNull();
    }

    [Fact]
    public void Tool_Should_Initialize_With_Default_Values()
    {
        // Act
        var tool = new Tool();

        // Assert
        tool.Id.Should().NotBe(Guid.Empty);
        tool.OwnerId.Should().Be(string.Empty);
        tool.Name.Should().Be(string.Empty);
        tool.Description.Should().Be(string.Empty);
        tool.Category.Should().Be(string.Empty);
        tool.Brand.Should().Be(string.Empty);
        tool.Model.Should().Be(string.Empty);
        tool.IsAvailable.Should().BeTrue();
        tool.Condition.Should().Be(string.Empty);
        tool.LocationDisplay.Should().Be(string.Empty);
        tool.IsDeleted.Should().BeFalse();
        tool.Images.Should().NotBeNull();
        tool.Rentals.Should().NotBeNull();
    }

    [Fact]
    public void Tool_Should_Have_Valid_Guid_Id()
    {
        // Act
        var tool1 = new Tool();
        var tool2 = new Tool();

        // Assert
        tool1.Id.Should().NotBe(Guid.Empty);
        tool2.Id.Should().NotBe(Guid.Empty);
        tool1.Id.Should().NotBe(tool2.Id);
    }

    [Fact]
    public void Payment_Should_Initialize_With_Default_Values()
    {
        // Act
        var payment = new Payment();

        // Assert
        payment.Id.Should().NotBe(Guid.Empty);
        payment.RentalId.Should().Be(Guid.Empty); // RentalId is not auto-generated, needs to be set
        payment.PayerId.Should().Be(string.Empty);
        payment.Type.Should().Be(PaymentType.RentalPayment);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.Provider.Should().Be(PaymentProvider.PayPal);
        payment.Currency.Should().Be("USD");
        payment.IsRefunded.Should().BeFalse();
    }

    [Fact]
    public void BaseEntity_Should_Have_Timestamps()
    {
        // Act
        var tool = new Tool();
        var payment = new Payment();

        // Assert
        tool.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        tool.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        
        payment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData("test@example.com", "Test", "User")]
    [InlineData("admin@test.org", "Admin", "Administrator")]
    public void User_Should_Accept_Valid_Properties(string email, string firstName, string lastName)
    {
        // Act
        var user = new User
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            DataProcessingConsent = true,
            TermsOfServiceAccepted = true
        };

        // Assert
        user.Email.Should().Be(email);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.EmailConfirmed.Should().BeTrue();
        user.DataProcessingConsent.Should().BeTrue();
        user.TermsOfServiceAccepted.Should().BeTrue();
    }

    [Theory]
    [InlineData("Power Drill", "DeWalt", "DW123", 25.00)]
    [InlineData("Hammer", "Stanley", "H456", 5.00)]
    public void Tool_Should_Accept_Valid_Properties(string name, string brand, string model, decimal dailyRate)
    {
        // Act
        var tool = new Tool
        {
            Name = name,
            Brand = brand,
            Model = model,
            DailyRate = dailyRate,
            WeeklyRate = dailyRate * 6,
            MonthlyRate = dailyRate * 25,
            DepositRequired = dailyRate * 2
        };

        // Assert
        tool.Name.Should().Be(name);
        tool.Brand.Should().Be(brand);
        tool.Model.Should().Be(model);
        tool.DailyRate.Should().Be(dailyRate);
        tool.WeeklyRate.Should().Be(dailyRate * 6);
        tool.MonthlyRate.Should().Be(dailyRate * 25);
        tool.DepositRequired.Should().Be(dailyRate * 2);
    }
}