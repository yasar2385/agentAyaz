using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using ImpactSupport.Api.Support.Data;
using ImpactSupport.Api.TestCaseViewer.Data;
using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class PlaywrightRunService : IPlaywrightRunService
{
    private const string All = "ALL";
    private static readonly ConcurrentDictionary<int, Process> RunningProcesses = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SupportDbContext _dbContext;
    private readonly PlaywrightOptions _options;
    private readonly PlaywrightCommandBuilder _commandBuilder;
    private readonly ILogger<PlaywrightRunService> _logger;

    public PlaywrightRunService(
        IServiceScopeFactory scopeFactory,
        SupportDbContext dbContext,
        IOptions<PlaywrightOptions> options,
        PlaywrightCommandBuilder commandBuilder,
        ILogger<PlaywrightRunService> logger)
    {
        _scopeFactory = scopeFactory;
        _dbContext = dbContext;
        _options = options.Value;
        _commandBuilder = commandBuilder;
        _logger = logger;
    }

    public async Task<PlaywrightReadinessResponse> GetReadinessAsync(CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();
        var workingDirectory = _options.WorkingDirectory.Trim();
        var projectFound = !string.IsNullOrWhiteSpace(workingDirectory) && Directory.Exists(workingDirectory);
        if (!projectFound) issues.Add("Playwright working directory is not configured or does not exist.");

        var configFound = projectFound && Directory.EnumerateFiles(workingDirectory, "playwright.config.*", SearchOption.TopDirectoryOnly).Any();
        if (!configFound) issues.Add("playwright.config.* was not found in the configured spec repo.");

        var specFiles = projectFound
            ? Directory.EnumerateFiles(workingDirectory, "*.*", SearchOption.AllDirectories)
                .Where(IsSpecFile)
                .Take(500)
                .ToList()
            : [];
        var specText = string.Join('\n', specFiles.Select(path => SafeRead(path)));
        var moduleTags = Regex.IsMatch(specText, @"@module=", RegexOptions.IgnoreCase);
        var typeTags = Regex.IsMatch(specText, @"@type=", RegexOptions.IgnoreCase);
        var roleTags = Regex.IsMatch(specText, @"@role=", RegexOptions.IgnoreCase);
        var clientTags = Regex.IsMatch(specText, @"@client=", RegexOptions.IgnoreCase);
        var testcaseTags = Regex.IsMatch(specText, @"@testcase=", RegexOptions.IgnoreCase);
        var taggedSpecs = moduleTags || typeTags || roleTags || clientTags;
        if (!taggedSpecs) issues.Add("No tagged Playwright specs were found.");
        if (!moduleTags) issues.Add("No @module= tags were found in Playwright specs.");
        if (!typeTags) issues.Add("No @type= tags were found in Playwright specs.");
        if (!roleTags) issues.Add("No @role= tags were found in Playwright specs.");
        if (!clientTags) issues.Add("No @client= tags were found in Playwright specs.");
        if (!testcaseTags) issues.Add("No @testcase= tags were found in Playwright specs; exact bug verification will be blocked.");

        var node = await CommandWorksAsync("node", "--version", workingDirectory, cancellationToken);
        var npm = await CommandWorksAsync("npm", "--version", workingDirectory, cancellationToken);
        var playwright = projectFound && await CommandWorksAsync(_options.Command, "playwright --version", workingDirectory, cancellationToken);
        var browsers = projectFound && await CommandWorksAsync(_options.Command, "playwright install --dry-run", workingDirectory, cancellationToken);
        if (!node) issues.Add("Node.js is not available to the API host.");
        if (!npm) issues.Add("npm is not available to the API host.");
        if (!playwright) issues.Add("Playwright is not available from the configured working directory.");
        if (!browsers) issues.Add("Playwright browser availability could not be verified.");

        var masterData = await _dbContext.QaDashboardSheetCaches
            .AsNoTracking()
            .AnyAsync(sheet => sheet.FileCache != null && sheet.FileCache.ReportType == "master" && sheet.TotalTestCases > 0, cancellationToken);
        if (!masterData) issues.Add("No committed master test case data is available.");

        return new PlaywrightReadinessResponse
        {
            PlaywrightProjectFound = projectFound && configFound,
            TaggedSpecsFound = taggedSpecs,
            ModuleTagsFound = moduleTags,
            TypeTagsFound = typeTags,
            RoleTagsFound = roleTags,
            ClientTagsFound = clientTags,
            NodeAvailable = node,
            NpmAvailable = npm,
            PlaywrightAvailable = playwright,
            BrowsersAvailable = browsers,
            ManualMasterDataAvailable = masterData,
            RoleGateAvailable = true,
            WorkingDirectory = workingDirectory,
            PlaywrightTestsRef = projectFound ? await GetGitRefAsync(workingDirectory, cancellationToken) : string.Empty,
            BlockingIssues = issues
        };
    }

    public async Task<RunMetadataResponse> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.QaDashboardSheetCaches
            .AsNoTracking()
            .Where(sheet => sheet.FileCache != null && sheet.FileCache.ReportType == "master")
            .Select(sheet => new { sheet.Module, sheet.PurposeOfTesting, sheet.RowsJson })
            .ToListAsync(cancellationToken);

        var modules = rows
            .Select(row => FirstValue(row.Module, row.PurposeOfTesting))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();

        var rowText = string.Join(' ', rows.Select(row => row.RowsJson));
        return new RunMetadataResponse
        {
            Modules = modules,
            TestingTypes = DistinctRegexValues(rowText, "\"testingType\":\"([^\"]+)\"")
                .SelectMany(value => value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .DefaultIfEmpty("Regression")
                .ToList(),
            Roles = DistinctRegexValues(rowText, "\"preconditions\":\"([^\"]+)\"")
                .Select(value => Regex.Replace(value, @"\s*\([^)]+\)\s*$", string.Empty).Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .DefaultIfEmpty("Author")
                .ToList(),
            Clients = DistinctRegexValues(rowText, @"\(([A-Za-z0-9]+)\)")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .DefaultIfEmpty("LWW")
                .ToList(),
            ContentTypes = ["books", "journal"],
            Domains = ["UAT", "UAT_QA", "DEV", "DEV_QA", "LIVE", "PROD"],
            RoleWorkflows = ["author_editor_collator", "editor_author_collator", "author_collator", "editor_collator"],
            TestingUrls = ["author", "editor", "collator", "shared_author", "editor_author", "shared_collator"],
            RefStyles = ["number", "number_sup", "number_sup_parentheses", "unnumbered", "footnotes", "name-date", "name-date-cms-18"]
        };
    }

    public async Task<IReadOnlyList<TestRunConfigResponse>> GetConfigsAsync(CancellationToken cancellationToken = default)
    {
        var configs = await QueryConfigs().OrderBy(config => config.TestingName).ToListAsync(cancellationToken);
        return configs.Select(ToResponse).ToList();
    }

    public async Task<RecentRunsResponse> GetRecentRunsAsync(string scope, int limit, AuthUser? user, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TestRunExecutions
            .AsNoTracking()
            .Include(run => run.Config)
            .OrderByDescending(run => run.TriggeredAt)
            .AsQueryable();
        if (!scope.Equals("team", StringComparison.OrdinalIgnoreCase))
        {
            var username = user?.Username ?? string.Empty;
            query = query.Where(run => run.TriggeredBy == username);
        }

        var runs = await query.Take(Math.Clamp(limit, 1, 100)).ToListAsync(cancellationToken);
        return new RecentRunsResponse
        {
            Runs = runs.Select(ToResponse).ToList()
        };
    }

    public async Task<RunProgressResponse?> GetProgressAsync(int configId, AuthUser? user, CancellationToken cancellationToken = default)
    {
        var config = await QueryConfigs().FirstOrDefaultAsync(config => config.Id == configId, cancellationToken);
        if (config == null) return null;
        var progress = await FindProgressAsync(configId, user, cancellationToken);
        return new RunProgressResponse
        {
            ConfigId = configId,
            LastModuleName = progress?.LastModuleName ?? string.Empty,
            LastExecutionId = progress?.LastExecutionId,
            NextModuleName = NextModule(config, progress?.LastModuleName)
        };
    }

    public async Task<TestRunConfigResponse> CreateConfigAsync(TestRunConfigRequest request, AuthUser? user, CancellationToken cancellationToken = default)
    {
        EnsureCanManage(user);
        var config = new TestRunConfig
        {
            TestingName = request.TestingName.Trim(),
            Description = request.Description.Trim(),
            CreatedBy = user?.Username ?? string.Empty
        };
        ApplyConfig(config, request);
        _dbContext.TestRunConfigs.Add(config);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(config);
    }

    public async Task<TestRunConfigResponse?> UpdateConfigAsync(int configId, TestRunConfigRequest request, AuthUser? user, CancellationToken cancellationToken = default)
    {
        EnsureCanManage(user);
        var config = await QueryConfigs().FirstOrDefaultAsync(config => config.Id == configId, cancellationToken);
        if (config == null) return null;
        config.TestingName = request.TestingName.Trim();
        config.Description = request.Description.Trim();
        config.Targets.Clear();
        config.TestingTypes.Clear();
        config.Flags.Clear();
        config.WorkflowContext = null;
        ApplyConfig(config, request);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(config);
    }

    public async Task<TestRunExecutionResponse?> ContinueAsync(int configId, AuthUser? user, CancellationToken cancellationToken = default)
    {
        EnsureCanManage(user);
        var config = await QueryConfigs().FirstOrDefaultAsync(config => config.Id == configId, cancellationToken);
        if (config == null) return null;
        var progress = await FindProgressAsync(configId, user, cancellationToken);
        var nextModule = NextModule(config, progress?.LastModuleName);
        if (string.IsNullOrWhiteSpace(nextModule)) throw new InvalidOperationException("No next module is available for this config.");
        var execution = await CreateExecutionAsync(config, user, "STANDARD", nextModule, string.Empty, config.WorkflowContext?.MantisTicket ?? string.Empty, cancellationToken);
        progress ??= new TestRunProgress { ConfigId = configId, UserId = ProgressUser(user) };
        progress.LastModuleName = nextModule;
        progress.LastExecutionId = execution.Id;
        progress.UpdatedAt = DateTimeOffset.UtcNow;
        if (progress.Id == 0) _dbContext.TestRunProgresses.Add(progress);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _ = Task.Run(() => RunExecutionAsync(execution.Id));
        return ToResponse(execution);
    }

    public async Task<TestRunExecutionResponse> VerifyFixAsync(VerifyFixRequest request, AuthUser? user, CancellationToken cancellationToken = default)
    {
        EnsureCanManage(user);
        var testCaseId = request.TestCaseId.Trim();
        if (string.IsNullOrWhiteSpace(testCaseId)) throw new ArgumentException("testCaseId is required");
        var rows = await _dbContext.QaDashboardSheetCaches.AsNoTracking().Where(sheet => sheet.RowsJson.Contains(testCaseId)).ToListAsync(cancellationToken);
        if (rows.Count == 0) throw new ArgumentException($"Test case {testCaseId} was not found in committed master data.");

        var config = new TestRunConfig
        {
            TestingName = $"Verify {testCaseId}",
            Description = request.MantisTicket.Trim(),
            CreatedBy = user?.Username ?? string.Empty,
            Flags =
            [
                new() { FlagKey = "ui", FlagValue = "off" },
                new() { FlagKey = "role_based", FlagValue = All },
                new() { FlagKey = "role_based_client", FlagValue = All }
            ]
        };
        _dbContext.TestRunConfigs.Add(config);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var readiness = await GetReadinessAsync(cancellationToken);
        var blocking = readiness.BlockingIssues.ToList();
        if (blocking.Count > 0) throw new InvalidOperationException(string.Join(" ", blocking));
        var execution = new TestRunExecution
        {
            ConfigId = 0,
            TriggeredBy = user?.Username ?? string.Empty,
            Status = "QUEUED",
            RunKind = "BUG_VERIFY",
            TestCaseId = testCaseId,
            MantisTicket = request.MantisTicket.Trim(),
            ModuleName = rows.Select(row => FirstValue(row.Module, row.PurposeOfTesting)).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty,
            PlaywrightCommand = $"{_options.Command} {_options.ArgumentsPrefix} --grep=@testcase={testCaseId}",
            PlaywrightTestsRef = readiness.PlaywrightTestsRef,
            ReportPath = Path.GetFullPath(Path.Combine(_options.WorkingDirectory, _options.ReportRelativePath))
        };
        execution.ConfigId = config.Id;
        _dbContext.TestRunExecutions.Add(execution);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _ = Task.Run(() => RunExecutionAsync(execution.Id));
        return ToResponse(execution);
    }

    public async Task<TestRunExecutionResponse?> TriggerAsync(int configId, AuthUser? user, CancellationToken cancellationToken = default)
    {
        EnsureCanManage(user);
        var readiness = await GetReadinessAsync(cancellationToken);
        if (readiness.BlockingIssues.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", readiness.BlockingIssues));
        }

        var config = await QueryConfigs().FirstOrDefaultAsync(config => config.Id == configId, cancellationToken);
        if (config == null) return null;

        var execution = await CreateExecutionAsync(config, user, "STANDARD", string.Empty, string.Empty, config.WorkflowContext?.MantisTicket ?? string.Empty, cancellationToken);
        _ = Task.Run(() => RunExecutionAsync(execution.Id));
        return ToResponse(execution);
    }

    private async Task<TestRunExecution> CreateExecutionAsync(TestRunConfig config, AuthUser? user, string runKind, string moduleName, string testCaseId, string mantisTicket, CancellationToken cancellationToken)
    {
        var readiness = await GetReadinessAsync(cancellationToken);
        if (readiness.BlockingIssues.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", readiness.BlockingIssues));
        }

        var execution = new TestRunExecution
        {
            ConfigId = config.Id,
            TriggeredBy = user?.Username ?? string.Empty,
            Status = "QUEUED",
            RunKind = runKind,
            ModuleName = moduleName,
            TestCaseId = testCaseId,
            MantisTicket = mantisTicket,
            PlaywrightCommand = _commandBuilder.BuildDisplayCommand(config, moduleName),
            PlaywrightTestsRef = readiness.PlaywrightTestsRef,
            ReportPath = Path.GetFullPath(Path.Combine(_options.WorkingDirectory, _options.ReportRelativePath))
        };
        _dbContext.TestRunExecutions.Add(execution);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return execution;
    }

    public async Task<TestRunExecutionResponse?> GetExecutionAsync(int executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.TestRunExecutions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == executionId, cancellationToken);
        return execution == null ? null : ToResponse(execution);
    }

    public async Task<TestRunExecutionResponse?> CancelAsync(int executionId, AuthUser? user, CancellationToken cancellationToken = default)
    {
        EnsureCanManage(user);
        var execution = await _dbContext.TestRunExecutions.FirstOrDefaultAsync(item => item.Id == executionId, cancellationToken);
        if (execution == null) return null;

        if (RunningProcesses.TryRemove(executionId, out var process) && !process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }

        execution.Status = "CANCELLED";
        execution.FailureSummary = $"Cancelled by {user?.Username ?? "user"}";
        execution.FinishedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(execution);
    }

    public async Task<string?> GetReportPathAsync(int executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.TestRunExecutions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == executionId, cancellationToken);
        return execution == null || string.IsNullOrWhiteSpace(execution.ReportPath) || !File.Exists(execution.ReportPath)
            ? null
            : execution.ReportPath;
    }

    private async Task RunExecutionAsync(int executionId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SupportDbContext>();
        var builder = scope.ServiceProvider.GetRequiredService<PlaywrightCommandBuilder>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<PlaywrightOptions>>().Value;
        var execution = await db.TestRunExecutions.Include(item => item.Config).ThenInclude(config => config!.Targets)
            .Include(item => item.Config).ThenInclude(config => config!.TestingTypes)
            .Include(item => item.Config).ThenInclude(config => config!.Flags)
            .FirstAsync(item => item.Id == executionId);

        try
        {
            execution.Status = "RUNNING";
            await db.SaveChangesAsync();
            var args = string.IsNullOrWhiteSpace(execution.TestCaseId)
                ? builder.BuildArguments(execution.Config!, execution.ModuleName)
                : $"{options.ArgumentsPrefix} --grep=@testcase={execution.TestCaseId}";
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = options.Command,
                    Arguments = args,
                    WorkingDirectory = options.WorkingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            if (!process.Start())
            {
                MarkError(execution, "Execution environment error - Playwright could not start");
                await db.SaveChangesAsync();
                return;
            }

            RunningProcesses[executionId] = process;
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            var timeout = TimeSpan.FromMinutes(Math.Max(1, options.ExecutionTimeoutMinutes));
            var waitTask = process.WaitForExitAsync();
            var completed = await Task.WhenAny(waitTask, Task.Delay(timeout)) == waitTask;
            if (!completed)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                MarkError(execution, $"Execution timed out after {timeout.TotalMinutes:0} minutes - process killed");
            }
            else if (execution.Status == "CANCELLED")
            {
                execution.FinishedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                var output = $"{await outputTask}\n{await errorTask}";
                execution.ExitCode = process.ExitCode;
                ApplyTerminalStatus(execution, output);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Playwright execution {ExecutionId} failed.", executionId);
            MarkError(execution, "Execution environment error - Playwright could not start");
        }
        finally
        {
            RunningProcesses.TryRemove(executionId, out _);
            execution.FinishedAt ??= DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    private static void ApplyTerminalStatus(TestRunExecution execution, string output)
    {
        if (output.Contains("no tests found", StringComparison.OrdinalIgnoreCase))
        {
            MarkError(execution, "No matching tests found for this configuration - check module/role/client/type tags");
            return;
        }

        if (execution.ExitCode == 0)
        {
            execution.Status = "PASSED";
            execution.FailureSummary = string.Empty;
            return;
        }

        execution.Status = "FAILED";
        execution.FailureSummary = BuildFailureSummary(output);
    }

    private static string BuildFailureSummary(string output)
    {
        var match = Regex.Match(output, @"(\d+)\s+failed", RegexOptions.IgnoreCase);
        return match.Success ? $"{match.Groups[1].Value} tests failed - see report" : "One or more tests failed - see report";
    }

    private static void MarkError(TestRunExecution execution, string summary)
    {
        execution.Status = "ERROR";
        execution.FailureSummary = summary;
    }

    private IQueryable<TestRunConfig> QueryConfigs()
    {
        return _dbContext.TestRunConfigs
            .Include(config => config.Targets)
            .Include(config => config.TestingTypes)
            .Include(config => config.Flags)
            .Include(config => config.WorkflowContext);
    }

    private static void ApplyConfig(TestRunConfig config, TestRunConfigRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TestingName)) throw new ArgumentException("testingName is required");
        foreach (var module in request.Modules.Where(value => !IsAll(value)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            config.Targets.Add(new TestRunConfigTarget { ModuleName = module.Trim() });
        }

        foreach (var type in request.TestingTypes.Where(value => !IsAll(value)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            config.TestingTypes.Add(new TestRunConfigTestingType { Value = type.Trim() });
        }

        config.Flags.Add(new TestRunConfigFlag { FlagKey = "ui", FlagValue = request.Ui.Equals("on", StringComparison.OrdinalIgnoreCase) ? "on" : "off" });
        config.Flags.Add(new TestRunConfigFlag { FlagKey = "role_based", FlagValue = NormalizeAll(request.RoleBased) });
        config.Flags.Add(new TestRunConfigFlag { FlagKey = "role_based_client", FlagValue = NormalizeAll(request.RoleBasedClient) });
        config.WorkflowContext = new TestRunConfigWorkflowContext
        {
            Client = NormalizeAllowed(request.Client, ["ALL", "oup", "lww", "oso", "oho", "tnf"], "ALL"),
            ContentType = NormalizeAllowed(request.ContentType, ["books", "journal"], "books"),
            Domain = NormalizeAllowed(request.Domain, ["UAT", "UAT_QA", "DEV", "DEV_QA", "LIVE", "PROD"], "UAT"),
            RoleWorkflow = NormalizeAllowed(request.RoleWorkflow, ["author_editor_collator", "editor_author_collator", "author_collator", "editor_collator"], "author_editor_collator"),
            TestingUrl = NormalizeAllowed(request.TestingUrl, ["author", "editor", "collator", "shared_author", "editor_author", "shared_collator"], "author"),
            MantisTicket = request.MantisTicket.Trim(),
            RefStyle = NormalizeAllowed(request.RefStyle, ["number", "number_sup", "number_sup_parentheses", "unnumbered", "footnotes", "name-date", "name-date-cms-18"], "number")
        };
    }

    private static TestRunConfigResponse ToResponse(TestRunConfig config)
    {
        var role = config.Flags.FirstOrDefault(flag => flag.FlagKey == "role_based")?.FlagValue ?? All;
        var client = config.Flags.FirstOrDefault(flag => flag.FlagKey == "role_based_client")?.FlagValue ?? All;
        var ui = config.Flags.FirstOrDefault(flag => flag.FlagKey == "ui")?.FlagValue ?? "off";
        var context = config.WorkflowContext ?? new TestRunConfigWorkflowContext();
        return new TestRunConfigResponse
        {
            Id = config.Id,
            TestingName = config.TestingName,
            Description = config.Description,
            CreatedBy = config.CreatedBy,
            IsActive = config.IsActive,
            Modules = config.Targets.Select(target => target.ModuleName).OrderBy(value => value).ToList(),
            TestingTypes = config.TestingTypes.Select(type => type.Value).OrderBy(value => value).ToList(),
            RoleBased = role,
            RoleBasedClient = client,
            Ui = ui,
            IsFullRun = config.Targets.Count == 0 && config.TestingTypes.Count == 0 && IsAll(role) && IsAll(client) && !ui.Equals("on", StringComparison.OrdinalIgnoreCase),
            WorkflowContext = new WorkflowContextResponse
            {
                Client = context.Client,
                ContentType = context.ContentType,
                Domain = context.Domain,
                RoleWorkflow = context.RoleWorkflow,
                TestingUrl = context.TestingUrl,
                MantisTicket = context.MantisTicket,
                RefStyle = context.RefStyle
            }
        };
    }

    private static TestRunExecutionResponse ToResponse(TestRunExecution execution)
    {
        return new TestRunExecutionResponse
        {
            Id = execution.Id,
            ConfigId = execution.ConfigId,
            TestingName = execution.Config?.TestingName ?? string.Empty,
            TriggeredBy = execution.TriggeredBy,
            Status = execution.Status,
            PlaywrightCommand = execution.PlaywrightCommand,
            PlaywrightTestsRef = execution.PlaywrightTestsRef,
            ReportPath = execution.ReportPath,
            RunKind = execution.RunKind,
            ModuleName = execution.ModuleName,
            TestCaseId = execution.TestCaseId,
            MantisTicket = execution.MantisTicket,
            FixSignal = execution.RunKind == "BUG_VERIFY"
                ? execution.Status == "PASSED" ? "Fixed" : execution.Status == "FAILED" ? "Still Failing" : execution.Status is "ERROR" or "CANCELLED" ? "Error" : string.Empty
                : string.Empty,
            ExitCode = execution.ExitCode,
            FailureSummary = execution.FailureSummary,
            TriggeredAt = execution.TriggeredAt,
            FinishedAt = execution.FinishedAt
        };
    }

    private static void EnsureCanManage(AuthUser? user)
    {
        var role = user?.Role ?? string.Empty;
        if (!role.Contains("Dev", StringComparison.OrdinalIgnoreCase) && !role.Contains("Manager", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Dev or Manager role is required.");
        }
    }

    private static bool IsAll(string value) => string.IsNullOrWhiteSpace(value) || value.Equals(All, StringComparison.OrdinalIgnoreCase);
    private static string NormalizeAll(string value) => IsAll(value) ? All : value.Trim();
    private static string FirstValue(params string[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private async Task<TestRunProgress?> FindProgressAsync(int configId, AuthUser? user, CancellationToken cancellationToken)
    {
        var progressUser = ProgressUser(user);
        return await _dbContext.TestRunProgresses.FirstOrDefaultAsync(item => item.ConfigId == configId && item.UserId == progressUser, cancellationToken);
    }

    private static string ProgressUser(AuthUser? user) => user?.Id?.Trim() is { Length: > 0 } id ? id : user?.Username ?? string.Empty;

    private static string NextModule(TestRunConfig config, string? lastModuleName)
    {
        var modules = config.Targets.Select(target => target.ModuleName).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToList();
        if (modules.Count == 0) return string.Empty;
        if (string.IsNullOrWhiteSpace(lastModuleName)) return modules[0];
        var index = modules.FindIndex(value => value.Equals(lastModuleName, StringComparison.OrdinalIgnoreCase));
        return index < 0 || index + 1 >= modules.Count ? string.Empty : modules[index + 1];
    }

    private static string NormalizeAllowed(string value, IReadOnlyList<string> allowed, string fallback)
    {
        var trimmed = value.Trim();
        return allowed.FirstOrDefault(item => item.Equals(trimmed, StringComparison.OrdinalIgnoreCase)) ?? fallback;
    }

    private static bool IsSpecFile(string path)
    {
        var name = Path.GetFileName(path);
        return name.Contains(".spec.", StringComparison.OrdinalIgnoreCase) || name.Contains(".test.", StringComparison.OrdinalIgnoreCase);
    }

    private static string SafeRead(string path)
    {
        try { return File.ReadAllText(path); } catch { return string.Empty; }
    }

    private static IReadOnlyList<string> DistinctRegexValues(string value, string pattern)
    {
        return Regex.Matches(value, pattern, RegexOptions.IgnoreCase)
            .Select(match => match.Groups[1].Value)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<bool> CommandWorksAsync(string command, string arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory)
                    ? Environment.CurrentDirectory
                    : workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process == null) return false;
            var waitTask = process.WaitForExitAsync(cancellationToken);
            var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(8), cancellationToken)) == waitTask;
            if (!completed)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string> GetGitRefAsync(string workingDirectory, CancellationToken cancellationToken)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse HEAD",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process == null) return string.Empty;
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0 ? output.Trim() : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
