# LearnToCode

## Local test setup

The app can be run locally while using the server PostgreSQL, Redis and MinIO services. The API uses `API/appsettings.Development.json` when `ASPNETCORE_ENVIRONMENT=Development`.

### API

Run from the repository root:

```bash
dotnet run --project API/LearnToCode.csproj --launch-profile https
```

The API listens on `https://localhost:8080` and connects to:

- PostgreSQL: `192.168.50.242:5432`
- Redis: `192.168.50.242:6379`
- MinIO: `192.168.50.242:9000`

### UI

Run in another terminal:

```bash
cd UI
npm start
```

The UI listens on `https://localhost:5173` and proxies `/api` to `https://localhost:8080`.

## Server deployment

On the server, use Docker Compose:

```bash
docker compose up -d --build
```

The API container runs with `ASPNETCORE_ENVIRONMENT=Production`. `docker-compose.yaml` overrides the default `appsettings.json` values with container network names and `.env` secrets:

- PostgreSQL: `postgres:5432`
- Redis: `redis:6379`
- MinIO: `minio:9000`

So local testing and server deployment can use the same code without editing config files before deploy.
