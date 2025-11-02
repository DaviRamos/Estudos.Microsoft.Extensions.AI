using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OllamaSharp;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();
var env = app.Environment;

IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

var client =
    env.IsDevelopment()
        ? new OllamaApiClient(new Uri("http://localhost:11434/"), "phi3:latest")
        : new OpenAI.Chat.ChatClient("gpt-4o-mini", openAiKey)
            .AsIChatClient();

var cachedClient = new ChatClientBuilder(client)
    .UseDistributedCache(cache)
    .Build();

app.MapPost("/", async (Question question) =>
{
    var result = await client.GetResponseAsync(question.Prompt);
    return Results.Ok(result.Text);
});

app.MapPost("/v2", async (Question question) =>
{
    var result = await client.GetResponseAsync(
    [
        new ChatMessage(ChatRole.System, "You are a very technical weather expert. Answer me with just 10 words."),
        new ChatMessage(ChatRole.User, question.Prompt),
    ]);
    return Results.Ok(result.Text);
});

app.MapPost("/v3", async (Question question) =>
{
    var result = await cachedClient.GetResponseAsync(
    [
        new ChatMessage(ChatRole.System, "You are a very technical weather expert. Answer me with just 10 words."),
        new ChatMessage(ChatRole.User, question.Prompt),
    ]);
    return Results.Ok(result.Text);
});

app.MapGet("/",() => "Hello World!");

app.Run();

public record Question(string Prompt);