using Azure.AI.OpenAI.Assistants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Azure.AI.Runtime
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class FunctionCallAttribute : Attribute
    {
    }


    public class UserFunctionTool
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string ParameterJSON { get; set; }
    }

    public partial class SystemFunctions
    {
        [FunctionName("ListFunctions")]
        public async Task<IActionResult> ListFunctions(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tools")] HttpRequestMessage req)
        {
            FunctionToolDefinition[] result = await this.invoker.ListFunctionsInternal();

            IList<UserFunctionTool> functionTools = new List<UserFunctionTool>();
            foreach (FunctionToolDefinition functionTool in result)
            {
                if (functionTool.Name != "SendAgentMessage")
                {
                    functionTools.Add(
                        new UserFunctionTool
                        {
                            Name = functionTool.Name,
                            Description = functionTool.Description,
                            ParameterJSON = functionTool.Parameters.ToString()
                        });
                }
            }
            return new OkObjectResult(functionTools.ToArray());
        }
    }
}
