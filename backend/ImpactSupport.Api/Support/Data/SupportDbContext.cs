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
    public DbSet<QaImportBatch> QaImportBatches => Set<QaImportBatch>();
    public DbSet<QaImportBatchSheet> QaImportBatchSheets => Set<QaImportBatchSheet>();
    public DbSet<QaImportBatchRow> QaImportBatchRows => Set<QaImportBatchRow>();
    public DbSet<QaImportBatchError> QaImportBatchErrors => Set<QaImportBatchError>();
    public DbSet<TestRunConfig> TestRunConfigs => Set<TestRunConfig>();
    public DbSet<TestRunConfigTarget> TestRunConfigTargets => Set<TestRunConfigTarget>();
    public DbSet<TestRunConfigTestingType> TestRunConfigTestingTypes => Set<TestRunConfigTestingType>();
    public DbSet<TestRunConfigFlag> TestRunConfigFlags => Set<TestRunConfigFlag>();
    public DbSet<TestRunExecution> TestRunExecutions => Set<TestRunExecution>();
    public DbSet<TestRunConfigWorkflowContext> TestRunConfigWorkflowContexts => Set<TestRunConfigWorkflowContext>();
    public DbSet<TestRunProgress> TestRunProgresses => Set<TestRunProgress>();

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
            entity.Property(x => x.SourceUrl).IsRequired();
            entity.Property(x => x.FolderUrl).IsRequired();
            entity.Property(x => x.ScanStatus).IsRequired();
            entity.Property(x => x.ScanError).IsRequired();
            entity.Property(x => x.LocalTsvPath).IsRequired();
            entity.Property(x => x.SyncStatus).IsRequired();
            entity.Property(x => x.SyncError).IsRequired();
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
            entity.Property(x => x.LocalTsvPath).IsRequired();
            entity.Property(x => x.SyncStatus).IsRequired();
            entity.Property(x => x.SyncError).IsRequired();
            entity.Property(x => x.RefreshStatus).IsRequired();
            entity.Property(x => x.RefreshError).IsRequired();

            entity
                .HasOne(x => x.FileCache)
                .WithMany(x => x.Sheets)
                .HasForeignKey(x => x.FileCacheId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QaImportBatch>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UploadKind).IsRequired();
            entity.Property(x => x.ResultMode).IsRequired();
            entity.Property(x => x.FileName).IsRequired();
            entity.Property(x => x.UploadedBy).IsRequired();
            entity.Property(x => x.Status).IsRequired();
            entity.HasIndex(x => new { x.UploadKind, x.Status });
        });

        modelBuilder.Entity<QaImportBatchSheet>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SheetName).IsRequired();
            entity.Property(x => x.NormalizedSheetName).IsRequired();
            entity.Property(x => x.ModuleName).IsRequired();
            entity.Property(x => x.ConflictStatus).IsRequired();
            entity.Property(x => x.SelectedAction).IsRequired();
            entity.HasIndex(x => new { x.ImportBatchId, x.NormalizedSheetName }).IsUnique();

            entity
                .HasOne(x => x.ImportBatch)
                .WithMany(x => x.Sheets)
                .HasForeignKey(x => x.ImportBatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QaImportBatchRow>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TestCaseId).IsRequired();
            entity.Property(x => x.RowJson).IsRequired();
            entity.HasIndex(x => new { x.ImportBatchId, x.TestCaseId }).IsUnique();

            entity
                .HasOne(x => x.ImportBatch)
                .WithMany(x => x.Rows)
                .HasForeignKey(x => x.ImportBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.ImportBatchSheet)
                .WithMany(x => x.Rows)
                .HasForeignKey(x => x.ImportBatchSheetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QaImportBatchError>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RawValue).IsRequired();
            entity.Property(x => x.ErrorMessage).IsRequired();

            entity
                .HasOne(x => x.ImportBatch)
                .WithMany(x => x.Errors)
                .HasForeignKey(x => x.ImportBatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestRunConfig>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.TestingName).IsUnique();
            entity.Property(x => x.TestingName).IsRequired();
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.CreatedBy).IsRequired();
        });

        modelBuilder.Entity<TestRunConfigTarget>(entity =>
        {
            entity.HasKey(x => new { x.ConfigId, x.ModuleName });
            entity.Property(x => x.ModuleName).IsRequired();
            entity
                .HasOne(x => x.Config)
                .WithMany(x => x.Targets)
                .HasForeignKey(x => x.ConfigId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestRunConfigTestingType>(entity =>
        {
            entity.HasKey(x => new { x.ConfigId, x.Value });
            entity.Property(x => x.Value).IsRequired();
            entity
                .HasOne(x => x.Config)
                .WithMany(x => x.TestingTypes)
                .HasForeignKey(x => x.ConfigId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestRunConfigFlag>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ConfigId, x.FlagKey }).IsUnique();
            entity.Property(x => x.FlagKey).IsRequired();
            entity.Property(x => x.FlagValue).IsRequired();
            entity
                .HasOne(x => x.Config)
                .WithMany(x => x.Flags)
                .HasForeignKey(x => x.ConfigId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestRunExecution>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ConfigId, x.Status });
            entity.Property(x => x.TriggeredBy).IsRequired();
            entity.Property(x => x.Status).IsRequired();
            entity.Property(x => x.PlaywrightCommand).IsRequired();
            entity.Property(x => x.PlaywrightTestsRef).IsRequired();
            entity.Property(x => x.ReportPath).IsRequired();
            entity.Property(x => x.RunKind).IsRequired();
            entity.Property(x => x.ModuleName).IsRequired();
            entity.Property(x => x.TestCaseId).IsRequired();
            entity.Property(x => x.MantisTicket).IsRequired();
            entity.Property(x => x.FailureSummary).IsRequired();
            entity
                .HasOne(x => x.Config)
                .WithMany(x => x.Executions)
                .HasForeignKey(x => x.ConfigId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestRunConfigWorkflowContext>(entity =>
        {
            entity.HasKey(x => x.ConfigId);
            entity.Property(x => x.Client).IsRequired();
            entity.Property(x => x.ContentType).IsRequired();
            entity.Property(x => x.Domain).IsRequired();
            entity.Property(x => x.RoleWorkflow).IsRequired();
            entity.Property(x => x.TestingUrl).IsRequired();
            entity.Property(x => x.MantisTicket).IsRequired();
            entity.Property(x => x.RefStyle).IsRequired();
            entity
                .HasOne(x => x.Config)
                .WithOne(x => x.WorkflowContext)
                .HasForeignKey<TestRunConfigWorkflowContext>(x => x.ConfigId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestRunProgress>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ConfigId, x.UserId }).IsUnique();
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.LastModuleName).IsRequired();
            entity
                .HasOne(x => x.Config)
                .WithMany()
                .HasForeignKey(x => x.ConfigId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(x => x.LastExecution)
                .WithMany()
                .HasForeignKey(x => x.LastExecutionId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
