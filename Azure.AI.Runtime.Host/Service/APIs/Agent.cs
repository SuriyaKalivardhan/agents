using Azure.AI.OpenAI.Assistants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Azure.AI.Runtime;

public class Agent
{
    public Agent()
    {
        this.UserFunctions = new string[] { };
        this.Indexes = new string[] { };
    }

    public string Id { get; set; }

    [Required]
    [StringLength(60, MinimumLength = 3)]
    public string Name { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 3)]
    public string Description { get; set; }


    public string Instructions { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Model { get; set; }

    public string[] UserFunctions { get; set; }

    public string[] Indexes { get; set; }

    public bool EnableCodeInterpretor { get; set; }

    public bool EnableMemory { get; set; }

    public bool EnableSemanticCaching { get; set; }
}

public partial class SystemFunctions
{
    [FunctionName("CreateAgent")]
    public async Task<IActionResult> CreateAgent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "agents")]
        HttpRequestMessage req)
    {
        AssistantCreationOptions assistantCreationOptions = null;
        Agent agent = null;

        using (StreamReader reader = new(req.Content.ReadAsStream()))
        {
            string requestBody = await reader.ReadToEndAsync();
            agent = JsonConvert.DeserializeObject<Agent>(requestBody);
        }

        assistantCreationOptions = new AssistantCreationOptions(agent.Model);
        assistantCreationOptions.Instructions = agent.Instructions;
        assistantCreationOptions.Description = agent.Description;
        assistantCreationOptions.Name = agent.Name;

        FunctionToolDefinition[] availableFunctions = await this.invoker.ListFunctionsInternal();

        if (agent != null && agent.UserFunctions != null && agent.UserFunctions.Length != 0)
        {
            foreach (string toolName in agent.UserFunctions)
            {
                if (string.IsNullOrEmpty(toolName)) continue;

                FunctionToolDefinition? selectedToolDefinition = availableFunctions.SingleOrDefault(tool => (tool.Name == toolName));

                if (selectedToolDefinition != default(FunctionToolDefinition))
                {
                    assistantCreationOptions.Tools.Add(selectedToolDefinition);
                }
                else
                {
                    return new BadRequestObjectResult(string.Format("Tool {0} is not found", toolName));
                }
            }
        }

        //todo, INJECT System Tool Calls Here.

        Response<Assistant> assistantResponse = await this.assistantsClient.CreateAssistantAsync(
            assistantCreationOptions,
            CancellationToken.None);

        return new OkObjectResult(assistantResponse);
    }

    [FunctionName("UpdateAgent")]
    public async Task<IActionResult> UpdateAgent(
      [Microsoft.Azure.WebJobs.HttpTrigger(Microsoft.Azure.WebJobs.Extensions.Http.AuthorizationLevel.Anonymous, "put", Route = "agents/{agentId}")] HttpRequestMessage req, string agentId)
    {
        UpdateAssistantOptions assistantCreationOptions = null;
        Agent agent = null;

        using (StreamReader reader = new(req.Content.ReadAsStream()))
        {
            string requestBody = await reader.ReadToEndAsync();
            agent = JsonConvert.DeserializeObject<Agent>(requestBody);
        }

        assistantCreationOptions = new UpdateAssistantOptions();
        assistantCreationOptions.Instructions = agent.Instructions;
        assistantCreationOptions.Description = agent.Description;
        assistantCreationOptions.Name = agent.Name;
        assistantCreationOptions.Model = agent.Model;

        FunctionToolDefinition[] availableFunctions = await this.invoker.ListFunctionsInternal();

        if (agent != null && agent.UserFunctions != null && agent.UserFunctions.Length != 0)
        {
            foreach (string toolName in agent.UserFunctions)
            {
                FunctionToolDefinition? selectedToolDefinition = availableFunctions.SingleOrDefault(tool => (tool.Name == toolName));

                if (selectedToolDefinition != default(FunctionToolDefinition))
                {
                    assistantCreationOptions.Tools.Add(selectedToolDefinition);
                }
                else
                {
                    return new BadRequestObjectResult(string.Format("Tool {0} is not found", toolName));
                }
            }
        }

        //todo, INJECT System Tool Calls Here.

        Response<Assistant> assistantResponse = await this.assistantsClient.UpdateAssistantAsync(
            agentId,
            assistantCreationOptions,
            CancellationToken.None);

        return new OkObjectResult(assistantResponse);
    }


}