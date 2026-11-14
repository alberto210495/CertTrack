namespace CertTrack;

public static class Commands
{
    public static int Add(string[] args, ObjectiveStore store)
    {
        var cert = GetOption(args, "--cert");
        var domainText = GetOption(args, "--domain");

        if (args.Length < 2 || args[1].StartsWith("--") || cert is null || !int.TryParse(domainText, out var domain))
        {
            Console.WriteLine("Usage: certtrack add \"<title>\" --cert <cert> --domain <number>");
            return 1;
        }

        var objectives = store.Load();
        var objective = new Objective
        {
            Id = objectives.Count == 0 ? 1 : objectives.Max(o => o.Id) + 1,
            Cert = cert,
            Domain = domain,
            Title = args[1]
        };

        objectives.Add(objective);
        store.Save(objectives);

        Console.WriteLine($"Added #{objective.Id}: {objective.Title}");
        return 0;
    }

    public static int List(string[] args, ObjectiveStore store)
    {
        IEnumerable<Objective> objectives = store.Load();

        var cert = GetOption(args, "--cert");
        if (cert is not null)
            objectives = objectives.Where(o => o.Cert.Equals(cert, StringComparison.OrdinalIgnoreCase));

        var statusText = GetOption(args, "--status");
        if (statusText is not null)
        {
            if (!Enum.TryParse<Status>(statusText, ignoreCase: true, out var status) || !Enum.IsDefined(status))
            {
                Console.WriteLine($"Unknown status '{statusText}'. Use notstarted, studying or done.");
                return 1;
            }

            objectives = objectives.Where(o => o.Status == status);
        }

        var results = objectives.ToList();
        if (results.Count == 0)
        {
            Console.WriteLine("No objectives found.");
            return 0;
        }

        foreach (var o in results)
            Console.WriteLine($"{o.Id,3}  {o.Status,-10}  {o.Cert,-9}  D{o.Domain}  {o.Title}");

        return 0;
    }

    public static int Done(string[] args, ObjectiveStore store)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out var id))
        {
            Console.WriteLine("Usage: certtrack done <id>");
            return 1;
        }

        var objectives = store.Load();
        var objective = objectives.Find(o => o.Id == id);
        if (objective is null)
        {
            Console.WriteLine($"No objective with id {id}.");
            return 1;
        }

        objective.Status = Status.Done;
        objective.LastReviewed = DateTime.Now;
        store.Save(objectives);

        Console.WriteLine($"Done #{objective.Id}: {objective.Title}");
        return 0;
    }

    public static int Stats(ObjectiveStore store)
    {
        var stats = StatsCalculator.ByDomain(store.Load());
        if (stats.Count == 0)
        {
            Console.WriteLine("No objectives yet.");
            return 0;
        }

        foreach (var certGroup in stats.GroupBy(s => s.Cert))
        {
            Console.WriteLine(certGroup.Key);
            foreach (var s in certGroup)
                Console.WriteLine($"  Domain {s.Domain}: {s.Percent,3:0}% ({s.Done}/{s.Total})");
        }

        return 0;
    }

    // Returns the value that follows an option like "--cert", or null if it is missing.
    private static string? GetOption(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
