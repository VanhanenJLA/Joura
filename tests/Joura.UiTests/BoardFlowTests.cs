using Microsoft.Playwright;
using Xunit;

namespace Joura.UiTests;

[Collection(UiTestCollection.Name)]
public sealed class BoardFlowTests(TestAppFixture fixture)
{
    [Fact]
    public async Task UserCanMoveIssueBetweenBoardColumns()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await UiTestHelpers.LoginAsAsync(page, fixture.BaseUrl, "avery@joura.local");

        var title = $"UI board move {Guid.NewGuid():N}";
        var issue = await UiTestHelpers.CreateIssueAsync(page, fixture.BaseUrl, title);

        await page.GotoAsync($"{fixture.BaseUrl}/board");
        await page.GetByTestId("board-page").WaitForAsync();

        var ticketTestId = $"board-ticket-{issue.Key.ToLowerInvariant()}";
        var ticket = page.GetByTestId(ticketTestId);
        var targetColumn = page.GetByTestId("board-column-in-progress");

        await ticket.WaitForAsync();
        await ticket.DragToAsync(targetColumn);
        await targetColumn.Locator($"[data-testid='{ticketTestId}']").WaitForAsync();

        await targetColumn.Locator($"[data-testid='{ticketTestId}'] a").ClickAsync();
        await page.GetByTestId("issue-details-page").WaitForAsync();

        Assert.Equal("In Progress", (await page.GetByTestId("issue-status").TextContentAsync())?.Trim());
    }
}
