\set ON_ERROR_STOP on
-- One-time repair authorized by the owner on 2026-09-29 for existing TEST data.
-- Run only with the reviewed migration in ONE transaction after a verified backup.
-- This file intentionally has no COMMIT. The upgrade runner supplies the transaction.
-- Aborts if the known affected record counts or migration state have changed.
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '60s';
LOCK TABLE "__EFMigrationsHistory", "TriageTickets", "PatientProfiles", "DoctorProfiles",
 "NurseProfiles", "DispatchTickets", "RouteLogs", "OnSiteVitalsRecords",
 "AppointmentSlots", "ConsultationRecords", "RiskAssessments", "ApprovalQueues", "AiTriageLogs"
 IN SHARE ROW EXCLUSIVE MODE;

DO $$ BEGIN
 IF EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928050315_IntegratedWorkflowSafety') THEN
   RAISE EXCEPTION 'Upgrade already applied; do not repeat this repair.';
 END IF;
END $$;

CREATE TEMP TABLE repair_triage ON COMMIT DROP AS
 SELECT t."Id" FROM "TriageTickets" t
 WHERE NOT EXISTS (SELECT 1 FROM "PatientProfiles" p WHERE p."Id" = t."PatientProfileId");
CREATE TEMP TABLE repair_slots ON COMMIT DROP AS
 SELECT s."Id" FROM "AppointmentSlots" s WHERE s."PatientId" IS NOT NULL
 AND NOT EXISTS (SELECT 1 FROM "PatientProfiles" p WHERE p."Id" = s."PatientId");
CREATE TEMP TABLE repair_dispatch ON COMMIT DROP AS
 SELECT d."Id", d."NurseId", d."Status" FROM "DispatchTickets" d
 WHERE NOT EXISTS (SELECT 1 FROM "TriageTickets" t WHERE t."Id" = d."TriageTicketId")
 OR d."TriageTicketId" IN (SELECT "Id" FROM repair_triage)
 OR NOT EXISTS (SELECT 1 FROM "DoctorProfiles" p WHERE p."Id" = d."DoctorId");
CREATE TEMP TABLE repair_duplicate_vitals ON COMMIT DROP AS
 SELECT "Id" FROM (SELECT "Id", row_number() OVER (
   PARTITION BY "DispatchTicketId" ORDER BY "RecordedAt" DESC, "CreatedAt" DESC, "Id" DESC) AS n
   FROM "OnSiteVitalsRecords" WHERE "DispatchTicketId" NOT IN (SELECT "Id" FROM repair_dispatch)) v WHERE n > 1;
DO $$ BEGIN
 IF (SELECT count(*) FROM repair_triage) <> 7 OR
    (SELECT count(*) FROM repair_slots) <> 2 OR
    (SELECT count(*) FROM repair_dispatch) <> 3 OR
    (SELECT count(*) FROM repair_duplicate_vitals) <> 0 THEN
   RAISE EXCEPTION 'Affected data changed; stop and review the archive set again.';
 END IF;
END $$;

