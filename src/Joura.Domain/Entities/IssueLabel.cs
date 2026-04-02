namespace Joura.Domain.Entities;

public sealed class IssueLabel
{
    public Guid IssueId { get; set; }
    public Issue Issue { get; set; } = null!;
    public Guid LabelId { get; set; }
    public Label Label { get; set; } = null!;
}
