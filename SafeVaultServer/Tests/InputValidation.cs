using NUnit.Framework;
using Microsoft.Data.SqlClient;

[TestFixture]
public class TestInputValidation
{
    private const string ConnectionString = "Server=localhost;Database=SafeVault;Trusted_Connection=True;";

    [Test]
    public void SQLInjection()
    {
        // Placeholder for SQL Injection test
    }

    [Test]
    public void XSS()
    {
        // Placeholder for XSS test
    }

    public static bool IsValidInput(string input, string allowedSpecialChars = "")
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        var validChars = allowedSpecialChars.ToHashSet();
        return input.All(c => char.IsLetterOrDigit(c) || validChars.Contains(c));
    }

    public bool IsValidPassword(string password)
    {
        bool valid = IsValidInput(password, "!@#$%^&*?");
        return valid;
    }

    public bool Login(string username, string password)
    {
        bool usernameValid = IsValidInput(username);
        bool passwordValid = IsValidPassword(password);

        if (!usernameValid || !passwordValid)
        {
            Console.WriteLine("Invalid input. User not inserted.");
            return false;
        }

        string query = "SELECT Count(1) FROM Users WHERE Username = @Username AND Password = @Password";

        using SqlConnection connection = new SqlConnection(ConnectionString);
        using SqlCommand command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@Username", username);
        command.Parameters.AddWithValue("@Password", password);

        connection.Open();
        int count = (int)command.ExecuteScalar();
        return count > 0;
    }

    public bool InsertUser(string username, string password)
    {
        bool usernameValid = IsValidInput(username);
        bool passwordValid = IsValidPassword(password);

        if (!usernameValid || !passwordValid)
        {
            Console.WriteLine("Invalid input. User not inserted.");
            return false;
        }

        string query = "INSERT INTO Users (Username, Password) VALUES (@Username, @Password); SELECT @@ROWCOUNT";

        using SqlConnection connection = new SqlConnection(ConnectionString);
        using SqlCommand command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@Username", username);
        command.Parameters.AddWithValue("@Password", password);

        connection.Open();
        int count = (int)command.ExecuteScalar();
        return count > 0;
    }
}