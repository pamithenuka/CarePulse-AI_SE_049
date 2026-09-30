# CarePulse — simple setup

Start the **backend**, **web app**, and **Flutter app** in three separate terminals. Keep all three running while testing.

You need .NET 8, Node.js 22, Flutter 3.47.1 (Dart 3.13.1), and a PostgreSQL database. Docker is optional.

## 1. Fill in your private settings

Open `backend-api/appsettings.Local.json`. **If it already exists, keep it.** Otherwise copy `backend-api/appsettings.Local.example.json` and rename the copy to `appsettings.Local.json`.

Change only these values:

| Setting | What to enter |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | Your PostgreSQL connection string in .NET format: `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require` for Neon. Do not paste a `postgresql://...` URL directly. |
| `Jwt:Secret` | Your own random secret, at least 32 characters. Replace the example placeholder. Keep it unchanged between runs. |
| `AI:GeminiApiKey` | Your Gemini API key. Needed to test live AI responses and AI doctor search. |
| `Seed:AdminEmail` | The email you want to use for the first web admin account. |
| `Seed:AdminPassword` | A new strong password with uppercase, lowercase and a number; at least 8 characters. |
| `Database:SeedOnStartup` | `true` for first setup to create roles and the admin account. |

Keep `AI:GeminiModel` as `gemini-3-flash-preview` and `Seed:DemoPatients` as `false`. After the first successful admin login, set `SeedOnStartup` to `false` and clear `AdminPassword`. This setting creates a new admin; it does not reset an existing admin's password.

**Do not put secrets in `appsettings.json` or commit `appsettings.Local.json`. No C#, React or Dart source-code replacements are needed.**

## 2. Prepare the database once

From the project root:

```sh
dotnet tool restore
dotnet restore CarePulse.sln
```

**Using our existing shared Neon database?** It was upgraded successfully on 29 September 2026. If your connection points to that same database, **skip the migration commands below and go to step 3**. See [upgrade result](docs/NEON_UPGRADE_STATUS.md). For a different existing database, follow [Database upgrade](docs/DATABASE_UPGRADE.md) first.

**Starting with a new local database?** If Docker is installed, run:

```sh
docker compose up -d postgres
```

Use the local connection from the example settings file. Set that same connection in the terminal before migrating:

```sh
# macOS/Linux
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=carepulse_dev;Username=postgres;Password=local-dev-only'
```

```powershell
# Windows PowerShell — use this instead of export
$env:ConnectionStrings__DefaultConnection = 'Host=localhost;Port=5432;Database=carepulse_dev;Username=postgres;Password=local-dev-only'
```

Then:

```sh
dotnet ef database update --project backend-api --context CarePulseDbContext
```

The migration tool reads this environment variable, not `appsettings.Local.json`. For another database, use its reviewed connection instead. Environment variables also override the settings file when running the backend; make sure both point to the intended database.

## 3. Terminal 1 — backend

From the project root:

```sh
dotnet run --project backend-api --launch-profile http
```

Leave it running. If Swagger opens automatically, you can close that browser tab; app testing does not need it.

## 4. Terminal 2 — web app

From the project root:

```sh
cd web-admin
npm ci
npm start
```

Use the browser window opened by the command. Log in with your configured admin account. The default backend address already works on the same laptop. If `web-admin/.env.local` exists, ensure it contains `REACT_APP_API_BASE_URL=http://localhost:5014/api/v1`.

## 5. Terminal 3 — Flutter app

From the project root:

```sh
cd mobile-app
flutter pub get
```

Choose **one** option:

```sh
# Android emulator: start the emulator first
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5014/api/v1
```

```sh
# iOS simulator: start the simulator first (Mac only)
flutter run --dart-define=API_BASE_URL=http://localhost:5014/api/v1
```

```sh
# Easiest alternative: Flutter in Chrome
flutter run -d chrome --web-port=8080 --dart-define=API_BASE_URL=http://localhost:5014/api/v1
```

If Flutter lists several devices, choose the intended one. A real phone needs your laptop's LAN address and a backend listening on the network; see [Detailed setup](docs/SETUP_REFERENCE.md). Use an emulator or Chrome first to avoid that extra setup. Browser tests do not replace testing GPS/camera on a phone or emulator.

## 6. Test the four members' work

Open [App-only manual testing](docs/APP_MANUAL_TEST_PLAN.md). It uses web menus and Flutter screens—no Swagger, copied IDs, or API requests.

The older [Manual test plan](docs/MANUAL_TEST_PLAN.md) includes deeper Swagger/API checks too. See [Verified results](docs/IMPLEMENTATION_STATUS.md) for what has already passed.

On later runs, normally repeat only steps 3–5. Rerun dependency installation when dependency files change, and apply reviewed new migrations when the database model changes.

## Quick fixes

| Problem | Check |
| --- | --- |
| Web/mobile cannot connect | Backend terminal is still running; device API address above is correct. |
| Database error or missing column | Connection is correct and the required migrations were applied. |
| Admin cannot log in | First-run seeding and password; existing accounts retain their old password. |
| AI unavailable/manual review | Gemini key, network, provider quota and model access. Missing AI does not authorize automatic dispatch. |
| Session expired | Sign in again. |
| No appointment slots | Doctor must save a roster and generate future slots. |
| No case to assign | Doctor must approve it first; an available nurse and confirmed destination are required. |
