using System.Collections.Concurrent;
using JiraScraping.Console.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace JiraScraping.Console.Tools;

public record TicketStatus
{
    public string Name { get; set; }
    public int Order { get; set; }
}

public class JiraWebScraping : IDisposable
{
    private readonly string _userName;
    private readonly string _password;
    private readonly string _html;

    private readonly string[] _statusses =
    {
        "TO DO", "IN PROGRESS", "AWAITING FEEDBACK", "AWAITING MR APPROVAL", "IN DEVELOP", "IN DEV", "IN SIT",
        "IN UAT", "DONE"
    };

    private readonly string[] _projects =
    {
        "DCP", "ESnR", "BSnR", "TM", "CT", "C360", "CCM", "EnS", "MAD"
    };

    private readonly IWebDriver _driver;

    public JiraWebScraping(
        string userName, string password,
        string html = "https://aws-tools.standardbank.co.za/jira/secure/RapidBoard.jspa?rapidView=8947")
    {
        _userName = userName;
        _password = password;
        _html = html;
        _driver = new ChromeDriver();
    }

    private bool _isDisposed;
    private string[] _namesToExclude = { };

    public static JiraWebScraping NewInstance(string userName, string password,
        string html = "https://aws-tools.standardbank.co.za/jira/secure/RapidBoard.jspa?rapidView=8947")

    {
        return new JiraWebScraping(userName, password, html);
    }

    public JiraWebScraping ExcludeNames(params string[] names)
    {
        _namesToExclude = names;
        return this;
    }

    public IReadOnlyList<Ticket> Scrape()
    {
        //Check if already been disposed
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(JiraWebScraping));
        }

        var tickets = new ConcurrentBag<Ticket>();

        try
        {
            _driver.Navigate().GoToUrl(_html);

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(4 * 60));

            IWebElement submitButton = wait.Until(ExpectedConditions.ElementIsVisible((By.Id("login-form-submit"))));

            IWebElement userNameTextBox =
                wait.Until(ExpectedConditions.ElementIsVisible((By.Id("login-form-username"))));
            IWebElement passwordTextBox =
                wait.Until(ExpectedConditions.ElementIsVisible((By.Id("login-form-password"))));

            Thread.Sleep(1000);

            passwordTextBox.SendKeys(_password);
            userNameTextBox.SendKeys(_userName);

            submitButton.Click();

            IWebElement pool =
                wait.Until(ExpectedConditions.ElementIsVisible((By.Id("ghx-pool"))));

            var swimLaneElements = pool.FindElements(By.ClassName("ghx-swimlane"));

            Parallel.ForEach(swimLaneElements, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, swimLaneElement =>
            {
                var headerElement = swimLaneElement.FindElement(By.ClassName("ghx-swimlane-header"));
                var headingElement = headerElement.FindElement(By.ClassName("ghx-heading"));
                var spanElements = headingElement.FindElements(By.TagName("span"));
                if (spanElements.Count >= 0)
                {
                    var user = spanElements[0].Text;

                    if (_namesToExclude?.Any(a => user.Contains(a, StringComparison.InvariantCultureIgnoreCase)) ??
                        false)
                        return;

                    var containerColumnElement = swimLaneElement.FindElement(By.ClassName("ghx-columns"));
                    var columnElements = containerColumnElement.FindElements(By.ClassName("ghx-column"));

                    int statusCounter = 0;

                    foreach (var columnElement in columnElements)
                    {
                        var cardElements = columnElement.FindElements(By.ClassName("js-detailview"));

                        if (cardElements.Count > 0)
                        {
                            foreach (var cardElement in cardElements)
                            {
                                var projectKey = cardElement
                                    .FindElement(By.ClassName("ghx-key-link-project-key"))
                                    .Text;
                                var projectNum = cardElement
                                    .FindElement(By.ClassName("ghx-key-link-issue-num"))
                                    .Text;
                                var projectDescription = cardElement.FindElement(By.ClassName("ghx-inner")).Text;
                                var projectSize = cardElement.FindElement(By.ClassName("ghx-extra-field-content"))
                                    .Text;
                                var projectStatus = _statusses[statusCounter];

                                int.TryParse(projectSize, out int size);

                                //Check if projectDescription starts with _projects
                                var project = _projects.FirstOrDefault(f =>
                                    projectDescription.StartsWith(f, StringComparison.InvariantCultureIgnoreCase));

                                if (project is null)
                                    project = "Unknown";

                                tickets.Add(new Ticket
                                {
                                    User = user,
                                    Number = $"{projectKey}{projectNum}",
                                    Description = projectDescription,
                                    Status = projectStatus,
                                    Size = size,
                                    Project = project
                                });
                            }
                        }

                        statusCounter++;
                    }
                }
            });
        }
        catch (Exception e)
        {
            System.Console.WriteLine(e);
            Dispose();
            throw;
        }

        //Sort
        return tickets.OrderBy(o => o.User).ThenBy(o => _statusses.ToList().IndexOf(o.Status)).ThenBy(o => o.Project)
            .ThenBy(o => o.Size)
            .ToList();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _driver.Dispose();
        _isDisposed = true;
    }
}