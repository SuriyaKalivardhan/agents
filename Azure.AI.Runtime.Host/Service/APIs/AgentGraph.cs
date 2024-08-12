using Azure.AI.OpenAI.Assistants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Azure.AI.Runtime;

public class AgentGraph : Agent
{
    public AgentGraph()
    {
        this.Agents = new string[] { };
        this.Instructions = "Reserved for system";
    }

    public string[] Agents { get; set; }


    /// <summary>
    /// Relay
    /// Aggregator
    /// Selector
    /// Dense
    /// Custom
    /// </summary>
    public string Kind
    {
        get;
        set;
    }    
}

public partial class SystemFunctions
{
    [FunctionName("CreateAgentGraph")]
    public async Task<IActionResult> CreateAgentGraph(
      [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "agentGraph")] HttpRequestMessage req)
    {
        AssistantCreationOptions assistantCreationOptions = null;
        AgentGraph agent = null;

        using (StreamReader reader = new(req.Content.ReadAsStream()))
        {
            string requestBody = await reader.ReadToEndAsync();
            agent = JsonConvert.DeserializeObject<AgentGraph>(requestBody);
        }

        assistantCreationOptions = new AssistantCreationOptions(agent.Model);

        if (agent.Kind == "Custom")
        {
            assistantCreationOptions.Instructions = string.Format(@"You are an autonomous multi-agent chat coordinator. You cannot wait for human input." +
                    "There are two set of instructions for you. First instruction is how to send message with other agents. Second instruction is when and what to send to agent. " +
                    " ### How to exchange message with agents? ####" +
                    "You are provided reference to set of agents; their identifiers are {0} each agents have unique identifier with which you can address them" +
                    "You can send a message to any chosen agents via special function tool SendAgentMessage provided to you, the reply from the agent" +
                    "will be returned as function call response to SendAgentMessage. Only you can initiate message exchange with the agents. Other agents simply respond to your request." +
                    "Each agent can remember the context from previous message as long as you include the ThreadId informtation they returned from previous call" +
                    "Each agent have unique thread and run id, do not mix them across agents and send only the thread id received from previous interaction with the agent" +
                    "For the first interaction with the agent you can set the TargetThreadId and TargetRunID as null, this will initiate a new session with the agent" +
                    "Response you get from Agent from SendAgentMessage is composed of two parts, the first part is session related information which contains ThreadId parameters which you must" +
                    "use to send subsequent message to the agent" +
                    "The second part is the actual response for the message you have sent, it starts after 'Here is my actual response to your query --' use only that part of response as conversation" +
                    "context to make the decision. Do not share agent routing information with other agents as part of context message." +

                    " ### When and what to exchange as message? {1}", string.Join(",", agent.Agents), agent.Instructions);

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
        }
        else if (agent.Kind == "Relay")
        {
            //Instruct For TwoWay Chat
            assistantCreationOptions.Instructions = string.Format(@"You are an autonomous multi-agent chat coordinator. You cannot wait for human input." +
                    "There are two set of instructions for you. First instruction is how to send message with other agents. Second instruction is when and what to send to agent. " +
                    " ### How to exchange message with agents? ####" +
                    "You are provided reference to two agents; their identifiers are {0},{1} each agents have unique identifier with which you can address them" +
                    "You can send a message to any chosen agents via special function tool SendAgentMessage provided to you, the reply from the agent" +
                    "will be returned as function call response to SendAgentMessage. Only you can initiate message exchange with the agents. Other agents simply respond to your request." +
                    "Each agent can remember the context from previous message as long as you include the ThreadId informtation they returned from previous call" +
                    "Each agent have unique thread and run id, do not mix them across agents and send only the thread id received from previous interaction with the agent" +
                    "For the first interaction with the agent you can set the TargetThreadId as null, this will initiate a new session with the agent" +
                    "Response you get from Agent from SendAgentMessage is composed of two parts, the first part is session related information which contains ThreadId parameters which you must" +
                    "use to send subsequent message to the agent" +
                    "The second part is the actual response for the message you have sent, it starts after 'Here is my actual response to your query --' use only that part of response as conversation" +
                    "context to make the decision. Do not share agent routing information with other agents as part of context message." +

                    " ### When and what to exchange as message?" +
                    "You send the initial thread message to agent 1 and get the response and pass it to agent 2 as is, use the address information for conversation routing but do not include them as message to other agents." +
                    "You continue to forward each other responses to other agents until they stop providing any meaningful response or after exchanging 10 messages each. Finally provide summary of the chat as part of last message", agent.Agents[0], agent.Agents[1]);
        }
        else if (agent.Kind == "Aggregator")
        {
            //Instruct For Group Chat
            assistantCreationOptions.Instructions = string.Format(@"You are an autonomous multi-agent chat coordinator. You cannot wait for human input." +
                    "There are two set of instructions for you. First instruction is how to send message with other agents. Second instruction is when and what to send to agent. " +
                    " ### How to exchange message with agents? ####" +
                    "You are provided reference to set of agents; their identifiers are {0} each agents have unique identifier with which you can address them" +
                    "You can send a message to any chosen agents via special function tool SendAgentMessage provided to you, the reply from the agent" +
                    "will be returned as function call response to SendAgentMessage. Only you can initiate message exchange with the agents. Other agents simply respond to your request." +
                    "Each agent can remember the context from previous message as long as you include the ThreadId information they returned from previous call" +
                    "Each agent have unique thread and run id, do not mix them across agents and send only the thread id received from previous interaction with the agent" +
                    "For the first interaction with the agent you can set the TargetThreadId as null, this will initiate a new session with the agent" +
                    "Response you get from Agent from SendAgentMessage is composed of two parts, the first part is session related information which contains ThreadId parameters which you must" +
                    "use to send subsequent message to the agent" +
                    "The second part is the actual response for the message you have sent, it starts after 'Here is my actual response to your query --' use only that part of response as conversation" +
                    "context to make the decision. Do not share agent routing information with other agents as part of context message." +

                    " ### When and what to exchange as message?" +
                    " Initiate the conversation with all of the agents on 1:1 fashion and try to use every response form every agent to complete the task" +
                    "find the best way to aggregate the response and summarize them as part of your last message, stop the conversation when there is no progress is made or" +
                    "you have reached more than 20 conversation turns", string.Join(",", agent.Agents));
        }
        else if (agent.Kind == "Dense")
        {
            //Instruct For Group Chat
            assistantCreationOptions.Instructions = string.Format(@"You are an autonomous multi-agent chat coordinator. You cannot wait for human input.""  +
                        ""There are two set of instructions for you. First instruction is how to send message with other agents. Second instruction is when and what to send to agent. "" +
                        "" ### How to exchange message with agents? ####"" +
                        ""You are provided reference to set of agents; their identifiers are {0} each agents have unique identifier with which you can address them"" +
                        ""You can send a message to any chosen agents via special function tool SendAgentMessage provided to you, the reply from the agent"" +
                        ""will be returned as function call response to SendAgentMessage. Only you can initiate message exchange with the agents. Other agents simply respond to your request."" +
                        ""Each agent can remember the context from previous message as long as you include the ThreadId information they returned from previous call"" +
                        ""Each agent have unique thread and run id, do not mix them across agents and send only the thread id received from previous interaction with the agent"" +
                        ""For the first interaction with the agent you can set the TargetThreadId as null, this will initiate a new session with the agent"" +
                        ""Response you get from Agent from SendAgentMessage is composed of two parts, the first part is session related information which contains ThreadId parameters which you must"" +
                        ""use to send subsequent message to the agent"" +                        
                        ""The second part is the actual response for the message you have sent, it starts after 'Here is my actual response to your query --' use only that part of response as conversation"" +
                        ""context to make the decision. Do not share agent routing information with other agents as part of context message." +

                    " ### When and what to exchange as message?" +
                    "Initiate the conversation with the every agent with goal of arriving at similar conclusion" +
                    "ensure every agents are aware of each other and allow them to ask questions and discuss additional clarification information through you" +
                    "you can coordinate the discussions since only you can initiate direct communication with agents, other agent can request additional information" +
                    "from the group only through you. Continue the discussion until everyone agress to or stop after 20 turns and finally summarize the session as part of last message", string.Join(",", agent.Agents));
        }
        else if (agent.Kind == "Selector")
        {
            //Instruct For Group Chat
            assistantCreationOptions.Instructions = string.Format(@"You are an autonomous multi-agent chat coordinator. You cannot wait for human input." +
                    "There are two set of instructions for you. First instruction is how to send message with other agents. Second instruction is when and what to send to agent. " +
                    " ### How to exchange message with agents? ####" +
                    "You are provided reference to set of agents; their identifiers are {0} each agents have unique identifier with which you can address them" +
                    "You can send a message to any chosen agents via special function tool SendAgentMessage provided to you, the reply from the agent" +
                    "will be returned as function call response to SendAgentMessage. Only you can initiate message exchange with the agents. Other agents simply respond to your request." +
                    "Each agent can remember the context from previous message as long as you include the ThreadId informtation they returned from previous call" +
                    "Each agent have unique thread and run id, do not mix them across agents and send only the thread and received from previous interaction with the agent" +
                    "For the first interaction with the agent you can set the TargetThreadId as null, this will initiate a new session with the agent" +
                    "Response you get from Agent from SendAgentMessage is composed of two parts, the first part is session related information which contains ThreadId parameters which you must" +
                    "use to send subsequent message to the agent" +
                    "The second part is the actual response for the message you have sent, it starts after 'Here is my actual response to your query --' use only that part of response as conversation" +
                    "context to make the decision. Do not share agent routing information with other agents as part of context message." +

                    " ### When and what to exchange as message?" +
                    " your role is to identify the capacbility of each agents given to you and use the agent or set of agents which have most relevant skills to solve the current task at hane",
                    string.Join(",", agent.Agents));
        }

        assistantCreationOptions.Description = agent.Description;
        assistantCreationOptions.Name = agent.Name;
        assistantCreationOptions.Tools.Add(this.invoker.GetSystemToolDefinition());

        Response<Assistant> assistantResponse = await this.assistantsClient.CreateAssistantAsync(
            assistantCreationOptions,
            CancellationToken.None);

        return new OkObjectResult(assistantResponse);
    }
}