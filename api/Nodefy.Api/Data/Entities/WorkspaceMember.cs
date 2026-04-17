namespace Nodefy.Api.Data.Entities;

public class WorkspaceMember
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }                      // == workspaces.id
    public Guid UserId { get; set; }
    public string Role { get; set; } = "member";           // 'admin' | 'member'
    public DateTimeOffset JoinedAt { get; set; }
}
