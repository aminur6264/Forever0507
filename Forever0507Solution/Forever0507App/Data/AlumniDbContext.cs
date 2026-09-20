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
    public DbSet<EventSettings> EventSettings => Set<EventSettings>();
    public DbSet<WelcomeNote> WelcomeNotes => Set<WelcomeNote>();
    public DbSet<WhyJoinItem> WhyJoinItems => Set<WhyJoinItem>();
    public DbSet<Khoroch> Khorochs => Set<Khoroch>();
    public DbSet<KhorochItem> KhorochItems => Set<KhorochItem>();
    public DbSet<GalleryImage> GalleryImages => Set<GalleryImage>();
    public DbSet<IpAddress> IpAddresses => Set<IpAddress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasIndex(r => r.RegistrationNo).IsUnique();
            entity.HasIndex(r => r.Phone);
        });

        modelBuilder.Entity<AppUser>().HasIndex(u => u.Phone).IsUnique();

        // Exactly one settings row, seeded with Id = 1 — never identity-generated.
        modelBuilder.Entity<EventSettings>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        // lookup data — one row per name/value
        modelBuilder.Entity<District>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<School>().HasIndex(s => s.Name).IsUnique();
        modelBuilder.Entity<JerseySizeOption>().HasIndex(j => j.Value).IsUnique();
        modelBuilder.Entity<PaymentMediumOption>().HasIndex(m => m.Name).IsUnique();

        // An invoice's items die with it.
        modelBuilder.Entity<KhorochItem>()
            .HasOne(i => i.Khoroch)
            .WithMany(k => k.Items)
            .HasForeignKey(i => i.KhorochId)
            .OnDelete(DeleteBehavior.Cascade);

        // Registrations reference the captured submitter IP by Id only (no navigation);
        // the IP record must survive even if registrations were ever removed.
        modelBuilder.Entity<Registration>()
            .HasOne<IpAddress>()
            .WithMany()
            .HasForeignKey(r => r.IpAddressId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
