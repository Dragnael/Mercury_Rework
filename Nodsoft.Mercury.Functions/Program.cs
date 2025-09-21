using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nodsoft.Mercury.Data;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.AddCosmosDbContext<MercuryDbContext>("forms-db");

builder.Services
	.AddApplicationInsightsTelemetryWorkerService()
	.ConfigureFunctionsApplicationInsights();

await builder.Build().RunAsync();