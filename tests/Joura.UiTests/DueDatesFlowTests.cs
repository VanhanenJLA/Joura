using System.Globalization;
using Microsoft.Playwright;
using Xunit;

namespace Joura.UiTests;

[Collection(UiTestCollection.Name)]
public sealed class DueDatesFlowTests(TestAppFixture fixture)
{
    [Fact]
    public async Task UserCanNavigateAndFilterDueDatesCalendar()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await UiTestHelpers.LoginAsAsync(page, fixture.BaseUrl, "avery@joura.local");

        var dueDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var issue = await UiTestHelpers.CreateIssueAsync(
            page,
            fixture.BaseUrl,
            $"UI due {Guid.NewGuid().ToString("N")[..8]}",
            dueDate: dueDate);

        await page.GotoAsync($"{fixture.BaseUrl}/due-dates");
        await page.GetByTestId("due-dates-page").WaitForAsync();

        var issueLink = page.GetByTestId($"due-date-issue-{issue.Key.ToLowerInvariant()}");
        await issueLink.WaitForAsync();

        var currentMonth = (await page.GetByTestId("due-dates-month-title").TextContentAsync())?.Trim()
            ?? throw new InvalidOperationException("Due dates month title was not rendered.");
        await page.GetByTestId("due-dates-next-month").ClickAsync();
        await Assertions.Expect(page.GetByTestId("due-dates-month-title")).Not.ToHaveTextAsync(currentMonth);
        Assert.NotEqual(currentMonth, (await page.GetByTestId("due-dates-month-title").TextContentAsync())?.Trim());

        await page.GetByTestId("due-dates-previous-month").ClickAsync();
        await Assertions.Expect(page.GetByTestId("due-dates-month-title")).ToHaveTextAsync(currentMonth);
        await issueLink.WaitForAsync();

        await page.GetByTestId("due-dates-status-filter").SelectOptionAsync("Done");
        await issueLink.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Detached });

        await page.GetByTestId("due-dates-status-filter").SelectOptionAsync("ToDo");
        await issueLink.WaitForAsync();
    }
}
