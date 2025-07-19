namespace ToolsSharing.Tests.Helpers;

/// <summary>
/// Helper methods and utilities for testing
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// Generates a valid email address for testing
    /// </summary>
    public static string GenerateTestEmail(string prefix = "test")
    {
        return $"{prefix}{Guid.NewGuid().ToString("N")[..8]}@example.com";
    }

    /// <summary>
    /// Generates a secure password for testing
    /// </summary>
    public static string GenerateTestPassword()
    {
        return "TestPassword123!";
    }

    /// <summary>
    /// Generates a test user ID
    /// </summary>
    public static string GenerateTestUserId()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Generates a random decimal between min and max
    /// </summary>
    public static decimal GenerateRandomDecimal(decimal min = 1.0m, decimal max = 100.0m)
    {
        var random = new Random();
        var range = max - min;
        return min + (decimal)(random.NextDouble() * (double)range);
    }

    /// <summary>
    /// Generates a random date within the specified range
    /// </summary>
    public static DateTime GenerateRandomDate(int daysFromNow = 30)
    {
        var random = new Random();
        return DateTime.UtcNow.AddDays(random.Next(1, daysFromNow));
    }

    /// <summary>
    /// Creates a test tool name with optional prefix
    /// </summary>
    public static string GenerateTestToolName(string prefix = "Test Tool")
    {
        return $"{prefix} {Guid.NewGuid().ToString("N")[..6]}";
    }

    /// <summary>
    /// Validates that two decimals are approximately equal (useful for rate calculations)
    /// </summary>
    public static bool AreApproximatelyEqual(decimal value1, decimal value2, decimal tolerance = 0.01m)
    {
        return Math.Abs(value1 - value2) <= tolerance;
    }

    /// <summary>
    /// Validates that a date is approximately equal to DateTime.UtcNow
    /// </summary>
    public static bool IsApproximatelyNow(DateTime dateTime, TimeSpan? tolerance = null)
    {
        var actualTolerance = tolerance ?? TimeSpan.FromSeconds(10);
        var difference = Math.Abs((DateTime.UtcNow - dateTime).TotalMilliseconds);
        return difference <= actualTolerance.TotalMilliseconds;
    }

    /// <summary>
    /// Creates a collection of test data
    /// </summary>
    public static List<T> CreateTestCollection<T>(Func<int, T> factory, int count = 5)
    {
        return Enumerable.Range(1, count).Select(factory).ToList();
    }

    /// <summary>
    /// Validates email format using a simple regex
    /// </summary>
    public static bool IsValidEmailFormat(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        return email.Contains("@") && 
               email.Contains(".") && 
               email.IndexOf("@") < email.LastIndexOf(".") &&
               email.Length >= 5;
    }

    /// <summary>
    /// Validates password strength
    /// </summary>
    public static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return false;

        return password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(ch => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(ch));
    }

    /// <summary>
    /// Calculates tool rates with standard ratios
    /// </summary>
    public static (decimal weekly, decimal monthly) CalculateStandardRates(decimal dailyRate)
    {
        var weeklyRate = dailyRate * 6; // 6 days for weekly discount
        var monthlyRate = dailyRate * 25; // 25 days for monthly discount
        return (weeklyRate, monthlyRate);
    }

    /// <summary>
    /// Calculates security deposit as percentage of total cost
    /// </summary>
    public static decimal CalculateSecurityDeposit(decimal totalCost, decimal percentage = 0.2m)
    {
        return Math.Round(totalCost * percentage, 2);
    }

    /// <summary>
    /// Creates a future date range for rental testing
    /// </summary>
    public static (DateTime startDate, DateTime endDate) CreateFutureDateRange(int startDaysFromNow = 1, int duration = 3)
    {
        var startDate = DateTime.UtcNow.AddDays(startDaysFromNow);
        var endDate = startDate.AddDays(duration);
        return (startDate, endDate);
    }

    /// <summary>
    /// Validates that a Guid is not empty
    /// </summary>
    public static bool IsValidGuid(Guid guid)
    {
        return guid != Guid.Empty;
    }

    /// <summary>
    /// Creates test metadata dictionary
    /// </summary>
    public static Dictionary<string, object> CreateTestMetadata()
    {
        return new Dictionary<string, object>
        {
            ["testId"] = Guid.NewGuid(),
            ["createdBy"] = "test-framework",
            ["timestamp"] = DateTime.UtcNow,
            ["version"] = "1.0.0"
        };
    }

    /// <summary>
    /// Simulates async delay for testing async operations
    /// </summary>
    public static async Task SimulateAsyncDelay(int milliseconds = 10)
    {
        await Task.Delay(milliseconds);
    }

    /// <summary>
    /// Creates a test exception with stack trace
    /// </summary>
    public static Exception CreateTestException(string message = "Test exception")
    {
        try
        {
            throw new InvalidOperationException(message);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Validates that a string is not null or empty
    /// </summary>
    public static bool IsValidString(string? value)
    {
        return !string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Converts enum to list of values for testing
    /// </summary>
    public static List<T> GetEnumValues<T>() where T : struct, Enum
    {
        return Enum.GetValues<T>().ToList();
    }

    /// <summary>
    /// Creates a random selection from an array
    /// </summary>
    public static T GetRandomItem<T>(T[] items)
    {
        var random = new Random();
        return items[random.Next(items.Length)];
    }

    /// <summary>
    /// Test categories for organizing tests
    /// </summary>
    public static class Categories
    {
        public const string Unit = "Unit";
        public const string Integration = "Integration";
        public const string Performance = "Performance";
        public const string Security = "Security";
        public const string Database = "Database";
        public const string API = "API";
        public const string Authentication = "Authentication";
        public const string Payment = "Payment";
        public const string Messaging = "Messaging";
    }

    /// <summary>
    /// Common test data for reuse
    /// </summary>
    public static class TestData
    {
        public static readonly string[] ToolCategories = { "Power Tools", "Hand Tools", "Garden Tools", "Automotive", "Construction" };
        public static readonly string[] ToolBrands = { "DeWalt", "Makita", "Bosch", "Ryobi", "Milwaukee" };
        public static readonly string[] Conditions = { "Excellent", "Good", "Fair" };
        public static readonly string[] Cities = { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix" };
    }
}