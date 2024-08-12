using AgentStudio.Components;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Assistants;
using Azure.Identity;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient()    
    .AddRazorComponents()
    .AddInteractiveServerComponents();

string apiEndpoint = builder.Configuration.GetConnectionString("AZURE_OPENAI_ENDPOINT");
string apiKey = builder.Configuration.GetConnectionString("AZURE_OPENAI_KEY");
AzureOpenAIClient openAIClient = new AzureOpenAIClient(new Uri(apiEndpoint),
    new Azure.AzureKeyCredential(apiKey));

AssistantsClient client = new AssistantsClient(new Uri("https://aoai-eus-dogfood.openai.azure.com/"),
    new Azure.AzureKeyCredential("7d919ea59e85474591f60e31e4ff44ce"));
builder.Services.AddSingleton(client);
builder.Services.AddSingleton(openAIClient);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
