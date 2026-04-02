using Joura.Domain.Common;

namespace Joura.Domain.Entities;

public sealed class Tenant : Entity
{
    public required string Name { get; set; }
    public string Key { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
