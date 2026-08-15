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
    public DbSet<MasterTemplate> MasterTemplates => Set<MasterTemplate>();
    public DbSet<MasterTestDetails> MasterTestDetails => Set<MasterTestDetails>();
    public DbSet<MasterModule> MasterModules => Set<MasterModule>();
    public DbSet<MasterPreconditionRole> MasterPreconditionRoles => Set<MasterPreconditionRole>();
    public DbSet<MasterTestingType> MasterTestingTypes => Set<MasterTestingType>();
    public DbSet<MasterIssueType> MasterIssueTypes => Set<MasterIssueType>();
    public DbSet<MasterQaStatus> MasterQaStatuses => Set<MasterQaStatus>();
    public DbSet<MasterDevStatus> MasterDevStatuses => Set<MasterDevStatus>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<RefStyle> RefStyles => Set<RefStyle>();
    public DbSet<RoleWorkflow> RoleWorkflows => Set<RoleWorkflow>();
    public DbSet<ContentType> Types => Set<ContentType>();
    public DbSet<DtdType> DtdTypes => Set<DtdType>();
    public DbSet<TestingUrl> TestingUrls => Set<TestingUrl>();
    public DbSet<MasterTemplateTestingType> MasterTemplateTestingTypes => Set<MasterTemplateTestingType>();
    public DbSet<MasterTemplateRemark> MasterTemplateRemarks => Set<MasterTemplateRemark>();
    public DbSet<TestingMetaResult> TestingMetaResults => Set<TestingMetaResult>();
    public DbSet<TestingMetaResultLink> TestingMetaResultLinks => Set<TestingMetaResultLink>();
    public DbSet<TestingMetaResultTestingType> TestingMetaResultTestingTypes => Set<TestingMetaResultTestingType>();
    public DbSet<TestingMetaResultModuleStat> TestingMetaResultModuleStats => Set<TestingMetaResultModuleStat>();
    public DbSet<TestingDataResult> TestingDataResults => Set<TestingDataResult>();

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

        ConfigureMasterTestingSchema(modelBuilder);
    }

    private static void ConfigureMasterTestingSchema(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MasterTemplate>(entity =>
        {
            entity.ToTable("MasterTemplate");
            entity.HasKey(x => x.MasterId);
            entity.HasIndex(x => x.MasterTestId).IsUnique();
            entity.Property(x => x.MasterTestId).IsRequired();
            entity.Property(x => x.MasterTestNo).IsRequired();
            entity.Property(x => x.MasterSourceSheet).IsRequired();
            entity.Property(x => x.MasterPreparedBy).IsRequired();
            entity.Property(x => x.MasterPreparedDate).IsRequired();
            entity.Property(x => x.MasterTestData).IsRequired();
            entity.Property(x => x.MasterExpectedResult).IsRequired();
            entity.Property(x => x.MasterActualResult).IsRequired();
        });

        modelBuilder.Entity<MasterTestDetails>(entity =>
        {
            entity.ToTable("MasterTestDetails");
            entity.HasKey(x => x.MasterId);
            entity.Property(x => x.MasterDescription).IsRequired();
            entity.Property(x => x.MasterTestSteps).IsRequired();
            entity.HasOne(x => x.MasterTemplate).WithOne(x => x.Details).HasForeignKey<MasterTestDetails>(x => x.MasterId).OnDelete(DeleteBehavior.Cascade);
        });

        ConfigureLookup(modelBuilder.Entity<MasterModule>(), "MasterModules", x => x.Name);
        ConfigureLookup(modelBuilder.Entity<MasterPreconditionRole>(), "MasterPreconditionRoles", x => x.Value);
        ConfigureLookup(modelBuilder.Entity<MasterTestingType>(), "MasterTestingTypes", x => x.Value);
        ConfigureLookup(modelBuilder.Entity<MasterIssueType>(), "MasterIssueTypes", x => x.Value);
        ConfigureLookup(modelBuilder.Entity<MasterQaStatus>(), "MasterQaStatuses", x => x.Value);
        ConfigureLookup(modelBuilder.Entity<MasterDevStatus>(), "MasterDevStatuses", x => x.Value);
        ConfigureLookup(modelBuilder.Entity<Client>(), "Clients", x => x.Code);
        ConfigureLookup(modelBuilder.Entity<RefStyle>(), "RefStyles", x => x.Value);
        ConfigureLookup(modelBuilder.Entity<ContentType>(), "Types", x => x.Value);
        ConfigureLookup(modelBuilder.Entity<DtdType>(), "DtdType", x => x.Value);

        modelBuilder.Entity<RoleWorkflow>(entity =>
        {
            entity.ToTable("RoleWorkflows");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Value).IsUnique();
            entity.Property(x => x.Value).IsRequired();
            entity.HasData(
                new RoleWorkflow { Id = 1, Value = "Author_Editor_Collator", IsDefault = true },
                new RoleWorkflow { Id = 2, Value = "Editor_Author_Collator" },
                new RoleWorkflow { Id = 3, Value = "Author_Collator" },
                new RoleWorkflow { Id = 4, Value = "Editor_Collator" });
        });

        modelBuilder.Entity<TestingUrl>(entity =>
        {
            entity.ToTable("TestingUrls");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Value).IsUnique();
            entity.Property(x => x.Value).IsRequired();
            entity.Property(x => x.UrlType).IsRequired();
            entity.HasData(
                new TestingUrl { Id = 1, Value = "author", UrlType = "single" },
                new TestingUrl { Id = 2, Value = "editor", UrlType = "single" },
                new TestingUrl { Id = 3, Value = "collator", UrlType = "single" },
                new TestingUrl { Id = 4, Value = "shared_author", UrlType = "multi_author" },
                new TestingUrl { Id = 5, Value = "shared_editor", UrlType = "multi_author" },
                new TestingUrl { Id = 6, Value = "shared_collator", UrlType = "multi_author" });
        });

        modelBuilder.Entity<MasterTestingType>().HasData(
            new MasterTestingType { Id = 1, Value = "Basic" },
            new MasterTestingType { Id = 2, Value = "Mock" },
            new MasterTestingType { Id = 3, Value = "Browser" },
            new MasterTestingType { Id = 4, Value = "Regression" },
            new MasterTestingType { Id = 5, Value = "Tomcat_Reg" });
        modelBuilder.Entity<ContentType>().HasData(new ContentType { Id = 1, Value = "Journal" }, new ContentType { Id = 2, Value = "Book" });
        modelBuilder.Entity<DtdType>().HasData(new DtdType { Id = 1, Value = "JATS" }, new DtdType { Id = 2, Value = "BITS" }, new DtdType { Id = 3, Value = "DOCBOOK" });
        modelBuilder.Entity<MasterPreconditionRole>().HasData(new MasterPreconditionRole { Id = 1, Value = "Author" }, new MasterPreconditionRole { Id = 2, Value = "PE" }, new MasterPreconditionRole { Id = 3, Value = "Collator" }, new MasterPreconditionRole { Id = 4, Value = "Editor" });
        modelBuilder.Entity<MasterIssueType>().HasData(new MasterIssueType { Id = 1, Value = "Bug" }, new MasterIssueType { Id = 2, Value = "Change Request" }, new MasterIssueType { Id = 3, Value = "Enhancement" });
        modelBuilder.Entity<MasterQaStatus>().HasData(new MasterQaStatus { Id = 1, Value = "Pass" }, new MasterQaStatus { Id = 2, Value = "Fail" }, new MasterQaStatus { Id = 3, Value = "Fixed" }, new MasterQaStatus { Id = 4, Value = "Rejected" }, new MasterQaStatus { Id = 5, Value = "WIP" });
        modelBuilder.Entity<MasterDevStatus>().HasData(new MasterDevStatus { Id = 1, Value = "Fixed" }, new MasterDevStatus { Id = 2, Value = "Rejected" }, new MasterDevStatus { Id = 3, Value = "WIP" }, new MasterDevStatus { Id = 4, Value = "Open" });

        modelBuilder.Entity<MasterTemplateTestingType>(entity =>
        {
            entity.ToTable("MasterTemplateTestingTypes");
            entity.HasKey(x => new { x.MasterId, x.TestingTypeId });
            entity.HasOne(x => x.MasterTemplate).WithMany(x => x.TestingTypes).HasForeignKey(x => x.MasterId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MasterTemplateRemark>(entity =>
        {
            entity.ToTable("MasterTemplateRemarks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.QaRemark).IsRequired();
            entity.Property(x => x.DevRemark).IsRequired();
            entity.HasOne(x => x.MasterTemplate).WithMany(x => x.Remarks).HasForeignKey(x => x.MasterId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestingMetaResult>(entity =>
        {
            entity.ToTable("TestingMetaResults");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.RunThrough).IsRequired();
        });
        modelBuilder.Entity<TestingMetaResultLink>().ToTable("TestingMetaResultLinks").HasKey(x => new { x.TestingMetaResultId, x.RoleId });
        modelBuilder.Entity<TestingMetaResultTestingType>().ToTable("TestingMetaResultTestingTypes").HasKey(x => new { x.TestingMetaResultId, x.TestingTypeId });
        modelBuilder.Entity<TestingMetaResultModuleStat>(entity =>
        {
            entity.ToTable("TestingMetaResultModuleStats");
            entity.HasKey(x => new { x.TestingMetaResultId, x.MasterModuleId });
            entity.HasOne(x => x.TestingMetaResult).WithMany(x => x.ModuleStats).HasForeignKey(x => x.TestingMetaResultId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<TestingDataResult>(entity =>
        {
            entity.ToTable("TestingDataResults");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MasterTestId).IsRequired();
            entity.HasOne(x => x.TestingMetaResult).WithMany(x => x.DataResults).HasForeignKey(x => x.TestingMetaResultId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureLookup<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity, string tableName, System.Linq.Expressions.Expression<Func<TEntity, object?>> uniqueProperty)
        where TEntity : class
    {
        entity.ToTable(tableName);
        entity.HasKey("Id");
        entity.HasIndex(uniqueProperty).IsUnique();
        entity.Property(uniqueProperty).IsRequired();
    }
}
