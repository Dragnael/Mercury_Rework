using Aspire.Hosting.Azure;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Enable Azure provisioning for deployment
var azure = builder.AddAzureProvisioning();

IResourceBuilder<AzureStorageResource> bulkStore = builder.AddAzureStorage("bulk-storage")
	.RunAsEmulator();

// Database: CosmosDB (serverless)
#pragma warning disable ASPIRECOSMOSDB001
IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB("cosmosdb")
	.RunAsPreviewEmulator(emulator =>
	{
		emulator.WithDataVolume();
		emulator.WithDataExplorer();
		emulator.WithLifetime(ContainerLifetime.Persistent);
	});

IResourceBuilder<AzureCosmosDBDatabaseResource> formsDb = cosmos.AddCosmosDatabase("formsdb");
#pragma warning restore ASPIRECOSMOSDB001

IResourceBuilder<AzureServiceBusResource> notificationsMq = builder.AddAzureServiceBus("notificationsmq")
	.RunAsEmulator(e =>
	{
		e.WithLifetime(ContainerLifetime.Persistent);
	});

IResourceBuilder<AzureServiceBusQueueResource> submissionsQueue = notificationsMq.AddServiceBusQueue("submissionsqueue", "submissions");

// Azure Functions configuration
IResourceBuilder<AzureFunctionsProjectResource> submissionsFunc = builder.AddAzureFunctionsProject<Nodsoft_Mercury_Functions_Submission>("submissionsfunc")
	.WithReference(formsDb).WaitFor(formsDb)
	.WithReference(submissionsQueue).WaitFor(submissionsQueue)
	.WithExternalHttpEndpoints();

IResourceBuilder<AzureFunctionsProjectResource> notificationsFunc = builder.AddAzureFunctionsProject<Nodsoft_Mercury_Functions_Notification>("notificationsfunc")
	.WithReference(formsDb).WaitFor(formsDb)
	.WithReference(notificationsMq).WaitFor(notificationsMq);

builder.Build().Run();
