namespace BiometricClockingSystem.Api.Models;
public class User
{
    public Guid Id { get; set; }

    public string EmployeeNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}