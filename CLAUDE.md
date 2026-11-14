# CertTrack

A C# (.NET 10) command-line study tracker for CompTIA exam objectives. This is a portfolio project for GitHub, built by a software engineering student who is studying for CompTIA A+ first, then Network+ and Security+.

## How to work with me on this

- This is a learning project. Guide and review rather than writing whole features unprompted. Write code only when asked.
- Be direct. Point out mistakes explicitly and suggest better approaches.
- Keep answers short and scannable.
- One commit per build step, with clear commit messages.

## Current state

- Project skeleton created and committed: `CertTrack.csproj`, `Program.cs`, `.gitignore`, `README.md`.
- `dotnet run` works (SDK 10.0.404) and prints a usage message.
- `Program.cs` only prints usage. No features are implemented yet.
- Not yet pushed to GitHub.

## Build order

1. `Objective` model: `Id`, `Cert`, `Domain`, `Title`, `Status` (NotStarted, Studying, Done), `LastReviewed`.
2. Storage: load and save a `List<Objective>` to `objectives.json` with `System.Text.Json`.
3. Commands, parsed from `args`:
   - `certtrack add "1.1 Mobile device hardware" --cert A+ --domain 1`
   - `certtrack list [--cert A+] [--status studying]`
   - `certtrack done <id>`
   - `certtrack stats` (percent done per domain)
4. Polish: colored output, friendly errors for bad input, a `help` command.
5. Tests: an xUnit project (`CertTrack.Tests`) covering storage and stats logic.
6. CI: a GitHub Actions workflow running `dotnet build` and `dotnet test` on every push.
7. README: what it does, how to install, a screenshot of `stats`.

## Commands

```
dotnet run
dotnet run -- list
dotnet build
dotnet test
```

## Notes

- `objectives.json` is local data and is in `.gitignore`.
- Never commit secrets.
- Later plan: reuse the model and logic in an ASP.NET Core API with SQLite (EF Core), login and a simple front end.
