using FileAnalisysService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileAnalisysService.Data;

public class AnalysisDb : DbContext
{
    public AnalysisDb(DbContextOptions<AnalysisDb> options) : base(options) { }

    public DbSet<ReportData> Reports => Set<ReportData>();
}
