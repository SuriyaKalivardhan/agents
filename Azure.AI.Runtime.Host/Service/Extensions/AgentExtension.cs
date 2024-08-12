namespace Azure.AI.Runtime.Functions
{
    using Azure.AI.OpenAI.Assistants;
    using Microsoft.Azure.WebJobs.Description;
    using Microsoft.Azure.WebJobs.Host.Config;
    using Microsoft.Azure.WebJobs.Host.Listeners;
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;


    [Extension("Runtime")]
    partial class AgentsExtension : IExtensionConfigProvider
    {
        readonly AssistantsClient assistantClient;
        readonly FunctionCallTriggerBindingProvider functionCallTriggerBindingProvider;

        public AgentsExtension(
            AssistantsClient assistantClient,
            FunctionCallTriggerBindingProvider functionCallTriggerBindingProvider)
        {
            this.assistantClient = assistantClient ?? throw new ArgumentNullException(nameof(assistantClient));
            this.functionCallTriggerBindingProvider = functionCallTriggerBindingProvider ?? throw new ArgumentNullException(nameof(functionCallTriggerBindingProvider));
        }

        void IExtensionConfigProvider.Initialize(ExtensionConfigContext context)
        {
            // FunctionCall trigger support
            context.AddBindingRule<OpenAIFunctionCallTriggerAttribute>()
                .BindToTrigger(this.functionCallTriggerBindingProvider);
        }
    }
}