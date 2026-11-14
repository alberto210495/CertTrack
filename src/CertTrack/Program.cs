// CertTrack - a CLI study tracker for CompTIA exam objectives.
//
// Build order (one commit per step):
//   1. Objective model (Id, Cert, Domain, Title, Status, LastReviewed)
//   2. Load/save a List<Objective> to objectives.json with System.Text.Json
//   3. Commands: add, list, done, stats
//   4. Polish: colors, friendly errors, help
//   5. xUnit tests
//   6. GitHub Actions: dotnet build + dotnet test

using System.Text.Json;
using CertTrack;

if (args.Length == 0)
    return Commands.Help();

var store = new ObjectiveStore();

try
{
    switch (args[0])
    {
        case "add":
            return Commands.Add(args, store);
        case "list":
            return Commands.List(args, store);
        case "study":
            return Commands.Study(args, store);
        case "done":
            return Commands.Done(args, store);
        case "stats":
            return Commands.Stats(store);
        case "help" or "--help" or "-h":
            return Commands.Help();
        default:
            Ui.Error($"Unknown command '{args[0]}'. Run 'certtrack help' to see the commands.");
            return 1;
    }
}
catch (JsonException)
{
    Ui.Error("objectives.json is not valid. Fix the file, or delete it to start over.");
    return 1;
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
{
    Ui.Error($"Could not read or write objectives.json: {ex.Message}");
    return 1;
}
