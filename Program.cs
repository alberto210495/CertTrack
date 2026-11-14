// CertTrack - a CLI study tracker for CompTIA exam objectives.
//
// Build order (one commit per step):
//   1. Objective model (Id, Cert, Domain, Title, Status, LastReviewed)
//   2. Load/save a List<Objective> to objectives.json with System.Text.Json
//   3. Commands: add, list, done, stats
//   4. Polish: colors, friendly errors, help
//   5. xUnit tests
//   6. GitHub Actions: dotnet build + dotnet test

if (args.Length == 0)
{
    Console.WriteLine("certtrack - CompTIA study tracker");
    Console.WriteLine("Usage: certtrack <add|list|done|stats> [options]");
    return;
}

Console.WriteLine($"Command '{args[0]}' is not implemented yet.");
