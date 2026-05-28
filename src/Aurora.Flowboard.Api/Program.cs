using Aurora.Flowboard.Api;
using Aurora.Flowboard.Api.Endpoints;
using Aurora.Flowboard.Api.Extensions;
using Aurora.Flowboard.Application;
using Aurora.Flowboard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder
    .AddApiServices()
    .AddErrorHandling();

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddEndpoints();

var app = builder.Build();

app.MapDefaultEndpoints();

RouteGroupBuilder routeGroup = app.MapGroup("/api/v1/flowboard");
app.MapEndpoints(routeGroup);

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "Flowboard API";
});

if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

await app.RunAsync();

// REMARK: Required for functional and integration tests to work.
namespace Aurora.Flowboard.Api
{
    public partial class Program;
}