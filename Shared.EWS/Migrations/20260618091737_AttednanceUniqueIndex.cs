using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.EWS.Migrations
{
    /// <inheritdoc />
    public partial class AttednanceUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attendances_user_id_attendance_date",
                table: "attendances");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_user_id_attendance_date",
                table: "attendances",
                columns: new[] { "user_id", "attendance_date" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attendances_user_id_attendance_date",
                table: "attendances");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_user_id_attendance_date",
                table: "attendances",
                columns: new[] { "user_id", "attendance_date" },
                unique: true);
        }
    }
}
