namespace JiraScraping.Console.Models;

public record Ticket
{
    public string User { get; set; }
    public string Project { get; set; }
    public string Number { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public int? Size { get; set; }
}