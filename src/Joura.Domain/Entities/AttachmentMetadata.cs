using Joura.Domain.Common;

namespace Joura.Domain.Entities;

public sealed class AttachmentMetadata : Entity
{
    public Guid IssueId { get; set; }
    public Issue Issue { get; set; } = null!;
    public required string FileName { get; set; }
    public required string BlobPath { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset UploadedUtc { get; set; } = DateTimeOffset.UtcNow;
}
