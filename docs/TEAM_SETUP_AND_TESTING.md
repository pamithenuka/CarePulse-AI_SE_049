# Team guide: pull develop, run CarePulse and finish testing

Use this guide after the integration changes have been merged into `develop`. It assumes you are using **our existing shared Neon database**. You do not need Swagger or database queries for these tests.

## 1. Get the latest code

Open a terminal in your existing CarePulse project folder. Save your files and run:

```sh
git status
```

If you have unfinished changes, commit them to your own branch or stash them before switching. Do not discard them. Then run:

```sh
git fetch origin
git switch develop
git pull --ff-only origin develop
```

If Git reports conflicts or divergent branches, stop and coordinate with the team; do not force-reset your work. If you stashed changes, leave them on your original branch while testing the clean integration.

For a first checkout, clone the team's repository using its GitHub **Code** button, open the cloned folder, and switch to `develop`.

## 2. Check the required software

You need Git, .NET 8, Node.js 22 with npm, and Flutter 3.47.1 / Dart 3.13.1. Android Studio with a working Android emulator is recommended for the nurse location test.

```sh
dotnet --version
node --version
npm --version
flutter --version
flutter doctor
```

Resolve any Flutter issues affecting your chosen device. Docker and a local PostgreSQL installation are **not needed** when using our shared Neon database.

## 3. Prepare your private settings

Inside `backend-api`, copy `appsettings.Local.example.json` and name the copy **appsettings.Local.json**. If your local file already exists, update it instead of overwriting it blindly.

