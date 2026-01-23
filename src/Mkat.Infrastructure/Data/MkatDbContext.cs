using Microsoft.EntityFrameworkCore;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Infrastructure.Data;

public class MkatDbContext : DbContext, IUnitOfWork
{
    public MkatDbContext(DbContextOptions<MkatDbContext> options) : base(options)
    {
    }

    public DbSet<Service> Services => Set<Service>();
    public DbSet<Monitor> Monitors => Set<Monitor>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<NotificationChannel> NotificationChannels => Set<NotificationChannel>();
    public DbSet<MuteWindow> MuteWindows => Set<MuteWindow>();
    public DbSet<MetricReading> MetricReadings => Set<MetricReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.State).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.PreviousState).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Severity).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Monitor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.ConfigJson).HasMaxLength(4000);
            entity.Property(e => e.ThresholdStrategy).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.Service)
                .WithMany(s => s.Monitors)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MetricReading>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MonitorId, e.RecordedAt });
            entity.HasOne(e => e.Monitor)
                .WithMany()
                .HasForeignKey(e => e.MonitorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Severity).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Metadata).HasMaxLength(4000);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.DispatchedAt);
            entity.HasOne(e => e.Service)
                .WithMany(s => s.Alerts)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationChannel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ConfigJson).IsRequired().HasMaxLength(4000);
        });

        modelBuilder.Entity<MuteWindow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.HasIndex(e => new { e.ServiceId, e.StartsAt, e.EndsAt });
            entity.HasOne(e => e.Service)
                .WithMany(s => s.MuteWindows)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
