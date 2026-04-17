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
            b.HasQueryFilter(c => c.TenantId == _tenantId);
        });
    }
}
