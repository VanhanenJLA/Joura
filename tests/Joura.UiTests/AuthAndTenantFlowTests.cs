using Microsoft.Playwright;
using Xunit;

namespace Joura.UiTests;

[Collection(UiTestCollection.Name)]
public sealed class AuthAndTenantFlowTests(TestAppFixture fixture)
{
    [Fact]
    public async Task LoginHonorsReturnUrlAndLogoutReturnsToLogin()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/login?returnUrl=/issues");
        await page.GetByTestId(UiTestHelpers.ToLoginButtonTestId("avery@joura.local")).ClickAsync();

        await page.WaitForURLAsync("**/issues");
        await page.GetByTestId("issues-page").WaitForAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/me");
        await page.GetByTestId("logout-submit").ClickAsync();

        await page.WaitForURLAsync("**/login");
        await page.GetByTestId("login-page").WaitForAsync();
    }

    [Fact]
    public async Task UserOnlySeesCurrentTenantIssues()
    {
        await using var context = await fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await UiTestHelpers.LoginAsAsync(page, fixture.BaseUrl, "nina@joura.local");
        Assert.Equal("Itsu Company", (await page.GetByTestId("home-tenant-name").TextContentAsync())?.Trim());

        await page.GotoAsync($"{fixture.BaseUrl}/issues");
        await page.GetByTestId("issues-page").WaitForAsync();
        await UiTestHelpers.FillAndChangeAsync(page.GetByTestId("issues-search"), "JOU-1");

        await Assertions.Expect(page.GetByTestId("issues-result-count")).ToHaveTextAsync("0 issues");
        Assert.Equal("0 issues", (await page.GetByTestId("issues-result-count").TextContentAsync())?.Trim());
    }
}
