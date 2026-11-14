namespace CertTrack;

public static class Commands
{
    private const string AddUsage = "certtrack add \"<title>\" --cert <cert> --domain <number>";
    private const string ListUsage = "certtrack list [--cert <cert>] [--status <status>]";
    private const string StudyUsage = "certtrack study <id>";
    private const string DoneUsage = "certtrack done <id>";

    public static int Help()
    {
        Console.WriteLine("certtrack - CompTIA study tracker");
        Console.WriteLine();
        Console.WriteLine("Usage: certtrack <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  add \"<title>\" --cert <cert> --domain <number>   Add an objective");
        Console.WriteLine("  list [--cert <cert>] [--status <status>]        List objectives");
        Console.WriteLine("  study <id>                                      Mark an objective as studying");
        Console.WriteLine("  done <id>                                       Mark an objective as done");
        Console.WriteLine("  stats                                           Show percent done per domain");
        Console.WriteLine("  help                                            Show this help");
        Console.WriteLine();
        Console.WriteLine("Statuses: notstarted, studying, done");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  certtrack add \"1.1 Mobile device hardware\" --cert A+ --domain 1");
        Console.WriteLine("  certtrack list --cert A+ --status studying");
        Console.WriteLine("  certtrack study 3");
        Console.WriteLine("  certtrack done 3");
        Console.WriteLine("  certtrack stats");
        return 0;
    }

    public static int Add(string[] args, ObjectiveStore store)
    {
        if (args.Length < 2 || args[1].StartsWith("--") || string.IsNullOrWhiteSpace(args[1]))
            return Fail("Missing the objective title.", AddUsage);

        var cert = GetOption(args, "--cert");
        if (cert is null)
            return Fail("Missing --cert, for example --cert A+.", AddUsage);

        var domainText = GetOption(args, "--domain");
        if (domainText is null)
            return Fail("Missing --domain, for example --domain 1.", AddUsage);

        if (!int.TryParse(domainText, out var domain) || domain < 1)
            return Fail($"The domain must be a positive number, not '{domainText}'.", AddUsage);

        var objectives = store.Load();

        // Reuse the spelling of a cert that is already stored, so "a+" and "A+" stay one cert.
        var sameCert = objectives.Find(o => o.Cert.Equals(cert, StringComparison.OrdinalIgnoreCase));

        var objective = new Objective
        {
            Id = objectives.Count == 0 ? 1 : objectives.Max(o => o.Id) + 1,
            Cert = sameCert?.Cert ?? cert,
            Domain = domain,
            Title = args[1]
        };

        objectives.Add(objective);
        store.Save(objectives);

        Ui.Success($"Added #{objective.Id}: {objective.Title}");
        return 0;
    }

    public static int List(string[] args, ObjectiveStore store)
    {
        if (IsMissingValue(args, "--cert"))
            return Fail("--cert needs a value, for example --cert A+.", ListUsage);

        if (IsMissingValue(args, "--status"))
            return Fail("--status needs a value: notstarted, studying or done.", ListUsage);

        IEnumerable<Objective> objectives = store.Load();

        var cert = GetOption(args, "--cert");
        if (cert is not null)
            objectives = objectives.Where(o => o.Cert.Equals(cert, StringComparison.OrdinalIgnoreCase));

        var statusText = GetOption(args, "--status");
        if (statusText is not null)
        {
            if (!Enum.TryParse<Status>(statusText, ignoreCase: true, out var status) || !Enum.IsDefined(status))
                return Fail($"Unknown status '{statusText}'. Use notstarted, studying or done.");

            objectives = objectives.Where(o => o.Status == status);
        }

        var results = objectives.ToList();
        if (results.Count == 0)
        {
            Console.WriteLine("No objectives found.");
            return 0;
        }

        foreach (var o in results)
        {
            Console.Write($"{o.Id,3}  ");
            Ui.Write($"{o.Status,-10}", StatusColor(o.Status));
            Console.WriteLine($"  {o.Cert,-9}  D{o.Domain}  {o.Title}");
        }

        return 0;
    }

    public static int Study(string[] args, ObjectiveStore store)
    {
        return SetStatus(args, store, Status.Studying, StudyUsage);
    }

    public static int Done(string[] args, ObjectiveStore store)
    {
        return SetStatus(args, store, Status.Done, DoneUsage);
    }

    private static int SetStatus(string[] args, ObjectiveStore store, Status status, string usage)
    {
        if (args.Length < 2)
            return Fail("Missing the objective id.", usage);

        if (!int.TryParse(args[1], out var id))
            return Fail($"The id must be a number, not '{args[1]}'.", usage);

        var objectives = store.Load();
        var objective = objectives.Find(o => o.Id == id);
        if (objective is null)
            return Fail($"No objective with id {id}. Run 'certtrack list' to see the ids.");

        objective.Status = status;
        objective.LastReviewed = DateTime.Now;
        store.Save(objectives);

        Ui.Success($"{status} #{objective.Id}: {objective.Title}");
        return 0;
    }

    public static int Stats(ObjectiveStore store)
    {
        var stats = StatsCalculator.ByDomain(store.Load());
        if (stats.Count == 0)
        {
            Console.WriteLine("No objectives yet. Add one with 'certtrack add'.");
            return 0;
        }

        foreach (var certGroup in stats.GroupBy(s => s.Cert))
        {
            Ui.Write(certGroup.Key, ConsoleColor.Cyan);
            Console.WriteLine();

            foreach (var s in certGroup)
            {
                Console.Write($"  Domain {s.Domain}: ");
                Ui.Write($"{s.Percent,3:0}%", PercentColor(s));
                Console.WriteLine($" ({s.Done}/{s.Total})");
            }
        }

        return 0;
    }

    private static int Fail(string message, string? usage = null)
    {
        Ui.Error(message);
        if (usage is not null)
            Console.Error.WriteLine($"Usage: {usage}");

        return 1;
    }

    private static ConsoleColor StatusColor(Status status) => status switch
    {
        Status.Done => ConsoleColor.Green,
        Status.Studying => ConsoleColor.Yellow,
        _ => ConsoleColor.DarkGray
    };

    private static ConsoleColor PercentColor(DomainStats stats)
    {
        if (stats.Done == stats.Total)
            return ConsoleColor.Green;

        return stats.Done > 0 ? ConsoleColor.Yellow : ConsoleColor.DarkGray;
    }

    // Returns the value that follows an option like "--cert", or null if it is missing.
    private static string? GetOption(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        if (index < 0 || index + 1 >= args.Length || args[index + 1].StartsWith("--"))
            return null;

        return args[index + 1];
    }

    private static bool IsMissingValue(string[] args, string name)
    {
        return args.Contains(name) && GetOption(args, name) is null;
    }
}
