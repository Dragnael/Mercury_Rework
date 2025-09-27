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

IResourceBuilder<AzureFunctionsProjectResource> azFunc = builder.AddAzureFunctionsProject<Nodsoft_Mercury_Functions>("functions")
	.WithReference(cosmos)
	.WaitFor(cosmos)
	.WithExternalHttpEndpoints()
	.WithHostStorage(opsStore);

builder.Build().Run();
