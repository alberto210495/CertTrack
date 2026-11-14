// CertTrack - a CLI study tracker for CompTIA exam objectives.
//
// Build order (one commit per step):
//   1. Objective model (Id, Cert, Domain, Title, Status, LastReviewed)
//   2. Load/save a List<Objective> to objectives.json with System.Text.Json
//   3. Commands: add, list, done, stats
//   4. Polish: colors, friendly errors, help
//   5. xUnit tests
//   6. GitHub Actions: dotnet build + dotnet test

using CertTrack;

const string Usage = "Usage: certtrack <add|list|done|stats> [options]";

if (args.Length == 0)
{
    Console.WriteLine("certtrack - CompTIA study tracker");
    Console.WriteLine(Usage);
    return 0;
}

var store = new ObjectiveStore();

switch (args[0])
{
    case "add":
        return Commands.Add(args, store);
    case "list":
        return Commands.List(args, store);
    case "done":
        return Commands.Done(args, store);
    case "stats":
        return Commands.Stats(store);
    default:
        Console.WriteLine($"Unknown command '{args[0]}'.");
        Console.WriteLine(Usage);
        return 1;
}
