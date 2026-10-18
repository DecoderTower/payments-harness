using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payments.Api.Persistence
{
    /// <inheritdoc />
    public partial class Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Deposits",
                columns: table => new
                {
                    DepositId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountReference = table.Column<string>(type: "TEXT", nullable: false),
                    CustomerReference = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", nullable: false),
                    PayloadFingerprint = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderReference = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    LastProviderStatus = table.Column<string>(type: "TEXT", nullable: true),
                    LastFailureCode = table.Column<string>(type: "TEXT", nullable: true),
                    LastFailureMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ClientSecret = table.Column<string>(type: "TEXT", nullable: true),
                    NextActionType = table.Column<string>(type: "TEXT", nullable: true),
                    NextActionRedirectUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConfirmationAttemptedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ReconciledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deposits", x => x.DepositId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_IdempotencyKey",
                table: "Deposits",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Deposits");
        }
    }
}
