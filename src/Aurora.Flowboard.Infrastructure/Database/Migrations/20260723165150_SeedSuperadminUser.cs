using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aurora.Flowboard.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeedSuperadminUser : Migration
    {
        private const string SuperadminUserId = "e4f2e3ae-2a8b-4e67-9d17-d563b6064d9b";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                INSERT INTO flowboard.users (id, first_name, last_name, email, password_hash, is_active, created_on_utc)
                VALUES (
                    '{SuperadminUserId}',
                    'Gerardo',
                    'Garnica',
                    'gerardo.garnica@gmail.com',
                    'AcGc/f8LHX5Bu0+MDOLkiHdHdTnXLfUFiRhzq7uLPmftxzjg+i7SaBq3GS65y+J+6w==',
                    TRUE,
                    NOW() AT TIME ZONE 'UTC'
                )
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO flowboard.user_roles (user_id, name)
                VALUES ('{SuperadminUserId}', 'Administrator')
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DELETE FROM flowboard.user_roles WHERE user_id = '{SuperadminUserId}';
                """);

            migrationBuilder.Sql($"""
                DELETE FROM flowboard.users WHERE id = '{SuperadminUserId}';
                """);
        }
    }
}
