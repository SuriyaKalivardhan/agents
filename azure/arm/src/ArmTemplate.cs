using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src
{
    public interface IArmResourceProperties
    {
    }

    public class ArmTemplate
    {
        /*
            "$schema": "http://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json",
            "contentVersion": "1.0.0.0",
            "parameters": [{
                ...
            }]
            "resources": [{
                ...
            }]
        */

        public ArmTemplate()
        {
            this.Schema = "http://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json";
            this.ContentVersion = "1.0.0.0";
            this.Resources = new List<ResourceWithoutProperties>();
            this.Parameters = new Dictionary<string, ArmTemplateParameterInfo>();
        }

        [JsonProperty("$schema")]
        public string Schema { get; set; }

        [JsonProperty(PropertyName = "contentVersion")]
        public string ContentVersion { get; set; }

        [JsonProperty(PropertyName = "parameters")]
        public Dictionary<string, ArmTemplateParameterInfo> Parameters { get; set; }

        [JsonProperty(PropertyName = "resources")]
        public List<ResourceWithoutProperties> Resources { get; set; }

        public void AddParameter(string parameterName, string parameterType)
        {
            this.Parameters.Add(parameterName, new ArmTemplateParameterInfo(parameterType));
        }
    }

    public class ArmTemplateParameterInfo
    {
        public ArmTemplateParameterInfo()
        {
        }

        public ArmTemplateParameterInfo(string type)
        {
            this.Type = type;
        }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }

    public class ArmTemplateParameterValue
    {
        public ArmTemplateParameterValue()
        {
        }

        public ArmTemplateParameterValue(string value)
        {
            this.Value = value;
        }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }

    #region ARMResource

    public abstract class ResourceWithoutProperties
    {
        /*
            "name": "[parameters('lbName')]",
            "type": "Microsoft.Network/loadBalancers",
            "apiVersion": "2015-06-15",
            "location": "[resourceGroup().location]",
            "dependsOn": [
                "[concat('Microsoft.Network/publicIPAddresses/', parameters('publicIPAddressName'))]"
            ],
            "tags": {
                "FQPoolName": "[parameters('fqPoolNameTagValue')]"
            },
            "sku": {
                "name": "[parameters('vmSize')]",
                "capacity": "[parameters('numberOfInstances')]"
            },
            "identities": {
                ...
            },
        */

        protected ResourceWithoutProperties()
        {
        }

        protected ResourceWithoutProperties(
            string name,
            string type,
            string apiVersion,
            string location = null,
            List<string> dependsOn = null,
            Dictionary<string, string> tags = null,
            ResourceSku sku = null,
            Plan plan = null,
            object identity = null,
            List<string> zones = null,
            string kind = null)
        {
            this.Name = name;
            this.Type = type;
            this.ApiVersion = apiVersion;
            this.Location = location;
            this.DependsOn = dependsOn ?? new List<string>();
            this.Tags = tags;
            this.Sku = sku;
            this.Plan = plan;
            this.Identity = identity;
            this.Zones = zones;
            this.Kind = Kind;
        }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "apiVersion")]
        public string ApiVersion { get; set; }

        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "dependsOn")]
        public List<string> DependsOn { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public Dictionary<string, string> Tags { get; set; }

        [JsonProperty(PropertyName = "sku")]
        public ResourceSku Sku { get; set; }

        [JsonProperty(PropertyName = "kind")]
        public string Kind { get; set; }

        [JsonProperty(PropertyName = "plan")]
        public Plan Plan { get; set; }

        [JsonProperty(PropertyName = "identity")]
        public object Identity { get; set; }

        [JsonProperty(PropertyName = "zones")]
        public List<string> Zones { get; set; }

        public string GenerateResourceId(string subscriptionId, string resourceGroupName)
        {
            return $@"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/{this.Type}/{this.Name}";
        }
    }

    public class ResourceSku
    {
        /*
            "name": "[parameters('vmSize')]",
            "capacity": "[parameters('numberOfInstances')]"
        */

        public ResourceSku()
        {
        }

        public ResourceSku(string name)
        {
            this.Name = name;
        }

        public ResourceSku(string name, int capacity)
        {
            this.Name = name;
            this.Capacity = capacity.ToString();
        }

        public ResourceSku(string name, string tier)
        {
            this.Name = name;
            this.Tier = tier;
        }

        public ResourceSku(string name, string tier, int capacity)
        {
            this.Name = name;
            this.Tier = tier;
            this.Capacity = capacity.ToString();
        }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "tier")]
        public string Tier { get; set; }

        [JsonProperty(PropertyName = "capacity")]
        public string Capacity { get; set; }
    }

    public class Plan
    {
        /*
            "plan": {
                "name": "",
                "publisher": ""
                "product": ""
        */

        public Plan()
        {
        }

        public Plan(string name, string publisher, string product)
        {
            name = this.Name;
            publisher = this.Publisher;
            product = this.Product;
        }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "publisher")]
        public string Publisher { get; set; }

        [JsonProperty(PropertyName = "product")]
        public string Product { get; set; }
    }

    public class ArmResource<T> : ResourceWithoutProperties
        where T : IArmResourceProperties
    {
        /*
            "name": "[parameters('lbName')]",
            "type": "Microsoft.Network/loadBalancers",
            "apiVersion": "2015-06-15",
            "location": "[resourceGroup().location]",
            "dependsOn": [
                "[concat('Microsoft.Network/publicIPAddresses/', parameters('publicIPAddressName'))]"
            ],
            "properties": {
                ...
            }
        */

        public ArmResource()
        {
        }

        public ArmResource(
            string name,
            string type,
            string apiVersion,
            string location,
            List<string> dependsOn,
            T properties,
            Dictionary<string, string> tags = null,
            ResourceSku sku = null,
            Plan plan = null,
            object identity = null,
            List<string> zones = null,
            string kind = null)
            : base(name, type, apiVersion, location, dependsOn, tags, sku, plan, identity, zones, kind)
        {
            this.Properties = properties;
        }

        [JsonProperty(Order = 0, PropertyName = "properties")]
        public T Properties { get; set; }
    }

    public class ArmResourceWithSubResource<T, T1> : ResourceWithoutProperties
        where T : IArmResourceProperties
        where T1 : IArmResourceProperties
    {
        public ArmResourceWithSubResource(
            string name,
            string type,
            string apiVersion,
            string location,
            List<string> dependsOn,
            T properties,
            Dictionary<string, string> tags = null,
            ResourceSku sku = null,
            Plan plan = null,
            object identity = null,
            List<string> zones = null,
            string kind = null)
            : base(name, type, apiVersion, location, dependsOn, tags, sku, plan, identity, zones, kind)
        {
            this.Properties = properties;
        }

        [JsonProperty(Order = 0, PropertyName = "properties")]
        public T Properties { get; set; }

        [JsonProperty(PropertyName = "resources")]
        public List<ArmSubResource<T1>> Resources { get; set; } = new List<ArmSubResource<T1>>();
    }

    public class ArmSubResource<T> : ResourceWithoutProperties
        where T : IArmResourceProperties
    {
        public ArmSubResource(
            string name,
            string type,
            string apiVersion,
            List<string> dependsOn,
            T properties,
            Dictionary<string, string> tags = null)
            : base(name, type, apiVersion, dependsOn: dependsOn, tags: tags)
        {
            this.Properties = properties;
        }

        [JsonProperty(Order = 0, PropertyName = "properties")]
        public T Properties { get; set; }
    }

    public class ArmResourceId
    {
        /*
            "id": "[variables('publicIPAddressID')]"
        */

        public ArmResourceId()
        {
        }

        public ArmResourceId(string id)
        {
            this.Id = id;
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }

    public class ARMResourceUri
    {
        /*
            "uri":"https://byosub.blob.core.windows.net/vhds/mycustomubuntuvm-osDisk.54d80079-7bc0-44ef-85ac-7619f43b4c56.vhd"
        */

        public ARMResourceUri()
        {
        }

        public ARMResourceUri(string uri)
        {
            this.Uri = uri;
        }

        [JsonProperty(PropertyName = "uri")]
        public string Uri { get; set; }
    }
    # endregion

    #region Subnet
    public class SubnetProperties : IArmResourceProperties
    {
        public SubnetProperties()
        {
        }

        public SubnetProperties(
            string addressPrefix,
            string delegation)
        {
            this.AddressPrefix = addressPrefix;

            if (!string.IsNullOrWhiteSpace(delegation))
            {
                this.Delegations = new List<ArmDelegation>()
                {
                    new ArmDelegation
                    {
                        Name = delegation,
                        Properities = new DelegationProperities
                        {
                            ServiceName = delegation,
                        }
                    }
                };
            }
        }

        [JsonProperty(PropertyName = "addressPrefix")]
        public string AddressPrefix { get; set; }

        [JsonProperty(PropertyName = "networkSecurityGroup")]
        public ArmResourceId NetworkSecurityGroup { get; set; }

        [JsonProperty(PropertyName = "privateEndpointNetworkPolicies")]
        public string PrivateEndpointNetworkPolicies { get; set; }

        [JsonProperty(PropertyName = "serviceEndpointPolicies")]
        public List<ArmResourceId> ServiceEndpointPolicies { get; set; }

        [JsonProperty(PropertyName = "delegations")]
        public List<ArmDelegation> Delegations { get; set; }
    }

    public class ArmDelegation
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public DelegationProperities Properities { get; set; }
    }

    public class DelegationProperities : IArmResourceProperties
    {
        [JsonProperty(PropertyName = "serviceName")]
        public string ServiceName { get; set; }
    }

    public class Subnet : IArmResourceProperties
    {

        public Subnet()
        {
        }

        public Subnet(string name, SubnetProperties properties)
        {
            this.Name = name;
            this.Properties = properties;
        }

        [JsonProperty(PropertyName = "properties")]
        public SubnetProperties Properties { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
    # endregion

    #region ApplicationEnvironment

    public class ApplicationEnvironmentProperties : IArmResourceProperties
    {
        public ApplicationEnvironmentProperties()
        {
        }

        public ApplicationEnvironmentProperties(string workloadProfileType, string infrastructureSubnetId, string customerSubnetId, bool enablePrivate)
        {
            this.WorkloadProfiles = new List<ApplicationEnvironmentWorkdloadProfile>
        {
            new ApplicationEnvironmentWorkdloadProfile
            {
                Name = workloadProfileType,
                WorkloadProfileType = workloadProfileType
            }
        };

            if (!string.IsNullOrWhiteSpace(infrastructureSubnetId))
            {
                this.VnetConfiguration = new ApplicationEnvironmentVnetConfiguration
                {
                    InfrastructureSubnetId = infrastructureSubnetId,
                    Internal = enablePrivate
                };
            }

            if (!string.IsNullOrWhiteSpace(customerSubnetId))
            {
                this.FirstPartyConfiguration = new ApplicationEnvironmentFirstPartyConfiguration
                {
                    CustomerSubnetId = customerSubnetId,
                    PullImageViaEnvironmentSubnet = enablePrivate
                };
            }
        }

        [JsonProperty(PropertyName = "workloadProfiles")]
        public List<ApplicationEnvironmentWorkdloadProfile> WorkloadProfiles { get; set; }

        [JsonProperty(PropertyName = "vnetConfiguration")]
        public ApplicationEnvironmentVnetConfiguration VnetConfiguration { get; set; }

        [JsonProperty(PropertyName = "firstPartyConfiguration")]
        public ApplicationEnvironmentFirstPartyConfiguration FirstPartyConfiguration { get; set; }
    }

    public class ApplicationEnvironmentWorkdloadProfile
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "workloadProfileType")]

        public string WorkloadProfileType { get; set; }
    }

    public class ApplicationEnvironmentVnetConfiguration
    {
        [JsonProperty(PropertyName = "infrastructureSubnetId")]
        public string InfrastructureSubnetId { get; set; }

        [JsonProperty(PropertyName = "internal")]
        public bool Internal { get; set; }
    }

    public class ApplicationEnvironmentFirstPartyConfiguration
    {
        [JsonProperty(PropertyName = "customerSubnetId")]
        public string CustomerSubnetId { get; set; }

        [JsonProperty(PropertyName = "pullImageViaEnvironmentSubnet")]
        public bool PullImageViaEnvironmentSubnet { get; set; }
    }
    #endregion

    #region ContainerApplication

    public class ContainerApplicationProperties : IArmResourceProperties
    {
        public ContainerApplicationProperties()
        {
        }

        public ContainerApplicationProperties(string appName, string envId, int targetPort, string containerImage, string workloadProfileName)
        {
            this.EnvironmentId = envId;
            this.configuration = new ContainerApplicationConfiguration
            {
                Ingress = new ContainerApplicationIngress
                {
                    External = true,
                    Transport = "Auto",
                    AllowInsecure = true,
                    TargetPort = targetPort,
                    StickySessions = new ContainerApplicationStickySessions
                    {
                        Affinity = "none"
                    }
                }
            };
            this.Template = new ContainerApplicationTemplate
            {
                Containers = new List<ContainerApplicationContainer>
            {
                new ContainerApplicationContainer
                {
                    Name = appName,
                    Image = containerImage,
                    Resources = new ContainerApplicationContainerResources
                    {
                        CPU = "0.5",
                        Memory = "1Gi"
                    }
                }
            },
                Scale = new ContainerApplicationScale
                {
                    MinReplicas = 1
                }
            };
            this.WorkloadProfileName = workloadProfileName;

        }

        [JsonProperty(PropertyName = "environmentId")]
        public string EnvironmentId { get; set; }

        [JsonProperty(PropertyName = "configuration")]
        public ContainerApplicationConfiguration configuration { get; set; }

        [JsonProperty(PropertyName = "template")]
        public ContainerApplicationTemplate Template { get; set; }

        [JsonProperty(PropertyName = "workloadProfileName")]
        public string WorkloadProfileName { get; set; }
    }

    public class ContainerApplicationConfiguration
    {
        [JsonProperty(PropertyName = "ingress")]
        public ContainerApplicationIngress Ingress { get; set; }
    }

    public class ContainerApplicationIngress
    {
        [JsonProperty(PropertyName = "external")]
        public bool External { get; set; }

        [JsonProperty(PropertyName = "transport")]
        public string Transport { get; set; }

        [JsonProperty(PropertyName = "allowInsecure")]
        public bool AllowInsecure { get; set; }

        [JsonProperty(PropertyName = "targetPort")]
        public int TargetPort { get; set; }

        [JsonProperty(PropertyName = "stickySessions")]
        public ContainerApplicationStickySessions StickySessions { get; set; }
    }

    public class ContainerApplicationStickySessions
    {
        [JsonProperty(PropertyName = "affinity")]
        public string Affinity { get; set; }
    }

    public class ContainerApplicationTemplate
    {
        [JsonProperty(PropertyName = "containers")]
        public List<ContainerApplicationContainer> Containers { get; set; }

        [JsonProperty(PropertyName = "scale")]
        public ContainerApplicationScale Scale { get; set; }
    }

    public class ContainerApplicationContainer
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "image")]
        public string Image { get; set; }

        [JsonProperty(PropertyName = "resources")]
        public ContainerApplicationContainerResources Resources { get; set; }
    }

    public class ContainerApplicationContainerResources
    {
        [JsonProperty(PropertyName = "cpu")]
        public string CPU { get; set; }

        [JsonProperty(PropertyName = "memory")]
        public string Memory { get; set; }
    }

    public class ContainerApplicationScale
    {
        [JsonProperty(PropertyName = "minReplicas")]
        public int MinReplicas { get; set; }
    }
    #endregion

    #region SubResource

    public class SubResource
    {
        public SubResource()
        {
        }

        public SubResource(string id)
        {
            this.Id = id;
        }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    #endregion
}