using System.ComponentModel.DataAnnotations;

namespace FileAnalisysService.Models;

public class ReportData
{
    [Key]
    public Guid FileId { get; set; }
    public string Hash { get; set; } = "";
    public int Paragraphs { get; set; }
    public int Words { get; set; }
    public int Characters { get; set; }
    public Guid? CloudId { get; set; }
}
