# CertTrack documentation

CertTrack is a command-line study tracker for CompTIA exam objectives. You add the objectives of an exam, mark them as you study, and see how much of each domain is done.

It is written in C# on .NET 10 and stores its data in one JSON file.

## Contents

- [Running it](#running-it)
- [Commands](#commands)
- [Errors and exit codes](#errors-and-exit-codes)
- [The data file](#the-data-file)
- [How the code is organised](#how-the-code-is-organised)
- [How each part works](#how-each-part-works)
- [C# features used](#c-features-used)
- [Known limitations](#known-limitations)
- [Roadmap](#roadmap)

## Running it

You need the .NET 10 SDK.

```
dotnet build
dotnet run -- <command> [options]
```

Everything after `--` is passed to CertTrack. The examples below write `certtrack` for short; during development that means `dotnet run --`.

## Commands

| Command | What it does |
|---|---|
| `certtrack add "<title>" --cert <cert> --domain <number>` | Adds an objective |
| `certtrack list [--cert <cert>] [--status <status>]` | Lists objectives, optionally filtered |
| `certtrack study <id>` | Marks an objective as studying |
| `certtrack done <id>` | Marks an objective as done |
| `certtrack stats` | Shows percent done per domain |
| `certtrack help` | Shows the command list (also `--help`, `-h`, or no arguments) |

### add

```
> certtrack add "1.1 Mobile device hardware" --cert A+ --domain 1
Added #1: 1.1 Mobile device hardware
```

- The title must be the first argument after `add`. Put it in quotes if it contains spaces.
- `--domain` must be a whole number, 1 or higher.
- New objectives start as `NotStarted`.
- If the cert is already stored with different capitalisation, the stored spelling is reused. After one objective with `--cert A+`, a later `--cert a+` is saved as `A+`. The first spelling you type is the one that sticks.

### list

```
> certtrack list
  1  Done        A+         D1  1.1 Mobile device hardware
  2  Studying    A+         D1  1.2 Mobile device accessories
  3  NotStarted  Network+   D1  1.1 OSI model
```

The columns are id, status, cert, domain and title.

- `--cert A+` shows one cert only. The match ignores capitalisation.
- `--status` accepts `notstarted`, `studying` or `done`, in any capitalisation.
- Both filters can be combined.
- The status is coloured: green for done, yellow for studying, grey for not started.

### study and done

```
> certtrack study 2
Studying #2: 1.2 Mobile device accessories

> certtrack done 1
Done #1: 1.1 Mobile device hardware
```

Both set the status and record the current date and time as `LastReviewed`. `study` also works on an objective that is already done, which moves it back to studying.

### stats

```
> certtrack stats
A+
  Domain 1:  50% (1/2)
  Domain 2:   0% (0/1)
Network+
  Domain 1: 100% (1/1)
```

- Objectives are grouped by cert, then by domain.
- Only `Done` counts towards the percentage. `Studying` counts as not done.
- The percentage is green when the domain is complete, yellow when partly done and grey at zero.

## Errors and exit codes

Bad input prints a red `Error:` line and, where it helps, the usage of the command:

```
> certtrack add "Bad domain" --cert A+ --domain one
Error: The domain must be a positive number, not 'one'.
Usage: certtrack add "<title>" --cert <cert> --domain <number>
```

- Error messages are written to stderr, so they stay out of piped output.
- The exit code is `0` on success and `1` on any error.
- If `objectives.json` cannot be parsed, CertTrack reports it and leaves the file untouched.

## The data file

Objectives are stored in `objectives.json` in the folder you run the command from. With `dotnet run` that is the project folder. The file is listed in `.gitignore` because it is personal data.

```json
[
  {
    "Id": 1,
    "Cert": "A+",
    "Domain": 1,
    "Title": "1.1 Mobile device hardware",
    "Status": "Done",
    "LastReviewed": "2026-11-13T22:50:38.2276721-06:00"
  }
]
```

| Field | Type | Notes |
|---|---|---|
| `Id` | number | Assigned by `add` |
| `Cert` | text | For example `A+` or `Network+` |
| `Domain` | number | The exam domain the objective belongs to |
| `Title` | text | The objective as you typed it |
| `Status` | text | `NotStarted`, `Studying` or `Done` |
| `LastReviewed` | date and time, or `null` | Set by `study` and `done`; `null` until then |

If the file does not exist, CertTrack treats it as an empty list and creates it on the first `add`.

## How the code is organised

| File | Responsibility |
|---|---|
| `Program.cs` | Entry point. Picks the command from `args[0]` and returns its exit code. Catches file and JSON errors. |
| `Commands.cs` | One method per command, the help text, input validation and option parsing. |
| `Objective.cs` | The `Objective` class and the `Status` enum. |
| `ObjectiveStore.cs` | Loads and saves the list of objectives as JSON. |
| `StatsCalculator.cs` | Works out done and total per cert and domain. No console output. |
| `Ui.cs` | Coloured console output: success, error and coloured text. |

A command flows through the files like this:

```
args -> Program.cs -> Commands.cs -> ObjectiveStore.cs -> objectives.json
                          |
                          +-> StatsCalculator.cs (stats only)
                          +-> Ui.cs (colours and errors)
```

Every command that changes data follows the same three steps: load the whole list, change it in memory, save the whole list.

## How each part works

**Choosing the command.** `Program.cs` uses a `switch` on `args[0]`. Each command method returns an `int`, and `Program.cs` returns that as the process exit code.

**Reading options.** `GetOption` in `Commands.cs` looks for an option name such as `--cert` anywhere in the arguments and returns the argument after it. If there is nothing after it, or the next argument starts with `--`, it returns `null`. `IsMissingValue` uses that to tell "option not given" apart from "option given without a value".

**Assigning ids.** `add` uses 1 for the first objective, and the highest existing id plus 1 after that.

**Changing status.** `study` and `done` both call `SetStatus`, which validates the id, finds the objective, sets the status and `LastReviewed`, and saves.

**Filtering the list.** `list` starts from every objective and applies a `Where` filter for each option that was given.

**Calculating stats.** `StatsCalculator.ByDomain` groups objectives by cert and domain, sorts the groups, and returns one `DomainStats` per group holding the done count and the total. `DomainStats.Percent` is `100 * Done / Total`. Printing is done separately in `Commands.Stats`, which keeps the calculation easy to test.

**Saving JSON.** `ObjectiveStore` sets three serializer options:

| Option | Effect |
|---|---|
| `WriteIndented` | One field per line, so the file is readable |
| `JsonStringEnumConverter` | Writes `"Status": "Done"` instead of `"Status": 2` |
| `UnsafeRelaxedJsonEscaping` | Writes `A+` instead of `A+`. The "unsafe" refers to embedding JSON in HTML, which this file never is. |

**Handling a broken file.** `Program.cs` wraps the commands in a `try`. A `JsonException` (invalid JSON, or an objective missing `Cert` or `Title`) and file access errors are turned into a one-line error instead of a stack trace.

## C# features used

| Feature | Where | What it does |
|---|---|---|
| Top-level statements | `Program.cs` | The file is the `Main` method; no class or method wrapper is needed |
| `required` properties | `Objective.cs` | The compiler rejects `new Objective { ... }` unless `Cert` and `Title` are set |
| Nullable value type `DateTime?` | `Objective.cs` | `null` means "never reviewed" |
| Primary constructor | `ObjectiveStore.cs` | `class ObjectiveStore(string path = "objectives.json")` declares the constructor parameter on the class line |
| Collection expression `[]` | `ObjectiveStore.cs` | Shorthand for an empty list |
| Null-coalescing `??` | `ObjectiveStore.cs`, `Commands.cs` | Uses the right-hand value when the left is `null` |
| `record` | `StatsCalculator.cs` | `DomainStats` is a small immutable data type declared in one line |
| LINQ (`Where`, `GroupBy`, `OrderBy`, `Max`) | `Commands.cs`, `StatsCalculator.cs` | Filtering, grouping and sorting lists |
| Switch expression | `Commands.cs` | `StatusColor` maps each status to a colour |
| Exception filter `when` | `Program.cs` | One `catch` handles two exception types |

## Known limitations

- There is no command to delete or edit an objective. To fix a mistake, edit `objectives.json` by hand.
- The data file depends on the folder you run from. Running from a different folder starts a new, empty list.
- Options that CertTrack does not know, such as `list --foo`, are ignored without a warning.
- Domain numbers are not checked against the real exam. `--domain 99` is accepted.
- There are no automated tests yet.

## Roadmap

Planned, not built yet:

1. Unit tests with xUnit for storage and stats.
2. A GitHub Actions workflow that runs `dotnet build` and `dotnet test` on every push.
3. A fuller README with a screenshot of `stats`.
4. A web version that reuses the model and stats logic: an ASP.NET Core API with SQLite, login, input validation and a simple front end.
