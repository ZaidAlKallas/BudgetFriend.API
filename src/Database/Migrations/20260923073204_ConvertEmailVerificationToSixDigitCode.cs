using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetFriend.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConvertEmailVerificationToSixDigitCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmailVerificationTokenHash",
                table: "Users",
                newName: "EmailVerificationCodeHash");

            migrationBuilder.RenameColumn(
                name: "EmailVerificationExpiresAtUtc",
                table: "Users",
                newName: "EmailVerificationCodeExpiresAtUtc");

            migrationBuilder.AddColumn<int>(
                name: "EmailVerificationAttemptCount",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerificationAttemptCount",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "EmailVerificationCodeHash",
                table: "Users",
                newName: "EmailVerificationTokenHash");

            migrationBuilder.RenameColumn(
                name: "EmailVerificationCodeExpiresAtUtc",
                table: "Users",
                newName: "EmailVerificationExpiresAtUtc");
        }
    }
}
