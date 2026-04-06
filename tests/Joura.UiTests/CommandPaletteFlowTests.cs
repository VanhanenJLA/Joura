using Microsoft.Playwright;
using Xunit;

namespace Joura.UiTests;

[Collection(UiTestCollection.Name)]
public sealed class CommandPaletteFlowTests(TestAppFixture fixture)
{
    [Fact]
    public async Task UserCanNavigateWithCommandPalette()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await UiTestHelpers.LoginAsAsync(page, fixture.BaseUrl, "avery@joura.local");

        await page.GetByTestId("command-palette-trigger").WaitForAsync();
        await page.GetByTestId("command-palette-trigger").ClickAsync();
        await page.GetByTestId("command-palette").WaitForAsync();
        await page.GetByTestId("command-palette-input").FillAsync("reports");
        await page.GetByTestId("command-palette-result-reports").WaitForAsync();
        await page.GetByTestId("command-palette-result-reports").ClickAsync();

        await page.WaitForURLAsync("**/reports");
        await page.GetByTestId("reports-page").WaitForAsync();

        var issue = await UiTestHelpers.CreateIssueAsync(page, fixture.BaseUrl, $"UI cmd {Guid.NewGuid().ToString("N")[..8]}");

        await page.GetByTestId("command-palette-trigger").ClickAsync();
        await page.GetByTestId("command-palette").WaitForAsync();
        await page.GetByTestId("command-palette-input").FillAsync(issue.Key);

        var issueResult = page.Locator("[data-testid^='command-palette-result-issue-']")
            .Filter(new LocatorFilterOptions { HasText = issue.Key })
            .First;
        await issueResult.WaitForAsync();
        await issueResult.ClickAsync();

        await page.GetByTestId("issue-details-page").WaitForAsync();
        Assert.Equal(issue.Key, (await page.GetByTestId("issue-key").TextContentAsync())?.Trim());
    }
}
