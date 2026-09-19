using Eventify.Catalog.Application;
using Eventify.Catalog.Infrastructure;
using Eventify.Catalog.Infrastructure.Persistence;
using Eventify.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSomethingINotDecidedShouldDo();

builder.AddServiceDefaults();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();

app.Run();
