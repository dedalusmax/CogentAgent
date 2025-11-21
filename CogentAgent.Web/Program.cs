using Azure.AI.OpenAI;
using CogentAgent.Web.Components;
using CogentAgent.Web.Models;
using CogentAgent.Web.Services;
using CogentAgent.Web.Services.Ingestion;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

var token = builder.Configuration["AZURE_OPENAI_KEY"]!;
var endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"]!;
var model = builder.Configuration["AZURE_OPENAI_DEPLOYMENT"]!;
var embeddingModel = builder.Configuration["AZURE_OPENAI_EMBEDDING_DEPLOYMENT"]!;

var openAIClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(token));

var chatClient = openAIClient
    .GetChatClient(model)
    .AsIChatClient();

var embeddingGenerator = openAIClient
    .GetEmbeddingClient(embeddingModel)
    .AsIEmbeddingGenerator();

var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "vector-store.db");
var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
builder.Services.AddSqliteCollection<string, IngestedChunk>("data-cogentagent_web-chunks", vectorStoreConnectionString);
builder.Services.AddSqliteCollection<string, IngestedDocument>("data-cogentagent_web-documents", vectorStoreConnectionString);

builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();

builder.Services.AddChatClient(chatClient)
    .UseFunctionInvocation()
    .UseLogging();

builder.Services.AddEmbeddingGenerator(embeddingGenerator);

builder.Services.AddSingleton<CogentGame>();

builder.AddAIAgent("LoreMaster", (sp, key) => 
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var aiAgent = chatClient.CreateAIAgent(name: key, instructions: Prompts.ADVISOR_INSTRUCTIONS);
    return aiAgent;
});

builder.AddAIAgent("DungeonMaster", (sp, key) => 
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var game = sp.GetRequiredService<CogentGame>();
    var loreMasterAgent = sp.GetRequiredKeyedService<AIAgent>("LoreMaster");

    var aiAgent = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = key,
                Instructions = Prompts.DM_INSTRUCTIONS,
                ChatOptions = new ChatOptions
                {
                    Tools = [
                        loreMasterAgent.AsAIFunction(),
                        AIFunctionFactory.Create(game.InitializeGame),
                        AIFunctionFactory.Create(game.AddCharacter),
                        AIFunctionFactory.Create(game.ApplySkillCheck),
                        AIFunctionFactory.Create(game.NarrateScene),
                        AIFunctionFactory.Create(game.RespondToAction),
                        AIFunctionFactory.Create(game.RollDice),
                    ],
                }
            });

    return aiAgent;
});

var workflow = builder.AddWorkflow("my-workflow", (sp, key) =>
{
    var loreMaster = sp.GetRequiredKeyedService<AIAgent>("LoreMaster");
    var dungeonMaster = sp.GetRequiredKeyedService<AIAgent>("DungeonMaster");
    return AgentWorkflowBuilder.BuildSequential(key, [loreMaster, dungeonMaster]);
})
.AddAsAIAgent();

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// By default, we ingest PDF files from the /wwwroot/Data directory. You can ingest from
// other sources by implementing IIngestionSource.
// Important: ensure that any content you ingest is trusted, as it may be reflected back
// to users or could be a source of prompt injection risk.
await DataIngestor.IngestDataAsync(
    app.Services,
    new PDFDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data")));

app.MapOpenAIResponses();
app.MapOpenAIConversations();

if (app.Environment.IsDevelopment())
{
    app.MapDevUI();
}

app.Run();
