using Azure.AI.OpenAI.Assistants;
using Azure.Identity;
using Azure.AI.Runtime.Functions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Azure.AI.Runtime.Extensions
{
    internal static class AgentsWebJobBuilderExtension
    {
        public static IHostBuilder ConfigureAgents(this IHostBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Register the WebJobs extension, which enables the bindings.
            builder.ConfigureWebJobs(delegate(IWebJobsBuilder wjBuilder)
            {
                wjBuilder.AddAgentsBindings();
            });            

            return builder;
        }

        /// <summary>
        /// Registers AI bindings with the WebJobs host.
        /// </summary>
        /// <param name="builder">The WebJobs builder.</param>
        /// <returns>Returns the <paramref name="builder"/> reference to support fluent-style configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is <c>null</c>.</exception>
        public static IWebJobsBuilder AddAgentsBindings(this IWebJobsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<LoggerFactory>();

            // Register the client for Azure Open AI
            Uri azureOpenAIEndpoint = GetAzureOpenAIEndpoint();
            string openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            string azureOpenAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");

            if (azureOpenAIEndpoint != null && !string.IsNullOrEmpty(azureOpenAIKey))
            {
                RegisterAssistantClient(builder.Services, azureOpenAIEndpoint, azureOpenAIKey);
            }
            else if (azureOpenAIEndpoint != null)
            {
                RegisterAzureOpenAIADAuthClient(builder.Services, azureOpenAIEndpoint);
            }
            else if (!string.IsNullOrEmpty(openAIKey))
            {
                RegisterOpenAIClient(builder.Services, openAIKey);
            }
            else
            {
                throw new InvalidOperationException("Must set AZURE_OPENAI_ENDPOINT or OPENAI_API_KEY environment variables.");
            }


            // Register the WebJobs extension, which enables the bindings.
            builder.AddExtension<AgentsExtension>();            
            builder.Services
                .AddSingleton<FunctionInvoker>()
                .AddSingleton<IFunctionInvoker>(p => p.GetRequiredService<FunctionInvoker>());
            builder.Services.AddSingleton<FunctionCallTriggerBindingProvider>();
            return builder;
        }

        static void RegisterAssistantClient(IServiceCollection services, Uri azureOpenAIEndpoint, string azureOpenAIKey)
        {
            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddAssistantsClient(azureOpenAIEndpoint, new AzureKeyCredential(azureOpenAIKey));
            });
        }

        static void RegisterAzureOpenAIADAuthClient(IServiceCollection services, Uri azureOpenAIEndpoint)
        {
            var managedIdentityClient = new AssistantsClient(azureOpenAIEndpoint, new DefaultAzureCredential());
            services.AddSingleton<AssistantsClient>(managedIdentityClient);
        }

        static void RegisterOpenAIClient(IServiceCollection services, string openAIKey)
        {
            var openAIClient = new AssistantsClient(openAIKey);
            services.AddSingleton<AssistantsClient>(openAIClient);
        }

        static Uri GetAzureOpenAIEndpoint()
        {
            if (Uri.TryCreate(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"), UriKind.Absolute, out var uri))
            {
                return uri;
            }

            return null;
        }
    }
}
