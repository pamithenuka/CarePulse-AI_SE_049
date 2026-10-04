using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Data.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Hand-edited after scaffolding: this repo's tracked migration history is
    /// missing the "AddDispatchEntities" migration that Student 4 applied
    /// directly to the shared Neon DB, so EF's snapshot didn't know those tables
    /// already existed and scaffolded CREATE TABLE for entities (PatientProfiles,
    /// AiWorkflows, etc.) that are already there, plus a FullName column on
    /// AspNetUsers that already exists. Those statements were removed. What
    /// remains is genuinely new: the DoctorProfiles table, NurseProfiles.UserId's
    /// type fix (Guid -> string, to match ApplicationUser.Id), and DeletedAt/
    /// DeletedBy columns on RouteLogs/OnSiteVitalsRecords/NurseProfiles/
    /// DispatchTickets - confirmed missing via a direct schema check against the
    /// live database (BaseEntity gained these two columns after those tables
    /// were first created).
    /// </remarks>
    public partial class AddDoctorProfileAndFixNurseUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RouteLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "RouteLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "OnSiteVitalsRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "OnSiteVitalsRecords",
                type: "text",
                nullable: true);

            // Raw SQL with an explicit USING cast: Postgres has no implicit
            // assignment cast from uuid to text, so a plain AlterColumn would
            // fail at runtime with "column cannot be cast automatically".
            migrationBuilder.Sql(
                "ALTER TABLE \"NurseProfiles\" ALTER COLUMN \"UserId\" TYPE text USING \"UserId\"::text;");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "NurseProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "NurseProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "DispatchTickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "DispatchTickets",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DoctorProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Specialty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorProfiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RouteLogs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "RouteLogs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "OnSiteVitalsRecords");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "OnSiteVitalsRecords");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "NurseProfiles");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "NurseProfiles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "DispatchTickets");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DispatchTickets");

            migrationBuilder.Sql(
                "ALTER TABLE \"NurseProfiles\" ALTER COLUMN \"UserId\" TYPE uuid USING \"UserId\"::uuid;");
        }
    }
}
