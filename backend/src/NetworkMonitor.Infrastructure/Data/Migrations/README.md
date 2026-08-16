# Migrations

Empty on purpose — this project's actual schema is bootstrapped once by
`infrastructure/postgres/init.sql` (see docs/database.md, "Migrations vs.
init.sql"). Generate the first EF Core migration from the configurations in
`../Configurations/` once you're ready to let EF own schema changes going
forward:

```bash
cd backend/src/NetworkMonitor.Infrastructure
dotnet ef migrations add InitialCreate \
  --startup-project ../NetworkMonitor.API \
  --context AppDbContext \
  --output-dir Data/Migrations
```

Requires the `dotnet-ef` tool (`dotnet tool install --global dotnet-ef`)
and a reachable Postgres instance matching the `Default` connection string
in `NetworkMonitor.API`'s `appsettings.Development.json`.

After that, `init.sql` becomes redundant for anything beyond a fresh local
`docker compose up` — new environments would run
`dotnet ef database update` (or apply migrations at API startup, if you
wire that into `Program.cs`) instead of relying on Postgres's
`docker-entrypoint-initdb.d` bootstrap.
