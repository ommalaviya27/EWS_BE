using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.EWS.Migrations
{
    /// <inheritdoc />
    public partial class reportingIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "team_lead_id",
                table: "users",
                newName: "reporting_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "reporting_id",
                table: "users",
                newName: "team_lead_id");
        }
    }
}
