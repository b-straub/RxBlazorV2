namespace ReactivePatternSample.Storage.Models;

/// <summary>
/// User authentication operations for StorageModel.
/// </summary>
public partial class StorageModel
{
    /// <summary>
    /// Seeds demo users for fake authentication.
    /// </summary>
    private void SeedUsers()
    {
        Users.Add(new User
        {
            Id = "demo",
            Username = "demo",
            PasswordHash = HashPassword("demo123"),
            DisplayName = "Demo User"
        });

        Users.Add(new User
        {
            Id = "admin",
            Username = "admin",
            PasswordHash = HashPassword("admin123"),
            DisplayName = "Administrator"
        });
    }

    /// <summary>
    /// Validates user credentials and returns the user if valid.
    /// </summary>
    public User? ValidateCredentials(string username, string password)
    {
        var hashedPassword = HashPassword(password);
        return Users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
            u.PasswordHash == hashedPassword);
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    public User? GetUser(string userId)
    {
        return Users.FirstOrDefault(u => u.Id == userId);
    }

    /// <summary>
    /// Simple password hashing (for demo purposes only - not secure!).
    /// </summary>
    private static string HashPassword(string password)
    {
        // In a real app, use a proper hashing algorithm like BCrypt
        return Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(password + "salt")));
    }
}
