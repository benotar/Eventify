using Asp.Versioning;
using Carter;
using Eventify.Catalog.Api.Middleware;
using Eventify.Catalog.Application;
using Eventify.Catalog.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// DI
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddCarter();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Middleware + routing
app.UseExceptionHandler();

app.MapOpenApi();

app.MapScalarApiReference();

app.MapCarter();

app.Run();
