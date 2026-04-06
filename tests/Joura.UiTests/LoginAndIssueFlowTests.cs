using Microsoft.Playwright;
using Xunit;

namespace Joura.UiTests;

[Collection(UiTestCollection.Name)]
public sealed class LoginAndIssueFlowTests(TestAppFixture fixture)
{
    [Fact]
    public async Task UserCanLoginCreateIssueAndComment()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await UiTestHelpers.LoginAsAsync(page, fixture.BaseUrl, "avery@joura.local");
        Assert.Equal("Demo Company", await page.GetByTestId("home-tenant-name").TextContentAsync());

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var title = $"UI acceptance issue {uniqueSuffix}";
        var comment = $"UI comment {uniqueSuffix}";

        await page.GotoAsync($"{fixture.BaseUrl}/board?createIssue=true");
        await page.GetByTestId("create-issue-modal").WaitForAsync();
        await page.GetByTestId("create-issue-project").SelectOptionAsync(new SelectOptionValue { Index = 0 });
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("create-issue-title"), title);
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("create-issue-description"), "Created by the Playwright UI suite.");
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("create-issue-labels"), "ui-tests");
        await page.GetByTestId("create-issue-submit").ClickAsync();

        await page.GetByTestId("board-page").WaitForAsync();
        var createdTicket = page.Locator(".board-ticket").Filter(new LocatorFilterOptions { HasText = title }).First;
        await createdTicket.WaitForAsync();
        await createdTicket.Locator("a").ClickAsync();

        await page.GetByTestId("issue-details-page").WaitForAsync();
        Assert.Equal(title, await page.GetByTestId("issue-title").TextContentAsync());

        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("comment-input"), comment);
        await page.GetByTestId("comment-submit").ClickAsync();

        var newestComment = page.GetByTestId("comment-item").Filter(new LocatorFilterOptions { HasText = comment }).First;
        await newestComment.WaitForAsync();
    }
}
