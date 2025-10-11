using Aspire.Hosting.Azure;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<AzureStorageResource> bulkStore = builder.AddAzureStorage("bulk-storage")
	.RunAsEmulator();

// Database: CosmosDB (serverless)
#pragma warning disable ASPIRECOSMOSDB001
IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB("cosmosdb")
	.RunAsPreviewEmulator(emulator =>
	{
		emulator.WithDataVolume();
		emulator.WithDataExplorer();
	});

IResourceBuilder<AzureCosmosDBDatabaseResource> formsDb = cosmos.AddCosmosDatabase("forms-db");
#pragma warning restore ASPIRECOSMOSDB001

IResourceBuilder<AzureStorageResource> opsStore = builder.AddAzureStorage("ops-storage")
	.RunAsEmulator();

IResourceBuilder<AzureServiceBusResource> notificationsMq = builder.AddAzureServiceBus("notifications-mq")
	.RunAsEmulator(e =>
	{
		e.WithLifetime(ContainerLifetime.Persistent);
	});

IResourceBuilder<AzureServiceBusQueueResource> submissionsQueue = notificationsMq.AddServiceBusQueue("submissions-queue", "submissions");

IResourceBuilder<AzureFunctionsProjectResource> submissionsFunc = builder.AddAzureFunctionsProject<Nodsoft_Mercury_Functions_Submission>("submissions-func")
	.WithReference(formsDb).WaitFor(formsDb)
	.WithReference(submissionsQueue).WaitFor(submissionsQueue)
	.WithExternalHttpEndpoints()
	.WithHostStorage(opsStore);

IResourceBuilder<AzureFunctionsProjectResource> notificationsFunc = builder.AddAzureFunctionsProject<Nodsoft_Mercury_Functions_Notification>("notifications-func")
	.WithReference(formsDb).WaitFor(formsDb)
	.WithReference(notificationsMq).WaitFor(notificationsMq)
	.WithHostStorage(opsStore);

builder.Build().Run();
