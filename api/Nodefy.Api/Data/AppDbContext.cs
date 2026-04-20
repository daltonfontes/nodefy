using Microsoft.EntityFrameworkCore;
using Nodefy.Api.Data.Entities;
using Nodefy.Api.Tenancy;

namespace Nodefy.Api.Data;

public class AppDbContext : DbContext
{
    private readonly Guid _tenantId;

    // ITenantService is Scoped (per-request) — Pitfall 3 in RESEARCH.md
    public AppDbContext(DbContextOptions<AppDbContext> opts, ITenantService tenantService) : base(opts)
    {
        _tenantId = tenantService.TenantId;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Pipeline> Pipelines => Set<Pipeline>();
    public DbSet<Stage> Stages => Set<Stage>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Map snake_case database columns to PascalCase entity properties
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.Email).HasColumnName("email");
            b.Property(e => e.Name).HasColumnName("name");
            b.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            b.Property(e => e.Provider).HasColumnName("provider");
            b.Property(e => e.ProviderAccountId).HasColumnName("provider_account_id");
            b.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Workspace>(b =>
        {
            b.ToTable("workspaces");
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.Name).HasColumnName("name");
            b.Property(e => e.Slug).HasColumnName("slug");
            b.Property(e => e.Currency).HasColumnName("currency");
            b.Property(e => e.CurrencyLocked).HasColumnName("currency_locked");
            b.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<WorkspaceMember>(b =>
        {
            b.ToTable("workspace_members");
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.TenantId).HasColumnName("tenant_id");
            b.Property(e => e.UserId).HasColumnName("user_id");
            b.Property(e => e.Role).HasColumnName("role");
            b.Property(e => e.JoinedAt).HasColumnName("joined_at");
            // Global query filter — applied automatically to every query.
            // The ONLY allowed bypass is .IgnoreQueryFilters() inside InviteEndpoints.AcceptInvite.
            b.HasQueryFilter(m => m.TenantId == _tenantId);
        });

        modelBuilder.Entity<Invitation>(b =>
        {
            b.ToTable("invitations");
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.TenantId).HasColumnName("tenant_id");
            b.Property(e => e.Email).HasColumnName("email");
            b.Property(e => e.Role).HasColumnName("role");
            b.Property(e => e.Token).HasColumnName("token");
            b.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            b.Property(e => e.AcceptedAt).HasColumnName("accepted_at");
            b.Property(e => e.CreatedAt).HasColumnName("created_at");
            b.HasQueryFilter(i => i.TenantId == _tenantId);
        });

        modelBuilder.Entity<Card>(b =>
        {
            b.ToTable("cards");
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.TenantId).HasColumnName("tenant_id");
            b.Property(e => e.Position).HasColumnName("position");
            b.Property(e => e.StageEnteredAt).HasColumnName("stage_entered_at");
            b.Property(e => e.CreatedAt).HasColumnName("created_at");
            b.Property(e => e.Title).HasColumnName("title");
            b.Property(e => e.Description).HasColumnName("description");
            b.Property(e => e.MonetaryValue).HasColumnName("monetary_value").HasColumnType("numeric(15,2)");
            b.Property(e => e.PipelineId).HasColumnName("pipeline_id");
            b.Property(e => e.StageId).HasColumnName("stage_id");
            b.Property(e => e.AssigneeId).HasColumnName("assignee_id");
            b.Property(e => e.CloseDate).HasColumnName("close_date");
            b.Property(e => e.ArchivedAt).HasColumnName("archived_at");
            b.HasQueryFilter(c => c.TenantId == _tenantId && c.ArchivedAt == null);
            b.HasOne<Stage>().WithMany().HasForeignKey(c => c.StageId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Pipeline>().WithMany().HasForeignKey(c => c.PipelineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pipeline>(b =>
        {
            b.ToTable("pipelines");
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.TenantId).HasColumnName("tenant_id");
            b.Property(e => e.Name).HasColumnName("name");
            b.Property(e => e.Position).HasColumnName("position");
            b.Property(e => e.CreatedAt).HasColumnName("created_at");
            b.HasQueryFilter(e => e.TenantId == _tenantId);
        });

        modelBuilder.Entity<Stage>(b =>
        {
            b.ToTable("stages");
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.TenantId).HasColumnName("tenant_id");
            b.Property(e => e.PipelineId).HasColumnName("pipeline_id");
            b.Property(e => e.Name).HasColumnName("name");
            b.Property(e => e.Position).HasColumnName("position");
            b.Property(e => e.CreatedAt).HasColumnName("created_at");
            b.HasQueryFilter(e => e.TenantId == _tenantId);
            b.HasOne<Pipeline>().WithMany().HasForeignKey(s => s.PipelineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActivityLog>(b =>
        {
            b.ToTable("activity_logs");
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.TenantId).HasColumnName("tenant_id");
            b.Property(e => e.CardId).HasColumnName("card_id");
            b.Property(e => e.ActorId).HasColumnName("actor_id");
            b.Property(e => e.Action).HasColumnName("action");
            b.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb");
            b.Property(e => e.CreatedAt).HasColumnName("created_at");
            b.HasQueryFilter(e => e.TenantId == _tenantId);
            b.HasOne<Card>().WithMany().HasForeignKey(a => a.CardId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
