using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPatientCrudAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Allergies",
                table: "PatientProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PatientProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "PatientProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentMedications",
                table: "MedicalHistories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "MedicalHistories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "MedicalHistories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsResolved",
                table: "MedicalHistories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ResolvedOn",
                table: "MedicalHistories",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "MedicalDocuments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "MedicalDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EmergencyContacts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "EmergencyContacts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmergencyAlertLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggeredByUserId = table.Column<string>(type: "text", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BloodGroupSnapshot = table.Column<string>(type: "text", nullable: true),
                    AllergiesSnapshot = table.Column<string>(type: "text", nullable: true),
                    ActiveMedicationsSnapshot = table.Column<string>(type: "text", nullable: true),
                    ChronicConditionsSnapshot = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyAlertLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyAlertLogs_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ChangesJson = table.Column<string>(type: "text", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "text", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyAlertNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmergencyAlertLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactName = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    DeliveryStatus = table.Column<int>(type: "integer", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyAlertNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyAlertNotifications_EmergencyAlertLogs_EmergencyAle~",
                        column: x => x.EmergencyAlertLogId,
                        principalTable: "EmergencyAlertLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyAlertLogs_PatientProfileId",
                table: "EmergencyAlertLogs",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyAlertNotifications_EmergencyAlertLogId",
                table: "EmergencyAlertNotifications",
                column: "EmergencyAlertLogId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAuditLogs_PatientProfileId",
                table: "PatientAuditLogs",
                column: "PatientProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmergencyAlertNotifications");

            migrationBuilder.DropTable(
                name: "PatientAuditLogs");

            migrationBuilder.DropTable(
                name: "EmergencyAlertLogs");

            migrationBuilder.DropColumn(
                name: "Allergies",
                table: "PatientProfiles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PatientProfiles");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PatientProfiles");

            migrationBuilder.DropColumn(
                name: "CurrentMedications",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "IsResolved",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "ResolvedOn",
                table: "MedicalHistories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MedicalDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MedicalDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmergencyContacts");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "EmergencyContacts");
        }
    }
}
