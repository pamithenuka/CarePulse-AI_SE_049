-- READ ONLY. Run against a backup/Neon branch first. Do not paste credentials in this file.
-- Requires all migrations through AddStudent2TriageIntegration. Inspect migration history first.
BEGIN TRANSACTION READ ONLY;
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";

-- Every count below must be zero before applying IntegratedWorkflowSafety.
SELECT 'triage_missing_patient' AS issue, count(*) FROM "TriageTickets" t
LEFT JOIN "PatientProfiles" p ON p."Id" = t."PatientProfileId" WHERE p."Id" IS NULL
UNION ALL SELECT 'appointment_missing_patient', count(*) FROM "AppointmentSlots" s
LEFT JOIN "PatientProfiles" p ON p."Id" = s."PatientId" WHERE s."PatientId" IS NOT NULL AND p."Id" IS NULL
UNION ALL SELECT 'appointment_missing_doctor', count(*) FROM "AppointmentSlots" s
LEFT JOIN "DoctorProfiles" d ON d."Id" = s."DoctorId" WHERE d."Id" IS NULL
UNION ALL SELECT 'roster_missing_doctor', count(*) FROM "ClinicRosters" r
LEFT JOIN "DoctorProfiles" d ON d."Id" = r."DoctorId" WHERE d."Id" IS NULL
UNION ALL SELECT 'dispatch_missing_triage', count(*) FROM "DispatchTickets" d
LEFT JOIN "TriageTickets" t ON t."Id" = d."TriageTicketId" WHERE t."Id" IS NULL
UNION ALL SELECT 'dispatch_missing_nurse', count(*) FROM "DispatchTickets" d
LEFT JOIN "NurseProfiles" n ON n."Id" = d."NurseId" WHERE n."Id" IS NULL
UNION ALL SELECT 'dispatch_missing_doctor', count(*) FROM "DispatchTickets" t
LEFT JOIN "DoctorProfiles" d ON d."Id" = t."DoctorId" WHERE d."Id" IS NULL
UNION ALL SELECT 'route_missing_dispatch', count(*) FROM "RouteLogs" r
LEFT JOIN "DispatchTickets" d ON d."Id" = r."DispatchTicketId" WHERE d."Id" IS NULL
UNION ALL SELECT 'vitals_missing_dispatch', count(*) FROM "OnSiteVitalsRecords" v
LEFT JOIN "DispatchTickets" d ON d."Id" = v."DispatchTicketId" WHERE d."Id" IS NULL
UNION ALL SELECT 'unsupported_dispatch_status', count(*) FROM "DispatchTickets"
WHERE "Status" NOT IN ('Assigned','EnRoute','ArrivedOnSite','Completed') AND NOT "IsDeleted"
UNION ALL SELECT 'duplicate_active_nurse', count(*) FROM (
  SELECT "NurseId" FROM "DispatchTickets" WHERE NOT "IsDeleted" AND "Status" IN ('Assigned','EnRoute','ArrivedOnSite')
  GROUP BY "NurseId" HAVING count(*) > 1) duplicates
UNION ALL SELECT 'duplicate_dispatch_triage', count(*) FROM (
  SELECT "TriageTicketId" FROM "DispatchTickets" GROUP BY "TriageTicketId" HAVING count(*) > 1) duplicates
UNION ALL SELECT 'duplicate_dispatch_vitals', count(*) FROM (
  SELECT "DispatchTicketId" FROM "OnSiteVitalsRecords" GROUP BY "DispatchTicketId" HAVING count(*) > 1) duplicates
UNION ALL SELECT 'duplicate_doctor_day_roster', count(*) FROM (
  SELECT "DoctorId", "DayOfWeek" FROM "ClinicRosters" GROUP BY "DoctorId", "DayOfWeek" HAVING count(*) > 1) duplicates;

-- Old active cases need a staff-confirmed destination after migration; do not infer coordinates.
SELECT "Id", "Status" FROM "DispatchTickets" WHERE NOT "IsDeleted" AND "Status" <> 'Completed';
ROLLBACK;
