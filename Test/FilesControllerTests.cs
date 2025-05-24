using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using FileStoringService.Controllers;
using FileStoringService;
using FileStoringService.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;


public class FilesControllerTests
{
    private FilesDb CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FilesDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FilesDb(options);
    }

    private class FakeEnv : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "TestApp";
        public string WebRootPath { get; set; } = "";
        public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "FileTests");
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(Path.GetTempPath());
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Path.GetTempPath());
    }

    [Fact]
    public async Task Upload_Should_Return_BadRequest_When_File_Is_Null()
    {
        // Arrange
        var db = CreateDbContext();
        var env = new FakeEnv();
        var controller = new FilesController(db, env);

        var dto = new FileUploadDto { File = null };

        // Act
        var result = await controller.Upload(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_Should_Return_Ok_With_Id_When_File_Is_Valid()
    {
        // Arrange
        var db = CreateDbContext();
        var env = new FakeEnv();
        var controller = new FilesController(db, env);

        var content = "Hello test file";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "file", "test.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var dto = new FileUploadDto { File = file };

        // Act
        var result = await controller.Upload(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var idProp = okResult.Value?.GetType().GetProperty("id");
        Assert.NotNull(idProp);
        Assert.IsType<Guid>(idProp!.GetValue(okResult.Value));
    }
}