CREATE SCHEMA carepulse_archive_20260929;
REVOKE ALL ON SCHEMA carepulse_archive_20260929 FROM PUBLIC;
CREATE TABLE carepulse_archive_20260929.records (
 source_table text NOT NULL, record_id uuid NOT NULL, reason text NOT NULL,
 archived_at timestamptz NOT NULL DEFAULT now(), original_record jsonb NOT NULL,
 PRIMARY KEY (source_table, record_id)
);
INSERT INTO carepulse_archive_20260929.records (source_table,record_id,reason,original_record)
 SELECT 'TriageTickets',t."Id",'Test case references a missing patient',to_jsonb(t) FROM "TriageTickets" t WHERE t."Id" IN (SELECT "Id" FROM repair_triage)
 UNION ALL SELECT 'RiskAssessments',t."Id",'History of archived test case',to_jsonb(t) FROM "RiskAssessments" t WHERE t."TriageTicketId" IN (SELECT "Id" FROM repair_triage)
 UNION ALL SELECT 'ApprovalQueues',t."Id",'History of archived test case',to_jsonb(t) FROM "ApprovalQueues" t WHERE t."TriageTicketId" IN (SELECT "Id" FROM repair_triage)
 UNION ALL SELECT 'AiTriageLogs',t."Id",'History of archived test case',to_jsonb(t) FROM "AiTriageLogs" t WHERE t."TriageTicketId" IN (SELECT "Id" FROM repair_triage)
 UNION ALL SELECT 'AppointmentSlots',t."Id",'Test booking references a missing patient',to_jsonb(t) FROM "AppointmentSlots" t WHERE t."Id" IN (SELECT "Id" FROM repair_slots)
 UNION ALL SELECT 'ConsultationRecords',t."Id",'Consultation of archived test booking',to_jsonb(t) FROM "ConsultationRecords" t WHERE t."SlotId" IN (SELECT "Id" FROM repair_slots)
 UNION ALL SELECT 'DispatchTickets',t."Id",'Test dispatch references missing case or doctor',to_jsonb(t) FROM "DispatchTickets" t WHERE t."Id" IN (SELECT "Id" FROM repair_dispatch)
 UNION ALL SELECT 'RouteLogs',t."Id",'Route history of archived test dispatch',to_jsonb(t) FROM "RouteLogs" t WHERE t."DispatchTicketId" IN (SELECT "Id" FROM repair_dispatch)
 UNION ALL SELECT 'OnSiteVitalsRecords',t."Id",'Archived dispatch history or older duplicate test completion; latest retained',to_jsonb(t) FROM "OnSiteVitalsRecords" t WHERE t."DispatchTicketId" IN (SELECT "Id" FROM repair_dispatch) OR t."Id" IN (SELECT "Id" FROM repair_duplicate_vitals)
 UNION ALL SELECT 'NurseProfiles',t."Id",'Availability snapshot before releasing archived active assignment',to_jsonb(t) FROM "NurseProfiles" t WHERE t."Id" IN (SELECT "NurseId" FROM repair_dispatch WHERE "Status" IN ('Assigned','EnRoute','ArrivedOnSite'));

DELETE FROM "AiTriageLogs" WHERE "TriageTicketId" IN (SELECT "Id" FROM repair_triage);
DELETE FROM "ApprovalQueues" WHERE "TriageTicketId" IN (SELECT "Id" FROM repair_triage);
DELETE FROM "RiskAssessments" WHERE "TriageTicketId" IN (SELECT "Id" FROM repair_triage);
DELETE FROM "ConsultationRecords" WHERE "SlotId" IN (SELECT "Id" FROM repair_slots);
DELETE FROM "AppointmentSlots" WHERE "Id" IN (SELECT "Id" FROM repair_slots);
DELETE FROM "RouteLogs" WHERE "DispatchTicketId" IN (SELECT "Id" FROM repair_dispatch);
DELETE FROM "OnSiteVitalsRecords" WHERE "DispatchTicketId" IN (SELECT "Id" FROM repair_dispatch) OR "Id" IN (SELECT "Id" FROM repair_duplicate_vitals);
DELETE FROM "DispatchTickets" WHERE "Id" IN (SELECT "Id" FROM repair_dispatch);
DELETE FROM "TriageTickets" WHERE "Id" IN (SELECT "Id" FROM repair_triage);
UPDATE "NurseProfiles" n SET "IsAvailable" = true, "UpdatedAt" = now()
 WHERE n."Id" IN (SELECT "NurseId" FROM repair_dispatch WHERE "Status" IN ('Assigned','EnRoute','ArrivedOnSite'))
 AND NOT EXISTS (SELECT 1 FROM "DispatchTickets" d WHERE d."NurseId"=n."Id" AND NOT d."IsDeleted" AND d."Status" IN ('Assigned','EnRoute','ArrivedOnSite'));
SELECT source_table, count(*) AS archived_rows FROM carepulse_archive_20260929.records GROUP BY source_table ORDER BY source_table;
