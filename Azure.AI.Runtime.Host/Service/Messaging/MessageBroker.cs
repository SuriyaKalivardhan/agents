using Azure.AI.OpenAI.Assistants;
using Azure.AI.Runtime.Data;
using Azure.AI.Runtime.Functions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using Azure;

namespace Azure.AI.Runtime.Messaging
{
    internal sealed class MessageBroker
    {
        AssistantsClient assistantClient = null;
        IFunctionInvoker functionInvoker = null;
        ILogger logger = null;

        public MessageBroker(AssistantsClient client, IFunctionInvoker functionInvoker, ILoggerFactory loggerFactory)
        {
            assistantClient = client;
            this.functionInvoker = functionInvoker;
            this.logger = loggerFactory.CreateLogger("MessageBroker");
        }

        public Task __SendAgentMessage__(
            [OpenAIFunctionCallTriggerAttribute(
                    "SendAgentMessage",
                    @"Special function tool to send message to a given agent and get response from the agent as tool response"" +
                    ""this tool is useful when functioning as multi-agent coordinator")]
                    AgentMessage message)
        {
            throw new NotImplementedException();
        }
        

        [Microsoft.Azure.WebJobs.FunctionName("SendAgentMessage")]
        public async Task SendAgentMessage(            
            [OrchestrationTrigger]
            IDurableOrchestrationContext context)
        {
            AgentMessage message = context.GetInput<AgentMessage>();
            if (string.IsNullOrEmpty(message.TargetThreadId))
            {
                message = await context.CallActivityAsync<AgentMessage>("CreateThreadAndRun",
                    message);

                context.SetCustomStatus(message);
            }
            else
            {
                message = await context.CallActivityAsync<AgentMessage>("CreateMessageAndRun",
                    message);

                context.SetCustomStatus(message);
            }

            message = await context.CallActivityAsync<AgentMessage>(
                    "RunConversationSegmentLoop",
                    message);

            context.SetOutput(message);
        }

        [FunctionName(nameof(CreateThreadAndRun))]
        public async Task<AgentMessage> CreateThreadAndRun([ActivityTrigger] AgentMessage message)
        {
            ThreadInitializationMessage initializationMessage = new ThreadInitializationMessage(MessageRole.User, message.Message);
            AssistantThreadCreationOptions options = new AssistantThreadCreationOptions();
            options.Messages.Add(initializationMessage);

            Response<ThreadRun> thread = await assistantClient.CreateThreadAndRunAsync(
                new CreateAndRunThreadOptions(message.TargetAgentId)
                {
                    Thread = options
                });
            
            message.TargetThreadId = thread.Value.ThreadId;
            message.TargetRunId = thread.Value.Id;
            return message;
        }

        [FunctionName(nameof(CreateMessageAndRun))]
        public async Task<AgentMessage> CreateMessageAndRun([ActivityTrigger] AgentMessage message)
        {
            Response<ThreadMessage> threadResponse = await assistantClient.CreateMessageAsync(
                    message.TargetThreadId,
                    MessageRole.User,
                    message.Message);

            Response<ThreadRun> thread = await assistantClient.CreateRunAsync(
                message.TargetThreadId,
                new CreateRunOptions(message.TargetAgentId));

            message.TargetRunId = thread.Value.Id;
            return message;   
        }

        [Microsoft.Azure.WebJobs.FunctionName("RunConversationSegmentLoop")]
        public async Task<AgentMessage> RunConversationSegmentLoop(            
            [ActivityTrigger]
            AgentMessage agentMessage,
            [DurableClient] IDurableOrchestrationClient orchestrationClient)
        {
            HashSet<string> pendingCalls = new HashSet<string>();
            Dictionary<string, AgentMessage> completedCalls = new Dictionary<string, AgentMessage>();

            do
            {
                Response<ThreadRun> runResponse = await assistantClient.GetRunAsync(
                    agentMessage.TargetThreadId,
                    agentMessage.TargetRunId);

                while (runResponse.Value.Status == RunStatus.Queued ||
                    runResponse.Value.Status == RunStatus.InProgress)
                {
                    pendingCalls.Clear();
                    completedCalls.Clear();

                    await Task.Delay(TimeSpan.FromMilliseconds(100));

                    runResponse = await assistantClient.GetRunAsync(
                        agentMessage.TargetThreadId,
                        agentMessage.TargetRunId);
                }

                if (runResponse.Value.Status == RunStatus.RequiresAction &&
                    runResponse.Value.RequiredAction is SubmitToolOutputsAction submitTools)
                {
                    foreach (RequiredToolCall toolCall in submitTools.ToolCalls)
                    {
                        RequiredFunctionToolCall requiredFunctionCall = toolCall as RequiredFunctionToolCall;

                        if (requiredFunctionCall != null)
                        {
                            if (requiredFunctionCall.Name == "SendAgentMessage")
                            {
                                AgentMessage callMessage = JsonSerializer.Deserialize<AgentMessage>(requiredFunctionCall.Arguments);
                                string uniqueId = runResponse.Value.ThreadId + "_" + runResponse.Value.Id + "_" + requiredFunctionCall.Id;

                                if(!pendingCalls.Contains(uniqueId))
                                {
                                    pendingCalls.Add(uniqueId);

                                    await orchestrationClient.StartNewAsync(
                                        "SendAgentMessage",
                                        uniqueId,    
                                        new AgentMessage
                                        {
                                            CallId = requiredFunctionCall.Id,
                                            CallingAgentId = agentMessage.TargetAgentId,
                                            CallingThreadId = agentMessage.TargetThreadId,
                                            CallingRunId = agentMessage.TargetRunId,
                                            TargetAgentId = callMessage.TargetAgentId,                    
                                            TargetThreadId = callMessage.TargetThreadId,
                                            Message = callMessage.Message
                                        });
                                }
                                else if(!completedCalls.ContainsKey(uniqueId))
                                {
                                    DurableOrchestrationStatus status = await orchestrationClient.GetStatusAsync(
                                        uniqueId);

                                    if(status.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
                                    {
                                        completedCalls.Add(uniqueId, status.Output.ToObject<AgentMessage>());

                                        //All calls have completed. Lets submit the response and resume the run.
                                        if(completedCalls.Count == submitTools.ToolCalls.Count)
                                        {                                            
                                            IList<ToolOutput> outputs = new List<ToolOutput>();
                                            foreach(RequiredToolCall tCall in submitTools.ToolCalls)
                                            {
                                                if(tCall is RequiredFunctionToolCall rFnCall)
                                                {
                                                    string uniqueCallId = runResponse.Value.ThreadId + "_" + runResponse.Value.Id + "_" + rFnCall.Id;
                                                    outputs.Add(new ToolOutput(rFnCall.Id,
                                                        FormatAgentResponse(
                                                            completedCalls[uniqueCallId])));
                                                }
                                            }

                                            await assistantClient.SubmitToolOutputsToRunAsync(
                                                runResponse.Value,
                                                outputs.ToArray());

                                            pendingCalls.Clear();
                                            completedCalls.Clear();
                                        }
                                    }
                                    else if(status.RuntimeStatus != OrchestrationRuntimeStatus.Running &&
                                        status.RuntimeStatus != OrchestrationRuntimeStatus.Pending)
                                    {
                                        throw new Exception(string.Format("Failing the run {0} since the child run has failed", uniqueId));
                                    }
                                }
                            }
                            else
                            {
                                string functionResponse = await this.functionInvoker.InvokeAsync(
                                    requiredFunctionCall,
                                    CancellationToken.None);

                                ToolOutput userFunctionOutput = new ToolOutput(requiredFunctionCall.Id, functionResponse);

                                await assistantClient.SubmitToolOutputsToRunAsync(
                                    runResponse.Value,
                                    new ToolOutput[] { userFunctionOutput });
                            }
                        }
                    }

                    if (pendingCalls.Count != 0)
                    {
                        //Avoid spinning tight when we have launched cross agent message.
                        await Task.Delay(TimeSpan.FromMilliseconds(500));
                    }
                }
                else
                {
                    break;
                }
            } while (true);

            Response<PageableList<ThreadMessage>> afterRunMessagesResponse = await assistantClient.GetMessagesAsync(
                agentMessage.TargetThreadId,
                null,
                ListSortOrder.Descending);
            IReadOnlyList<ThreadMessage> messages = afterRunMessagesResponse.Value.Data;

            AgentMessage response = new AgentMessage
            {
                TargetAgentId = agentMessage.CallingAgentId,
                TargetThreadId = agentMessage.CallingThreadId,
                TargetRunId = agentMessage.CallingRunId,
                CallingRunId = agentMessage.TargetRunId,
                CallingAgentId = agentMessage.TargetAgentId,
                CallingThreadId = agentMessage.TargetThreadId
            };

            StringBuilder responseBuilder = new StringBuilder();

            foreach (ThreadMessage threadMessage in messages)
            {
                if (threadMessage.RunId == agentMessage.TargetRunId && threadMessage.Role == MessageRole.Assistant)
                {
                    foreach (MessageContent contentItem in threadMessage.ContentItems)
                    {
                        if (contentItem is MessageTextContent textContent)
                        {
                            responseBuilder.AppendLine(textContent.Text);
                        }
                    }
                }
                else
                {
                    break; //Break as soon as we encounter user message.
                }
            }
            response.Message = responseBuilder.ToString();
            return response;
        }

        static string FormatAgentResponse(AgentMessage agentMessage)
        {
            string responseTemplate = @"Here are my routing information to resume conversation with me for future interaction. My AgentID is {0} My ThreadID is {1} & My Run Id is {2} --This is end of routing part." +
                        "Here is my actual response to your query -- {3}";
            return string.Format(responseTemplate,
                agentMessage.CallingAgentId,
                agentMessage.CallingThreadId,
                agentMessage.CallingRunId,
                agentMessage.Message);         
        }
    }
}
