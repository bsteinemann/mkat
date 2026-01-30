using Microsoft.EntityFrameworkCore;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
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
    public DbSet<Peer> Peers => Set<Peer>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactChannel> ContactChannels => Set<ContactChannel>();
    public DbSet<ServiceContact> ServiceContacts => Set<ServiceContact>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<MonitorEvent> MonitorEvents => Set<MonitorEvent>();
    public DbSet<MonitorRollup> MonitorRollups => Set<MonitorRollup>();

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
            entity.Property(e => e.HealthCheckUrl).HasMaxLength(2000);
            entity.Property(e => e.HttpMethod).HasMaxLength(10);
            entity.Property(e => e.ExpectedStatusCodes).HasMaxLength(200);
            entity.Property(e => e.BodyMatchRegex).HasMaxLength(1000);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.Service)
                .WithMany(s => s.Monitors)
                .HasForeignKey(e => e.ServiceId)
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

        modelBuilder.Entity<Peer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
            entity.Property(e => e.HeartbeatToken).IsRequired().HasMaxLength(100);
            entity.Property(e => e.WebhookToken).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<ContactChannel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Configuration).IsRequired().HasMaxLength(4000);
            entity.HasOne(e => e.Contact)
                .WithMany(c => c.Channels)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceContact>(entity =>
        {
            entity.HasKey(e => new { e.ServiceId, e.ContactId });
            entity.HasOne(e => e.Service)
                .WithMany(s => s.ServiceContacts)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Contact)
                .WithMany(c => c.ServiceContacts)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PushSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Endpoint).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.P256dhKey).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AuthKey).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.Endpoint).IsUnique();
        });

        modelBuilder.Entity<MonitorEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.HasIndex(e => new { e.MonitorId, e.CreatedAt });
            entity.HasIndex(e => new { e.ServiceId, e.CreatedAt });
            entity.HasOne(e => e.Monitor)
                .WithMany()
                .HasForeignKey(e => e.MonitorId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MonitorRollup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Granularity).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => new { e.MonitorId, e.Granularity, e.PeriodStart }).IsUnique();
            entity.HasIndex(e => new { e.ServiceId, e.Granularity, e.PeriodStart });
            entity.HasOne(e => e.Monitor)
                .WithMany()
                .HasForeignKey(e => e.MonitorId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
