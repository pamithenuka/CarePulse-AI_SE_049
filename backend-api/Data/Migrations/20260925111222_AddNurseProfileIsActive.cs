using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNurseProfileIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Defaults existing nurses to active (true) - a plain AddColumn<bool>
            // without an explicit default would otherwise default to false and
            // silently lock out every already-registered nurse.
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "NurseProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "NurseProfiles");
        }
    }
}
