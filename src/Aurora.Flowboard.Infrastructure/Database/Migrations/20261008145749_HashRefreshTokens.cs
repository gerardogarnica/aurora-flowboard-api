using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aurora.Flowboard.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class HashRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows hold raw refresh tokens and JWTs. They cannot be hashed in place, so every
            // session is invalidated and users log in again once.
            migrationBuilder.Sql("DELETE FROM flowboard.user_tokens;");

            migrationBuilder.DropIndex(
                name: "ix_user_tokens_refresh_token",
                schema: "flowboard",
                table: "user_tokens");

            migrationBuilder.DropColumn(
                name: "access_token",
                schema: "flowboard",
                table: "user_tokens");

            migrationBuilder.DropColumn(
                name: "refresh_token",
                schema: "flowboard",
                table: "user_tokens");

            migrationBuilder.AddColumn<string>(
                name: "access_token_id",
                schema: "flowboard",
                table: "user_tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "refresh_token_hash",
                schema: "flowboard",
                table: "user_tokens",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: false);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "flowboard",
                table: "user_tokens",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "ix_user_tokens_refresh_token_expires_on_utc",
                schema: "flowboard",
                table: "user_tokens",
                column: "refresh_token_expires_on_utc");

            migrationBuilder.CreateIndex(
                name: "ix_user_tokens_refresh_token_hash",
                schema: "flowboard",
                table: "user_tokens",
                column: "refresh_token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Hashed tokens cannot be turned back into raw ones, and every row would get the same empty
            // refresh_token, breaking the unique index below. Sessions are not recovered in either direction.
            migrationBuilder.Sql("DELETE FROM flowboard.user_tokens;");

            migrationBuilder.DropIndex(
                name: "ix_user_tokens_refresh_token_expires_on_utc",
                schema: "flowboard",
                table: "user_tokens");

            migrationBuilder.DropIndex(
                name: "ix_user_tokens_refresh_token_hash",
                schema: "flowboard",
                table: "user_tokens");

            migrationBuilder.DropColumn(
                name: "access_token_id",
                schema: "flowboard",
                table: "user_tokens");

            migrationBuilder.DropColumn(
                name: "refresh_token_hash",
                schema: "flowboard",
                table: "user_tokens");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "flowboard",
                table: "user_tokens");

            migrationBuilder.AddColumn<string>(
                name: "access_token",
                schema: "flowboard",
                table: "user_tokens",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "refresh_token",
                schema: "flowboard",
                table: "user_tokens",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_user_tokens_refresh_token",
                schema: "flowboard",
                table: "user_tokens",
                column: "refresh_token",
                unique: true);
        }
    }
}
