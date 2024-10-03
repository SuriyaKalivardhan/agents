using Newtonsoft.Json;
using src;
using System.Net;
using System.Text;

namespace test;

public class DeploymentTests
{
    HttpClient _client;
    private static Random random = new Random();

    static readonly string BaseAddress = "http://localhost:33707";
    static readonly string RequestPath = "/rag/v1.0/adminProcess/RAGTemplate";

    static readonly string Region = "eastus2euap";
    static readonly string ClientId = "9534e9ff-faa9-4f13-9279-287d7a00f600";
    static readonly string Subscription = "ea4faa5b-5e44-4236-91f6-5483d5b17d14";
    static readonly string ResourceGroup = "suriyakcus0test";

    static readonly string DeploymentKey = "Deployment";
    static readonly string TemplateKey = "Template";

    public DeploymentTests()
    {
        _client = new HttpClient();
        _client.BaseAddress = new Uri(BaseAddress);
    }

    private static string RandomString(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static Dictionary<string, string> GetBaseInput()
    {
        return new Dictionary<string, string>
        {
            {"Subscription", Subscription},
            {"ResourceGroup", ResourceGroup },
            { "ClientId", ClientId}
        };
    }

    private static StringContent? ConstructContent(ArmTemplate template)
    {
        var jsonString = JsonConvert.SerializeObject(template, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        var inputDto = new AdminProcessDto();
        inputDto.Inputs = GetBaseInput();
        inputDto.Inputs.Add(DeploymentKey, RandomString());
        inputDto.Inputs.Add(TemplateKey, jsonString);

        var payload = System.Text.Json.JsonSerializer.Serialize(inputDto);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        return content;
    }


    [Fact]
    public async Task SubnetDeployment()
    {
        var subTemplate = ArmTemplateGenerator.CreateSubnetResource("custvnet0", "tests198", "172.16.198.0/24", new List<string>());
        var content = ConstructContent(subTemplate);

        var result = await _client.PostAsync(RequestPath, content);
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
        string resultContent = await result.Content.ReadAsStringAsync();
        Assert.NotNull(resultContent);
    }
}