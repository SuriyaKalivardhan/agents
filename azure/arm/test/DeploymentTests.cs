using src;
using System.Net;
using System.Security.AccessControl;

namespace test;

public class DeploymentTests
{
    HttpClient _client;
    private static Random random = new Random();

    public DeploymentTests()
    {
        _client = new HttpClient();
        _client.BaseAddress = new Uri(InputConstants.BaseAddress);
    }

    [Fact]
    public async Task CustomerSubnetDeployment()
    {
        var subTemplate = ArmTemplateGenerator.CreateSubnetResource("custvnet0", $"ctsub{InputConstants.PrefixId}", $"172.16.{InputConstants.PrefixId}.0/24", new List<string>());

        ArmTemplate armTemplate = new ArmTemplate();
        armTemplate.Resources.Add(subTemplate);
        var content = TestUtilities.ConstructContent(armTemplate, DeploymentType.Customer);

        var result = await _client.PostAsync(InputConstants.RequestPath, content);
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
        string resultContent = await result.Content.ReadAsStringAsync();
        Assert.NotNull(resultContent);
    }

    [Fact]
    public async Task InfraSubnetDeployment()
    {
        var subTemplate = ArmTemplateGenerator.CreateSubnetResource("infra-vnet0", $"itsub{InputConstants.PrefixId}", $"10.{InputConstants.PrefixId}.0.0/24", new List<string>());

        ArmTemplate armTemplate = new ArmTemplate();
        armTemplate.Resources.Add(subTemplate); 
        var content = TestUtilities.ConstructContent(armTemplate, DeploymentType.Infra);

        var result = await _client.PostAsync(InputConstants.RequestPath, content);
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
        string resultContent = await result.Content.ReadAsStringAsync();
        Assert.NotNull(resultContent);
    }

    [Fact]
    public async Task HoBoAppContainersAllDeployment()
    {
        string customerSubnetId = $"/subscriptions/{InputConstants.CustomerSubscription}/resourceGroups/{InputConstants.CustomerResourceGroup}/providers/Microsoft.Network/virtualNetworks/custvnet0/subnets/ctsub{InputConstants.PrefixId}";
        string infraSubnetId = $"/subscriptions/{InputConstants.InfraSubscription}/resourceGroups/{InputConstants.InfraResourceGroup}/providers/Microsoft.Network/virtualNetworks/infra-vnet0/subnets/itsub{InputConstants.PrefixId}";
        string envName = $"envthobo{InputConstants.PrefixId}";
        string envId = $"/subscriptions/{InputConstants.HoBoSubscription}/resourceGroups/{InputConstants.HoBoResourceGroup}/providers/Microsoft.App/managedEnvironments/{envName}";
        string appName = $"appthobo{InputConstants.PrefixId}";

        var envTemplate = ArmTemplateGenerator.CreateApplicationEnvironmentResource(
            InputConstants.Region, envName, ComputeConstants.WorkloadProfileType, infraSubnetId, customerSubnetId, false, TestUtilities.GetEnvironmentTags());
        var appTemplate = ArmTemplateGenerator.CreateContainerApplicationResource(
            InputConstants.Region, appName, envId, ComputeConstants.DataProxyPort, ComputeConstants.DataProxyContainerImage, ComputeConstants.WorkloadProfileType, null);

        appTemplate.DependsOn = new List<string>()
        {
            ArmTemplateGenerator.GetDependsOnString(envTemplate.Type, envTemplate.Name)
        };
        ArmTemplate armTemplate = new ArmTemplate();
        armTemplate.Resources.Add(envTemplate);
        armTemplate.Resources.Add(appTemplate);

        var content = TestUtilities.ConstructContent(armTemplate, DeploymentType.Hobo);

        var result = await _client.PostAsync(InputConstants.RequestPath, content);
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
        string resultContent = await result.Content.ReadAsStringAsync();
        Assert.NotNull(resultContent);
    }
}