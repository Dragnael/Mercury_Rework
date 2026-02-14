using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Functions.Submission.Services;
using Nodsoft.Mercury.Functions.Submission.Services.Middlewares;

Console.WriteLine("NSYS Mercury - Submission API");

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);

IHostEnvironment environment = builder.Environment;
IConfiguration configuration = builder.Configuration;

builder.ConfigureFunctionsWebApplication(options =>
{
    // HTTPS redirection only in Development
    if (environment.IsDevelopment())
    {
        options.UseHttpsRedirection();
    }
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

if (environment.IsDevelopment())
{
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

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

if (environment.IsDevelopment())
{
    await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
    await using MercuryDbContext context =
        scope.ServiceProvider.GetRequiredService<MercuryDbContext>();

    await context.Database.EnsureCreatedAsync();
}

await host.WaitForShutdownAsync();

