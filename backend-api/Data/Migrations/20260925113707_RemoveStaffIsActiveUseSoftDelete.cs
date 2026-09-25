using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStaffIsActiveUseSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Doctor/Nurse now use the same IsDeleted soft-delete flag (from BaseEntity)
            // that PatientProfile already uses, instead of a separate IsActive flag.
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "NurseProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "DoctorProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "NurseProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }
    }
}
