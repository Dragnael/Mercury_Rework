using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models;
using Nodsoft.Mercury.Data.Models.Notifications;
using Nodsoft.Mercury.Functions.Notification.Services;
using Nodsoft.Mercury.Models;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddCosmosDbContext<MercuryDbContext>("forms-db", "forms-db");
// builder.AddAzureServiceBusClient("submissions-queue");

builder.ConfigureFunctionsWebApplication();

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();

builder.ConfigureOpenTelemetry();

builder.Services
	.AddApplicationInsightsTelemetryWorkerService()
	.ConfigureFunctionsApplicationInsights();

builder.Services.AddScoped<NotificationConfigurationService>();
builder.Services.AddScoped<SubmissionNotificationService>();

builder.Services.AddScoped<INotificationService<FormSubmissionNotificationDto, FormSubmission, SubmissionNotificationOptions>, EmailNotificationService>();

builder.Build().Run();