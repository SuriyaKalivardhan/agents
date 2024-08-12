using Azure.AI.OpenAI.Assistants;
using Azure.AI.Runtime.Functions;
using Microsoft.Extensions.Logging;

namespace Azure.AI.Runtime
{
    public partial class SystemFunctions
    {
        private readonly ILogger<SystemFunctions> _logger;
        FunctionInvoker invoker;
        AssistantsClient assistantsClient;

        public SystemFunctions(ILogger<SystemFunctions> logger, FunctionInvoker invoker, AssistantsClient client)
        {
            _logger = logger;
            this.invoker = invoker;
            this.assistantsClient = client;
        }        
    }
}
