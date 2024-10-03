namespace src
{
    public class TemplateSettings
    {
        public string ApiVersion { get; set; }

        public string ResourceType { get; set; }
    }

    public class ApplicationEnvironmentSettings : TemplateSettings
    {
    }

    public class ContainerApplicationSettings : TemplateSettings
    {
    }

    public class SubnetSettings : TemplateSettings
    {
    }

    public class ArmTemplateGenerator
    {
        static ApplicationEnvironmentSettings ApplicationEnvironmentSettings = new ApplicationEnvironmentSettings
        {

            ResourceType = "Microsoft.App/managedEnvironments",
            ApiVersion = "2024-02-02-preview"
        };
        static ContainerApplicationSettings ContainerApplicationSettings = new ContainerApplicationSettings
        {
            ResourceType = "Microsoft.App/containerapps",
            ApiVersion = "2024-02-02-preview"
        };
        static SubnetSettings SubnetSettings = new SubnetSettings
        {
            ApiVersion = "2023-11-01",
            ResourceType = "Microsoft.Network/virtualNetworks/subnets"
        };

        public static ArmResource<ApplicationEnvironmentProperties> CreateApplicationEnvironmentResource(
            string location,
            string name,
            string workloadProfileType,
            string infrastructureSubnetId,
            string customerSubnetId,
            bool enablePrivate,
            Dictionary<string, string> tags)
        {
            var properties = new ApplicationEnvironmentProperties(workloadProfileType, infrastructureSubnetId, customerSubnetId, enablePrivate);

            return new ArmResource<ApplicationEnvironmentProperties>(
                name,
                ApplicationEnvironmentSettings.ResourceType,
                ApplicationEnvironmentSettings.ApiVersion,
                location,
                null,
                properties,
                tags);
        }

        public static ArmResource<ContainerApplicationProperties> CreateContainerApplicationResource(
            string location,
            string name,
            string envId,
            int targetPort,
            string containerImage,
            string workloadProfileName,
            Dictionary<string, string> tags)
        {
            var properties = new ContainerApplicationProperties(name, envId, targetPort, containerImage, workloadProfileName);

            return new ArmResource<ContainerApplicationProperties>(
                name,
                ContainerApplicationSettings.ResourceType,
                ContainerApplicationSettings.ApiVersion,
                location,
                null,
                properties,
                tags);
        }

        public static ArmSubResource<SubnetProperties> CreateSubnetResource(
            string virtualNetworkName,
            string name,
            string addressPrefix,
            List<string> dependsOn)
        {
            return new ArmSubResource<SubnetProperties>(
                name: $"{virtualNetworkName}/{name}",
                type: SubnetSettings.ResourceType,
                apiVersion: SubnetSettings.ApiVersion,
                properties: new SubnetProperties(addressPrefix, "Microsoft.App/environments"),
                dependsOn: dependsOn);
        }

        public static string GetDependsOnString(string resourceType, string resourceName)
        {
            return $"[concat('{resourceType}/', '{resourceName}')]";
        }
    }
}
