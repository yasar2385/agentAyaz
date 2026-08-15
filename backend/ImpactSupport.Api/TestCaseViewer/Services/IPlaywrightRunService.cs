using ImpactSupport.Api.TestCaseViewer.Models;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public interface IPlaywrightRunService
{
    Task<PlaywrightReadinessResponse> GetReadinessAsync(CancellationToken cancellationToken = default);
    Task<RunMetadataResponse> GetMetadataAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TestRunConfigResponse>> GetConfigsAsync(CancellationToken cancellationToken = default);
    Task<RecentRunsResponse> GetRecentRunsAsync(string scope, int limit, AuthUser? user, CancellationToken cancellationToken = default);
    Task<RunProgressResponse?> GetProgressAsync(int configId, AuthUser? user, CancellationToken cancellationToken = default);
    Task<TestRunConfigResponse> CreateConfigAsync(TestRunConfigRequest request, AuthUser? user, CancellationToken cancellationToken = default);
    Task<TestRunConfigResponse?> UpdateConfigAsync(int configId, TestRunConfigRequest request, AuthUser? user, CancellationToken cancellationToken = default);
    Task<TestRunExecutionResponse?> TriggerAsync(int configId, AuthUser? user, CancellationToken cancellationToken = default);
    Task<TestRunExecutionResponse?> ContinueAsync(int configId, AuthUser? user, CancellationToken cancellationToken = default);
    Task<TestRunExecutionResponse> VerifyFixAsync(VerifyFixRequest request, AuthUser? user, CancellationToken cancellationToken = default);
    Task<TestRunExecutionResponse?> GetExecutionAsync(int executionId, CancellationToken cancellationToken = default);
    Task<TestRunExecutionResponse?> CancelAsync(int executionId, AuthUser? user, CancellationToken cancellationToken = default);
    Task<string?> GetReportPathAsync(int executionId, CancellationToken cancellationToken = default);
}
