namespace Nodefy.Api.Data.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public string Provider { get; set; } = "";              // 'github' | 'google' | 'microsoft'
    public string ProviderAccountId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
