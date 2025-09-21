using Aspire.Hosting.Azure;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Database: CosmosDB (serverless)
#pragma warning disable ASPIRECOSMOSDB001
IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB("cosmosdb")
	.RunAsPreviewEmulator(emulator =>
	{
		emulator.WithDataVolume();
		emulator.WithDataExplorer();
	});

IResourceBuilder<AzureCosmosDBDatabaseResource> formsDb = cosmos.AddCosmosDatabase("forms-db");
IResourceBuilder<AzureCosmosDBContainerResource> formSources = formsDb.AddContainer("sources", "/id");
IResourceBuilder<AzureCosmosDBContainerResource> templates = formsDb.AddContainer("templates", "/id");
IResourceBuilder<AzureCosmosDBContainerResource> submissions = formsDb.AddContainer("submissions", "/id");
#pragma warning restore ASPIRECOSMOSDB001

IResourceBuilder<AzureFunctionsProjectResource> azFunc = builder.AddAzureFunctionsProject<Nodsoft_Mercury_Functions>("functions")
	.WithReference(formsDb, "formsDb")
	.WaitFor(formsDb);

builder.Build().Run();
