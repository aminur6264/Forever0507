using Forever0507App.Models;
using Microsoft.EntityFrameworkCore;

namespace Forever0507App.Data;

public class AlumniDbContext(DbContextOptions<AlumniDbContext> options) : DbContext(options)
{
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<JerseySizeOption> JerseySizeOptions => Set<JerseySizeOption>();
    public DbSet<PaymentMediumOption> PaymentMediumOptions => Set<PaymentMediumOption>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasIndex(r => r.RegistrationNo).IsUnique();
            entity.HasIndex(r => r.Phone);
        });

        modelBuilder.Entity<AppUser>().HasIndex(u => u.Phone).IsUnique();

        // lookup data — one row per name/value
        modelBuilder.Entity<District>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<School>().HasIndex(s => s.Name).IsUnique();
        modelBuilder.Entity<JerseySizeOption>().HasIndex(j => j.Value).IsUnique();
        modelBuilder.Entity<PaymentMediumOption>().HasIndex(m => m.Name).IsUnique();
    }
}
