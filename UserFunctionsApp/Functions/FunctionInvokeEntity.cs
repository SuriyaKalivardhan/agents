using Azure.AI.Runtime.Functions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AI.Runtime.Host.Service.Functions
{

    [JsonObject(MemberSerialization.OptIn)]
    public class FunctionInvokeEntity
    {
        readonly IClientFunctionInvoker _invoker;

        [JsonProperty("value")]
        public string CurrentValue { get; set; }

        public FunctionInvokeEntity(IClientFunctionInvoker invoker)
        {
            _invoker = invoker;
        }

        public async Task<string> InvokeAsync(SerializedFunctionCall call)
        {
            this.CurrentValue = await this._invoker.InvokeAsync(call, CancellationToken.None);
            return this.CurrentValue;
        }

        [Microsoft.Azure.WebJobs.FunctionName(nameof(FunctionInvokeEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext dispatcher)
        => dispatcher.DispatchAsync<FunctionInvokeEntity>();

        [Microsoft.Azure.WebJobs.FunctionName("InvokeFunctionAsync")]
        public async Task InvokeFunctionAsync(
            [OrchestrationTrigger]
            IDurableOrchestrationContext context)
        {
            SerializedFunctionCall call = context.GetInput<SerializedFunctionCall>();

            string response = await context.CallActivityAsync<string>("CallUserCode", call);

            context.SetOutput(response);
        }

        [Microsoft.Azure.WebJobs.FunctionName("InvokeQueueFunctionAsync")]
        [return:Queue("function-response")]
        public async Task<string> InvokeQueueFunctionAsync(
            [QueueTrigger("function-request")]
            string message)
        {
            IDictionary<string, SerializedFunctionResponse> response = new Dictionary<string, SerializedFunctionResponse>();
            IList<Task<string>> tasks = new List<Task<string>>();

            SerializedFunctionCall[] calls = JsonConvert.DeserializeObject<SerializedFunctionCall[]>(message);

            foreach(SerializedFunctionCall call in calls)
            {
                response.Add(call.Id, new SerializedFunctionResponse
                {
                    Id = call.Id,
                    InstanceId = call.InstanceId
                });

                tasks.Add(this.InvokeAsync(call));
            }

            await Task.WhenAll(tasks);

            for(int i = 0; i < calls.Length; ++i)
            {
                response[calls[i].Id].Response = tasks[i].Result;
            }

            return JsonConvert.SerializeObject(response.Values.ToArray());
        }



        [Microsoft.Azure.WebJobs.FunctionName("CallUserCode")]
        public async Task<string> CallUserCode(
           [ActivityTrigger]
            SerializedFunctionCall call)
        {
            return await this._invoker.InvokeAsync(call, CancellationToken.None);
        }
    }

    public class SerializedFunctionCall
    {
        public string InstanceId { get; set; }
        public string Id { get; set; }

        public string Name { get; set; }
        public string Arguments { get; set; }
    }

    public class SerializedFunctionResponse
    {
        public string InstanceId { get; set; }
        public string Id { get; set; }
        public string Response { get; set; }
    }
}
