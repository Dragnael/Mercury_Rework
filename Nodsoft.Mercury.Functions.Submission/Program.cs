using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Functions.Submission.Services;
using Nodsoft.Mercury.Functions.Submission.Services.Middlewares;

Console.WriteLine("NSYS Mercury - Submission API");

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication(options =>
{
    // Enforce HTTPS redirection when possible (local/dev scenarios)
    options.UseHttpsRedirection();
});

builder.AddCosmosDbContext<MercuryDbContext>("formsdb", "forms-db");
// builder.AddAzureServiceBusClient("submissions-queue");

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("Nodsoft.Mercury");
        tracing.AddHttpClientInstrumentation();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddRuntimeInstrumentation();
        metrics.AddHttpClientInstrumentation();
    });

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Middleware pipeline
builder.UseMiddleware<AccessTokenMiddleware>();

// Business services
builder.Services.AddScoped<FormSourceService>();
builder.Services.AddScoped<FormTemplateService>();
builder.Services.AddScoped<FormAccessService>();
builder.Services.AddScoped<FormSubmissionService>();

using IHost host = builder.Build();

// Warmup start
await host.StartAsync();

// Ensure Cosmos DB is ready (safe for dev / small scale)
// ⚠️ En production à forte charge, préférer migration contrôlée
await using (AsyncServiceScope scope = host.Services.CreateAsyncScope())
{
    await using MercuryDbContext context =
        scope.ServiceProvider.GetRequiredService<MercuryDbContext>();

    await context.Database.EnsureCreatedAsync();
}

await host.WaitForShutdownAsync();
