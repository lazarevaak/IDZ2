using FileAnalisysService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileAnalisysService.Controllers;

[ApiController]
[Route("scan")]
public class AnalysisController : ControllerBase
{
    private readonly ITextAnalyzer _service;

    public AnalysisController(ITextAnalyzer service)
    {
        _service = service;
    }

    [HttpPost("{id:guid}")]
    public async Task<IActionResult> Run(Guid id)
    {
        try
        {
            var result = await _service.ProcessAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка анализа файла: " + ex.Message);
            return StatusCode(500, "Ошибка анализа файла: " + ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        try
        {
            var result = await _service.ProcessAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка получения анализа: " + ex.Message);
            return StatusCode(500, "Ошибка получения анализа: " + ex.Message);
        }
    }

    [HttpGet("{id:guid}/cloud")]
    public async Task<IActionResult> Cloud(Guid id)
    {
        try
        {
            var content = await _service.BuildCloudAsync(id);
            return File(content, "image/png", $"{id}.png");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка генерации облака слов: " + ex.Message);
            return StatusCode(500, "Ошибка генерации облака слов: " + ex.Message);
        }
    }
}
