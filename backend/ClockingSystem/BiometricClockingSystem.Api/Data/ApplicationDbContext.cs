using Microsoft.EntityFrameworkCore;
using BiometricClockingSystem.Api.Models;

namespace BiometricClockingSystem.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
}