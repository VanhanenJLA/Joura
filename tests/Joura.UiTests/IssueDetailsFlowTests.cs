using System.Globalization;
using Microsoft.Playwright;
using Xunit;

namespace Joura.UiTests;

[Collection(UiTestCollection.Name)]
public sealed class IssueDetailsFlowTests(TestAppFixture fixture)
{
    [Fact]
    public async Task UserCanEditIssueFields()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await UiTestHelpers.LoginAsAsync(page, fixture.BaseUrl, "avery@joura.local");

        var issue = await UiTestHelpers.CreateIssueAsync(page, fixture.BaseUrl, $"UI edit {Guid.NewGuid().ToString("N")[..8]}");
        var updatedTitle = $"{issue.Title} updated";
        var dueDate = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        await page.GetByTestId("issue-edit-button").ClickAsync();
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("issue-edit-title"), updatedTitle);
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("issue-edit-description"), "Updated through the Playwright issue edit flow.");
        await page.GetByTestId("issue-edit-priority").SelectOptionAsync(new SelectOptionValue { Label = "High" });
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("issue-edit-due-date"), dueDate);
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("issue-edit-labels"), "ui-tests, edited");
        await page.GetByTestId("issue-edit-save").ClickAsync();

        await Assertions.Expect(page.GetByTestId("issue-title")).ToHaveTextAsync(updatedTitle);

        Assert.Equal(updatedTitle, (await page.GetByTestId("issue-title").TextContentAsync())?.Trim());
        Assert.Equal("High", (await page.GetByTestId("issue-priority").TextContentAsync())?.Trim());
        Assert.Contains("edited", await page.GetByTestId("issue-labels").TextContentAsync());
    }

    [Fact]
    public async Task UserCanAddEditAndDeleteIssueWorklog()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await UiTestHelpers.LoginAsAsync(page, fixture.BaseUrl, "avery@joura.local");

        var issue = await UiTestHelpers.CreateIssueAsync(page, fixture.BaseUrl, $"UI log {Guid.NewGuid().ToString("N")[..8]}");
        var note = $"UI worklog note {Guid.NewGuid():N}";
        var editedNote = $"{note} edited";
        var start = DateTime.Today.AddHours(9);
        var end = start.AddHours(1.5);

        await page.GetByTestId("worklog-open-modal").ClickAsync();
        await page.GetByTestId("worklog-modal").WaitForAsync();
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("worklog-start"), ToDateTimeLocal(start));
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("worklog-end"), ToDateTimeLocal(end));
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("worklog-note"), note);
        await page.GetByTestId("worklog-submit").ClickAsync();

        var createdWorklog = page.GetByTestId("issue-worklog-item").Filter(new LocatorFilterOptions { HasText = note }).First;
        await createdWorklog.WaitForAsync();

        await createdWorklog.GetByTestId("issue-worklog-edit").ClickAsync();
        await page.GetByTestId("worklog-modal").WaitForAsync();
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("worklog-note"), editedNote);
        await page.GetByTestId("worklog-submit").ClickAsync();

        var editedWorklog = page.GetByTestId("issue-worklog-item").Filter(new LocatorFilterOptions { HasText = editedNote }).First;
        await editedWorklog.WaitForAsync();

        await editedWorklog.GetByTestId("issue-worklog-edit").ClickAsync();
        await page.GetByTestId("worklog-modal").WaitForAsync();
        await page.GetByTestId("worklog-delete").ClickAsync();

        await editedWorklog.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        Assert.Equal(0, await page.GetByTestId("issue-worklog-item").Filter(new LocatorFilterOptions { HasText = editedNote }).CountAsync());
        Assert.Equal(issue.Key, (await page.GetByTestId("issue-key").TextContentAsync())?.Trim());
    }

    private static string ToDateTimeLocal(DateTime value)
    {
        return value.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
    }
}
