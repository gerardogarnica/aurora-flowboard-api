using Aurora.Flowboard.Api;
using Aurora.Flowboard.Api.Endpoints;
using Aurora.Flowboard.Api.Extensions;
using Aurora.Flowboard.Application;
using Aurora.Flowboard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder
    .AddApiServices()
    .AddErrorHandling()
    .AddObservability()
    .AddHealthCheckServices()
    .AddCorsServices();

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddEndpoints();

var app = builder.Build();

app.MapDefaultEndpoints();

RouteGroupBuilder routeGroup = app.MapGroup("/api/v1/flowboard");
app.MapEndpoints(routeGroup);

app.MapHealthCheckEndpoints();

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "Flowboard API";
    });

    await app.ApplyMigrationsAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsExtensions.FrontendPolicy);

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

await app.RunAsync();

// REMARK: Required for functional and integration tests to work.
namespace Aurora.Flowboard.Api
{
    public partial class Program;
}