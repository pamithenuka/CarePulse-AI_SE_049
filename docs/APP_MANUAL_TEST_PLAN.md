# CarePulse — test using only the web and Flutter apps

This guide uses screens, menus and buttons. You do **not** need Swagger, API requests, browser address-bar routes, database queries or copied record IDs.

Start all three applications using the [simple README](../README.md). Use synthetic people and symptoms. Keep the backend terminal running. A working Gemini key is needed to evaluate live AI behavior.

## Before testing: create your accounts

1. In the web app, log in as the admin configured in your private settings.
2. Open **Doctors → + Register Doctor**. Create **Demo Doctor One**, choose **CARDIOLOGY**, and save the email/password somewhere private.
3. Create **Demo Doctor Two** with another email.
4. Open **Nurses → + Register Nurse**. Create **Demo Nurse One** and **Demo Nurse Two**, each with a different email. New accounts start unavailable. In the **Nurses** list, click each nurse’s name, then click **Mark available** on their detail page. Check that the badge changes to **Available**. Availability can be changed by Admin only; active dispatches must be completed first.
5. In Flutter, choose **Patient**, open registration, and create **Demo Patient One**. Complete the patient profile.
6. Create **Demo Patient Two** in another Flutter session, or sign out and register the second account. Use different identity details.
7. Keep the admin, doctor, patient and nurse accounts clearly separated. Use separate browser profiles for admin and doctor if possible. For patient and nurse, use two devices/emulators, or log out and restart Flutter to choose the other role. One app session cannot be both users at once.

**Expected:** staff appear in the web lists, patients appear under **Patients**, and each account can sign in. Staff accounts are created by the admin, not by patient registration.

## Student 1 — patient identity, records and AI plan

### A. Profile and medical records

1. In Flutter as Patient One, open **Profile**, change a non-sensitive test field, and save.
2. In the web app, open **Patients**, select Patient One, and inspect **Overview**. Reload the page if needed.
3. In Flutter, open **History** and tap the **+** button at the bottom right. Enter **Condition:** `Demo condition A`, **Diagnosed on:** today, **Notes:** `First test entry`; leave medications blank and chronic unchecked, then tap **Save**. Add a second entry named `Demo condition B` with notes `Temporary test entry`. Flutter currently supports adding/viewing history; editing and removal are on the web. In the web app as Admin or Doctor, open **Patients → Demo Patient → Medical History & Medications**. Edit entry A’s notes to `Updated test entry` and save. Remove entry B using its removal control and confirm if prompted. Pull down to refresh Flutter History: A should show the updated notes and B should disappear.
4. Open **Contacts**, add a synthetic emergency contact, edit it, then delete a spare contact.
5. On the web patient's page, inspect **Medical History & Medications** and **Emergency Contacts**.
6. Sign out of Flutter, sign in again, and check the saved profile/records.

**Expected:** both apps show the same saved information. Deleted items stay removed after reopening. Signing in as Patient Two shows Patient Two's own records.

### B. Documents

1. In Flutter, open **Documents**, use the add/upload control, and select a small synthetic image. Allow camera/photo access if requested.
2. Open the uploaded document in Flutter.
3. In the web patient's **Documents** tab, find and download that same document.
4. Delete a spare document through the UI; reload both clients.

**Expected:** the correct document opens and the deleted entry disappears. Do not use real medical documents for this test.

### C. AI plan and emergency alert

1. In Flutter, open **AI Plan**, enter a synthetic care objective such as `Test patient: dry cough and fatigue for two days. Plan assessment and clinician review.`, and tap **Generate AI Plan**.
2. In the web patient's **AI Care Plan** tab, inspect the saved plan and its review controls as an authorized staff user.
3. In Flutter, use **SOS** and follow its confirmation steps. In the web app, check **Emergency Alerts**.

**Expected:** the new plan shows **PlanCreated** with DomainAnalysis, ActionTool and Validation steps, and appears in the web app after refreshing. It does not automatically book or dispatch. An explicit AI error verifies error handling, but does **not** pass the successful generation test. The emergency alert is recorded. SMS delivery is simulated, so do not expect a real phone message.

If you previously received an AI service error, restart the backend after updating its configuration and generate a **new** plan. Old failed attempts remain in history. The current example uses `AI:GeminiModel` = `gemini-3-flash-preview`, verified with a synthetic live care-plan request on 29 September 2026. Each teammate's ignored `appsettings.Local.json` must use a model their own Google project can access; pulling code does not update that private file.

## Student 3 — prepare appointments, then test booking

Do this before triage so the scheduling agent has real availability to find.

### A. Create doctor availability

1. In the web app, log out of admin and sign in as Doctor One.
2. Open **Rosters** and select Doctor One if a doctor selector appears.
3. Under **Set availability for a day**, choose a day and a valid start/end time. Save.
4. Under **Generate bookable slots**, choose a date range that includes the chosen weekday and generate slots.
5. Open **Slots** and select the corresponding date.

**Expected:** actual future slots appear. The roster uses Sri Lanka clinic time. For a quick consultation test, include a slot starting a few minutes from now and book it before it starts.

### B. Book through the patient app

1. In Flutter as Patient One, open **Doctors**.
2. Search for Doctor One, open the doctor, and choose the date you generated.
3. Book an open slot and wait for confirmation.
4. In the web app, open **Consultations** as Doctor One and look in **Booked appointment** for Patient One and the chosen time.
5. In Flutter, also try **Ask CarePulse** from doctor search. Ask for a cardiology appointment on your generated date. Book another returned slot if available.

