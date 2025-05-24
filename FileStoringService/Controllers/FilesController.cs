using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using FileStoringService;
using FileStoringService.DTOs;

namespace FileStoringService.Controllers;

[ApiController]
[Route("files")]
public class FilesController : ControllerBase
{
    private readonly FilesDb _db;
    private readonly IWebHostEnvironment _env;

    public FilesController(FilesDb db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    /// <summary>Загрузить файл</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] FileUploadDto dto)
    {
        try
        {
            var input = dto.File;
            if (input == null) return BadRequest("Файл не получен");

            using var md5 = MD5.Create();
            using var stream = input.OpenReadStream();
            var hash = Convert.ToHexString(md5.ComputeHash(stream));

            var exists = await _db.StoredFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.HashSum == hash);

            if (exists != null)
                return Ok(new { id = exists.Id });

            var id = Guid.NewGuid();
            var folder = Path.Combine(_env.ContentRootPath, "Files");
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(input.FileName);
            var path = Path.Combine(folder, $"{id}{ext}");

            await using var fs = System.IO.File.Create(path);
            await input.CopyToAsync(fs);

            _db.StoredFiles.Add(new FileMeta
            {
                Id = id,
                FilePath = path,
                HashSum = hash,
                OriginalName = input.FileName
            });

            await _db.SaveChangesAsync();
            return Ok(new { id });
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка загрузки файла: " + ex.Message);
            return StatusCode(500, "Внутренняя ошибка сервера: " + ex.Message);
        }
    }


    [HttpGet("file/{id:guid}")]
    public async Task<IActionResult> GetFile(Guid id)
    {
        var entry = await _db.StoredFiles.FindAsync(id);
        if (entry == null)
            return NotFound();

        var content = await System.IO.File.ReadAllBytesAsync(entry.FilePath);
        return File(content, "application/octet-stream", entry.OriginalName);
    }
}
