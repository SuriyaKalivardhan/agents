using Azure.AI.Runtime;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Azure;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AI.Runtime.Functions
{
    internal class FunctionCallListener : IListener
    {
        private readonly ListenerFactoryContext _listenerFactoryContext;
        private readonly FunctionInvoker functionInvoker;
        private string _functionName;
        private OpenAIFunctionCallTriggerAttribute attribute;
        private readonly ParameterInfo parameterInfo;        

        public FunctionCallListener(
            string functionName,
            OpenAIFunctionCallTriggerAttribute attribute,
            ParameterInfo parameterInfo,
            ListenerFactoryContext listenerFactoryContext,
            FunctionInvoker functionInvoker)
        {
            _functionName = functionName;
            this.attribute = attribute;
            this.parameterInfo = parameterInfo;
            this._listenerFactoryContext = listenerFactoryContext;
            this.functionInvoker = functionInvoker;            
        }

        public void Cancel()
        {
            
        }

        public void Dispose()
        {
            
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.functionInvoker.RegisterSkill(
                this._functionName,
                this.attribute,
                this.parameterInfo,
                this._listenerFactoryContext.Executor);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.functionInvoker.UnregisterSkill(this._functionName);
            return Task.CompletedTask;
        }
    }
}
