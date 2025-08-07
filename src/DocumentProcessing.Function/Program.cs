using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DocumentProcessing.Application.Abstractions;
using DocumentProcessing.Application.Infrastructure;
using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Application.Handlers;
using DocumentProcessing.Infrastructure.Services;
using DocumentProcessing.Infrastructure.ApiClients;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Register dispatchers
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<IQueryDispatcher, QueryDispatcher>();
        services.AddSingleton<IEventDispatcher, EventDispatcher>();

        // Register command handlers
        services.AddScoped<ICommandHandler<ProcessDocumentBatchCommand, ProcessDocumentBatchResult>, ProcessDocumentBatchCommandHandler>();
        services.AddScoped<ICommandHandler<DownloadAndValidateXmlCommand, DownloadAndValidateXmlResult>, DownloadAndValidateXmlCommandHandler>();
        services.AddScoped<ICommandHandler<ProcessDocumentsCommand, ProcessDocumentsResult>, ProcessDocumentsCommandHandler>();
        services.AddScoped<ICommandHandler<SendValidationResultCommand, SendValidationResultResult>, SendValidationResultCommandHandler>();
        services.AddScoped<ICommandHandler<SendToPrintServiceCommand, SendToPrintServiceResult>, SendToPrintServiceCommandHandler>();

        // Register Azure services
        services.AddSingleton(provider =>
        {
            var connectionString = configuration.GetConnectionString("AzureStorage") 
                ?? throw new InvalidOperationException("AzureStorage connection string not found");
            return new BlobServiceClient(connectionString);
        });

        // Register HTTP clients
        services.AddHttpClient<IBlobStorageService, BlobStorageService>();
        services.AddHttpClient<IThirdPartyApiService, ThirdPartyApiService>();
        services.AddHttpClient<IPrintServiceApiClient, PrintServiceApiClient>();

        // Register application services
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IXmlValidationService, XmlValidationService>();
        services.AddScoped<IDocumentValidationService, DocumentValidationService>();
        services.AddScoped<IThirdPartyApiService, ThirdPartyApiService>();
        services.AddScoped<IPrintServiceApiClient, PrintServiceApiClient>();
    })
    .Build();

host.Run();