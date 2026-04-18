namespace Nodefy.Api.Data.Entities;

public class ActivityLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CardId { get; set; }
    public Guid ActorId { get; set; }
    public string Action { get; set; } = "";
    public string Payload { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}
