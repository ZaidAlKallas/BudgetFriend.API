using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetFriend.API.Database.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: true),
                FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Accounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                InitialBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Accounts", x => x.Id);
                table.ForeignKey(
                    name: "FK_Accounts_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                TransactionType = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", x => x.Id);
                table.ForeignKey(
                    name: "FK_Categories_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Transactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Transactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Transactions_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Transactions_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_UserId_Name",
            table: "Accounts",
            columns: ["UserId", "Name"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Categories_UserId_Name_TransactionType",
            table: "Categories",
            columns: ["UserId", "Name", "TransactionType"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_AccountId",
            table: "Transactions",
            column: "AccountId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_CategoryId",
            table: "Transactions",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_TransactionDate",
            table: "Transactions",
            column: "TransactionDate");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Transactions");

        migrationBuilder.DropTable(
            name: "Accounts");

        migrationBuilder.DropTable(
            name: "Categories");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
