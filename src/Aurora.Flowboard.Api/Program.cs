using Aurora.Flowboard.Api;
using Aurora.Flowboard.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddApiServices()
    .AddErrorHandling();

var app = builder.Build();

RouteGroupBuilder routeGroup = app.MapGroup("/api/v1/flowboard").WithTags("Flowboard API");
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