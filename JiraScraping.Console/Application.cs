using JiraScraping.Console.Models;
using JiraScraping.Console.Services;
using JiraScraping.Console.Tools;

namespace JiraScraping.Console;

public static class Application
{
    public static void Run()
    {
        var userLines = File.ReadAllLines("User.txt");
        var userName = userLines[0];
        var password = userLines[1];

        var jiraWebScraping = JiraWebScraping.NewInstance(userName, password);

        List<Ticket> tickets;

        try
        {
            tickets = jiraWebScraping.Scrape().ToList();
        }
        catch (Exception e)
        {
            System.Console.WriteLine(e);
            throw;
        }
        finally
        {
            jiraWebScraping.Dispose();
        }

        var excelService = new ExcelService();

        try
        {
            var userTickets = tickets.Where(w => w.User != "Unassigned").ToList();
            var unassignedTickets = tickets.Where(w => w.User == "Unassigned").ToList();
            var velocityByProjcetAndUser = userTickets.GroupBy(x => new { x.User, x.Project })
                .Select(s => new VelocityByProjcetAndUser
                {
                    User = s.Key.User,
                    Project = s.Key.Project,
                    CommittedNonProdFixes = s
                        .Where(s => (s.Status == "TO DO" || s.Status == "IN PROGRESS") &&
                                    !s.Description.Contains("Prod Bug", StringComparison.InvariantCultureIgnoreCase))
                        .Sum(x => x.Size ?? 0),
                    CompletedNonProdFixes = s
                        .Where(s => s.Status != "TO DO" && s.Status != "IN PROGRESS" &&
                                    s.Status != "AWAITING FEEDBACK" && s.Status != "DONE" &&
                                    !s.Description.Contains("Prod Bug", StringComparison.InvariantCultureIgnoreCase))
                        .Sum(x => x.Size ?? 0),
                    CompletedProdFixes = s
                        .Where(s => s.Status != "TO DO" && s.Status != "IN PROGRESS" &&
                                    s.Status != "AWAITING FEEDBACK" && s.Status != "DONE" &&
                                    s.Description.Contains("Prod Bug", StringComparison.InvariantCultureIgnoreCase))
                        .Sum(x => x.Size ?? 0),
                    DeployedNonProdFixes = s
                        .Where(s => s.Status == "DONE" &&
                                    !s.Description.Contains("Prod Bug", StringComparison.InvariantCultureIgnoreCase))
                        .Sum(x => x.Size ?? 0),
                    DeployedProdFixes = s
                        .Where(s => s.Status == "DONE" &&
                                    s.Description.Contains("Prod Bug", StringComparison.InvariantCultureIgnoreCase))
                        .Sum(x => x.Size ?? 0)
                }).ToList();


            var velocityByUser = velocityByProjcetAndUser.GroupBy(x => new { x.User })
                .Select(s => new VelocityByUser
                {
                    User = s.Key.User,
                    CommittedNonProdFixes = s.Sum(s => s.CommittedNonProdFixes),
                    CompletedNonProdFixes = s.Sum(s => s.CompletedNonProdFixes),
                    DeployedNonProdFixes = s.Sum(s => s.DeployedNonProdFixes),
                    CompletedProdFixes = s.Sum(s => s.CompletedProdFixes),
                    DeployedProdFixes = s.Sum(s => s.DeployedProdFixes)
                }).ToList();

            var velocityByProject = velocityByProjcetAndUser.GroupBy(x => new { x.Project })
                .Select(s => new VelocityByProject
                {
                    Project = s.Key.Project,
                    CommittedNonProdFixes = s.Sum(s => s.CommittedNonProdFixes),
                    CompletedNonProdFixes = s.Sum(s => s.CompletedNonProdFixes),
                    DeployedNonProdFixes = s.Sum(s => s.DeployedNonProdFixes),
                    CompletedProdFixes = s.Sum(s => s.CompletedProdFixes),
                    DeployedProdFixes = s.Sum(s => s.DeployedProdFixes)
                }).ToList();

            excelService.AddWorkSheet("User Tickets", userTickets);
            excelService.AddWorkSheet("Unassigned Tickets", unassignedTickets);
            excelService.AddWorkSheet("Velocity By Project and User", velocityByProjcetAndUser);
            excelService.AddWorkSheet("Velocity By User", velocityByUser);
            excelService.AddWorkSheet("Velocity By Project", velocityByProject);

            excelService.Save();
        }
        catch (Exception e)
        {
            System.Console.WriteLine(e);
            throw;
        }
        finally
        {
            excelService.Dispose();
        }
    }
}