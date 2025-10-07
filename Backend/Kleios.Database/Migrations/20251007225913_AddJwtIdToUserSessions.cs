using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kleios.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddJwtIdToUserSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "JwtId",
                table: "UserSessions",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JwtId",
                table: "UserSessions");
        }
    }
}
