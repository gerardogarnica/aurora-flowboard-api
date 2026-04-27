using Aurora.Flowboard.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

RouteGroupBuilder routeGroup = app.MapGroup("/api/v1/flowboard").WithTags("Flowboard API");
app.MapEndpoints(routeGroup);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseStatusCodePages();

await app.RunAsync();

// REMARK: Required for functional and integration tests to work.
namespace Aurora.Flowboard.Api
{
    public partial class Program;
}