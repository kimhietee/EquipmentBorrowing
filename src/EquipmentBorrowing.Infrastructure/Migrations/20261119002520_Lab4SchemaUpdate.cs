using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentBorrowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Lab4SchemaUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_StudentNumber",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_Name",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_Type",
                table: "Equipment");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Equipment",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Equipment"
                SET "Description" = NULLIF(BTRIM("Type"), '');
                """);

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Equipment");

            migrationBuilder.AlterColumn<string>(
                name: "StudentNumber",
                table: "Students",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "Contact",
                table: "Students",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedStudentNumber",
                table: "Students",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Students"
                SET "StudentNumber" = BTRIM("StudentNumber"),
                    "NormalizedStudentNumber" = UPPER(BTRIM("StudentNumber"));
                """);

            migrationBuilder.AddColumn<string>(
                name: "Identifier",
                table: "Equipment",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedIdentifier",
                table: "Equipment",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Equipment"
                SET "Identifier" = 'EQ-' || LPAD("Id"::text, 3, '0'),
                    "NormalizedIdentifier" = 'EQ-' || LPAD("Id"::text, 3, '0');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Students_Name",
                table: "Students",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Students_NormalizedStudentNumber",
                table: "Students",
                column: "NormalizedStudentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Name",
                table: "Equipment",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_NormalizedIdentifier",
                table: "Equipment",
                column: "NormalizedIdentifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_Name",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_NormalizedStudentNumber",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_Name",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_NormalizedIdentifier",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Contact",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "NormalizedStudentNumber",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Identifier",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "NormalizedIdentifier",
                table: "Equipment");

            migrationBuilder.AlterColumn<string>(
                name: "StudentNumber",
                table: "Students",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Equipment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentNumber",
                table: "Students",
                column: "StudentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Name",
                table: "Equipment",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Type",
                table: "Equipment",
                column: "Type");
        }
    }
}
