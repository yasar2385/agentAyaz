using ImpactSupport.Api.Support.Data;
using ImpactSupport.Api.TestCaseViewer.Data;
using ImpactSupport.Api.TestCaseViewer.Models;
using Microsoft.EntityFrameworkCore;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class MasterReviewService : IMasterReviewService
{
    private readonly SupportDbContext _dbContext;

    public MasterReviewService(SupportDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MasterModuleSummaryResponse>> GetModulesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MasterModules
            .AsNoTracking()
            .Select(module => new MasterModuleSummaryResponse
            {
                ModuleId = module.Id,
                ModuleName = module.Name,
                TestCaseCount = _dbContext.MasterTemplates.Count(master => master.MasterModules == module.Id && master.MasterIsActive)
            })
            .Where(module => module.TestCaseCount > 0)
            .OrderBy(module => module.ModuleName)
            .ToListAsync(cancellationToken);
    }

    public async Task<MasterTemplateListResponse> GetListAsync(int? moduleId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _dbContext.MasterTemplates.AsNoTracking().Where(master => master.MasterIsActive);
        if (moduleId.HasValue) query = query.Where(master => master.MasterModules == moduleId);

        var total = await query.CountAsync(cancellationToken);
        var modules = await _dbContext.MasterModules.AsNoTracking().ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var qaStatuses = await _dbContext.MasterQaStatuses.AsNoTracking().ToDictionaryAsync(item => item.Id, item => item.Value, cancellationToken);
        var devStatuses = await _dbContext.MasterDevStatuses.AsNoTracking().ToDictionaryAsync(item => item.Id, item => item.Value, cancellationToken);
        var items = await query
            .OrderBy(master => master.MasterTestId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(master => new
            {
                master.MasterId,
                master.MasterTestId,
                master.MasterTestNo,
                master.MasterModules,
                master.MasterQaStatus,
                master.MasterDevStatus,
                master.MasterUpdatedAt,
                master.MasterUpdatedBy
            })
            .ToListAsync(cancellationToken);

        return new MasterTemplateListResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items.Select(master => new MasterTemplateSummaryResponse
            {
                MasterId = master.MasterId,
                MasterTestId = master.MasterTestId,
                MasterTestNo = master.MasterTestNo,
                ModuleName = master.MasterModules.HasValue && modules.TryGetValue(master.MasterModules.Value, out var module) ? module : string.Empty,
                QaStatus = master.MasterQaStatus.HasValue && qaStatuses.TryGetValue(master.MasterQaStatus.Value, out var qa) ? qa : string.Empty,
                DevStatus = master.MasterDevStatus.HasValue && devStatuses.TryGetValue(master.MasterDevStatus.Value, out var dev) ? dev : string.Empty,
                MasterUpdatedAt = master.MasterUpdatedAt,
                MasterUpdatedBy = master.MasterUpdatedBy ?? string.Empty
            }).ToList()
        };
    }

    public async Task<MasterLookupsResponse> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        return new MasterLookupsResponse
        {
            Modules = await Lookup(_dbContext.MasterModules, item => item.Name, cancellationToken),
            PreconditionRoles = await Lookup(_dbContext.MasterPreconditionRoles, item => item.Value, cancellationToken),
            TestingTypes = await Lookup(_dbContext.MasterTestingTypes, item => item.Value, cancellationToken),
            IssueTypes = await Lookup(_dbContext.MasterIssueTypes, item => item.Value, cancellationToken),
            QaStatuses = await Lookup(_dbContext.MasterQaStatuses, item => item.Value, cancellationToken),
            DevStatuses = await Lookup(_dbContext.MasterDevStatuses, item => item.Value, cancellationToken),
            Clients = await Lookup(_dbContext.Clients, item => item.Code, cancellationToken),
            ContentTypes = await Lookup(_dbContext.Types, item => item.Value, cancellationToken),
            DtdTypes = await Lookup(_dbContext.DtdTypes, item => item.Value, cancellationToken),
            RoleWorkflows = await Lookup(_dbContext.RoleWorkflows, item => item.Value, cancellationToken)
        };
    }

    public async Task<MasterTemplateDetailResponse?> GetDetailAsync(string masterTestId, CancellationToken cancellationToken = default)
    {
        var master = await LoadMasterAsync(masterTestId, asTracking: false, cancellationToken);
        return master == null ? null : await ToDetailAsync(master, cancellationToken);
    }

    public async Task<MasterTemplateDetailResponse> CreateAsync(MasterTemplateCreateRequest request, AuthUser? user, CancellationToken cancellationToken = default)
    {
        EnsureCanEdit(user);
        var testCaseId = request.MasterTestId.Trim();
        if (string.IsNullOrWhiteSpace(testCaseId)) throw new ArgumentException("MasterTestId is required.");
        if (await _dbContext.MasterTemplates.AnyAsync(item => item.MasterTestId == testCaseId, cancellationToken))
        {
            throw new ArgumentException($"MasterTestId '{testCaseId}' already exists.");
        }

        await ValidateIdsAsync(request, cancellationToken);
        var editedBy = user?.DisplayName ?? user?.Username ?? "unknown";
        var now = DateTimeOffset.UtcNow;
        var master = new MasterTemplate
        {
            MasterTestId = testCaseId,
            MasterCreatedAt = now,
            MasterUpdatedAt = now,
            MasterUpdatedBy = editedBy,
            MasterIsActive = true,
            Details = new MasterTestDetails()
        };
        _dbContext.MasterTemplates.Add(master);
        ApplyCreateValues(master, request);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _dbContext.MasterTemplateEditHistory.Add(new MasterTemplateEditHistory
        {
            MasterId = master.MasterId,
            FieldName = "Create",
            OldValue = string.Empty,
            NewValue = testCaseId,
            EditedBy = editedBy,
            EditedAt = now
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await ToDetailAsync(master, cancellationToken);
    }

    public async Task<MasterTemplateDetailResponse?> UpdateAsync(string masterTestId, MasterTemplateUpdateRequest request, AuthUser? user, CancellationToken cancellationToken = default)
    {
        EnsureCanEdit(user);
        var master = await LoadMasterAsync(masterTestId, asTracking: true, cancellationToken);
        if (master == null) return null;
        if (master.MasterUpdatedAt > request.LastKnownUpdatedAt.AddMilliseconds(1))
        {
            throw new ConcurrencyConflictException("This test case was updated after you opened it. Reload before saving.");
        }

        await ValidateIdsAsync(request, cancellationToken);
        var changed = new List<MasterTemplateEditHistory>();
        var editedBy = user?.DisplayName ?? user?.Username ?? "unknown";

        Track(changed, master.MasterId, "MasterTestNo", master.MasterTestNo, request.MasterTestNo, value => master.MasterTestNo = value ?? string.Empty);
        Track(changed, master.MasterId, "MasterModules", master.MasterModules, request.ModuleId, value => master.MasterModules = value);
        Track(changed, master.MasterId, "MasterPreconditionRole", master.MasterPreconditionRole, request.PreconditionRoleId, value => master.MasterPreconditionRole = value);
        Track(changed, master.MasterId, "MasterType", master.MasterType, request.MasterTypeId, value => master.MasterType = value);
        Track(changed, master.MasterId, "MasterDtdType", master.MasterDtdType, request.DtdTypeId, value => master.MasterDtdType = value);
        Track(changed, master.MasterId, "MasterRoleWorkflow", master.MasterRoleWorkflow, request.RoleWorkflowId, value => master.MasterRoleWorkflow = value);
        TrackBool(changed, master.MasterId, "MasterIsCollaborative", master.MasterIsCollaborative, request.MasterIsCollaborative, value => master.MasterIsCollaborative = value);
        TrackBool(changed, master.MasterId, "MasterIsSharedRole", master.MasterIsSharedRole, request.MasterIsSharedRole, value => master.MasterIsSharedRole = value);
        Track(changed, master.MasterId, "MasterPreparedBy", master.MasterPreparedBy, request.MasterPreparedBy, value => master.MasterPreparedBy = value ?? string.Empty);
        Track(changed, master.MasterId, "MasterPreparedDate", master.MasterPreparedDate, request.MasterPreparedDate, value => master.MasterPreparedDate = value ?? string.Empty);
        Track(changed, master.MasterId, "MasterTestData", master.MasterTestData, request.MasterTestData, value => master.MasterTestData = value ?? string.Empty);
        Track(changed, master.MasterId, "MasterExpectedResult", master.MasterExpectedResult, request.MasterExpectedResult, value => master.MasterExpectedResult = value ?? string.Empty);
        Track(changed, master.MasterId, "MasterActualResult", master.MasterActualResult, request.MasterActualResult, value => master.MasterActualResult = value ?? string.Empty);
        Track(changed, master.MasterId, "MasterIssueType", master.MasterIssueType, request.IssueTypeId, value => master.MasterIssueType = value);
        Track(changed, master.MasterId, "MasterQaStatus", master.MasterQaStatus, request.QaStatusId, value => master.MasterQaStatus = value);
        Track(changed, master.MasterId, "MasterDevStatus", master.MasterDevStatus, request.DevStatusId, value => master.MasterDevStatus = value);

        master.Details ??= new MasterTestDetails { MasterTemplate = master };
        Track(changed, master.MasterId, "MasterDescription", master.Details.MasterDescription, request.MasterDescription, value => master.Details.MasterDescription = value ?? string.Empty);
        Track(changed, master.MasterId, "MasterTestSteps", master.Details.MasterTestSteps, request.MasterTestSteps, value => master.Details.MasterTestSteps = value ?? string.Empty);

        if (request.TestingTypeIds != null)
        {
            ReplaceJoin(changed, master.MasterId, "TestingTypes", master.TestingTypes.Select(item => item.TestingTypeId), request.TestingTypeIds, () =>
            {
                master.TestingTypes.Clear();
                foreach (var id in request.TestingTypeIds.Distinct()) master.TestingTypes.Add(new MasterTemplateTestingType { TestingTypeId = id });
            });
        }

        if (request.ClientIds != null)
        {
            ReplaceJoin(changed, master.MasterId, "Clients", master.Clients.Select(item => item.ClientId), request.ClientIds, () =>
            {
                master.Clients.Clear();
                foreach (var id in request.ClientIds.Distinct()) master.Clients.Add(new MasterTemplateClient { ClientId = id });
                master.MasterClient = request.ClientIds.Count > 0 ? request.ClientIds[0] : null;
            });
        }

        if (request.Remarks != null)
        {
            var normalized = request.Remarks.Where(item => item.RoundNumber is >= 1 and <= 4).OrderBy(item => item.RoundNumber).ToList();
            var oldValue = string.Join(" | ", master.Remarks.OrderBy(item => item.RoundNumber).Select(item => $"{item.RoundNumber}:{item.QaRemark}/{item.DevRemark}"));
            var newValue = string.Join(" | ", normalized.Select(item => $"{item.RoundNumber}:{item.QaRemark}/{item.DevRemark}"));
            if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                changed.Add(History(master.MasterId, "Remarks", oldValue, newValue));
                master.Remarks.Clear();
                foreach (var remark in normalized)
                {
                    if (string.IsNullOrWhiteSpace(remark.QaRemark) && string.IsNullOrWhiteSpace(remark.DevRemark)) continue;
                    master.Remarks.Add(new MasterTemplateRemark { RoundNumber = remark.RoundNumber, QaRemark = remark.QaRemark, DevRemark = remark.DevRemark });
                }
            }
        }

        if (changed.Count > 0)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var history in changed)
            {
                history.EditedBy = editedBy;
                history.EditedAt = now;
                _dbContext.MasterTemplateEditHistory.Add(history);
            }
            master.MasterUpdatedBy = editedBy;
            master.MasterUpdatedAt = now;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await ToDetailAsync(master, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string masterTestId, AuthUser? user, CancellationToken cancellationToken = default)
    {
        EnsureCanEdit(user);
        var master = await _dbContext.MasterTemplates.FirstOrDefaultAsync(item => item.MasterTestId == masterTestId && item.MasterIsActive, cancellationToken);
        if (master == null) return false;

        var editedBy = user?.DisplayName ?? user?.Username ?? "unknown";
        var now = DateTimeOffset.UtcNow;
        master.MasterIsActive = false;
        master.MasterDeletedAt = now;
        master.MasterDeletedBy = editedBy;
        master.MasterUpdatedAt = now;
        master.MasterUpdatedBy = editedBy;
        _dbContext.MasterTemplateEditHistory.Add(new MasterTemplateEditHistory
        {
            MasterId = master.MasterId,
            FieldName = "Delete",
            OldValue = "Active",
            NewValue = "Inactive",
            EditedBy = editedBy,
            EditedAt = now
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ApplyCreateValues(MasterTemplate master, MasterTemplateCreateRequest request)
    {
        master.MasterTestNo = request.MasterTestNo ?? string.Empty;
        master.MasterModules = request.ModuleId;
        master.MasterPreconditionRole = request.PreconditionRoleId;
        master.MasterType = request.MasterTypeId;
        master.MasterDtdType = request.DtdTypeId;
        master.MasterRoleWorkflow = request.RoleWorkflowId;
        master.MasterIsCollaborative = request.MasterIsCollaborative ?? false;
        master.MasterIsSharedRole = request.MasterIsSharedRole ?? false;
        master.MasterPreparedBy = request.MasterPreparedBy ?? string.Empty;
        master.MasterPreparedDate = request.MasterPreparedDate ?? string.Empty;
        master.MasterTestData = request.MasterTestData ?? string.Empty;
        master.MasterExpectedResult = request.MasterExpectedResult ?? string.Empty;
        master.MasterActualResult = request.MasterActualResult ?? string.Empty;
        master.MasterIssueType = request.IssueTypeId;
        master.MasterQaStatus = request.QaStatusId;
        master.MasterDevStatus = request.DevStatusId;
        master.Details ??= new MasterTestDetails { MasterTemplate = master };
        master.Details.MasterDescription = request.MasterDescription ?? string.Empty;
        master.Details.MasterTestSteps = request.MasterTestSteps ?? string.Empty;
        master.TestingTypes.Clear();
        foreach (var id in request.TestingTypeIds?.Distinct() ?? [])
        {
            master.TestingTypes.Add(new MasterTemplateTestingType { TestingTypeId = id });
        }
        master.Clients.Clear();
        foreach (var id in request.ClientIds?.Distinct() ?? [])
        {
            master.Clients.Add(new MasterTemplateClient { ClientId = id });
        }
        master.MasterClient = request.ClientIds?.Count > 0 ? request.ClientIds[0] : null;
        master.Remarks.Clear();
        var remarks = request.Remarks == null
            ? Enumerable.Empty<MasterRemarkResponse>()
            : request.Remarks.Where(item => item.RoundNumber is >= 1 and <= 4).OrderBy(item => item.RoundNumber);
        foreach (var remark in remarks)
        {
            if (string.IsNullOrWhiteSpace(remark.QaRemark) && string.IsNullOrWhiteSpace(remark.DevRemark)) continue;
            master.Remarks.Add(new MasterTemplateRemark { RoundNumber = remark.RoundNumber, QaRemark = remark.QaRemark, DevRemark = remark.DevRemark });
        }
    }

    private async Task<MasterTemplate?> LoadMasterAsync(string masterTestId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = _dbContext.MasterTemplates
            .Include(item => item.Details)
            .Include(item => item.TestingTypes)
            .Include(item => item.Clients)
            .Include(item => item.Remarks)
            .Include(item => item.EditHistory)
            .Where(item => item.MasterTestId == masterTestId && item.MasterIsActive);
        if (!asTracking) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<MasterTemplateDetailResponse> ToDetailAsync(MasterTemplate master, CancellationToken cancellationToken)
    {
        var modules = await _dbContext.MasterModules.AsNoTracking().ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        return new MasterTemplateDetailResponse
        {
            MasterId = master.MasterId,
            MasterTestId = master.MasterTestId,
            MasterOriginalRawId = master.MasterOriginalRawId ?? string.Empty,
            MasterTestNo = master.MasterTestNo,
            MasterSourceSheet = master.MasterSourceSheet,
            MasterSourceRow = master.MasterSourceRow,
            ModuleId = master.MasterModules,
            ModuleName = master.MasterModules.HasValue && modules.TryGetValue(master.MasterModules.Value, out var module) ? module : string.Empty,
            PreconditionRoleId = master.MasterPreconditionRole,
            MasterTypeId = master.MasterType,
            DtdTypeId = master.MasterDtdType,
            RoleWorkflowId = master.MasterRoleWorkflow,
            MasterIsCollaborative = master.MasterIsCollaborative,
            MasterIsSharedRole = master.MasterIsSharedRole,
            MasterPreparedBy = master.MasterPreparedBy,
            MasterPreparedDate = master.MasterPreparedDate,
            MasterTestData = master.MasterTestData,
            MasterExpectedResult = master.MasterExpectedResult,
            MasterActualResult = master.MasterActualResult,
            IssueTypeId = master.MasterIssueType,
            QaStatusId = master.MasterQaStatus,
            DevStatusId = master.MasterDevStatus,
            MasterDescription = master.Details?.MasterDescription ?? string.Empty,
            MasterTestSteps = master.Details?.MasterTestSteps ?? string.Empty,
            TestingTypeIds = master.TestingTypes.OrderBy(item => item.TestingTypeId).Select(item => item.TestingTypeId).ToList(),
            ClientIds = master.Clients.OrderBy(item => item.ClientId).Select(item => item.ClientId).ToList(),
            Remarks = Enumerable.Range(1, 4).Select(round =>
            {
                var existing = master.Remarks.FirstOrDefault(item => item.RoundNumber == round);
                return new MasterRemarkResponse { RoundNumber = round, QaRemark = existing?.QaRemark ?? string.Empty, DevRemark = existing?.DevRemark ?? string.Empty };
            }).ToList(),
            EditHistory = master.EditHistory.OrderByDescending(item => item.EditedAt).Select(item => new MasterEditHistoryResponse
            {
                Id = item.Id,
                FieldName = item.FieldName,
                OldValue = item.OldValue,
                NewValue = item.NewValue,
                EditedBy = item.EditedBy,
                EditedAt = item.EditedAt
            }).ToList(),
            MasterUpdatedAt = master.MasterUpdatedAt,
            MasterUpdatedBy = master.MasterUpdatedBy ?? string.Empty
        };
    }

    private async Task ValidateIdsAsync(MasterTemplateUpdateRequest request, CancellationToken cancellationToken)
    {
        if (request.ModuleId.HasValue) await RequireExists(_dbContext.MasterModules, request.ModuleId.Value, "Module", cancellationToken);
        if (request.PreconditionRoleId.HasValue) await RequireExists(_dbContext.MasterPreconditionRoles, request.PreconditionRoleId.Value, "Precondition role", cancellationToken);
        if (request.MasterTypeId.HasValue) await RequireExists(_dbContext.Types, request.MasterTypeId.Value, "Type", cancellationToken);
        if (request.DtdTypeId.HasValue) await RequireExists(_dbContext.DtdTypes, request.DtdTypeId.Value, "DTD type", cancellationToken);
        if (request.RoleWorkflowId.HasValue) await RequireExists(_dbContext.RoleWorkflows, request.RoleWorkflowId.Value, "Role workflow", cancellationToken);
        if (request.IssueTypeId.HasValue) await RequireExists(_dbContext.MasterIssueTypes, request.IssueTypeId.Value, "Issue type", cancellationToken);
        if (request.QaStatusId.HasValue) await RequireExists(_dbContext.MasterQaStatuses, request.QaStatusId.Value, "QA status", cancellationToken);
        if (request.DevStatusId.HasValue) await RequireExists(_dbContext.MasterDevStatuses, request.DevStatusId.Value, "Dev status", cancellationToken);
        foreach (var id in request.TestingTypeIds ?? []) await RequireExists(_dbContext.MasterTestingTypes, id, "Testing type", cancellationToken);
        foreach (var id in request.ClientIds ?? []) await RequireExists(_dbContext.Clients, id, "Client", cancellationToken);
    }

    private static async Task RequireExists<T>(DbSet<T> set, int id, string label, CancellationToken cancellationToken) where T : class
    {
        var row = await set.FindAsync([id], cancellationToken);
        if (row == null)
        {
            throw new ArgumentException($"{label} id {id} does not exist.");
        }
    }

    private static async Task<IReadOnlyList<LookupItemResponse>> Lookup<T>(DbSet<T> set, Func<T, string> value, CancellationToken cancellationToken) where T : class
    {
        var rows = await set.AsNoTracking().ToListAsync(cancellationToken);
        return rows.Select(item => new LookupItemResponse { Id = (int)(typeof(T).GetProperty("Id")?.GetValue(item) ?? 0), Value = value(item) }).OrderBy(item => item.Value).ToList();
    }

    private static void Track<T>(List<MasterTemplateEditHistory> changes, int masterId, string field, T oldValue, T? newValue, Action<T?> apply)
    {
        if (newValue is null) return;
        if (EqualityComparer<T?>.Default.Equals(oldValue, newValue)) return;
        changes.Add(History(masterId, field, oldValue?.ToString() ?? string.Empty, newValue?.ToString() ?? string.Empty));
        apply(newValue);
    }

    private static void TrackBool(List<MasterTemplateEditHistory> changes, int masterId, string field, bool oldValue, bool? newValue, Action<bool> apply)
    {
        if (!newValue.HasValue || oldValue == newValue.Value) return;
        changes.Add(History(masterId, field, oldValue.ToString(), newValue.Value.ToString()));
        apply(newValue.Value);
    }

    private static void ReplaceJoin(List<MasterTemplateEditHistory> changes, int masterId, string field, IEnumerable<int> oldIds, IEnumerable<int> newIds, Action apply)
    {
        var oldValue = string.Join(",", oldIds.OrderBy(id => id));
        var newValue = string.Join(",", newIds.Distinct().OrderBy(id => id));
        if (oldValue == newValue) return;
        changes.Add(History(masterId, field, oldValue, newValue));
        apply();
    }

    private static MasterTemplateEditHistory History(int masterId, string field, string oldValue, string newValue) => new()
    {
        MasterId = masterId,
        FieldName = field,
        OldValue = oldValue,
        NewValue = newValue
    };

    private static void EnsureCanEdit(AuthUser? user)
    {
        var role = user?.Role ?? string.Empty;
        if (string.IsNullOrWhiteSpace(user?.Username)
            || (!role.Contains("QA", StringComparison.OrdinalIgnoreCase)
                && !role.Contains("Dev", StringComparison.OrdinalIgnoreCase)
                && !role.Contains("Manager", StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException("QA, Dev, or Manager role is required.");
        }
    }
}

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message) { }
}
