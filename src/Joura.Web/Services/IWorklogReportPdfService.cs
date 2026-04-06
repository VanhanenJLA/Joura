using Joura.Application.Models;

namespace Joura.Web.Services;

public interface IWorklogReportPdfService
{
    byte[] Generate(WorklogReportDto report);
}
