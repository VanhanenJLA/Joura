using Xunit;

namespace Joura.UiTests;

[Collection(UiTestCollection.Name)]
public sealed class HomeFlowTests(TestAppFixture fixture)
{
    [Fact]
    public async Task UserCanCreateProjectAndOpenBoard()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await UiTestHelpers.LoginAsAsync(page, fixture.BaseUrl, "avery@joura.local");

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var key = $"UI{suffix}";

        await page.GetByTestId("home-create-project").ClickAsync();
        await page.GetByTestId("create-project-modal").WaitForAsync();
        await page.GetByTestId("create-project-name").FillAsync($"UI Project {suffix}");
        await page.GetByTestId("create-project-key").FillAsync(key);
        await page.GetByTestId("create-project-description").FillAsync("Created by the Playwright project flow.");
        await page.GetByTestId("create-project-submit").ClickAsync();

        var projectCard = page.GetByTestId($"project-card-{key.ToLowerInvariant()}");
        await projectCard.WaitForAsync();
        await projectCard.ClickAsync();

        await page.GetByTestId("board-page").WaitForAsync();
        await page.GetByTestId("board-column-to-do").WaitForAsync();
        await page.GetByTestId("board-column-in-progress").WaitForAsync();
        await page.GetByTestId("board-column-done").WaitForAsync();
    }
}