Use this structure, replacing the three placeholder values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "PASTE_SHARED_NEON_CONNECTION_STRING"
  },
  "Jwt": {
    "Secret": "PASTE_YOUR_RANDOM_SECRET_AT_LEAST_32_CHARACTERS"
  },
  "AI": {
    "GeminiApiKey": "PASTE_VALID_GEMINI_API_KEY",
    "GeminiModel": "gemini-3-flash-preview"
  },
  "Database": { "SeedOnStartup": false },
  "Seed": {
    "AdminEmail": "",
    "AdminPassword": "",
    "DemoPatients": false
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:8080"]
  }
}
```

Ask the team coordinator privately for:

- The shared Neon connection string, in .NET format (`Host=...;Database=...;Username=...;Password=...;SSL Mode=Require`).
- The team Gemini key, or use your own key with access to the configured model. A shared key also shares provider quota.
- Existing test admin login credentials. These are login details, not the blank seed fields above.

Each separate local backend can use its own JWT secret. On macOS/Linux, generate one with `openssl rand -hex 32`. On Windows PowerShell:

```powershell
$jwtBytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($jwtBytes)
[Convert]::ToBase64String($jwtBytes)
$rng.Dispose()
```

Paste the generated value into `Jwt:Secret` and keep it unchanged between runs. Never post keys, passwords or the completed local file to GitHub. `appsettings.Local.json` is ignored; the shared settings and example file are committed.

Old environment variables such as `ConnectionStrings__DefaultConnection`, `AI__GeminiModel`, or `AI__GeminiApiKey` override this file. Remove obsolete overrides from your terminal/IDE configuration if it uses different settings. Restart the backend after changing configuration.

## 4. Restore dependencies without resetting Neon

From the project root:

```sh
dotnet tool restore
dotnet restore CarePulse.sln
```

These commands restore development tools and packages; they do not reset the database.

**Our shared Neon database was upgraded on 29 September 2026. Do not rerun the one-off repair script, reset the database, enable demo seeding, or apply migrations simply because you pulled the code.** See [the recorded upgrade status](NEON_UPGRADE_STATUS.md). If a later change needs another migration, coordinate it once for the shared database. A different/new database requires the separate [database guide](DATABASE_UPGRADE.md).

## 5. Start the three applications

Keep three terminals open. Start each from the project root.

### Terminal 1: backend

```sh
dotnet run --project backend-api --launch-profile http
```

Wait for the listening message on port **5014**. Leave it running. If startup fails, fix that before starting the test.

### Terminal 2: staff web app

```sh
cd web-admin
npm ci
npm start
```

Use the browser opened by the command (normally port **3000**). Sign in with the existing test admin account. If `web-admin/.env.local` exists, its API setting should be:

```dotenv
REACT_APP_API_BASE_URL=http://localhost:5014/api/v1
```

Restart the web app after editing its environment file. If port 3000 is occupied, stop your old copy rather than accepting an unexpected port that is not allowed by CORS.

### Terminal 3: patient/nurse Flutter app

Start the Android emulator first, then run:

```sh
cd mobile-app
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5014/api/v1
```

Choose the Android emulator if prompted. This address connects the Android emulator to the backend on **your own laptop**.

For an iOS simulator on a Mac, use instead:

```sh
flutter run --dart-define=API_BASE_URL=http://localhost:5014/api/v1
```

For a browser-only check, use instead:

```sh
flutter run -d chrome --web-port=8080 --dart-define=API_BASE_URL=http://localhost:5014/api/v1
```

Use an emulator for the simulated GPS steps below. Real-phone setup differs; see [the detailed setup guide](SETUP_REFERENCE.md). No source-code changes are needed to select an API address.

## 6. Choose accounts and coordinate testing

The shared Neon database contains everyone's saved accounts and test records.

- Testing one at a time: reuse a coordinated set of existing test accounts.
- Testing together: create a separate set per tester, for example **Alex Doctor One**, **Alex Nurse One**, **Alex Patient One**, plus a second doctor, nurse and patient for negative tests.
- Use distinct emails and synthetic identity/license details. Keep passwords private.
- The existing admin creates doctors and nurses through **Doctors / Nurses → Register**. Choose **CARDIOLOGY** for Doctor One.
- New nurses start unavailable. As Admin, click each nurse's name and choose **Mark available**.
- Patients register through Flutter and complete their profiles.

In the manual plan, read “Patient One”, “Doctor One” and “Nurse One” as **your chosen accounts**. Do not create another set if yours already exists. Do not seed another admin.

Use separate browser profiles for doctor/admin if possible. For patient/nurse, use separate emulators or log out and restart Flutter to choose the other role. One session cannot be both accounts.

All doctors currently share the pending-triage queue. Only review your own test patient's cases. Separate accounts do not isolate the shared database or shared queues. Coordinate the “no available nurse” check so you do not alter other testers' nurses.

## 7. Set the emulator's test location

Before patient triage submission, open Android emulator **⋮ → Location → Single points**.

1. Search for `6.9271, 79.8612`, select the point, and click **Set Location**.
2. Keep **Enable GPS signal** on.
3. In Flutter, allow location permission when submitting triage.

These are coordinates for a synthetic Colombo test destination. If the assignment screen still contains `37.4219983, -122.084`, that is the emulator's previous US location: replace the patient destination with the intended test coordinates before assignment.

For the nurse tracking test, set that nurse emulator's location to `6.9280, 79.8620`, then tap **Start / resume tracking**. Before confirming arrival, set the nurse location to the test destination `6.9271, 79.8612` and wait for the update. Changing coordinates on the assignment form does **not** change emulator GPS.

## 8. Run the manual plan in this order

Open [APP_MANUAL_TEST_PLAN.md](APP_MANUAL_TEST_PLAN.md). Follow its numbered steps, using these explanations where needed. No Swagger is required.

### First: Student 1 — identity and records

1. **Profile:** tap **Contact details → Edit**, change the demo address to `Test Campus, Building A`, and save. Confirm it on the web patient's Overview.
2. **History:** add Demo condition A and B. Edit A on the web; remove B with **Entered in error**. Confirm Flutter shows the saved changes after refreshing.
3. **Contacts and Documents:** add, view, edit where supported, and remove spare synthetic items. Check both clients and patient account separation.
4. **AI Plan:** generate the example objective in the manual plan. Expect **PlanCreated** and three delegation steps. In the web review controls, enter `Reviewed for synthetic manual testing`, approve, and confirm the review remains saved after refresh and appears in Flutter.
5. **SOS:** confirm a test alert and check web **Emergency Alerts**. It records an alert; SMS is simulated, and SOS does not automatically dispatch a nurse.

An AI error is a failed generation test, even when the error is displayed correctly. Check key/model/quota and retry after fixing configuration. Old failed attempts remain in history.

### Second: Student 3 — scheduling and booking

1. Save Doctor One's roster and generate slots for a matching date. Times use **Sri Lanka clinic time**.
2. For a quick test, choose **today**, with a roster starting around 15 minutes from now and enough time for at least two slots. Book before the slot starts. Do not copy old dates from screenshots.
3. Book one slot through Flutter **Doctors**. Verify it under web **Consultations → Booked appointment** for the correct doctor and patient.
4. Use **Ask CarePulse**, for example: `Find an available cardiology appointment on [your date] between [your start] and [your end].` Replace the brackets with actual values. Book a different returned slot and check it on the web.
5. Once the first booking's start time arrives, select it, enter `Synthetic consultation: test patient reviewed for workflow verification`, leave prescription blank, and complete the consultation. Check the patient's **Consultation History**.

Seeing the booking is enough for the booking step; do not complete it early. If the appointment is tomorrow, continue other tests and return later, or create and book a future slot today. Do not change the device clock to bypass the rule.

### Third: Student 2 — triage, approval and rejection

1. Submit the manual plan's synthetic severe case with location sharing.
2. As Doctor One, find your patient in **AI Triage** and inspect symptoms, risk and workflow execution details.
3. Enter `Reviewed symptoms, AI risk result and workflow details. Approved nurse dispatch for assessment and vitals collection in this synthetic test.` Click **Approve for Dispatch**.
4. Confirm Flutter changes to **Approved by Doctor**. This means authorized, not yet assigned or completed.
5. Complete **B. Check rejection and history** using a separate new case. Keep the original approved case for dispatch.

The recommended specialty is guidance; specialty-based review authorization is not currently enforced. A safety warning alone does not prove live Gemini execution, because fallback rules can produce the same warning. Inspect execution details and record fallback/error behavior honestly.

### Fourth: Student 4 — dispatch through completion

1. As the approving doctor, find your approved case in **Dispatch Center**, select your available Nurse One, and check the patient destination.
2. Click **Assign nurse**. Read warnings; for this synthetic test, acknowledge them and click again. Expect **Active dispatches → Assigned**.
3. Before completion, sign in as Nurse Two and confirm Nurse One's dispatch is absent. Then sign in as Nurse One and open it.
4. Set the nurse emulator location as described above. Tap **Start / resume tracking**, allow GPS, and keep the screen open.
5. Check the web shows **EnRoute**, Colombo coordinates, and a recent last-update time. “No telemetry received” means a location update has not arrived yet.
6. Move the simulated nurse location to the destination, wait for an update, and confirm arrival.
7. Check invalid vitals are rejected before saving valid ones. Then use heart rate **80**, systolic **120**, diastolic **80**, temperature **37**, oxygen **98**, and notes `Synthetic on-site assessment for manual testing`.
8. Click **Complete & Submit Vitals**. Confirm success, the dispatch disappears from the active list, the nurse becomes available, and the patient case shows **VISIT_COMPLETED**.
9. Follow the manual plan's remaining failure checks. Network failure requires an active route; escalation requires a separate arrived case. Coordinate the no-available-nurse scenario with everyone sharing Neon.

## 9. Finish and record results

Run one uninterrupted chain from patient submission through doctor approval, nurse assignment, tracking, arrival, vitals and patient completion. Also finish one booking and consultation. Do not mark the whole system passed just because login or AI planning worked.

Copy this table into your own test report, including tester name, date and commit (`git rev-parse --short HEAD`). Avoid passwords, keys and real patient information.

| Test | Observed result | Pass / Fail / Not tested |
| --- | --- | --- |
| Student 1: profile, history, contacts, documents | | |
| Student 1: successful AI plan, review and SOS alert | | |
| Student 3: roster, normal booking and AI search booking | | |
| Student 3: completed consultation and saved history | | |
| Student 2: approval, rejection and reopened history | | |
| Student 4: assignment, GPS, arrival and vitals | | |
| Student 4: nurse released and patient sees completion | | |
| Negative checks from the manual plan | | |
| Uninterrupted team workflow | | |

For a failure, record the action, expected result, actual error, account's test name and a screenshot without secrets. Report it to the team before retrying repeatedly. Do not delete shared records or reset Neon to fix it.

## 10. Common problems and later runs

| Problem | What to do |
| --- | --- |
| Login fails | Use existing account credentials; seed settings do not reset passwords. |
| App cannot reach backend | Keep Terminal 1 running; Android emulator uses `10.0.2.2`, web/iOS simulator use `localhost`. |
| AI generation/search fails | Check Gemini model access, key, network and shared quota. Restart backend after configuration changes. |
| No slots | Save roster, generate matching-date future slots, and check the intended doctor. |
| Nurse unavailable | Admin marks a new nurse available; a busy nurse must finish the active visit. |
| Approved case missing from waiting list | Check whether someone already assigned it; ensure this is the approved, not rejected, case. |
| Map spans the world | Nurse emulator GPS and patient destination are in different countries; set both deliberately. |
| No location update | Allow GPS, set emulator location, tap Start / resume tracking and leave the route screen open. |
| Session expired | Sign in again with the correct role. |

Stop your three local app processes with **Ctrl+C** when done. This does not remove database records. On later runs, start the three apps again; reinstall dependencies when their files change. Save your work before pulling new changes. Coordinate any future database upgrade with the team.
