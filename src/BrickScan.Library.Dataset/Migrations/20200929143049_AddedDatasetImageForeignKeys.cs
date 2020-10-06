using Microsoft.EntityFrameworkCore.Migrations;

namespace BrickScan.Library.Dataset.Migrations
{
    public partial class AddedDatasetImageForeignKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatasetImages_DatasetClasses_DatasetClassId",
                table: "DatasetImages");

            migrationBuilder.DropForeignKey(
                name: "FK_DatasetImages_DatasetClasses_DatasetClassId1",
                table: "DatasetImages");

            migrationBuilder.DropIndex(
                name: "IX_DatasetImages_DatasetClassId",
                table: "DatasetImages");

            migrationBuilder.DropIndex(
                name: "IX_DatasetImages_DatasetClassId1",
                table: "DatasetImages");

            migrationBuilder.DropColumn(
                name: "DatasetClassId",
                table: "DatasetImages");

            migrationBuilder.DropColumn(
                name: "DatasetClassId1",
                table: "DatasetImages");

            migrationBuilder.AddColumn<int>(
                name: "DisplayDatasetClassId",
                table: "DatasetImages",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrainDatasetClassId",
                table: "DatasetImages",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatasetImages_DisplayDatasetClassId",
                table: "DatasetImages",
                column: "DisplayDatasetClassId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetImages_TrainDatasetClassId",
                table: "DatasetImages",
                column: "TrainDatasetClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetImages_DatasetClasses_DisplayDatasetClassId",
                table: "DatasetImages",
                column: "DisplayDatasetClassId",
                principalTable: "DatasetClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetImages_DatasetClasses_TrainDatasetClassId",
                table: "DatasetImages",
                column: "TrainDatasetClassId",
                principalTable: "DatasetClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatasetImages_DatasetClasses_DisplayDatasetClassId",
                table: "DatasetImages");

            migrationBuilder.DropForeignKey(
                name: "FK_DatasetImages_DatasetClasses_TrainDatasetClassId",
                table: "DatasetImages");

            migrationBuilder.DropIndex(
                name: "IX_DatasetImages_DisplayDatasetClassId",
                table: "DatasetImages");

            migrationBuilder.DropIndex(
                name: "IX_DatasetImages_TrainDatasetClassId",
                table: "DatasetImages");

            migrationBuilder.DropColumn(
                name: "DisplayDatasetClassId",
                table: "DatasetImages");

            migrationBuilder.DropColumn(
                name: "TrainDatasetClassId",
                table: "DatasetImages");

            migrationBuilder.AddColumn<int>(
                name: "DatasetClassId",
                table: "DatasetImages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DatasetClassId1",
                table: "DatasetImages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatasetImages_DatasetClassId",
                table: "DatasetImages",
                column: "DatasetClassId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetImages_DatasetClassId1",
                table: "DatasetImages",
                column: "DatasetClassId1");

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetImages_DatasetClasses_DatasetClassId",
                table: "DatasetImages",
                column: "DatasetClassId",
                principalTable: "DatasetClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetImages_DatasetClasses_DatasetClassId1",
                table: "DatasetImages",
                column: "DatasetClassId1",
                principalTable: "DatasetClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
