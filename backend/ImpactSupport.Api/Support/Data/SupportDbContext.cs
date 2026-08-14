using Microsoft.EntityFrameworkCore;

using ImpactSupport.Api.TestCaseViewer.Data;

namespace ImpactSupport.Api.Support.Data;

public sealed class SupportDbContext : DbContext
{
    public SupportDbContext(DbContextOptions<SupportDbContext> options)
        : base(options)
    {
    }

    public DbSet<SupportSession> SupportSessions => Set<SupportSession>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<TestCaseViewerUser> TestCaseViewerUsers => Set<TestCaseViewerUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupportSession>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.SupportSessionId).IsUnique();
            entity.HasIndex(x => x.TicketNo).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.DocumentId, x.UserRole, x.Status });

            entity.Property(x => x.SupportSessionId).IsRequired();
            entity.Property(x => x.TicketNo).IsRequired();
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.UserRole).IsRequired();
            entity.Property(x => x.DocumentId).IsRequired();
            entity.Property(x => x.Status).IsRequired();
        });

        modelBuilder.Entity<SupportMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.MessageId).IsUnique();
            entity.HasIndex(x => x.SupportSessionId);

            entity.Property(x => x.MessageId).IsRequired();
            entity.Property(x => x.SupportSessionId).IsRequired();
            entity.Property(x => x.SenderUserId).IsRequired();
            entity.Property(x => x.MessageText).IsRequired();
            entity.Property(x => x.MessageType).IsRequired();

            entity
                .HasOne(x => x.SupportSession)
                .WithMany(x => x.Messages)
                .HasPrincipalKey(x => x.SupportSessionId)
                .HasForeignKey(x => x.SupportSessionId);
        });

        modelBuilder.Entity<TestCaseViewerUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.MongoId).IsUnique();
            entity.HasIndex(x => x.Username).IsUnique();

            entity.Property(x => x.MongoId).IsRequired();
            entity.Property(x => x.Username).IsRequired();
            entity.Property(x => x.Email).IsRequired();
            entity.Property(x => x.DisplayName).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.RoleJson).IsRequired();
        });
    }
}
