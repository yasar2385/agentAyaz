using ImpactSupport.Api.TestCaseViewer.Data;
using ImpactSupport.Api.TestCaseViewer.Options;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ImpactSupport.Api.Tests.TestCaseViewer;

public sealed class PlaywrightCommandBuilderTests
{
    [Fact]
    public void BuildArguments_OmitsAllDimensions()
    {
        var command = Build(new TestRunConfig
        {
            Flags =
            [
                new() { FlagKey = "ui", FlagValue = "off" },
                new() { FlagKey = "role_based", FlagValue = "ALL" },
                new() { FlagKey = "role_based_client", FlagValue = "ALL" }
            ]
        });

        Assert.Equal("playwright test", command);
    }

    [Fact]
    public void BuildArguments_CombinesModuleTypesRoleAndClient()
    {
        var command = Build(new TestRunConfig
        {
            Targets = [new() { ModuleName = "CKEditor" }],
            TestingTypes = [new() { Value = "Browser" }, new() { Value = "Regression" }],
            Flags =
            [
                new() { FlagKey = "ui", FlagValue = "on" },
                new() { FlagKey = "role_based", FlagValue = "Author" },
                new() { FlagKey = "role_based_client", FlagValue = "LWW" },
                new() { FlagKey = "unknown", FlagValue = "x" }
            ]
        });

        Assert.Contains("--grep=@module=CKEditor", command);
        Assert.Contains("--grep=(@type=Browser|@type=Regression)", command);
        Assert.Contains("--grep=@role=Author", command);
        Assert.Contains("--grep=@client=LWW", command);
        Assert.Contains("--headed", command);
    }

    private static string Build(TestRunConfig config)
    {
        var builder = new PlaywrightCommandBuilder(
            Options.Create(new PlaywrightOptions { ArgumentsPrefix = "playwright test" }),
            NullLogger<PlaywrightCommandBuilder>.Instance);
        return builder.BuildArguments(config);
    }
}
