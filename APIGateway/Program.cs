using System.Net.Http.Headers;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ───── Получение переменных окружения ─────
builder.Configuration.AddEnvironmentVariables();
var storageUrl = Environment.GetEnvironmentVariable("Downstream__Storage");
var analyzerUrl = Environment.GetEnvironmentVariable("Downstream__Analyzer");

Console.WriteLine("ENV Storage: " + storageUrl);
Console.WriteLine("ENV Analyzer: " + analyzerUrl);

// ───── Проверка наличия переменных ─────
if (string.IsNullOrWhiteSpace(storageUrl) || string.IsNullOrWhiteSpace(analyzerUrl))
{
    Console.WriteLine("ОШИБКА: Не заданы переменные окружения Downstream__Storage и/или Downstream__Analyzer");
    return;
}

// ───── 1. HttpClient конфигурация ─────
builder.Services.AddHttpClient("FS", c =>
{
    c.BaseAddress = new Uri(storageUrl);
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient("FA", c =>
{
    c.BaseAddress = new Uri(analyzerUrl);
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// ───── 2. Swagger конфигурация ─────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("gateway", new OpenApiInfo { Title = "Gateway API", Version = "v1" });
});

var app = builder.Build();

// ───── 3. Проксирование Swagger JSON (ДО UseSwagger) ─────
app.MapWhen(ctx => ctx.Request.Path == "/swagger/file-storing/swagger.json", sub =>
{
    sub.Run(async ctx =>
    {
        Console.WriteLine("📄 Проксируем Swagger JSON FileStoring");
        await ctx.Proxy("FS", "/swagger/v1/swagger.json");
    });
});

app.MapWhen(ctx => ctx.Request.Path == "/swagger/file-analysis/swagger.json", sub =>
{
    sub.Run(async ctx =>
    {
        Console.WriteLine("📄 Проксируем Swagger JSON FileAnalysis");
        await ctx.Proxy("FA", "/swagger/v1/swagger.json");
    });
});

// ───── 4. Swagger UI ─────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/gateway/swagger.json", "Gateway API");
    c.SwaggerEndpoint("/swagger/file-storing/swagger.json", "File Storing Service");
    c.SwaggerEndpoint("/swagger/file-analysis/swagger.json", "File Analysis Service");
});

// ───── 5. FileStoring endpoints ─────
app.MapPost("/files/upload", ctx => ctx.Proxy("FS", "/files/upload")); // ← исправлено
app.MapGet("/files/file/{id:guid}", ctx => ctx.Proxy("FS", $"/files/file/{ctx.GetRouteValue("id")}")); // ← ок

// ───── 6. FileAnalysis endpoints ─────
app.MapPost("/files/analysis/{id:guid}/start", ctx => ctx.Proxy("FA", $"/files/analysis/{ctx.GetRouteValue("id")}/start"));
app.MapGet("/files/analysis/{id:guid}", ctx => ctx.Proxy("FA", $"/files/analysis/{ctx.GetRouteValue("id")}"));
app.MapGet("/files/analysis/{id:guid}/wordcloud", ctx => ctx.Proxy("FA", $"/files/analysis/{ctx.GetRouteValue("id")}/wordcloud"));
app.MapPost("/scan/{id:guid}", ctx => ctx.Proxy("FA", $"/scan/{ctx.GetRouteValue("id")}"));
app.MapGet("/scan/{id:guid}", ctx => ctx.Proxy("FA", $"/scan/{ctx.GetRouteValue("id")}"));
app.MapGet("/scan/{id:guid}/cloud", ctx => ctx.Proxy("FA", $"/scan/{ctx.GetRouteValue("id")}/cloud"));


// ───── 7. Healthcheck ─────
app.MapGet("/health", () => Results.Ok("Gateway is healthy"));

app.Run();

// ───── 8. Прокси-хелпер ─────
static class ProxyExtensions
{
    private static readonly HashSet<string> HopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "connection", "keep-alive", "proxy-authenticate", "proxy-authorization",
        "te", "trailer", "transfer-encoding", "upgrade", "content-length"
    };

    public static async Task Proxy(this HttpContext ctx, string clientName, string path)
    {
        var factory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(clientName);

        var request = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), path);

        if (ctx.Request.ContentLength > 0 || ctx.Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            request.Content = new StreamContent(ctx.Request.Body);
            if (ctx.Request.ContentType != null)
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(ctx.Request.ContentType);
        }

        foreach (var (k, v) in ctx.Request.Headers)
            if (!HopHeaders.Contains(k))
                request.Headers.TryAddWithoutValidation(k, v.ToArray());

        HttpResponseMessage response;
        try
        {
            Console.WriteLine($"➡ Проксируем запрос: {clientName} {ctx.Request.Method} {path}");
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ctx.RequestAborted);
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = 503;
            await ctx.Response.WriteAsync($"Сервис {clientName} временно недоступен: {ex.Message}");
            Console.WriteLine($"Ошибка при проксировании к {clientName}: {ex}");
            return;
        }

        ctx.Response.StatusCode = (int)response.StatusCode;

        ctx.Response.OnStarting(() =>
        {
            foreach (var (k, v) in response.Headers)
                if (!HopHeaders.Contains(k))
                    ctx.Response.Headers[k] = v.ToArray();
            foreach (var (k, v) in response.Content.Headers)
                if (!HopHeaders.Contains(k))
                    ctx.Response.Headers[k] = v.ToArray();
            return Task.CompletedTask;
        });

        await response.Content.CopyToAsync(ctx.Response.Body);
    }
}
