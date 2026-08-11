var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "GeneratedBaseline",
    project = "DebugForge Studio"
}));

app.Run();

public partial class Program { }
