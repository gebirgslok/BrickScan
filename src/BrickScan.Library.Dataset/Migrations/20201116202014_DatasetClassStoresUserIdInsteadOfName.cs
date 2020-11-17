using Microsoft.EntityFrameworkCore.Migrations;

namespace BrickScan.Library.Dataset.Migrations
{
    public partial class DatasetClassStoresUserIdInsteadOfName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DatasetClasses");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "DatasetClasses",
                unicode: false,
                maxLength: 36,
                nullable: true,
                defaultValue: "f991d74a-50d7-4126-bdbf-19061eb43e4c");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "DatasetClasses");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "DatasetClasses",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }
    }
}
