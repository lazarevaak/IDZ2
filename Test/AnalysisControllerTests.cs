using Xunit;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using FileAnalisysService.Controllers;
using FileAnalisysService.Models;
using FileAnalisysService.Services;

public class AnalysisControllerTests
{
    // Фейковая реализация интерфейса
    private class FakeTextAnalyzer : ITextAnalyzer
    {
        public Task<ReportData> ProcessAsync(Guid fileId)
        {
            if (fileId == Guid.Empty)
                throw new InvalidOperationException("Invalid ID");

            return Task.FromResult(new ReportData
            {
                FileId = fileId,
                Paragraphs = 1,
                Words = 2,
                Characters = 3
            });
        }

        public Task<byte[]> BuildCloudAsync(Guid fileId)
        {
            return Task.FromResult(new byte[] { 1, 2, 3 });
        }
    }

    [Fact]
    public async Task Run_Should_Return_Ok_When_Id_Is_Valid()
    {
        // Arrange
        var service = new FakeTextAnalyzer();
        var controller = new AnalysisController(service);
        var id = Guid.NewGuid();

        // Act
        var result = await controller.Run(id);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var report = Assert.IsType<ReportData>(ok.Value);
        Assert.Equal(id, report.FileId);
    }

    [Fact]
    public async Task Run_Should_Return_500_When_Exception()
    {
        // Arrange
        var service = new FakeTextAnalyzer();
        var controller = new AnalysisController(service);

        // Act
        var result = await controller.Run(Guid.Empty);

        // Assert
        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
    }

    [Fact]
    public async Task Cloud_Should_Return_FileResult()
    {
        // Arrange
        var service = new FakeTextAnalyzer();
        var controller = new AnalysisController(service);
        var id = Guid.NewGuid();

        // Act
        var result = await controller.Cloud(id);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(new byte[] { 1, 2, 3 }, fileResult.FileContents);
    }
}
