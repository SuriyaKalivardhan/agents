namespace Azure.AI.Runtime.Host.UserFunctions
{
    using Azure.AI.Runtime;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel;    

    public class GeneveActions
    {
        [FunctionCall]
        public sealed class GetModelScaleSetInformationAction
        {
            [Required]
            [Description("Subscription Id Of The customer resource, this is not infrastructure subscription")]
            public string SubscriptionId { get; set; }

            [Description("Endpoint information of customer, if available")]
            public string EndpointResourceId { get; set; }
        }

        [FunctionCall]
        public sealed class GetVMScaleSetInformationAction
        {
            [Required]
            [Description("Identifier of the model pool.")]
            public string ModelPoolId { get; set; }

            [Description("Identifier of the model scale set inside the model pool.")]
            public string ModelScaleSetId { get; set; }
        }

        [FunctionCall]
        public sealed class GetVMScaleSetHealthAction
        {
            [Required]
            [Description("Subscription Id Of The infrastrcuture resource, this is not customer subscription")]
            public string SubscriptionId { get; set; }

            [Required]
            [Description("VM Scale Set Id Of The infrastructure resource")]
            public string VMSSId { get; set; }
        }

        [FunctionCall]
        public sealed class RestartNodeAction
        {
            [Required]
            [Description("Subscription Id Of The infrastrcuture resource, this is not customer subscription")]
            public string SubscriptionId { get; set; }

            [Required]
            [Description("VM Scale Set Id Of The infrastructure resource")]
            public string VMSSId { get; set; }

            [Required]
            [Description("Node Instance Id")]
            public string InstanceId { get; set; }

            [Required]
            [Description("Azure Region Where The infrastrucrue resource is located")]
            public string Region { get; set; }
        }

        private readonly ILogger<GeneveActions> _logger;
        IList<string> unhealthyNodes = new List<string>();

        public GeneveActions(ILogger<GeneveActions> logger)
        {
            _logger = logger;
            unhealthyNodes.Add("Node_5");
        }

        [FunctionName(nameof(GetModelScaleSetInformation))]
        public Task<string> GetModelScaleSetInformation(
            [OpenAIFunctionCallTrigger(
                    "GetModelScaleSetInformation",
                    @"This is a readonly operation. Given a customer public resource information like customer subsription and resource id, this operation will return internal resource
                      details which is model scale set associated with customer resource. This is helpful to get information about underlying physical resources like VMs, network and other low level resource details")]
                    GetModelScaleSetInformationAction mssInfoAction)
        {
            return Task.FromResult(string.Format("Found the customer resource {0}, it is placed in infrastructure in region {1} it is placed on model pool {2} and model scale set {3}",
                mssInfoAction.SubscriptionId,
                "East US",
                "GPT4-0-EASTUS",
                "MSS-PTUM-01"));
        }

        [FunctionName(nameof(GetVMScaleSetInformation))]
        public Task<string> GetVMScaleSetInformation(
            [OpenAIFunctionCallTrigger(
                    "GetVMScaleSetInformation",
                    @"This is a readonly operation. Given a model scale set information and region, this operation will return internal resource
                      details associated with customer resource. This is helpful to get information about underlying physical resources like VMs, network and other low level resource details")]
                    GetVMScaleSetInformationAction vmssInfoAction)
        {
            return Task.FromResult(string.Format("Model scale set {0} in region {1} is placed on infrastructure subscription {2} and infrastructure VMSS {3}",
                "MSS-PTUM-01",
                "East US",
                "infrasub176",
                "vmss190"));
        }

        [FunctionName(nameof(GetVMScaleSetHealth))]
        public Task<string> GetVMScaleSetHealth(
            [OpenAIFunctionCallTrigger(
                    "GetVMSSScaleSetHealth",
                    @"This is a readonly operation. Given a vmss scale set information, this operation will return resource health of individual node
                      that are part of VMSS. This operation is useful to identify the problematic node to restart in case of issues.")]
                    GetVMScaleSetHealthAction vmssHealthAction)
        {
            if (unhealthyNodes.Count > 0)
            {
                return Task.FromResult(string.Format("Health Information of Model Scale set {0} is {1}",
                    "MSS-PTUM-01",
                    "Unhealthy Nodes: Node_5; Healthy Nodes : Node_0,Node_1,Node_2,Node_3; "));
            }
            else
            {
                unhealthyNodes.Add("Node_5");
                return Task.FromResult("All nodes are healthy");
            }
        }

        [FunctionName(nameof(RestartNode))]
        public Task<string> RestartNode(
            [OpenAIFunctionCallTrigger(
                    "RestartNode",
                    @"Initiates restart operation on a node. Restart operation are invasive and idempotent.
                      Restart operation requires the write parameters to identify the specific node we need to restart")]
                    RestartNodeAction genevaActionInput)
        {
            if (genevaActionInput.InstanceId == "Node_5")
            {
                unhealthyNodes.Remove("Node_5");
            }
            return Task.FromResult("Node restarted succesfully");
        }
    }
}
