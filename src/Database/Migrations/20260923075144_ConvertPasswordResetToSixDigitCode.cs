using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetFriend.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConvertPasswordResetToSixDigitCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PasswordResetTokenHash",
                table: "Users",
                newName: "PasswordResetCodeHash");

            migrationBuilder.RenameColumn(
                name: "PasswordResetExpiresAtUtc",
                table: "Users",
                newName: "PasswordResetCodeExpiresAtUtc");

            migrationBuilder.AddColumn<int>(
                name: "PasswordResetAttemptCount",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetAttemptCount",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "PasswordResetCodeHash",
                table: "Users",
                newName: "PasswordResetTokenHash");

            migrationBuilder.RenameColumn(
                name: "PasswordResetCodeExpiresAtUtc",
                table: "Users",
                newName: "PasswordResetExpiresAtUtc");
        }
    }
}
