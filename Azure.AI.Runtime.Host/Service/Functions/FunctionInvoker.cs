using Azure.AI.OpenAI.Assistants;
using Azure.AI.Runtime.Messaging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using Azure.AI.Runtime;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Azure.AI.Runtime.Functions
{
    internal interface IFunctionInvoker
    {
        Task<string> InvokeAsync(RequiredFunctionToolCall call, CancellationToken cancellationToken);
    }

    public class FunctionInvoker : IFunctionInvoker
    {
        record Skill(
            string Name,
            OpenAIFunctionCallTriggerAttribute Attribute,
            ParameterInfo Parameter,
            Microsoft.Azure.WebJobs.Host.Executors.ITriggeredFunctionExecutor Executor);

        readonly ILogger logger;

        readonly Dictionary<string, Skill> skills = new(StringComparer.OrdinalIgnoreCase);

        public FunctionInvoker(LoggerFactory loggerFactory)
        {
            if(loggerFactory == null)
            {
                loggerFactory = new LoggerFactory();
            }
            this.logger = loggerFactory.CreateLogger<FunctionInvoker>();
        }       

        public FunctionToolDefinition GetSystemToolDefinition()
        {
            MethodInfo mInfo = typeof(MessageBroker).GetMethod("__SendAgentMessage__");

            ParameterInfo pInfo = mInfo.GetParameters().Single();

            OpenAIFunctionCallTriggerAttribute attribute = pInfo.GetCustomAttribute<OpenAIFunctionCallTriggerAttribute>();


            Skill systemSkill = new Skill("SendAgentMessage",
                attribute,
                pInfo,
                null);
                
            // The parameters can be defined in the attribute JSON or can be inferred from
            // the .NET (in-proc) function signature, if applicable.
            string parametersJson = systemSkill.Attribute.ParameterDescriptionJson ??
            JsonConvert.SerializeObject(GetParameterDefinition(systemSkill));

            return new FunctionToolDefinition(systemSkill.Name, systemSkill.Attribute.FunctionDescription, BinaryData.FromBytes(Encoding.UTF8.GetBytes(parametersJson)));
        }

        public Task<FunctionToolDefinition[]> ListFunctionsInternal()
        {
            if (this.skills.Count == 0)
            {                
                return Task.FromResult(new FunctionToolDefinition[] { });
            }

            IList<FunctionToolDefinition> functions = new List<FunctionToolDefinition>(capacity: this.skills.Count);
            foreach (Skill skill in this.skills.Values)
            {
                // The parameters can be defined in the attribute JSON or can be inferred from
                // the .NET (in-proc) function signature, if applicable.
                string parametersJson = skill.Attribute.ParameterDescriptionJson ??
                    JsonConvert.SerializeObject(GetParameterDefinition(skill));

                functions.Add(new FunctionToolDefinition(skill.Name, skill.Attribute.FunctionDescription, BinaryData.FromBytes(Encoding.UTF8.GetBytes(parametersJson))));
            }

            return Task.FromResult(functions.ToArray());
        }


        async Task<string> IFunctionInvoker.InvokeAsync(RequiredFunctionToolCall call, CancellationToken cancellationToken)
        {
            if (call is null)
            {
                throw new ArgumentNullException(nameof(call));
            }

            if (call.Name is null)
            {
                throw new ArgumentException("The function call must have a name", nameof(call));
            }

            if (!this.skills.TryGetValue(call.Name, out Skill skill))
            {
                return string.Format($"No skill registered with name '{0}, please check your function call signature'", call.Name); 
                throw new InvalidOperationException($"No skill registered with name '{call.Name}'");
            }

            // This call may throw if the Functions host is shutting down or if there is an internal error
            // in the Functions runtime. We don't currently try to handle these exceptions.
            object skillOutput = null;
            Microsoft.Azure.WebJobs.Host.Executors.FunctionResult result = await skill.Executor.TryExecuteAsync(
                new Microsoft.Azure.WebJobs.Host.Executors.TriggeredFunctionData
                {
                    TriggerValue = call.Arguments,
#pragma warning disable CS0618 // Approved for use by this extension
                    InvokeHandler = async userCodeInvoker =>
                    {
                        // We yield control to ensure this code is executed asynchronously relative to WebJobs.
                        // This ensures WebJobs is able to correctly cancel the invocation in the case of a timeout.
                        await Task.Yield();

                        // Invoke the function and attempt to get the result.
                        this.logger.LogInformation("Invoking user-code function '{0}'", call.Name);
                        Task invokeTask = userCodeInvoker.Invoke();
                        if (invokeTask is not Task<object> resultTask)
                        {
                            throw new InvalidOperationException(
                                "The WebJobs runtime returned a invocation task that does not support return values!");
                        }

                        skillOutput = await resultTask;
                    }
#pragma warning restore CS0618
                },
                cancellationToken);

            // If the function threw an exception, rethrow it here. This will cause the caller (e.g., the
            // assistant service) to receive an error response, which it should be prepared to catch and handle.
            if (result.Exception is not null)
            {
                ExceptionDispatchInfo.Throw(result.Exception);
            }

            if (skillOutput is null)
            {
                return null;
            }

            // Convert the output to JSON
            string jsonResult = JsonConvert.SerializeObject(skillOutput);
            this.logger.LogInformation(
                "Returning output of user-code function '{0}' as JSON: {1}", call.Name, jsonResult);
            return jsonResult;

        }

        internal void RegisterSkill(
            string name,
            OpenAIFunctionCallTriggerAttribute attribute,
            ParameterInfo parameter,
            Microsoft.Azure.WebJobs.Host.Executors.ITriggeredFunctionExecutor executor)
        {
            this.logger.LogInformation(string.Format("Registering skill '{0}'", name));
            this.skills.Add(name, new Skill(name, attribute, parameter, executor));
        }

        internal void UnregisterSkill(string name)
        {
            this.logger.LogInformation(string.Format("Unregistering skill '{0}'", name));
            this.skills.Remove(name);
        }

        static Dictionary<string, object> GetParameterDefinition(Skill skill)
        {
            // Try to infer from the .NET parameter type (only works with in-proc WebJobs)
            string type;
            switch (skill.Parameter.ParameterType)
            {
                case Type t when t == typeof(string):
                    type = "string";
                    break;
                case Type t when t == typeof(int):
                    type = "integer";
                    break;
                case Type t when t == typeof(bool):
                    type = "boolean";
                    break;
                case Type t when t == typeof(float):
                    type = "number";
                    break;
                case Type t when t == typeof(double):
                    type = "number";
                    break;
                case Type t when t == typeof(decimal):
                    type = "number";
                    break;
                case Type _ when typeof(System.Collections.IEnumerable).IsAssignableFrom(skill.Parameter.ParameterType):
                    type = "array";
                    break;                    
                default:
                    if (skill.Parameter.ParameterType.GetCustomAttribute<FunctionCallAttribute>() != null)
                    {
                        Dictionary<string, object> propertyDictionary = new Dictionary<string, object>();
                        IList<string> requiredParameters = new List<string>();

                        foreach (PropertyInfo memberInfo in skill.Parameter.ParameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                        {
                            Dictionary<string, string> paramDefinition = new Dictionary<string, string>();
                            paramDefinition.Add("type", memberInfo.PropertyType == typeof(string) ? "string" : "number");
                            DescriptionAttribute? descriptionAttribute = memberInfo.GetCustomAttribute<DescriptionAttribute>();
                            if (descriptionAttribute != null)
                            {
                                paramDefinition.Add("description", descriptionAttribute.Description);
                            }
                            RequiredAttribute? requiredAttribute = memberInfo.GetCustomAttribute<RequiredAttribute>();
                            
                            if(requiredAttribute != null)
                            {
                                requiredParameters.Add(memberInfo.Name);
                            }
                            propertyDictionary.Add(memberInfo.Name, paramDefinition);
                        }

                        return new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = propertyDictionary,
                            ["required"] = requiredParameters.ToArray()
                        };
                    }
                    else
                    {
                        type = "string";
                    }
                    break;
            }

            // Schema reference: https://platform.openai.com/docs/api-reference/chat/create#chat-create-tools
            return new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    [skill.Parameter.Name] = new { type }
                }
            };
        }

        
    }
}
