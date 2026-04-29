using Aurora.Flowboard.Api;
using Aurora.Flowboard.Api.Endpoints;
using Aurora.Flowboard.Application;
using Aurora.Flowboard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddApiServices()
    .AddErrorHandling();

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddEndpoints();

var app = builder.Build();

RouteGroupBuilder routeGroup = app.MapGroup("/api/v1/flowboard");
app.MapEndpoints(routeGroup);

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "Coinly API";
});

if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
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