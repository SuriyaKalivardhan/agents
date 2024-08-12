namespace Azure.AI.Runtime
{
    using System;

    [AttributeUsage(AttributeTargets.Parameter)]
    [Microsoft.Azure.WebJobs.Description.Binding]
    public sealed class OpenAIFunctionCallTriggerAttribute : Microsoft.Azure.Functions.Worker.Extensions.Abstractions.TriggerBindingAttribute
    {
        public OpenAIFunctionCallTriggerAttribute(string functionName, string functionDescription) 
        {
            this.FunctionName = functionName;
            this.FunctionDescription = functionDescription;
        }

        public string FunctionName { get; set; }

        public string FunctionDescription { get; }

        public string ParameterDescriptionJson { get; set; }
    }
}
