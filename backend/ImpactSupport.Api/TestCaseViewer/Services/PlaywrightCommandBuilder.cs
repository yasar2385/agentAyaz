using System.Text;
using ImpactSupport.Api.TestCaseViewer.Data;
using ImpactSupport.Api.TestCaseViewer.Options;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class PlaywrightCommandBuilder
{
    private readonly PlaywrightOptions _options;
    private readonly ILogger<PlaywrightCommandBuilder> _logger;

    public PlaywrightCommandBuilder(Microsoft.Extensions.Options.IOptions<PlaywrightOptions> options, ILogger<PlaywrightCommandBuilder> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string BuildArguments(TestRunConfig config, string moduleOverride = "")
    {
        var args = new List<string>();
        args.AddRange(SplitArguments(_options.ArgumentsPrefix));

        var modules = string.IsNullOrWhiteSpace(moduleOverride)
            ? config.Targets.Select(target => target.ModuleName)
            : [moduleOverride];
        foreach (var module in modules.Where(value => !string.IsNullOrWhiteSpace(value)).OrderBy(value => value))
        {
            args.Add($"--grep=@module={module}");
        }

        var selectedTypes = config.TestingTypes
            .Select(type => type.Value)
            .Where(value => !IsAll(value))
            .OrderBy(value => value)
            .ToList();
        if (selectedTypes.Count > 0)
        {
            var typeExpr = string.Join("|", selectedTypes.Select(value => $"@type={value}"));
            args.Add($"--grep=({typeExpr})");
        }

        foreach (var flag in config.Flags.OrderBy(flag => flag.FlagKey))
        {
            switch (flag.FlagKey)
            {
                case "ui":
                    if (flag.FlagValue.Equals("on", StringComparison.OrdinalIgnoreCase)) args.Add("--headed");
                    break;
                case "role_based":
                    if (!IsAll(flag.FlagValue)) args.Add($"--grep=@role={flag.FlagValue}");
                    break;
                case "role_based_client":
                    if (!IsAll(flag.FlagValue)) args.Add($"--grep=@client={flag.FlagValue}");
                    break;
                default:
                    _logger.LogWarning("Unknown Playwright run flag {FlagKey} skipped.", flag.FlagKey);
                    break;
            }
        }

        return string.Join(" ", args.Select(QuoteIfNeeded));
    }

    public string BuildDisplayCommand(TestRunConfig config, string moduleOverride = "")
    {
        return $"{_options.Command} {BuildArguments(config, moduleOverride)}";
    }

    private static bool IsAll(string value)
    {
        return value.Equals("ALL", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> SplitArguments(string value)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        foreach (var ch in value)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }

            current.Append(ch);
        }

        if (current.Length > 0) result.Add(current.ToString());
        return result;
    }

    private static string QuoteIfNeeded(string value)
    {
        return value.Any(char.IsWhiteSpace) ? $"\"{value.Replace("\"", "\\\"")}\"" : value;
    }
}
