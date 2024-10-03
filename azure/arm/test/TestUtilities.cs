using Newtonsoft.Json;
using src;
using System.Text;

namespace test
{
    internal enum DeploymentType
    {
        Customer,
        Infra,
        Hobo
    }

    internal class TestUtilities
    {
        static Random random = new Random();

        internal static string RandomString(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static Dictionary<string, string> GetBaseInput(DeploymentType deploymentType)
        {
            var result = new Dictionary<string, string>
            {
                { InputConstants.ClientIdKey, InputConstants.ClientId },
            };
            if (deploymentType == DeploymentType.Customer)
            {
                result.Add(InputConstants.SubscriptionKey, InputConstants.CustomerSubscription);
                result.Add(InputConstants.ResourceGroupKey, InputConstants.CustomerResourceGroup);
            }
            else if (deploymentType == DeploymentType.Infra)
            {
                result.Add(InputConstants.SubscriptionKey, InputConstants.InfraSubscription);
                result.Add(InputConstants.ResourceGroupKey, InputConstants.InfraResourceGroup);

            }
            else if (deploymentType == DeploymentType.Hobo)
            {
                result.Add(InputConstants.SubscriptionKey, InputConstants.HoBoSubscription);
                result.Add(InputConstants.ResourceGroupKey, InputConstants.HoBoResourceGroup);
            }
            else
            {
                throw new NotImplementedException(deploymentType.ToString());
            }
            return result;
        }

        internal static StringContent? ConstructContent(ArmTemplate template, DeploymentType deploymentType)
        {
            var jsonString = JsonConvert.SerializeObject(template, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var inputDto = new AdminProcessDto();
            inputDto.Inputs = GetBaseInput(deploymentType);
            inputDto.Inputs.Add(InputConstants.DeploymentKey, RandomString());
            inputDto.Inputs.Add(InputConstants.TemplateKey, jsonString);

            var payload = System.Text.Json.JsonSerializer.Serialize(inputDto);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            return content;
        }

        internal static Dictionary<string, string> GetEnvironmentTags()
        {
            return new Dictionary<string, string>
            {
                {ComputeConstants.HostTypeTagName, ComputeConstants.HostTypeTagValue },
                {ComputeConstants.ResourceOwnerTagName, ComputeConstants.ResourceOwnerTagValue },
            };
        }
    }
}
