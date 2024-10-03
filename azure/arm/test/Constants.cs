namespace test
{
    internal static class ComputeConstants
    {
        internal const string AppEnvironmentDelegation = "Microsoft.App/environments";
        internal const string WorkloadProfileType = "Consumption";

        //internal const string DataProxyContainerImage = "agentdataproxyacr.azurecr.io/agents/data-proxy:v0.100"; //TODO: Suriya Update real ACR image
        //internal const int DataProxyPort = 8086;
        internal const string DataProxyContainerImage = "docker.io/suriyakalivardhan/simpleserver:v2"; //TODO: Suriya Update real ACR image
        internal const int DataProxyPort = 8765;

        internal const string HostTypeTagName = "internal.containerapps.host-type";
        internal const string HostTypeTagValue = "shared";
        internal const string ResourceOwnerTagName = "internal.containerapps.resource-owner";
        internal const string ResourceOwnerTagValue = "suriyak-test";  //AIAgent for prod, suriyak-test for test
    }
    internal static class InputConstants
    {

        internal const string BaseAddress = "http://localhost:33707";
        internal const string RequestPath = "/rag/v1.0/adminProcess/RAGTemplate";
         
        internal const string Region = "eastus2euap";
        internal const string ClientId = "9534e9ff-faa9-4f13-9279-287d7a00f600";

        internal const string CustomerSubscription = "ea4faa5b-5e44-4236-91f6-5483d5b17d14";
        internal const string CustomerResourceGroup = "suriyakcus0test";

        internal const string InfraSubscription = "921496dc-987f-410f-bd57-426eb2611356";
        internal const string InfraResourceGroup = "suriyakinfra0";

        internal const string HoBoSubscription = "6a6fff00-4464-4eab-a6b1-0b533c7202e0";
        internal const string HoBoResourceGroup = "suriyakhobotest";

        internal const string ClientIdKey = "ClientId";
        internal const string SubscriptionKey = "Subscription";
        internal const string ResourceGroupKey = "ResourceGroup";
        internal const string DeploymentKey = "Deployment";
        internal const string TemplateKey = "Template";

        internal const int PrefixId = 197;
    }
}
