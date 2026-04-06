using Joura.Application.Abstractions;
using Joura.Application.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Joura.Web.Services;

public sealed class CommandPaletteService(
    IWorkTrackingService workTrackingService,
    AuthenticationStateProvider authenticationStateProvider)
{
    private static readonly IReadOnlyList<CommandPaletteItem> AnonymousCommands =
    [
        new("login", "Login", "Choose a tenant and user", "Page", "🔑", CommandPaletteItemKind.Navigate, "/login")
    ];

    private static readonly IReadOnlyList<CommandPaletteItem> AuthenticatedCommands =
    [
        new("home", "Home", "Tenant overview and recent issues", "Page", "🏠", CommandPaletteItemKind.Navigate, "/"),
        new("board", "Board", "Kanban overview", "Page", "🧮", CommandPaletteItemKind.Navigate, "/board"),
        new("issues", "Issues", "Search and bulk update issues", "Page", "🗃", CommandPaletteItemKind.Navigate, "/issues"),
        new("worklogs", "Worklogs", "Weekly time entry", "Page", "🕒", CommandPaletteItemKind.Navigate, "/worklogs"),
        new("reports", "Reports", "Search worklogs and export PDF", "Page", "🔎", CommandPaletteItemKind.Navigate, "/reports"),
        new("due-dates", "Due Dates", "Calendar view of upcoming issues", "Page", "📅", CommandPaletteItemKind.Navigate, "/due-dates"),
        new("me", "My Profile", "Account and tenant details", "Page", "👤", CommandPaletteItemKind.Navigate, "/me"),
        new("create-project", "Create Project", "Open the project creation flow", "Action", "➕", CommandPaletteItemKind.OpenCreateFlow, "/?createProject=true"),
        new("create-issue", "Create Issue", "Open the issue creation flow", "Action", "✳", CommandPaletteItemKind.OpenCreateFlow, "/board?createIssue=true")
    ];

    public async Task<IReadOnlyList<CommandPaletteItem>> SearchAsync(string searchText, CancellationToken cancellationToken = default)
    {
        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var isAuthenticated = authenticationState.User.Identity?.IsAuthenticated == true;
        var query = searchText.Trim();
        var normalizedQuery = query.ToLowerInvariant();

        var commands = (isAuthenticated ? AuthenticatedCommands : AnonymousCommands)
            .Where(command => Matches(command, normalizedQuery))
            .ToList();

        if (isAuthenticated && !string.IsNullOrWhiteSpace(query))
        {
            var issues = await workTrackingService.SearchIssuesAsync(new IssueFilter(query, null, null, null, null), cancellationToken);
            commands.AddRange(
                issues
                    .Take(6)
                    .Select(issue => new CommandPaletteItem(
                        $"issue-{issue.Id}",
                        issue.Key,
                        $"{issue.Title} · {issue.ProjectKey} · {issue.StatusName}",
                        "Issue",
                        "🎫",
                        CommandPaletteItemKind.OpenIssue,
                        $"/issues/{issue.Id}")));
        }

        return commands
            .OrderBy(command => GetGroupRank(command.Group))
            .ThenBy(command => GetMatchRank(command, normalizedQuery))
            .ThenBy(command => command.Title)
            .ToList();
    }

    private static bool Matches(CommandPaletteItem command, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return true;
        }

        return command.Title.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
            || command.Subtitle.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
            || command.Group.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetGroupRank(string group) => group switch
    {
        "Action" => 0,
        "Page" => 1,
        "Issue" => 2,
        _ => 3
    };

    private static int GetMatchRank(CommandPaletteItem command, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return 0;
        }

        if (command.Title.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (command.Subtitle.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 2;
    }
}

public sealed record CommandPaletteItem(
    string Id,
    string Title,
    string Subtitle,
    string Group,
    string Icon,
    CommandPaletteItemKind Kind,
    string Target);

public enum CommandPaletteItemKind
{
    Navigate,
    OpenCreateFlow,
    OpenIssue
}
