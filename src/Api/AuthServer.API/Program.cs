using AuthServer.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApi(builder.Configuration);

var app = builder.Build();

app.UseApi();

// Apply migrations and seed database only in Development
if (app.Environment.IsDevelopment())
{
    await AuthServer.Infrastructure.Seeding.SeedData.MigrateAndSeedAsync(app);
}

app.Run();
