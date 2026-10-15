# Pre-assignment nurse location and ETA

Restart the backend and hot restart Flutter after updating both components.
No database migration is required. Existing nurse availability is retained;
location sharing requires the nurse to tap the new control.

## Manual checks

1. Sign in as a nurse with no active assignment. Nurse Home shows availability.
2. Tap **Go available**, or **Share location** if already available. Enable GPS
   and grant permission. On an emulator, supply a simulated location first.
3. Wait for **Location updated at ...**. Keep Nurse Home open. The app requests
   a fresh GPS fix and sends it every 30 seconds, including while stationary.
4. As the approving doctor, select that nurse and a confirmed patient destination.
   Assign once and review warnings. A location recorded within five minutes is
   used for ETA; a missing, older, or future timestamp is treated as unknown.
5. Acknowledge warnings and assign. Refresh Nurse Home: it shows an active
   assignment and disables availability changes. Open the dispatch for existing
   route tracking. The server rejects pre-assignment GPS updates while busy.
6. Complete the visit. Return to Nurse Home, refresh and tap **Share location**
   to start pre-assignment sharing again.
7. Tap **Go unavailable**: location sharing stops and the server clears the
   previous location timestamp. The nurse cannot be selected as available.
8. Deny permission or disable GPS: availability can still be enabled, but the
   screen explains that ETA is unavailable. Missing ETA generates a safety
   warning requiring acknowledgement; it is never sent to the AI as zero.
9. Background the app: GPS refresh stops. Return and tap **Share location** to
   restart. Leaving Nurse Home or signing out also stops its GPS collection.
   A request already sent cannot be recalled. Last recorded location can remain
   usable for up to five minutes; availability itself remains until changed or
   assigned. This is foreground sharing, not background tracking.
10. Disconnect the network while sharing. The app stops sharing and shows a retry
    message instead of claiming a new location was saved. Restore connectivity
    and tap **Share location**.

## ETA limitation

The existing route service uses Haversine distance, a 1.4 distance multiplier and
an assumed speed of 40 km/h. ETA is approximate, not a live traffic or road-route
prediction. Gemini evaluates the supplied estimate and does not calculate it.

## Automated checks

NurseAvailabilityTests cover ownership, unavailable/busy restrictions and fresh,
stale and future location timestamps. ValidationAgentGoldenTests distinguish
unknown ETA from a genuine zero-minute estimate and retain approval requirements.
The existing mobile suite covers session expiry and dispatch tracking regressions.
The device permission/lifecycle checks above still require manual verification.
