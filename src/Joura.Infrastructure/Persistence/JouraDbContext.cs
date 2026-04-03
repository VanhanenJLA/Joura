using Joura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Joura.Infrastructure.Persistence;

public sealed class JouraDbContext(DbContextOptions<JouraDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<IssueStatus> IssueStatuses => Set<IssueStatus>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<IssueLabel> IssueLabels => Set<IssueLabel>();
    public DbSet<WorklogEntry> WorklogEntries => Set<WorklogEntry>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AttachmentMetadata> Attachments => Set<AttachmentMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.DisplayName).HasMaxLength(128);
            entity.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Key).HasMaxLength(20);
            entity.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Key).HasMaxLength(12);
            entity.HasIndex(x => new { x.TenantId, x.Key }).IsUnique();
        });

        modelBuilder.Entity<IssueStatus>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(64);
            entity.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<Label>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(64);
            entity.Property(x => x.Color).HasMaxLength(16);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<IssueLabel>(entity =>
        {
            entity.HasKey(x => new { x.IssueId, x.LabelId });
        });

        modelBuilder.Entity<Issue>(entity =>
        {
            entity.Property(x => x.Key).HasMaxLength(32);
            entity.Property(x => x.Title).HasMaxLength(240);
            entity.Property(x => x.Description).HasColumnType("text");
            entity.Property(x => x.DueDate).HasColumnType("date");
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.ProjectId, x.Key }).IsUnique();
            entity.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Reporter).WithMany().HasForeignKey(x => x.ReporterId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.Property(x => x.Body).HasColumnType("text");
            entity.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.Property(x => x.EventType).HasMaxLength(64);
            entity.Property(x => x.Description).HasColumnType("text");
            entity.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorklogEntry>(entity =>
        {
            entity.Property(x => x.StartAt).HasColumnType("timestamp without time zone");
            entity.Property(x => x.EndAt).HasColumnType("timestamp without time zone");
            entity.Property(x => x.Note).HasColumnType("text");
            entity.ToTable(table => table.HasCheckConstraint("CK_WorklogEntries_Duration_Positive", "\"EndAt\" > \"StartAt\""));
            entity.HasIndex(x => new { x.IssueId, x.StartAt });
            entity.HasIndex(x => new { x.UserId, x.StartAt });
            entity.HasOne(x => x.Issue).WithMany(x => x.WorklogEntries).HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.Message).HasMaxLength(240);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AttachmentMetadata>(entity =>
        {
            entity.Property(x => x.FileName).HasMaxLength(255);
            entity.Property(x => x.BlobPath).HasMaxLength(512);
        });
    }
}
