using FileAnalisysService;
using FileAnalisysService.Data;
using FileAnalisysService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddHealthChecks();

builder.Services.AddDbContext<AnalysisDb>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("AnalysisDb")));

builder.Services.AddHttpClient("Storage", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["FileStore:Url"]!);
});
builder.Services.AddHttpClient("Cloud", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["CloudGen:Url"]!);
});

builder.Services.AddScoped<TextAnalyzer>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AnalysisDb>();
    db.Database.Migrate();
}

app.Run();
