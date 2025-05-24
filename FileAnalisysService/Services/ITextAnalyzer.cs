using System;
using FileAnalisysService.Models;

namespace FileAnalisysService.Services
{
    public interface ITextAnalyzer
    {
        Task<ReportData> ProcessAsync(Guid fileId);
        Task<byte[]> BuildCloudAsync(Guid fileId);
    }
}


