using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Functions.Services;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.AddCosmosDbContext<MercuryDbContext>("cosmosDb", "forms-db");

builder.Services
	.AddApplicationInsightsTelemetryWorkerService()
	.ConfigureFunctionsApplicationInsights();

builder.ConfigureOpenTelemetry();

builder.Services.AddScoped<FormSourceService>();
builder.Services.AddScoped<FormTemplateService>();
builder.Services.AddScoped<FormAccessService>();

using IHost host = builder.Build();

await host.StartAsync();

await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
await using MercuryDbContext context = scope.ServiceProvider.GetRequiredService<MercuryDbContext>();
await context.Database.EnsureCreatedAsync();


await host.WaitForShutdownAsync();