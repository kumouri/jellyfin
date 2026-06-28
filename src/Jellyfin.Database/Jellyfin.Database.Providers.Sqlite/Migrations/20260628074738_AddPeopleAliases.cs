using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class AddPeopleAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PeopleAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PeopleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", nullable: false),
                    AliasNormalized = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeopleAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeopleAliases_Peoples_PeopleId",
                        column: x => x.PeopleId,
                        principalTable: "Peoples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeopleAliases_AliasNormalized",
                table: "PeopleAliases",
                column: "AliasNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_PeopleAliases_PeopleId",
                table: "PeopleAliases",
                column: "PeopleId");

            migrationBuilder.CreateIndex(
                name: "IX_PeopleAliases_PeopleId_AliasNormalized",
                table: "PeopleAliases",
                columns: new[] { "PeopleId", "AliasNormalized" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PeopleAliases");
        }
    }
}
