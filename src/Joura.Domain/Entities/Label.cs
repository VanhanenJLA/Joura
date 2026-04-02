using Joura.Domain.Common;

namespace Joura.Domain.Entities;

public sealed class Label : Entity
{
    public required string Name { get; set; }
    public string Color { get; set; } = "#345c49";
    public ICollection<IssueLabel> IssueLabels { get; set; } = new List<IssueLabel>();
}
