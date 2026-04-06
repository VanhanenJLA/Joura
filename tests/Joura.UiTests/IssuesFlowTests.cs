using Microsoft.Playwright;
using Xunit;

namespace Joura.UiTests;

[Collection(UiTestCollection.Name)]
public sealed class IssuesFlowTests(TestAppFixture fixture)
{
    [Fact]
    public async Task UserCanFilterAndBulkAssignIssues()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await UiTestHelpers.LoginAsAsync(page, fixture.BaseUrl, "avery@joura.local");

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var firstIssue = await UiTestHelpers.CreateIssueAsync(page, fixture.BaseUrl, $"UI bulk {suffix} alpha");
        await UiTestHelpers.CreateIssueAsync(page, fixture.BaseUrl, $"UI bulk {suffix} beta");

        await page.GotoAsync($"{fixture.BaseUrl}/issues");
        await page.GetByTestId("issues-page").WaitForAsync();
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("issues-search"), suffix);

        await Assertions.Expect(page.GetByTestId("issues-result-count")).ToHaveTextAsync("2 issues");
        Assert.Equal("2 issues", (await page.GetByTestId("issues-result-count").TextContentAsync())?.Trim());

        await page.GetByTestId("issues-select-all").CheckAsync();
        await Assertions.Expect(page.GetByTestId("issues-selection-count")).ToHaveTextAsync("2 selected");
        Assert.Equal("2 selected", (await page.GetByTestId("issues-selection-count").TextContentAsync())?.Trim());

        await page.GetByTestId("issues-bulk-actions").ClickAsync();
        await page.GetByTestId("bulk-actions-modal").WaitForAsync();
        await page.GetByTestId("bulk-action-kind").SelectOptionAsync(new SelectOptionValue { Value = "AssignAssignee" });
        await page.GetByTestId("bulk-assignee").SelectOptionAsync(new SelectOptionValue { Label = "Devon Developer" });
        await page.GetByTestId("bulk-apply").ClickAsync();

        await page.GetByTestId("bulk-actions-modal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        Assert.Equal("0 selected", (await page.GetByTestId("issues-selection-count").TextContentAsync())?.Trim());

        await page.GetByTestId($"issue-link-{firstIssue.Key.ToLowerInvariant()}").ClickAsync();
        await page.GetByTestId("issue-details-page").WaitForAsync();

        Assert.Contains("Devon Developer", await page.GetByTestId("issue-assignee").TextContentAsync());
    }
}
