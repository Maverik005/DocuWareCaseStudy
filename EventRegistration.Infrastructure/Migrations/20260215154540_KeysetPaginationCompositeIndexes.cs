using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventRegistration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class KeysetPaginationCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventId_RegisteredAt_Id",
                table: "Registrations",
                columns: new[] { "EventId", "RegisteredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_StartTime_Id",
                table: "Events",
                columns: new[] { "StartTime", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Registrations_EventId_RegisteredAt_Id",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Events_StartTime_Id",
                table: "Events");
        }
    }
}
