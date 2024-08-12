using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.Runtime;

namespace Azure.AI.Runtime.Functions
{
    internal class FunctionCallTriggerBinding : ITriggerBinding
    {
        private string functionName;
        private string functionDescription;
        private string paramterInfoJson;
        private FunctionInvoker invoker;
        private readonly ParameterInfo parameterInfo;
        private readonly OpenAIFunctionCallTriggerAttribute attribute;
        
        public Type TriggerValueType => typeof(string);

        public IReadOnlyDictionary<string, Type> BindingDataContract { get; } =
            new Dictionary<string, Type>
            {
                { "$return", typeof(object).MakeByRefType() },
            };

        public FunctionCallTriggerBinding(
            string functionName,
            string functionDescription,
            string parameterInfoJson,
            OpenAIFunctionCallTriggerAttribute attribute,
            FunctionInvoker invoker,
            ParameterInfo parameterInfo)
        {
            this.functionName = functionName;
            this.functionDescription = functionDescription;
            this.paramterInfoJson = parameterInfoJson;
            this.invoker = invoker;
            this.parameterInfo = parameterInfo;
            this.attribute = attribute;
        }

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            Type destinationType = this.parameterInfo.ParameterType;
            object convertedValue;

            FunctionCallAttribute functionCallAttribute = destinationType.GetCustomAttribute<FunctionCallAttribute>();
                        
            if (functionCallAttribute != null)
            {
                convertedValue = JsonConvert.DeserializeObject((string)value, destinationType);
            }
            else
            {
                // We expect that input to always be a string value in the form {"paramName":paramValue}
                string argumentsText = (string)value;
                
                if (!string.IsNullOrEmpty(argumentsText))
                {
                    JObject argsJson = JObject.Parse(argumentsText);
                    JToken paramValue = argsJson[this.parameterInfo.Name];
                    convertedValue = paramValue?.ToObject(destinationType);
                }
                else
                {
                    // Value types in .NET can't be assigned to null, so we use Activator.CreateInstance to 
                    // create a default value of the type (example, 0 for int).
                    convertedValue = destinationType.IsValueType ? Activator.CreateInstance(destinationType) : null;
                }
            }

            SimpleValueProvider inputValueProvider = new(convertedValue, destinationType);

            Dictionary<string, object> bindingData = new(StringComparer.OrdinalIgnoreCase); // TODO: Cache
            TriggerData triggerData = new(inputValueProvider, bindingData);
            return Task.FromResult<ITriggerData>(triggerData);
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            IListener listener = new FunctionCallListener(
                this.functionName,
                this.attribute,
                this.parameterInfo,
                context,
                this.invoker);
            return Task.FromResult(listener);
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor
            {
                Name = this.parameterInfo.Name,
                Type = this.parameterInfo.ParameterType.Name
            };
        }

        class SimpleValueProvider : IValueProvider
        {
            readonly object value;
            readonly Task<object> valueAsTask;
            readonly Type valueType;

            public SimpleValueProvider(object value, Type valueType)
            {
                if (value is not null && !valueType.IsAssignableFrom(value.GetType()))
                {
                    throw new ArgumentException($"Cannot convert {value} to {valueType.Name}.");
                }

                this.value = value;
                this.valueAsTask = Task.FromResult(value);
                this.valueType = valueType;
            }

            public Type Type
            {
                get { return this.valueType; }
            }

            public Task<object> GetValueAsync()
            {
                return this.valueAsTask;
            }

            public string ToInvokeString()
            {
                return this.value.ToString();
            }
        }
    }
}
