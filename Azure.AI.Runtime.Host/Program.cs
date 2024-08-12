using Azure.OpenAI.Agents.Extensions;
using Azure.OpenAI.Agents.Functions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication().
    ConfigureAgents()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<LoggerFactory>();
        services.AddSingleton<FunctionInvoker>();
    })    
    .Build();

host.Run();
