using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class IntegratedWorkflowSafety : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            foreach (var table in new[] { "AppointmentSlots", "ClinicRosters", "ConsultationRecords" })
            {
                // Merged installations may already contain these base audit columns.
                migrationBuilder.Sql($"""
                    ALTER TABLE "{table}" ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone;
                    ALTER TABLE "{table}" ADD COLUMN IF NOT EXISTS "DeletedBy" text;
                    """);
            }
            migrationBuilder.AddForeignKey(name: "FK_AppointmentSlots_PatientProfiles_PatientId", table: "AppointmentSlots",
                column: "PatientId", principalTable: "PatientProfiles", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            // On existing installations these two FKs may already have been created out of order.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_AppointmentSlots_DoctorProfiles_DoctorId') THEN
                    ALTER TABLE "AppointmentSlots" ADD CONSTRAINT "FK_AppointmentSlots_DoctorProfiles_DoctorId" FOREIGN KEY ("DoctorId") REFERENCES "DoctorProfiles" ("Id") ON DELETE CASCADE;
                  END IF;
                  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_ClinicRosters_DoctorProfiles_DoctorId') THEN
                    ALTER TABLE "ClinicRosters" ADD CONSTRAINT "FK_ClinicRosters_DoctorProfiles_DoctorId" FOREIGN KEY ("DoctorId") REFERENCES "DoctorProfiles" ("Id") ON DELETE CASCADE;
                  END IF;
                END $$;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "AssessmentFailed",
                table: "TriageTickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "TriageTickets",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "TriageTickets",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "TriageTickets",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTime>(
                name: "LocationRecordedAt",
                table: "NurseProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "NurseProfiles",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DispatchTickets",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArrivedAt",
                table: "DispatchTickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DestinationLat",
                table: "DispatchTickets",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DestinationLng",
                table: "DispatchTickets",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EscalationNotes",
                table: "DispatchTickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEscalated",
                table: "DispatchTickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SafetySummary",
                table: "DispatchTickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "DispatchTickets",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "ExecutionJson",
                table: "AiWorkflows",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ExecutionStatus",
                table: "AiWorkflows",
                type: "text",
                nullable: false,
                defaultValue: "PlanOnly");

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "AiWorkflows",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TriageTicketId",
                table: "AiWorkflows",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AiWorkflows",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);




            migrationBuilder.CreateIndex(
                name: "IX_TriageTickets_PatientProfileId",
                table: "TriageTickets",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteLogs_DispatchTicketId_RecordedAt",
                table: "RouteLogs",
                columns: new[] { "DispatchTicketId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OnSiteVitalsRecords_DispatchTicketId",
                table: "OnSiteVitalsRecords",
                column: "DispatchTicketId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchTickets_DoctorId",
                table: "DispatchTickets",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchTickets_NurseId",
                table: "DispatchTickets",
                column: "NurseId",
                unique: true,
                filter: "\"IsDeleted\" = false AND \"Status\" IN ('Assigned','EnRoute','ArrivedOnSite')");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchTickets_TriageTicketId",
                table: "DispatchTickets",
                column: "TriageTicketId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiWorkflows_TriageTicketId",
                table: "AiWorkflows",
                column: "TriageTicketId",
                unique: true);


            migrationBuilder.CreateIndex(
                name: "IX_AppointmentSlots_PatientId",
                table: "AppointmentSlots",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicRosters_DoctorId_DayOfWeek",
                table: "ClinicRosters",
                columns: new[] { "DoctorId", "DayOfWeek" },
                unique: true);


            migrationBuilder.AddForeignKey(
                name: "FK_AiWorkflows_TriageTickets_TriageTicketId",
                table: "AiWorkflows",
                column: "TriageTicketId",
                principalTable: "TriageTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchTickets_DoctorProfiles_DoctorId",
                table: "DispatchTickets",
                column: "DoctorId",
                principalTable: "DoctorProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchTickets_NurseProfiles_NurseId",
                table: "DispatchTickets",
                column: "NurseId",
                principalTable: "NurseProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchTickets_TriageTickets_TriageTicketId",
                table: "DispatchTickets",
                column: "TriageTicketId",
                principalTable: "TriageTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OnSiteVitalsRecords_DispatchTickets_DispatchTicketId",
                table: "OnSiteVitalsRecords",
                column: "DispatchTicketId",
                principalTable: "DispatchTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RouteLogs_DispatchTickets_DispatchTicketId",
                table: "RouteLogs",
                column: "DispatchTicketId",
                principalTable: "DispatchTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TriageTickets_PatientProfiles_PatientProfileId",
                table: "TriageTickets",
                column: "PatientProfileId",
                principalTable: "PatientProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_AppointmentSlots_PatientProfiles_PatientId", table: "AppointmentSlots");
            migrationBuilder.DropIndex(name: "IX_AppointmentSlots_PatientId", table: "AppointmentSlots");
            migrationBuilder.DropIndex(name: "IX_ClinicRosters_DoctorId_DayOfWeek", table: "ClinicRosters");
            // Retain base audit columns on rollback: some shared databases had them
            // before this migration, so dropping them could destroy existing history.
            migrationBuilder.DropForeignKey(
                name: "FK_AiWorkflows_TriageTickets_TriageTicketId",
                table: "AiWorkflows");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchTickets_DoctorProfiles_DoctorId",
                table: "DispatchTickets");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchTickets_NurseProfiles_NurseId",
                table: "DispatchTickets");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchTickets_TriageTickets_TriageTicketId",
                table: "DispatchTickets");

            migrationBuilder.DropForeignKey(
                name: "FK_OnSiteVitalsRecords_DispatchTickets_DispatchTicketId",
                table: "OnSiteVitalsRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_RouteLogs_DispatchTickets_DispatchTicketId",
                table: "RouteLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_TriageTickets_PatientProfiles_PatientProfileId",
                table: "TriageTickets");




            migrationBuilder.DropIndex(
                name: "IX_TriageTickets_PatientProfileId",
                table: "TriageTickets");

            migrationBuilder.DropIndex(
                name: "IX_RouteLogs_DispatchTicketId_RecordedAt",
                table: "RouteLogs");

            migrationBuilder.DropIndex(
                name: "IX_OnSiteVitalsRecords_DispatchTicketId",
                table: "OnSiteVitalsRecords");

            migrationBuilder.DropIndex(
                name: "IX_DispatchTickets_DoctorId",
                table: "DispatchTickets");

            migrationBuilder.DropIndex(
                name: "IX_DispatchTickets_NurseId",
                table: "DispatchTickets");

            migrationBuilder.DropIndex(
                name: "IX_DispatchTickets_TriageTicketId",
                table: "DispatchTickets");

            migrationBuilder.DropIndex(
                name: "IX_AiWorkflows_TriageTicketId",
                table: "AiWorkflows");

            migrationBuilder.DropColumn(
                name: "AssessmentFailed",
                table: "TriageTickets");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "TriageTickets");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "TriageTickets");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "TriageTickets");

            migrationBuilder.DropColumn(
                name: "LocationRecordedAt",
                table: "NurseProfiles");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "NurseProfiles");

            migrationBuilder.DropColumn(
                name: "ArrivedAt",
                table: "DispatchTickets");

            migrationBuilder.DropColumn(
                name: "DestinationLat",
                table: "DispatchTickets");

            migrationBuilder.DropColumn(
                name: "DestinationLng",
                table: "DispatchTickets");

            migrationBuilder.DropColumn(
                name: "EscalationNotes",
                table: "DispatchTickets");

            migrationBuilder.DropColumn(
                name: "IsEscalated",
                table: "DispatchTickets");

            migrationBuilder.DropColumn(
                name: "SafetySummary",
                table: "DispatchTickets");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "DispatchTickets");

            migrationBuilder.DropColumn(
                name: "ExecutionJson",
                table: "AiWorkflows");

            migrationBuilder.DropColumn(
                name: "ExecutionStatus",
                table: "AiWorkflows");

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "AiWorkflows");

            migrationBuilder.DropColumn(
                name: "TriageTicketId",
                table: "AiWorkflows");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AiWorkflows");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DispatchTickets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);
        }
    }
}
