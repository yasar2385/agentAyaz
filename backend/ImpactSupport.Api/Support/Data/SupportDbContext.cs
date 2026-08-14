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
    public DbSet<QaDashboardFileCache> QaDashboardFileCaches => Set<QaDashboardFileCache>();
    public DbSet<QaDashboardSheetCache> QaDashboardSheetCaches => Set<QaDashboardSheetCache>();

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

        modelBuilder.Entity<QaDashboardFileCache>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ReportType, x.FileId }).IsUnique();
            entity.Property(x => x.FileId).IsRequired();
            entity.Property(x => x.FileName).IsRequired();
            entity.Property(x => x.ReportType).IsRequired();
            entity.Property(x => x.ScanStatus).IsRequired();
            entity.Property(x => x.ScanError).IsRequired();
        });

        modelBuilder.Entity<QaDashboardSheetCache>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.FileId, x.SheetName }).IsUnique();
            entity.Property(x => x.FileId).IsRequired();
            entity.Property(x => x.SheetName).IsRequired();
            entity.Property(x => x.Module).IsRequired();
            entity.Property(x => x.PurposeOfTesting).IsRequired();
            entity.Property(x => x.DevStatus).IsRequired();
            entity.Property(x => x.DevRemarks).IsRequired();
            entity.Property(x => x.Remarks).IsRequired();
            entity.Property(x => x.SheetLink).IsRequired();
            entity.Property(x => x.Link).IsRequired();
            entity.Property(x => x.RowsJson).IsRequired();
            entity.Property(x => x.RefreshStatus).IsRequired();
            entity.Property(x => x.RefreshError).IsRequired();

            entity
                .HasOne(x => x.FileCache)
                .WithMany(x => x.Sheets)
                .HasForeignKey(x => x.FileCacheId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
