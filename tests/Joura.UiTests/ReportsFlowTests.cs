using Microsoft.Playwright;
using Xunit;

namespace Joura.UiTests;

[Collection(UiTestCollection.Name)]
public sealed class ReportsFlowTests(TestAppFixture fixture)
{
    [Fact]
    public async Task UserCanDownloadWorklogPdfFromReports()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await UiTestHelpers.LoginAsAsync(page, fixture.BaseUrl, "avery@joura.local");

        await page.GotoAsync($"{fixture.BaseUrl}/reports");
        await page.GetByTestId("reports-page").WaitForAsync();

        var firstSelectableEntry = page.Locator("[data-testid^='worklog-select-']").First;
        await firstSelectableEntry.WaitForAsync();
        await firstSelectableEntry.CheckAsync();

        var download = await page.RunAndWaitForDownloadAsync(async () =>
        {
            await page.GetByTestId("reports-download-pdf").ClickAsync();
        });

        Assert.EndsWith(".pdf", download.SuggestedFilename, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UserCanFilterAndPreviewSelectedWorklogReport()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await UiTestHelpers.LoginAsAsync(page, fixture.BaseUrl, "avery@joura.local");

        await UiTestHelpers.CreateIssueAsync(page, fixture.BaseUrl, $"UI rpt {Guid.NewGuid().ToString("N")[..8]}");
        var note = $"UI report note {Guid.NewGuid():N}";
        var start = DateTime.Today.AddHours(10);
        var end = start.AddHours(1);

        await page.GetByTestId("worklog-open-modal").ClickAsync();
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("worklog-start"), ToDateTimeLocal(start));
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("worklog-end"), ToDateTimeLocal(end));
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("worklog-note"), note);
        await page.GetByTestId("worklog-submit").ClickAsync();
        await page.GetByTestId("issue-worklog-item").Filter(new LocatorFilterOptions { HasText = note }).First.WaitForAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/reports");
        await page.GetByTestId("reports-page").WaitForAsync();
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("reports-search"), note);

        await Assertions.Expect(page.GetByTestId("reports-result-count")).ToHaveTextAsync("1 worklog items");
        Assert.Equal("1 worklog items", (await page.GetByTestId("reports-result-count").TextContentAsync())?.Trim());

        await page.Locator("[data-testid^='worklog-select-']").First.CheckAsync();

        await Assertions.Expect(page.GetByTestId("reports-selection-count")).ToHaveTextAsync("1 selected");
        await Assertions.Expect(page.GetByTestId("reports-selected-rows")).ToHaveTextAsync("1 rows");
        Assert.Equal("1 selected", (await page.GetByTestId("reports-selection-count").TextContentAsync())?.Trim());
        Assert.Equal("1 rows", (await page.GetByTestId("reports-selected-rows").TextContentAsync())?.Trim());
        await page.GetByTestId("report-project-group").WaitForAsync();
        await page.GetByTestId("report-issue-group").WaitForAsync();
        Assert.Contains(note, await page.GetByTestId("report-issue-group").TextContentAsync());
    }

    private static string ToDateTimeLocal(DateTime value)
    {
        return value.ToString("yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture);
    }
}
