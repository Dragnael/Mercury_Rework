using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Functions.Submission.Services;
using Nodsoft.Mercury.Functions.Submission.Services.Middlewares;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.AddCosmosDbContext<MercuryDbContext>("formsdb", "forms-db");
// builder.AddAzureServiceBusClient("submissions-queue");

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();

builder.ConfigureOpenTelemetry();
builder.UseMiddleware<AccessTokenMiddleware>();

builder.Services.AddScoped<FormSourceService>();
builder.Services.AddScoped<FormTemplateService>();
builder.Services.AddScoped<FormAccessService>();
builder.Services.AddScoped<FormSubmissionService>();


using IHost host = builder.Build();

await host.StartAsync();

await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
await using MercuryDbContext context = scope.ServiceProvider.GetRequiredService<MercuryDbContext>();
await context.Database.EnsureCreatedAsync();


await host.WaitForShutdownAsync();