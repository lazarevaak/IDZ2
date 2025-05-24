using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using FileAnalisysService.Data;
using FileAnalisysService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileAnalisysService.Services;

public class TextAnalyzer : ITextAnalyzer
{
    private readonly IHttpClientFactory _http;
    private readonly AnalysisDb _db;

    public TextAnalyzer(IHttpClientFactory http, AnalysisDb db)
    {
        _http = http;
        _db = db;
    }

    public async Task<ReportData> ProcessAsync(Guid fileId)
    {
        try
        {
            var cached = await _db.Reports.FindAsync(fileId);
            if (cached != null) return cached;

            var client = _http.CreateClient("Storage");
            var bytes = await client.GetByteArrayAsync($"/files/file/{fileId}");
            var text = Encoding.UTF8.GetString(bytes);

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

            if (await _db.Reports.AnyAsync(r => r.Hash == hash && r.FileId != fileId))
                throw new InvalidOperationException("100% match with another file");

            var result = new ReportData
            {
                FileId = fileId,
                Hash = hash,
                Paragraphs = Regex.Split(text.Trim(), @"\r?\n\s*\r?\n").Length,
                Words = Regex.Matches(text, @"\b\w+\b").Count,
                Characters = text.Length
            };

            _db.Reports.Add(result);
            await _db.SaveChangesAsync();

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка анализа файла: " + ex.Message);
            throw;
        }
    }

    public async Task<byte[]> BuildCloudAsync(Guid fileId)
    {
        try
        {
            var result = await _db.Reports.FindAsync(fileId)
                         ?? throw new InvalidOperationException("Сначала выполните анализ файла");

            if (result.CloudId != null)
            {
                return await _http.CreateClient("Storage")
                    .GetByteArrayAsync($"/files/file/{result.CloudId}");
            }

            var text = await _http.CreateClient("Storage")
                .GetStringAsync($"/files/file/{fileId}");

            var words = Regex.Replace(text.ToLower(), @"[^\p{L}\p{N}\s]", " ")
                             .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var csv = string.Join(",", words.Select(Uri.EscapeDataString));

            var png = await _http.CreateClient("Cloud")
                .GetByteArrayAsync($"/wordcloud?text={csv}&useWordList=true&removeStopwords=true&format=png");

            var content = new ByteArrayContent(png);
            content.Headers.ContentType = new("image/png");

            var form = new MultipartFormDataContent();
            form.Add(content, "file", $"{fileId}.png");

            var resp = await _http.CreateClient("Storage").PostAsync("/files/upload", form);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Ошибка загрузки PNG: {resp.StatusCode}");

            var json = await resp.Content.ReadFromJsonAsync<UploadResult>()
                       ?? throw new Exception("Ошибка парсинга ответа от FileStorage");

            result.CloudId = json.Id;
            await _db.SaveChangesAsync();

            return png;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка генерации облака слов: " + ex.Message);
            throw;
        }
    }
}