**Expected:** the real signed-in patient is attached to each booking. AI search returns actual available slots; it does not book until you press the booking button. A booked slot is no longer available to another patient.

### C. Record a consultation

1. Wait until the booked appointment's start time has arrived. Keep the other tests moving while you wait.
2. In the web **Consultations** page, select that booking, enter synthetic notes and an optional prescription, then click **Complete consultation**.
3. Open **Patients → Patient One → Consultation History**.

**Expected:** the saved consultation belongs to Patient One and Doctor One. Trying to complete a future appointment is rejected; do not change source code or your device clock to bypass this rule.

## Student 2 — symptoms, AI triage and doctor review

### A. Submit and review a case

1. In Flutter as Patient One, open **Triage**.
2. Enter synthetic symptoms such as `Severe chest pain and difficulty breathing`. Choose a duration and **Severe** severity.
3. Tap **Share current location for dispatch** and grant permission. On an emulator, configure a simulated location first. If location is unavailable, staff can confirm a test destination during assignment.
4. Tap **SUBMIT SYMPTOMS** once and wait for the result.
5. In the web app as Doctor One, open **AI Triage**, find the new case and open its review/details.
6. Inspect the symptoms, risk result and workflow execution details. Enter review notes and click **Approve for Dispatch**.
7. Return to the patient's status screen; allow a polling interval for the update.

**Expected:** the same patient's case appears on web and Flutter. A severe case requires doctor review. After approval it waits for nurse assignment—it is not already a completed visit. An unavailable AI provider must show failure/manual review rather than pretending it assessed successfully.

### B. Check rejection and history

1. Submit another synthetic severe case.
2. In web **AI Triage**, open it, add a rejection reason and use the rejection controls.
3. Confirm the patient sees rejection.
4. Restart Flutter, sign in as Patient One, open **Triage**, and tap the history icon (**My cases**).
5. Reopen the first approved case for the dispatch test below.

**Expected:** saved cases can be reopened without submitting again; rejected cases do not appear as approved assignments.

## Student 4 — nurse dispatch and completion

### A. Assign the approved case

1. In the web app as Doctor One (the approving doctor), open **Dispatch Center**.
2. Find Patient One under **Waiting for assignment**.
3. Select Demo Nurse One. Check the patient's destination coordinates; if missing, enter the known location of your synthetic test case. Do not use a random real person's address.
4. Click **Assign nurse**. If safety warnings appear, read them, select the acknowledgement checkbox and click again.
5. Check that the case moves into **Active dispatches**.

**Expected:** one dispatch exists and Nurse One is unavailable for another assignment. The waiting case is not lost if assignment fails. Coordinates displayed on the map come from the case and nurse telemetry, not a sample route.

### B. Use the nurse app

1. Open another Flutter session, choose **Field Nurse**, and log in as Nurse One. If using one device, first log out of the patient session and restart the app.
2. Open the assigned case from the nurse dashboard.
3. Tap **Start / resume tracking** and grant location permission. Keep the route screen open.
4. In web **Dispatch Center**, check the nurse's position and last-update time.
5. On the nurse screen, tap **Confirm arrival on site**, then **Record vitals**.
6. Enter these synthetic measurements: heart rate **80**, systolic **120**, diastolic **80**, temperature **37**, oxygen **98**, and a short clinical note.
7. Click **Complete & Submit Vitals** and wait for success.
8. Refresh web **Dispatch Center**. Then sign in as Patient One in Flutter and reopen the case through **My cases**.

**Expected:** the active dispatch disappears after completion, Nurse One becomes available again, and the patient sees **VISIT_COMPLETED**. The map is not turn-by-turn road navigation. GPS updates require the route screen to remain active.

### C. Quick failure checks using the apps

- **Wrong nurse:** before completion, log in as Nurse Two. Nurse One's case must not appear in Nurse Two's dashboard.
- **Invalid vitals:** try oxygen `150`, temperature `abc`, or diastolic pressure higher than systolic. Submission must be blocked or rejected clearly.
- **Network failure:** while a synthetic route is active, temporarily disconnect the nurse device. It must show a tracking/submission error. Reconnect and resume; failure must not show a completion success.
- **Escalation:** on another arrived test case, enter notes and press **Escalate Emergency**. Confirm it and check that web displays the escalation. This records an alert; it does not place a real call or send a real SMS.
- **No available nurse:** leave both nurses assigned to two active synthetic cases, then approve a third case. The third case must remain waiting. Complete one nurse's visit, refresh, and assign the waiting case.

## Final team run

Repeat one uninterrupted chain:

**Patient registers → submits symptoms → doctor reviews/approves → dispatcher assigns nurse → nurse shares location → confirms arrival → submits vitals → patient sees completion.**

Also check one appointment booking and one completed consultation. Use the existing records rather than creating a new patient for every step.

Record your results:

| Student / test | Expected result | Observed result | Pass / fail |
| --- | --- | --- | --- |
| 1: Profile, records, documents | Same saved data in both apps | | |
| 1: AI plan / SOS | Plan or explicit failure; recorded alert | | |
| 2: Approval / rejection / history | Correct decision and saved case | | |
| 3: Roster / booking / consultation | Correct doctor, patient and time | | |
| 4: Assignment / GPS / vitals | One completed visit; nurse available | | |
| Whole team | Patient sees final completion | | |

These UI checks cover practical workflows. They do not prove direct API ownership enforcement, concurrent-write guarantees or token-tampering resistance; those are covered by automated tests and the separate [advanced manual plan](MANUAL_TEST_PLAN.md).
