# CarePulse: Master System Architecture & Domain Context
**SE3090 — Software Engineering Frameworks (2026)**

> **AI INSTRUCTION NOTE:** When generating code, schemas, controllers, or UI components for CarePulse, strict adherence to the architecture, tech stack, constraints, and business rules outlined in this document is required.

---

## 1. Domain & Project Overview
CarePulse is an emergency healthcare triage and field response logistics platform designed to eliminate delay in critical triage cases[cite: 1].

### Core Integrated Architecture
* **Single Authority API:** React and Flutter applications communicate exclusively through a central C# ASP.NET Core REST API. Direct communication with external microservices or standalone AI servers is prohibited[cite: 1].
* **Unified Database:** PostgreSQL managed via Entity Framework Core.
* **Web Client (`web-admin/`):** React dashboard for Doctors and Admins (monitoring, approval queues, rosters, map dispatch).
* **Mobile Client (`mobile-app/`):** Flutter mobile app for Patients (symptom intake, tracking) and Field Nurses (GPS navigation, vitals entry)[cite: 1].
* **Agentic AI Subsystem:** 4 specialized C# Agents running internally behind ASP.NET Core controllers.

---

## 2. Global Safety & Business Guardrails
1. **Mandatory Doctor Approval Gate:** Triage requests evaluated as high-risk or emergency **MUST** pause and set status to `"NEEDS_DOCTOR_APPROVAL"`. Emergency nurse dispatches cannot execute without an authorized Doctor explicitly submitting approval via `POST /api/v1/triage/{id}/approve`[cite: 1].
2. **Read-Only AI Tools:** AI agents access data exclusively through read-only C# interfaces (`ReadOnlyCarePulseDbContext`). Agents are prohibited from performing database writes or directly executing state-changing logic.
3. **Prompt-Injection Defense:** Strict C# reflection allow-lists, schema validation, and identity claims are enforced server-side. Patient input data cannot alter authorization bounds.

---

## 3. Four Student Component Ownership & Specs

### Student 1: Patient Identity, Medical Records & Vault
* **Database Entities:** `PatientProfiles`, `MedicalHistories`, `EmergencyContacts`, `MedicalDocuments`
* **API Endpoints:**
  * `POST /api/v1/patients/profile`
  * `GET /api/v1/patients/{id}/profile`
  * `PUT /api/v1/patients/{id}/history`
  * `POST /api/v1/patients/{id}/documents`
  * `POST /api/v1/patients/{id}/emergency-broadcast`
* **Flutter Mobile Feature:** Profile UI & Document upload using **Camera & File Picker**.
* **React Web Feature:** Patient Master Registry table with sorting, filtering, and history audit viewer.
* **AI Agent:** **Agent 1 (Planner Agent)** — Creates structured execution plan from intake context.

### Student 2: AI Triage Ingestion, Risk Assessment & Approval Queue
* **Database Entities:** `TriageTickets`, `AiTriageLogs`, `RiskAssessments`, `ApprovalQueues`
* **API Endpoints:**
  * `POST /api/v1/triage/submit`
  * `GET /api/v1/triage/pending-approvals`
  * `GET /api/v1/triage/{id}/audit-log`
  * `DELETE /api/v1/triage/{id}`
  * `POST /api/v1/triage/{id}/approve`
* **Flutter Mobile Feature:** Symptom Intake Form with **Camera Photo Attachment** & Live Status Stepper[cite: 1].
* **React Web Feature:** Clinical Triage Approval Queue Dashboard with **Doctor Approval Modal**[cite: 1].
* **AI Agent:** **Agent 2 (Domain Analysis Agent)** — Scores clinical severity (1–10) and flags contraindications[cite: 1].

### Student 3: Doctor Rostering, Clinic Scheduling & Consultations
* **Database Entities:** `DoctorProfiles`, `ClinicRosters`, `AppointmentSlots`, `ConsultationRecords`
* **API Endpoints:**
  * `GET /api/v1/doctors/slots`
  * `POST /api/v1/appointments/book`
  * `PUT /api/v1/doctors/roster`
  * `GET /api/v1/consultations/{id}`
  * `POST /api/v1/consultations/complete`
* **Flutter Mobile Feature:** Specialist Directory & Appointment Booking with **Date/Time Picker**.
* **React Web Feature:** Interactive Calendar Roster Manager & Consultation Notes Editor.
* **AI Agent:** **Agent 3 (Action / Tool Agent)** — Queries active schedules and matches specialist availability[cite: 1].

### Student 4 (Leader): Field Nurse Dispatch, GPS Operations & Safety Validation
* **Database Entities:** `NurseProfiles`, `DispatchTickets`, `RouteLogs`, `OnSiteVitalsRecords`
* **API Endpoints:**
  * `POST /api/v1/dispatch/assign`
  * `PUT /api/v1/dispatch/{id}/location`
  * `GET /api/v1/dispatch/active`
  * `GET /api/v1/dispatch/{id}/vitals`
  * `POST /api/v1/dispatch/{id}/complete-onsite`
* **Dispatch Lifecycle:** `Assigned` $\rightarrow$ `EnRoute` $\rightarrow$ `ArrivedOnSite` $\rightarrow$ `Completed` (or `Escalated`).
* **Flutter Mobile Feature:** Active Dispatch Navigation Screen with **Device GPS & Map Integration**[cite: 1].
* **React Web Feature:** Emergency Control Center Map Dashboard displaying live nurse telemetry.
* **AI Agent:** **Agent 4 (Validation & Safety Agent)** — Validates safety rules and enforces mandatory Doctor approval.

---