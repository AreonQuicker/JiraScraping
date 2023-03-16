namespace JiraScraping.Console.Models;

public record VelocityByProjcetAndUser
{
    public string User { get; set; }
    public string Project { get; set; }
    public int CommittedNonProdFixes { get; set; }
    public int CompletedNonProdFixes { get; set; }
    public int DeployedNonProdFixes { get; set; }
    public int CompletedProdFixes { get; set; }
    public int DeployedProdFixes { get; set; }
    public int TotalCommittedNonProdFixes => CommittedNonProdFixes + CompletedNonProdFixes;
    public int TotalNonProdFixes => TotalCommittedNonProdFixes + DeployedNonProdFixes;
    public int TotalProdFixes => CompletedProdFixes + DeployedProdFixes;
    public int GrandTotal => TotalNonProdFixes + TotalProdFixes;
}