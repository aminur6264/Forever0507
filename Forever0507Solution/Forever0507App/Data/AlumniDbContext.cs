using Forever0507App.Models;
using Microsoft.EntityFrameworkCore;

namespace Forever0507App.Data;

public class AlumniDbContext(DbContextOptions<AlumniDbContext> options) : DbContext(options)
{
    public DbSet<Registration> Registrations => Set<Registration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasIndex(r => r.RegistrationNo).IsUnique();
            entity.HasIndex(r => r.Phone);
        });
    }
}
