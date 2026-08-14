using ImpactSupport.Api.TestCaseViewer.Data;
using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Options;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace ImpactSupport.Api.Tests.TestCaseViewer;

public class TestCaseViewerAccessServiceTests
{
    [Fact]
    public void CanSeeSheet_FiltersByConfiguredUsernameAndSheetPattern()
    {
        var service = new TestCaseViewerAccessService(Options.Create(new TestCaseViewerOptions
        {
            AccessRules =
            [
                new TestCaseViewerAccessRule
                {
                    Usernames = ["alice"],
                    ReportTypes = ["master"],
                    SheetPatterns = ["Billing*"]
                }
            ]
        }));
        var user = new AuthUser { Username = "alice" };
        var file = new QaDashboardFileCache { ReportType = "master", FileName = "Testcase_2026" };

        Assert.True(service.CanSeeSheet(user, file, new QaDashboardSheetCache { SheetName = "Billing Flow" }));
        Assert.False(service.CanSeeSheet(user, file, new QaDashboardSheetCache { SheetName = "Admin Flow" }));
    }

    [Fact]
    public void CanSeeSheet_AllowsAll_WhenNoRulesConfigured()
    {
        var service = new TestCaseViewerAccessService(Options.Create(new TestCaseViewerOptions()));

        Assert.True(service.CanSeeSheet(
            new AuthUser { Username = "anyone" },
            new QaDashboardFileCache { ReportType = "regression", FileName = "Regression A" },
            new QaDashboardSheetCache { SheetName = "Any Sheet" }));
    }
}
