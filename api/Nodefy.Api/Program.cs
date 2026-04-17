var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/health", () => Results.Ok("OK"));
app.Run();
public partial class Program { }   // exposes Program for WebApplicationFactory
