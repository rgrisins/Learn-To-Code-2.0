# LearnToCode

## Local setup

This repo now uses Redis in Docker Desktop and PostgreSQL installed locally on your machine.

### 1. Start Redis in Docker Desktop

Run this from the repository root:

```bash
docker compose up -d
```

Redis will be available on `localhost:6379`.

### 2. Start PostgreSQL locally

Create a local PostgreSQL database named `learn_to_code`.

The API expects this connection string by default in development:

`Host=localhost;Port=5432;Database=learn_to_code;Username=postgres;Password=postgres`

If your local PostgreSQL uses different credentials, set `ConnectionStrings__DefaultConnection` in your run profile or update `API/appsettings.Development.json`.

### 3. Run the app

Start the API:

```bash
dotnet run --project API/LearnToCode.csproj
```

The API listens on `https://localhost:8080`.

Start the UI:

```bash
cd UI
npm start
```

The UI listens on `https://localhost:5173`.

The local UI and API now run over HTTPS. If the browser warns about the local certificate, trust the ASP.NET Core development certificate once on your machine.
