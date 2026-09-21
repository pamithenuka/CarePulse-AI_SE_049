# CarePulse – Doctor Web App (React)

This is the doctor/staff-facing web app for your slice: weekly roster, appointment slots, and consultation summaries.

## One-time setup

1. **Install Node.js** if you don't have it: [nodejs.org](https://nodejs.org) (get the LTS version). Confirm with:
   ```
   node --version
   ```

2. **Copy this `frontend/` folder** into your cloned repo, alongside `backend/`:
   ```
   CarePulse-AI_SE_049/
     backend/
       CarePulse.Api/
     frontend/
   ```

3. **Open a terminal in VS Code inside the `frontend/` folder** and install dependencies:
   ```
   cd frontend
   npm install
   ```
   This downloads React and the other packages listed in `package.json` - may take a minute.

## Running it

**Your backend API must already be running** (`dotnet run` in `backend/CarePulse.Api`, in its own terminal tab) before you start the frontend - the web app talks to it directly.

In a new terminal, inside `frontend/`:
```
npm run dev
```
It'll print a local address, usually `http://localhost:5173`. Open that in your browser.

## What you'll see

- **Weekly roster** — set a doctor's recurring availability and generate real bookable slots for a date range
- **Appointment slots** — view open slots, book one (using a placeholder patient ID for now, until Student 1's patient module exists)
- **Consultations** — log notes + prescription against a booked slot

## Known placeholder

There's no patient login/selection yet since Student 1's patient module isn't built. Booking and consultations currently use a fixed placeholder Patient ID (`11111111-1111-1111-1111-111111111111`). When your team merges branches, swap this for a real logged-in patient ID.
