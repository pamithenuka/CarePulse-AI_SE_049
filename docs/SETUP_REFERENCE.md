# Detailed setup reference

SE3090 emergency-care coordination prototype: ASP.NET Core 8, PostgreSQL, React **Create React App** (`react-scripts`), Flutter, and Gemini. Rosters use **Asia/Colombo** time; API timestamps are UTC. See [implementation status](IMPLEMENTATION_STATUS.md), [manual tests](MANUAL_TEST_PLAN.md), and [assignment audit](INITIAL_AUDIT.md).

## First setup on each laptop

Prerequisites: .NET SDK 8, Node 22 with npm, Flutter **3.47.1 / Dart 3.13.1**, and PostgreSQL 18 (Docker is optional). Keep `package-lock.json` and `pubspec.lock`; use the pinned EF tool.

From the repository root:

```sh
dotnet tool restore
dotnet restore CarePulse.sln
# Optional local database; skip if PostgreSQL is already running:
docker compose up -d postgres
cp backend-api/appsettings.Local.example.json backend-api/appsettings.Local.json
```

PowerShell: replace `cp` with `Copy-Item`. Edit the copied JSON locally: set the database connection, a random JWT secret of at least 32 characters, your Gemini key, and a strong `Seed:AdminPassword`. Never commit this file. Environment variables (`ConnectionStrings__DefaultConnection`, `Jwt__Secret`, `AI__GeminiApiKey`, etc.) override it. The committed configuration contains no working secrets. .NET user secrets are also loaded in Development, but the optional Local JSON and environment variables take precedence.

For a **new local database**, apply migrations with the same connection you put in the file. The design-time factory intentionally does not read the private Local JSON:

```sh
# macOS/Linux
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=carepulse_dev;Username=postgres;Password=local-dev-only'
dotnet ef database update --project backend-api --context CarePulseDbContext
dotnet run --project backend-api --launch-profile http
```

```powershell
# PowerShell equivalent
$env:ConnectionStrings__DefaultConnection = 'Host=localhost;Port=5432;Database=carepulse_dev;Username=postgres;Password=local-dev-only'
dotnet ef database update --project backend-api --context CarePulseDbContext
dotnet run --project backend-api --launch-profile http
```

API: `http://localhost:5014`; liveness: `/health`; development Swagger: `/swagger`. First startup with `Database:SeedOnStartup=true` creates roles and the configured admin. After confirming login, set it to false and remove `Seed:AdminPassword`. Create doctor/nurse accounts through the admin staff pages; patients self-register. Seeding does **not** apply migrations. With an empty Gemini key, triage safely requests manual review; natural-language appointment search needs a valid key.

**Existing shared Neon databases:** follow [the migration guide](DATABASE_UPGRADE.md) first. Do not run `EnsureDeleted`, reset migrations, or point automated tests at the application database. Changes adding foreign keys will reject old orphan/demo records rather than deleting them.

## Clients

Web (separate terminal):

```sh
cd web-admin
npm ci
cp .env.example .env.local
npm start
```

Open `http://localhost:3000`. This is CRA, so the variable is `REACT_APP_API_BASE_URL`, including `/api/v1`; it is not a Vite variable. Rebuild after changing a deployed API URL.

Mobile (separate terminal):

```sh
cd mobile-app
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5014/api/v1
# Flutter Web, if desired:
flutter run -d chrome --web-port=8080 --dart-define=API_BASE_URL=http://localhost:5014/api/v1
```

Android emulator uses `10.0.2.2`; iOS simulator uses `localhost`. A physical phone needs the development computer's reachable LAN address, the API listening on `0.0.0.0:5014`, and its firewall allowing that port. No personal LAN address is committed. Android HTTP is enabled only in debug builds; release APKs require an HTTPS API. iOS permits local networking and prompts for foreground location access. GPS sharing runs while the route screen is active; background dispatch tracking is not implemented.

```sh
flutter build apk --release --dart-define=API_BASE_URL=https://YOUR_API_HOST/api/v1
```

Use your real host. Configure Android signing for distribution. APK generation and physical-device acceptance still need to be completed on the intended release configuration.

## Verification

Backend tests create and drop uniquely named databases on a **dedicated local/CI server**. Its account needs CREATE DATABASE permission. Do not use production/Neon credentials.

```sh
export CAREPULSE_TEST_CONNECTION='Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=local-dev-only'
dotnet test CarePulse.sln
cd web-admin
CI=true npm test -- --watchAll=false --runInBand
npm run build
cd ../mobile-app
flutter analyze
flutter test
```

PowerShell: set `$env:CAREPULSE_TEST_CONNECTION = '...'` and `$env:CI = 'true'` before the corresponding commands. CI runs backend tests with PostgreSQL, web tests/build, and Flutter analysis/tests for pushes to main/develop/features and PRs to main/develop. A workflow file is not evidence of a successful GitHub run: inspect Actions after pushing.

Use [the student-by-student manual plan](MANUAL_TEST_PLAN.md) with synthetic data. Test results and remaining submission requirements are recorded in [implementation status](IMPLEMENTATION_STATUS.md). Do not send live patient text during development or model evaluation.

## Deployment configuration

Supply DB/JWT/Gemini secrets through the host's secret settings; allow only deployed client origins in `Cors:AllowedOrigins`. Deploy behind HTTPS, keep uploads in persistent private storage (`backend-api/App_Data` relative to content root), and back up both PostgreSQL and uploaded files. `/health` is process liveness, not a database or AI readiness probe. `Swagger:Enabled=true` exposes evaluator documentation in a deployed environment; all protected API actions still require JWTs.

Gemini output is advisory; authorized staff perform high-impact actions. ETA is a straight-line estimate, SMS is simulated, and PII regex minimization is incomplete. This repository is a university prototype, not a clinically validated emergency service.
