using Azure.AI.Runtime.Functions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

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

            

            // Register the WebJobs extension, which enables the bindings.
            builder.AddExtension<AgentsExtension>();            
            builder.Services
                .AddSingleton<FunctionInvoker>()
                .AddSingleton<IClientFunctionInvoker>(p => p.GetRequiredService<FunctionInvoker>());
            builder.Services.AddSingleton<FunctionCallTriggerBindingProvider>();
            return builder;
        }        
    }
}
