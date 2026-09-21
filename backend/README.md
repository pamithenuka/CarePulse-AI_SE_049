# CarePulse – Student 3 Backend (Doctor Rostering, Scheduling & Consultations)

This folder is a self-contained ASP.NET Core Web API for your slice of CarePulse.

## What's in here
- `Models/` — the 4 database tables (DoctorProfile, ClinicRoster, AppointmentSlot, ConsultationRecord)
- `Data/CarePulseDbContext.cs` — EF Core database context
- `Controllers/` — the 4 required endpoints
- `DTOs/` — request/response shapes used by the API (keeps the raw DB tables from being exposed directly)

## One-time setup on your machine

1. **Install prerequisites** (if not already installed):
   - [.NET 8 SDK](https://dotnet.microsoft.com/download)
   - [PostgreSQL](https://www.postgresql.org/download/) (or run it via Docker: `docker run --name carepulse-db -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres`)

2. **Copy this `backend/` folder** into your cloned repo (`CarePulse-AI_SE_049/`), so you end up with e.g.:
   ```
   CarePulse-AI_SE_049/
     backend/
       CarePulse.Api/
   ```

3. **Update the connection string** in `CarePulse.Api/appsettings.json` if your local Postgres username/password/database name differ from the defaults.

4. **Create the database** (once Postgres is running):
   ```
   createdb -U postgres carepulse
   ```

5. **Install the EF Core CLI tool** (once, globally):
   ```
   dotnet tool install --global dotnet-ef
   ```

6. **From inside `backend/CarePulse.Api/`, restore packages and create your first migration:**
   ```
   dotnet restore
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
   This reads your 4 models and creates the actual tables in Postgres.

7. **Run the API:**
   ```
   dotnet run
   ```
   Then open the Swagger UI it prints in the terminal (something like `https://localhost:5001/swagger`) to see and test your 4 endpoints interactively — no frontend needed yet.

## Sanity-check the flow manually (via Swagger)
1. Insert a doctor row directly in Postgres (or add a small seed — ask me and I'll add one).
2. `PUT /api/doctors/roster` to give that doctor a weekly availability window.
3. (Next step, not built yet) generate `AppointmentSlots` from the roster — we'll add this next.
4. `GET /api/doctors/slots` to see open slots.
5. `POST /api/appointments/book` to book one.
6. `POST /api/consultations/summary` to log the visit outcome.

## What's intentionally not done yet
- No slot-generation logic yet (turning a `ClinicRoster` into actual `AppointmentSlot` rows) — this is the natural next step.
- No auth — the whole team will likely add this together later, or per your instructor's requirements.
- No seed data yet.
