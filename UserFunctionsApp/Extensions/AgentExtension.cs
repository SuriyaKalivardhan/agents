namespace Azure.AI.Runtime.Functions
{
    using Microsoft.Azure.WebJobs.Description;
    using Microsoft.Azure.WebJobs.Host.Config;
    using System;


    [Extension("AI.Runtime")]
    partial class AgentsExtension : IExtensionConfigProvider
    {        
        readonly FunctionCallTriggerBindingProvider functionCallTriggerBindingProvider;

        public AgentsExtension(         
            FunctionCallTriggerBindingProvider functionCallTriggerBindingProvider)
        {     
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