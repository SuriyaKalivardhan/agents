using Azure.AI.OpenAI.Assistants;
using Azure.AI.Runtime.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Azure.AI.Runtime
{
    public class AgentEvent
    {
        public AgentEvent() { }

        public string OriginatingAgentId { get; set; }

        public string TargetAgentId { get; set; }

        public string TargetAgentThreadId { get; set; }

        public string TargetAgentRunId { get; set; }

        public string Request { get; set; }

        public string Response { get; set; }
    }

    public partial class SystemFunctions
    {
        [FunctionName("ListAgentEvents")]
        public async Task<IActionResult> ListAgentEvents(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "agentEvents/{threadId}/{runId}")] HttpRequestMessage req,
          string threadId,
          string runId)
        {
            IList<AgentEvent> result = new List<AgentEvent>();

            var runSteps = await this.assistantsClient.GetRunStepsAsync(
                threadId,
                runId);

            foreach (RunStep runStep in runSteps.Value.Reverse())
            {
                if (runStep.StepDetails is RunStepToolCallDetails callDetails)
                {
                    foreach (RunStepToolCall toolCall in callDetails.ToolCalls)
                    {
                        if (toolCall is RunStepFunctionToolCall fnToolCall)
                        {
                            if (fnToolCall.Name == "SendAgentMessage")
                            {
                                AgentMessage inputMessage = null;
                                string messageOutput = null;
                                string targetRunId = null;
                                string targetThreadId = null;

                                if (!string.IsNullOrEmpty(fnToolCall.Arguments))
                                {
                                    inputMessage = JsonConvert.DeserializeObject<AgentMessage>(fnToolCall.Arguments);
                                }

                                if (!string.IsNullOrEmpty(fnToolCall.Output))
                                {
                                    string responseMarker = "Here is my actual response to your query --";
                                    int indexOfResponse = fnToolCall.Output.IndexOf(responseMarker);
                                    if (indexOfResponse != -1)
                                    {
                                        messageOutput = fnToolCall.Output.Substring(indexOfResponse + responseMarker.Length);
                                    }

                                    string runIdMarker = "My Run Id is ";
                                    int indexOfRunId = fnToolCall.Output.IndexOf(runIdMarker);
                                    if (indexOfRunId != -1)
                                    {
                                        int indexOfRunIdComplete = fnToolCall.Output.IndexOf(
                                            " --This is end of routing part.",
                                            indexOfRunId);
                                        targetRunId = fnToolCall.Output.Substring(indexOfRunId + runIdMarker.Length, indexOfRunIdComplete - indexOfRunId - runIdMarker.Length);
                                    }

                                    if (inputMessage.TargetThreadId == null)
                                    {
                                        string threadIdMarker = "My ThreadID is ";
                                        int indexOfThreadId = fnToolCall.Output.IndexOf(threadIdMarker);
                                        if (indexOfThreadId != -1)
                                        {
                                            int indexOfThreadIdComplete = fnToolCall.Output.IndexOf(
                                                " & My Run Id is ",
                                                indexOfThreadId);
                                            targetThreadId = fnToolCall.Output.Substring(indexOfThreadId + threadIdMarker.Length, indexOfThreadIdComplete - indexOfThreadId - threadIdMarker.Length);
                                        }
                                    }
                                }

                                if (inputMessage != null)
                                {
                                    AgentEvent agentEvent = new AgentEvent();
                                    agentEvent.OriginatingAgentId = runStep.AssistantId;
                                    agentEvent.TargetAgentId = inputMessage.TargetAgentId;
                                    agentEvent.Request = inputMessage.Message;
                                    agentEvent.Response = messageOutput;
                                    agentEvent.TargetAgentThreadId = inputMessage.TargetThreadId ?? targetThreadId;
                                    agentEvent.TargetAgentRunId = targetRunId;

                                    result.Add(agentEvent);
                                }
                            }
                        }
                    }
                }
            }

            return new OkObjectResult(result.ToArray());
        }
    }
}
