using Aspire.Hosting.Azure;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Database: CosmosDB (serverless)
#pragma warning disable ASPIRECOSMOSDB001
IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB("cosmosdb")
	.RunAsPreviewEmulator(emulator =>
	{
		emulator.WithLifetime(ContainerLifetime.Persistent);
		emulator.WithDataExplorer();
	});

IResourceBuilder<AzureCosmosDBDatabaseResource> formsDb = cosmos.AddCosmosDatabase("forms-db");
IResourceBuilder<AzureCosmosDBContainerResource> formSources = formsDb.AddContainer("sources", "/id");
IResourceBuilder<AzureCosmosDBContainerResource> forms = formsDb.AddContainer("forms", "/id");

builder.Build().Run();
