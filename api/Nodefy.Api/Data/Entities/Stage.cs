namespace Nodefy.Api.Data.Entities;

public class Stage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PipelineId { get; set; }
    public string Name { get; set; } = "";
    public double Position { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
