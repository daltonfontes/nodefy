namespace Nodefy.Api.Data.Entities;

public class Pipeline
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = "";
    public double Position { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
