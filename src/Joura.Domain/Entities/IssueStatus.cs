using Joura.Domain.Common;
using Joura.Domain.Enums;

namespace Joura.Domain.Entities;

public sealed class IssueStatus : Entity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public required string Name { get; set; }
    public IssueStatusCategory Category { get; set; }
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
}
