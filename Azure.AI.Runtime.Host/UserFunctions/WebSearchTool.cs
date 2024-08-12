namespace Azure.AI.Runtime.Host.UserFunctions
{
    using Azure.AI.Runtime;
    using Microsoft.Azure.WebJobs;
    using Newtonsoft.Json;

    public class WebSearchTool
    {
        public WebSearchTool()
        {

        }
        
        [FunctionName(nameof(DoBingSearch))]
        public async Task<string> DoBingSearch(
            [OpenAIFunctionCallTriggerAttribute(
                    "DoBingSearch",
                    @"This is a readonly operation. This is a search tool which can get real time information from web by invoking online search. You can provide any text prompt as request.")]
                    string query)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Environment.GetEnvironmentVariable("BING_SEARCH_KEY"));
            string response = await client.GetStringAsync("https://api.bing.microsoft.com/v7.0/search?q=" + Uri.EscapeDataString(query));
            var jsonResponse = JsonConvert.DeserializeObject(response);
            return JsonConvert.SerializeObject(jsonResponse, Formatting.Indented);
        }


    }
}
