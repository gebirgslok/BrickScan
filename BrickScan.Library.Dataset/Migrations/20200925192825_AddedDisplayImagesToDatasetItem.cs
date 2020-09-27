using Microsoft.EntityFrameworkCore.Migrations;

namespace BrickScan.Library.Dataset.Migrations
{
    public partial class AddedDisplayImagesToDatasetItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DatasetItemId",
                table: "DatasetImages",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatasetImages_DatasetItemId",
                table: "DatasetImages",
                column: "DatasetItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetImages_DatasetItems_DatasetItemId",
                table: "DatasetImages",
                column: "DatasetItemId",
                principalTable: "DatasetItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatasetImages_DatasetItems_DatasetItemId",
                table: "DatasetImages");

            migrationBuilder.DropIndex(
                name: "IX_DatasetImages_DatasetItemId",
                table: "DatasetImages");

            migrationBuilder.DropColumn(
                name: "DatasetItemId",
                table: "DatasetImages");
        }
    }
}
