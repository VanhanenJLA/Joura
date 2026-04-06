using System.Globalization;
using Microsoft.Playwright;
using Xunit;

namespace Joura.UiTests;

[Collection(UiTestCollection.Name)]
public sealed class WorklogsFlowTests(TestAppFixture fixture)
{
    [Fact]
    public async Task UserCanCreateWorklogFromWeeklyPlanner()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await UiTestHelpers.LoginAsAsync(page, fixture.BaseUrl, "avery@joura.local");

        var issue = await UiTestHelpers.CreateIssueAsync(page, fixture.BaseUrl, $"UI plan {Guid.NewGuid().ToString("N")[..8]}");
        var note = $"UI planner note {Guid.NewGuid():N}";
        var start = DateTime.Today.AddHours(11);
        var end = start.AddHours(1);

        await page.GotoAsync($"{fixture.BaseUrl}/worklogs");
        await page.GetByTestId("worklogs-page").WaitForAsync();
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("worklogs-issue-input"), issue.Key);
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("worklogs-start"), ToDateTimeLocal(start));
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("worklogs-end"), ToDateTimeLocal(end));
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("worklogs-note"), note);
        await page.GetByTestId("worklogs-submit").ClickAsync();

        await page.GetByTestId("worklog-planner-entry")
            .Filter(new LocatorFilterOptions { HasText = issue.Key })
            .First
            .WaitForAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/reports");
        await page.GetByTestId("reports-page").WaitForAsync();
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("reports-search"), note);

        await Assertions.Expect(page.GetByTestId("reports-result-count")).ToHaveTextAsync("1 worklog items");
        Assert.Equal("1 worklog items", (await page.GetByTestId("reports-result-count").TextContentAsync())?.Trim());
    }

    private static string ToDateTimeLocal(DateTime value)
    {
        return value.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
    }
}
