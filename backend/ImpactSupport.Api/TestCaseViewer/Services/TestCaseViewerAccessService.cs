using System.Text.RegularExpressions;
using ImpactSupport.Api.TestCaseViewer.Data;
using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Options;
using Microsoft.Extensions.Options;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public interface ITestCaseViewerAccessService
{
    bool CanSeeFile(AuthUser? user, QaDashboardFileCache file);
    bool CanSeeSheet(AuthUser? user, QaDashboardFileCache file, QaDashboardSheetCache sheet);
}

public sealed class TestCaseViewerAccessService : ITestCaseViewerAccessService
{
    private readonly TestCaseViewerOptions _options;

    public TestCaseViewerAccessService(IOptions<TestCaseViewerOptions> options)
    {
        _options = options.Value;
    }

    public bool CanSeeFile(AuthUser? user, QaDashboardFileCache file)
    {
        var rules = MatchingRules(user).ToList();
        if (rules.Count == 0)
        {
            return true;
        }

        return rules.Any(rule => MatchesAny(rule.ReportTypes, file.ReportType)
            && MatchesAny(rule.FilePatterns, file.FileName));
    }

    public bool CanSeeSheet(AuthUser? user, QaDashboardFileCache file, QaDashboardSheetCache sheet)
    {
        var rules = MatchingRules(user).ToList();
        if (rules.Count == 0)
        {
            return true;
        }

        return rules.Any(rule =>
            MatchesAny(rule.ReportTypes, file.ReportType)
            && MatchesAny(rule.FilePatterns, file.FileName)
            && MatchesAny(rule.SheetPatterns, sheet.SheetName)
            && MatchesAny(rule.ModulePatterns, FirstValue(sheet.Module, sheet.PurposeOfTesting)));
    }

    private IEnumerable<TestCaseViewerAccessRule> MatchingRules(AuthUser? user)
    {
        foreach (var rule in _options.AccessRules)
        {
            var usernameMatch = rule.Usernames.Length == 0 || MatchesAny(rule.Usernames, user?.Username ?? string.Empty);
            var roleMatch = rule.Roles.Length == 0 || MatchesAny(rule.Roles, user?.Role ?? string.Empty);
            if (usernameMatch && roleMatch)
            {
                yield return rule;
            }
        }
    }

    private static bool MatchesAny(IReadOnlyList<string> patterns, string value)
    {
        if (patterns.Count == 0)
        {
            return true;
        }

        return patterns.Any(pattern => WildcardMatch(value, pattern));
    }

    private static bool WildcardMatch(string value, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern) || pattern == "*")
        {
            return true;
        }

        var regex = "^" + Regex.Escape(pattern.Trim()).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(value ?? string.Empty, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string FirstValue(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }
}
