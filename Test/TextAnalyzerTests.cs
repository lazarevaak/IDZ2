using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using Xunit;
using FileAnalisysService.Models;
using FileAnalisysService.Services;
using FileAnalisysService.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

public class TextAnalyzerTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly byte[] _response;

        public FakeHttpMessageHandler(string content)
        {
            _response = Encoding.UTF8.GetBytes(content);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(_response)
            });
        }
    }

    private class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FakeHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private AnalysisDb CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AnalysisDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AnalysisDb(options);
    }

    [Fact]
    public async Task ProcessAsync_Should_Analyze_Text_And_Store_Report()
    {
        // Arrange
        var text = "Hello world.\n\nThis is a test.";
        var handler = new FakeHttpMessageHandler(text);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
        var factory = new FakeHttpClientFactory(client);
        var db = CreateDbContext();

        var analyzer = new TextAnalyzer(factory, db);
        var fileId = Guid.NewGuid();

        // Act
        var report = await analyzer.ProcessAsync(fileId);

        // Assert
        Assert.Equal(fileId, report.FileId);
        Assert.Equal(2, report.Paragraphs);
        Assert.True(report.Words > 0);
        Assert.True(report.Characters > 0);

        var inDb = await db.Reports.FindAsync(fileId);
        Assert.NotNull(inDb);
    }

    [Fact]
    public async Task ProcessAsync_Should_Return_Cached_Report_If_Exists()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var db = CreateDbContext();
        db.Reports.Add(new ReportData
        {
            FileId = fileId,
            Hash = "HASH",
            Paragraphs = 1,
            Words = 2,
            Characters = 3
        });
        await db.SaveChangesAsync();

        var factory = new FakeHttpClientFactory(new HttpClient()); // не используется
        var analyzer = new TextAnalyzer(factory, db);

        // Act
        var report = await analyzer.ProcessAsync(fileId);

        // Assert
        Assert.Equal(1, report.Paragraphs);
        Assert.Equal(2, report.Words);
        Assert.Equal(3, report.Characters);
    }
}
