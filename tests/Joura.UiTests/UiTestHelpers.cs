using Microsoft.Playwright;

namespace Joura.UiTests;

internal static class UiTestHelpers
{
    public static async Task LoginAsAsync(IPage page, string baseUrl, string email)
    {
        await page.GotoAsync($"{baseUrl}/login");
        await page.GetByTestId(ToLoginButtonTestId(email)).ClickAsync();
        await page.WaitForURLAsync("**/");
        await page.GetByTestId("home-page").WaitForAsync();
    }

    public static string ToLoginButtonTestId(string email)
    {
        return $"login-button-{email.Replace("@", "-at-").Replace(".", "-")}";
    }

    public static async Task FillAndChangeAsync(ILocator locator, string value)
    {
        var inputType = await locator.EvaluateAsync<string>("element => element.type || ''");

        await locator.ClickAsync();
        if (inputType is "date" or "datetime-local")
        {
            await locator.FillAsync(value);
            await locator.EvaluateAsync(
                """
                element => {
                    element.dispatchEvent(new Event('input', { bubbles: true }));
                    element.dispatchEvent(new Event('change', { bubbles: true }));
                    element.blur();
                }
                """);
            return;
        }

        await locator.FillAsync(string.Empty);
        await locator.PressSequentiallyAsync(value);
        await locator.EvaluateAsync("element => element.blur()");
    }

    public static async Task<CreatedIssue> CreateIssueAsync(
        IPage page,
        string baseUrl,
        string title,
        string description = "Created by the Playwright UI suite.",
        string labels = "ui-tests",
        int projectIndex = 0,
        string? dueDate = null,
        string? priority = null,
        string? assignee = null)
    {
        await page.GotoAsync($"{baseUrl}/board?createIssue=true");
        await page.GetByTestId("create-issue-modal").WaitForAsync();
        await page.GetByTestId("create-issue-project").SelectOptionAsync(new SelectOptionValue { Index = projectIndex });
        await FillAndChangeAsync(page.GetByTestId("create-issue-title"), title);
        await FillAndChangeAsync(page.GetByTestId("create-issue-description"), description);
        await FillAndChangeAsync(page.GetByTestId("create-issue-labels"), labels);

        if (!string.IsNullOrWhiteSpace(dueDate))
        {
            await FillAndChangeAsync(page.GetByTestId("create-issue-due-date"), dueDate);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            await page.GetByTestId("create-issue-priority").SelectOptionAsync(new SelectOptionValue { Label = priority });
        }

        if (!string.IsNullOrWhiteSpace(assignee))
        {
            await page.GetByTestId("create-issue-assignee").SelectOptionAsync(new SelectOptionValue { Label = assignee });
        }

        await page.GetByTestId("create-issue-submit").ClickAsync();
        await page.GetByTestId("board-page").WaitForAsync();

        var createdTicket = page.Locator(".board-ticket").Filter(new LocatorFilterOptions { HasText = title }).First;
        await createdTicket.WaitForAsync();
        await createdTicket.Locator("a").ClickAsync();

        await page.GetByTestId("issue-details-page").WaitForAsync();
        var key = (await page.GetByTestId("issue-key").TextContentAsync())?.Trim()
            ?? throw new InvalidOperationException("Created issue key was not rendered.");

        return new CreatedIssue(key, title, page.Url);
    }
}

internal sealed record CreatedIssue(string Key, string Title, string Url);
