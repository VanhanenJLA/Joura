using System.Globalization;
using Joura.Application.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Joura.Web.Services;

public sealed class WorklogReportPdfService : IWorklogReportPdfService
{
    static WorklogReportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(WorklogReportDto report)
    {
        return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Calibri));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(content => ComposeContent(content, report));
                    page.Footer()
                        .AlignRight()
                        .DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken2))
                        .Text(text =>
                        {
                            text.Span("Generated ");
                            text.Span(DateTimeOffset.Now.ToString("d MMM yyyy HH:mm", CultureInfo.InvariantCulture));
                        });
                });
            })
            .GeneratePdf();
    }

    private static void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text("Worklog Report").FontSize(20).SemiBold().FontColor(Colors.BlueGrey.Darken4);
            column.Item().Text("Invoice-style hierarchical summary of selected worklogs.")
                .FontSize(10)
                .FontColor(Colors.Grey.Darken2);
        });
    }

    private static void ComposeContent(IContainer container, WorklogReportDto report)
    {
        container.Column(column =>
        {
            column.Spacing(16);

            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"{report.TotalEntryCount} worklog rows").FontColor(Colors.Grey.Darken2);
                row.ConstantItem(140).AlignRight().Text($"Total: {FormatDuration(report.TotalMinutes)}").SemiBold();
            });

            column.Item().Element(x => ComposeTable(x, report));
        });
    }

    private static void ComposeTable(IContainer container, WorklogReportDto report)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(5);
                columns.RelativeColumn(1.2f);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Description");
                header.Cell().Element(HeaderCell).AlignRight().Text("Hours");
            });

            foreach (var project in report.Projects)
            {
                table.Cell().Element(ProjectCell).Text($"{project.ProjectName} ({project.ProjectKey})");
                table.Cell().Element(ProjectHoursCell).AlignRight().Text(FormatDuration(project.TotalMinutes)).SemiBold();

                foreach (var issue in project.Issues)
                {
                    table.Cell().Element(IssueCell).Text(text =>
                    {
                        text.Span($"{issue.IssueKey}: ").FontColor(Colors.Grey.Darken1);
                        text.Span(issue.IssueTitle);
                    });
                    table.Cell().Element(IssueHoursCell).AlignRight().Text(FormatDuration(issue.TotalMinutes));

                    foreach (var row in issue.Rows)
                    {
                        table.Cell().Element(RowCell).Text(text =>
                        {
                            text.Span(row.LoggerName).FontColor(Colors.Grey.Darken2);

                            if (!string.IsNullOrWhiteSpace(row.Note))
                            {
                                text.Span("  ");
                                text.Span(row.Note);
                            }
                        });
                        table.Cell().Element(RowHoursCell).AlignRight().Text(FormatDuration(row.Minutes));
                    }
                }
            }

            table.Cell().ColumnSpan(2).PaddingTop(8).BorderTop(1).BorderColor(Colors.Grey.Lighten1).Row(row =>
            {
                row.RelativeItem().Text("Total").SemiBold();
                row.ConstantItem(110).AlignRight().Text(FormatDuration(report.TotalMinutes)).SemiBold();
            });
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.PaddingVertical(8)
            .PaddingHorizontal(10)
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1);

    private static IContainer ProjectCell(IContainer container) =>
        container.PaddingTop(10)
            .PaddingBottom(4)
            .PaddingHorizontal(10)
            .Background(Colors.BlueGrey.Lighten5)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2);

    private static IContainer ProjectHoursCell(IContainer container) =>
        ProjectCell(container);

    private static IContainer IssueCell(IContainer container) =>
        container.PaddingVertical(6)
            .PaddingLeft(24)
            .PaddingRight(10)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3);

    private static IContainer IssueHoursCell(IContainer container) =>
        container.PaddingVertical(6)
            .PaddingHorizontal(10)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3);

    private static IContainer RowCell(IContainer container) =>
        container.PaddingVertical(5)
            .PaddingLeft(40)
            .PaddingRight(10)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten4);

    private static IContainer RowHoursCell(IContainer container) =>
        container.PaddingVertical(5)
            .PaddingHorizontal(10)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten4);

    private static string FormatDuration(int minutes)
    {
        var hours = minutes / 60;
        var remainder = minutes % 60;
        return remainder == 0 ? $"{hours}h" : $"{hours}h {remainder}m";
    }
}
