using Microsoft.Azure.WebJobs.Host.Triggers;
using System.Reflection;
using System.Threading.Tasks;

namespace Azure.AI.Runtime.Functions
{
    internal sealed class FunctionCallTriggerBindingProvider : ITriggerBindingProvider
    {
        static readonly Task<ITriggerBinding> NullTriggerBindingTask = Task.FromResult<ITriggerBinding>(null);

        FunctionInvoker invoker;

        public FunctionCallTriggerBindingProvider(FunctionInvoker functionInvoker)
        {
            this.invoker = functionInvoker;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            ParameterInfo pinfo = context.Parameter;
            OpenAIFunctionCallTriggerAttribute functionCallAttribute = pinfo.GetCustomAttribute<OpenAIFunctionCallTriggerAttribute>();
            if (functionCallAttribute != null)
            {
                ITriggerBinding binding = new FunctionCallTriggerBinding(
                    functionCallAttribute.FunctionName,
                    functionCallAttribute.FunctionDescription,
                    functionCallAttribute.ParameterDescriptionJson,
                    functionCallAttribute,
                    invoker,
                    pinfo);
                return Task.FromResult(binding);
            }
            else
            {
                return NullTriggerBindingTask;
            }
        }
    }
}
