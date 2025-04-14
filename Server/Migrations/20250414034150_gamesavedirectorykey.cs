using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class gamesavedirectorykey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GameSaves",
                table: "GameSaves");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameSaves",
                table: "GameSaves",
                columns: new[] { "UserId", "GameId", "Directory" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GameSaves",
                table: "GameSaves");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameSaves",
                table: "GameSaves",
                columns: new[] { "UserId", "GameId" });
        }
    }
}
