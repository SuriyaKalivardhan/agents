using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualBasic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Azure.AI.Runtime;

namespace Azure.AI.Runtime.Data
{
    [FunctionCall]
    public sealed class AgentMessage
    {
        public AgentMessage() { }

        public string CallingAgentId { get; set; }
                
        public string CallingThreadId { get; set; }

        public string CallingRunId { get; set; }
                
        public string CallId { get; set; }

        [Required]
        [Description("Identifier of the agent to send the message to.")]
        public string TargetAgentId { get; set; }

        [Description("Identifier of the conversation thread, this is returned as part of every response, echo this value for the subsequent conversation to resume the conversation in same context" +
            "by setting this to null, new conversation session will start with the agent.")]
        public string TargetThreadId { get; set; }

        [Required]
        [Description("Actual message to send to the target agent.")]
        public string Message { get; set; }

        public string TargetRunId { get; set; }        
    }
}
