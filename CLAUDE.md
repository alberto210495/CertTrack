# CertTrack

A C# (.NET 10) study tracker for CompTIA exam objectives. This is a portfolio project for GitHub, built by a software engineering student who is studying for CompTIA A+ first, then Network+ and Security+.

## Goal

A full-stack CRUD app with auth, tests and CI. "A boring idea done well beats a clever one done badly."

- JWT or session auth
- Input validation
- Unit tests
- A GitHub Actions pipeline

The command-line app is the first stage. The web version reuses its model and logic.

## How to work with me on this

- This is a learning project. Guide and review rather than writing whole features unprompted. Write code only when asked.
- Be direct. Point out mistakes explicitly and suggest better approaches.
- Keep answers short and scannable.
- One commit per build step, with clear commit messages.

## Current state

- The CLI works: `add`, `list`, `study`, `done`, `stats`, `help`, with colored output and friendly errors.
- CLI build steps 1 to 5 are done. Steps 6 (CI) and 7 (README) are not started.
- 36 xUnit tests cover storage, stats and the commands.
- The repo is on GitHub at https://github.com/alberto210495/CertTrack, branch `main`.
- Nothing of the web version exists yet.

## Layout

```
CertTrack.slnx                 solution with both projects
src/CertTrack/                 the CLI app
  Program.cs                   picks the command, returns the exit code
  Commands.cs                  one method per command, validation, option parsing
  Objective.cs                 Objective class and Status enum
  ObjectiveStore.cs            loads and saves objectives.json
  StatsCalculator.cs           done and total per cert and domain
  Ui.cs                        colored console output
tests/CertTrack.Tests/         xUnit tests
docs/DOCUMENTATION.md          how the app and the code work
```

## CLI build order

1. Done: `Objective` model.
2. Done: JSON storage in `objectives.json` with `System.Text.Json`.
3. Done: commands `add`, `list`, `done`, `stats` (plus `study`, added later).
4. Done: colored output, friendly errors, `help`.
5. Done: xUnit project `CertTrack.Tests`.
6. To do: a GitHub Actions workflow running `dotnet build` and `dotnet test` on every push.
7. To do: README with what it does, how to install, and a screenshot of `stats`.

## Full-stack plan

Proposed, not started, and open to change:

1. Split the solution into `CertTrack.Core` (model and stats), `CertTrack.Cli` and `CertTrack.Api`.
2. ASP.NET Core API with EF Core and SQLite, with full CRUD. Delete and edit do not exist yet.
3. Validation on the API request bodies.
4. Auth: register, login, hashed passwords, and each user sees only their own objectives. Leaning towards JWT because it can be tested without a browser; not decided.
5. A simple front end: one plain HTML and JavaScript page served by the API.
6. Tests for the API endpoints, run by the same CI workflow.

## Commands

```
dotnet build
dotnet test
dotnet run --project src/CertTrack
dotnet run --project src/CertTrack -- list
```

## Notes

- `objectives.json` is local data and is in `.gitignore`. It is written to the folder the command is run from.
- Never commit secrets.
- Keep `docs/DOCUMENTATION.md` in sync when commands or files change.
- Windows Smart App Control was turned off on this machine because it blocked freshly built DLLs with error `0x800711C7` ("An Application Control policy has blocked this file"). If that error comes back, Smart App Control is the cause.
- The GitHub CLI (`gh`) is not installed. Repos are created on the GitHub website.
