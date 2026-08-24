# CarePulse: Shared Developer & Architecture Guide
**Software Engineering Frameworks (SE3090) — 2026**

This document serves as the single source of truth for all team members (and AI coding assistants) working on the CarePulse monorepo.

---

## 1. Project Stack & Core Setup
- **Backend API:** ASP.NET Core Web API (C# .NET 8.0) inside `backend-api/`
- **Database:** PostgreSQL running locally on port `5432` (`carepulse_dev_db`)
- **ORM:** Entity Framework Core v8.0 with Npgsql provider
- **Web App:** React (Functional components + Hooks) inside `web-admin/`
- **Mobile App:** Flutter / Dart inside `mobile-app/`
- **Authentication:** JWT Bearer tokens + ASP.NET Core Identity
- **Logging & Docs:** Serilog console logging + Swagger UI (`http://localhost:5000/swagger`)

---

## 2. Monorepo Directory Layout
```text
SE3090_G07/
├── .github/workflows/ci.yml   <-- Automated CI build & test workflow
├── backend-api/               <-- ASP.NET Core REST API & AI Orchestrator
│   ├── Data/                  <-- CarePulseDbContext (EF Core)
│   ├── Entities/Base/         <-- BaseEntity (Audit fields)
│   ├── Middleware/            <-- GlobalExceptionMiddleware
│   └── Program.cs             <-- Service DI & JWT configuration
├── web-admin/                 <-- React Web Application
├── mobile-app/                <-- Flutter Mobile Application
├── tests/                     <-- C# xUnit Test Suites
└── docs/                      <-- Project ADRs & Traceability Matrix

3. Core Database & Base Entity Rules
All PostgreSQL entity models MUST inherit from CarePulse.Api.Entities.Base.BaseEntity:
C#

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}
* Primary Keys: Must be Guid.
* Soft deletes are automatically handled via global query filters in CarePulseDbContext.cs.
* Register all entity sets inside Data/CarePulseDbContext.cs.
4. Installed Packages in backend-api.csproj
* Microsoft.EntityFrameworkCore (v8.0.0)
* Npgsql.EntityFrameworkCore.PostgreSQL (v8.0.0)
* Microsoft.AspNetCore.Identity.EntityFrameworkCore (v8.0.0)
* Microsoft.AspNetCore.Authentication.JwtBearer (v8.0.0)
* Swashbuckle.AspNetCore (v6.5.0)
* Serilog.AspNetCore (v8.0.0)
5. Team Component Ownership Matrix
Student 1: Patient Identity, Medical Records & Document Vault
* Entities: PatientProfiles, MedicalHistories, EmergencyContacts, MedicalDocuments
* API Endpoints: POST /api/v1/patients/profile, GET /api/v1/patients/{id}/profile, PUT /api/v1/patients/{id}/history, POST /api/v1/patients/{id}/documents, POST /api/v1/patients/{id}/emergency-broadcast
* AI Agent: Agent 1 (Planner Agent)
Student 2: AI Triage Ingestion, Risk Assessment & Approval Queue
* Entities: TriageTickets, AiTriageLogs, RiskAssessments, ApprovalQueues
* API Endpoints: POST /api/v1/triage/submit, GET /api/v1/triage/pending-approvals, GET /api/v1/triage/{id}/audit-log, DELETE /api/v1/triage/{id}, POST /api/v1/triage/{id}/approve
* AI Agent: Agent 2 (Domain Analysis Agent)
Student 3: Doctor Rostering, Clinic Scheduling & Consultations
* Entities: DoctorProfiles, ClinicRosters, AppointmentSlots, ConsultationRecords
* API Endpoints: GET /api/v1/doctors/slots, POST /api/v1/appointments/book, PUT /api/v1/doctors/roster, GET /api/v1/consultations/{id}, POST /api/v1/consultations/complete
* AI Agent: Agent 3 (Action / Tool Agent)
Student 4 (Leader): Field Nurse Dispatch, GPS Operations & Safety Validation
* Entities: NurseProfiles, DispatchTickets, RouteLogs, OnSiteVitalsRecords
* API Endpoints: POST /api/v1/dispatch/assign, PUT /api/v1/dispatch/{id}/location, GET /api/v1/dispatch/active, GET /api/v1/dispatch/{id}/vitals, POST /api/v1/dispatch/{id}/complete-onsite
* AI Agent: Agent 4 (Validation & Safety Agent)
6. Shared Integration & Safety Rules
1. Mandatory Doctor Approval Gate: Triage requests flagged as emergency MUST set status to "NEEDS_DOCTOR_APPROVAL". Emergency dispatches cannot execute without an authorized Doctor calling POST /api/v1/triage/{id}/approve. 
2. Dispatch Lifecycle: Assigned $\rightarrow$ EnRoute $\rightarrow$ ArrivedOnSite $\rightarrow$ Completed (or Escalated).
3. Role Authorization: Use ASP.NET Core attributes: [Authorize(Roles = "Patient")], [Authorize(Roles = "Doctor")], [Authorize(Roles = "Nurse")], [Authorize(Roles = "Admin")].
7. Git & Development Guidelines
* Integration Branch: develop
* Feature Branches: feature/student1-patient-module, feature/student2-triage-module, feature/student3-roster-module, feature/student4-dispatch-module
* Run local backend: dotnet run --project backend-api/backend-api.csproj
* Access Swagger API docs: http://localhost:5000/swaggerEOF
