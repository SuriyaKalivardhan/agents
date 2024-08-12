namespace Azure.AI.Runtime.Host.UserFunctions
{
    using Azure.AI.Runtime;
    using Microsoft.Azure.WebJobs;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;    

    public class AgentPlanner
    {
        [FunctionCall]
        public sealed class SelectAgentParameter
        {
            [Required]
            [Description("Comma separated list of Available Agent")]
            public string Agents { get; set; }

            [Description("Context of task at hand. Like thread message or source request.")]
            public string RequestContext { get; set; }
        }

        [FunctionCall]
        public sealed class RankResponseParameter
        {
            [Required]
            [Description("set of response to rank, ensure responses are delimited based on the delimiter character specified in the parameter delimiter.")]
            public string ResponseArray { get; set; }

            [Description("Delimiter string to use to group the responses")]
            public string DelimiterValue { get; set; }
        }

        [FunctionName(nameof(RankResponse))]
        public Task<string> RankResponse(
            [OpenAIFunctionCallTriggerAttribute(
                    "RankResponse",
                    @"This is a readonly operation. Given a set of response from multiple participating agents it helps to rank the response based on the relevance. The ranking can be used to pick the best response")]
                    RankResponseParameter rankAction)
        {
            return Task.FromResult(
                rankAction.ResponseArray);
        }

        [FunctionName(nameof(SelectAgent))]
        public Task<string> SelectAgent(
            [OpenAIFunctionCallTriggerAttribute(
                    "SelectAgent",
                    @"This is a readonly operation. Given a set of possible agents for given context, it helps filter down the best agent to forward the request and optionally the prompt to send.")]
                    SelectAgentParameter selectAgent)
        {
            string[] agent = selectAgent.Agents.Split(",");
            string agentToSelect = "asst_pZ6AuwufnH4XbqunkyHOARSc";
            string agentPrompt = "Customer have trouble with the resources their endpoint information is 123 and subscription id is 132909 " + selectAgent.RequestContext;

            return Task.FromResult(string.Format("Please use the agent {0} and provide the following as context {1}",
                agentToSelect,
                agentPrompt));
        }
    }
}
