using Azure.AI.Runtime.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Azure.AI.Runtime;

public class AgentConversation
{    
    public string AgentId { get; set; }

    public string ThreadId { get; set; }

    public string RunId { get; set; }

    [Required]
    [StringLength(60, MinimumLength = 3)]
    public string Message { get; set; }

    public string Status { get; set; }    
}

public partial class SystemFunctions
{
    [FunctionName("ListConversations")]
    public async Task<IActionResult> ListConversations(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "agents/{agentId}/conversations")] HttpRequestMessage req,
          string agentId,
          [DurableClient] IDurableOrchestrationClient durableClient)
    {
        List<AgentConversation> result = new List<AgentConversation>();

        Microsoft.Azure.WebJobs.Extensions.DurableTask.OrchestrationStatusQueryResult queryResult = await durableClient.ListInstancesAsync(
            new Microsoft.Azure.WebJobs.Extensions.DurableTask.OrchestrationStatusQueryCondition()
            {
                InstanceIdPrefix = agentId
            },
            CancellationToken.None);

        foreach (Microsoft.Azure.WebJobs.Extensions.DurableTask.DurableOrchestrationStatus status in queryResult.DurableOrchestrationState)
        {
            if (status.CustomStatus != null)
            {
                AgentMessage message = status.CustomStatus.ToObject<AgentMessage>();
                result.Add(new AgentConversation
                {
                    AgentId = message.TargetAgentId,
                    RunId = message.TargetRunId,
                    ThreadId = message.TargetThreadId,
                    Status = status.RuntimeStatus.ToString()
                });
            }
        }
        return new OkObjectResult(result.ToArray());
    }

    [FunctionName("StartConversation")]
    public async Task<HttpResponseMessage> StartConversation(
          [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "agents/{agentId}/conversations")] HttpRequestMessage req, string agentId,
          [DurableClient] IDurableOrchestrationClient durableClient)
    {
        AgentConversation agentConversation = null;

        using (StreamReader reader = new(req.Content.ReadAsStream()))
        {
            string requestBody = await reader.ReadToEndAsync();
            agentConversation = JsonConvert.DeserializeObject<AgentConversation>(requestBody);
        }

        AgentMessage message = new AgentMessage();
        message.Message = agentConversation.Message;
        message.TargetAgentId = agentId;
        message.TargetThreadId = agentConversation.ThreadId;
        message.TargetRunId = agentConversation.RunId;

        string responseId = await durableClient.StartNewAsync(
            "SendAgentMessage",
            agentId + "_" + Guid.NewGuid().ToString("N"),
            message);

        this._logger.LogInformation("Started orchestration with ID = '{instanceId}'.", responseId);

        HttpResponseMessage responseMessage = durableClient.CreateCheckStatusResponse(req, responseId);

        return responseMessage;
    }
}