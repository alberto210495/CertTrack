# CertTrack

A command-line study tracker for CompTIA exam objectives.

## Run

```
dotnet run --project src/CertTrack
dotnet run --project src/CertTrack -- list
```

## Test

```
dotnet test
```

## Commands

- `certtrack add "1.1 Mobile device hardware" --cert A+ --domain 1`
- `certtrack list [--cert A+] [--status studying]`
- `certtrack study <id>`
- `certtrack done <id>`
- `certtrack stats`
- `certtrack help`

See [docs/DOCUMENTATION.md](docs/DOCUMENTATION.md) for how the app and the code work.
