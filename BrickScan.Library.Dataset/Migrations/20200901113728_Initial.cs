using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BrickScan.Library.Dataset.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatasetClasses",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<byte>(nullable: false),
                    CreatedBy = table.Column<string>(maxLength: 32, nullable: false),
                    CreatedOn = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetClasses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatasetColors",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BricklinkColorId = table.Column<int>(nullable: false),
                    BricklinkColorName = table.Column<string>(unicode: false, maxLength: 32, nullable: false),
                    BricklinkColorType = table.Column<string>(unicode: false, maxLength: 32, nullable: false),
                    BricklinkColorHtmlCode = table.Column<string>(unicode: false, maxLength: 9, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetColors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatasetImages",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(unicode: false, maxLength: 2083, nullable: false),
                    Status = table.Column<byte>(nullable: false),
                    CreatedOn = table.Column<DateTime>(nullable: false),
                    DatasetClassId = table.Column<int>(nullable: true),
                    DatasetClassId1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetImages_DatasetClasses_DatasetClassId",
                        column: x => x.DatasetClassId,
                        principalTable: "DatasetClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DatasetImages_DatasetClasses_DatasetClassId1",
                        column: x => x.DatasetClassId1,
                        principalTable: "DatasetClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatasetItems",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(unicode: false, maxLength: 64, nullable: false),
                    AdditionalIdentifier = table.Column<string>(unicode: false, maxLength: 20, nullable: true),
                    DatasetClassId = table.Column<int>(nullable: false),
                    DatasetColorId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetItems_DatasetClasses_DatasetClassId",
                        column: x => x.DatasetClassId,
                        principalTable: "DatasetClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DatasetItems_DatasetColors_DatasetColorId",
                        column: x => x.DatasetColorId,
                        principalTable: "DatasetColors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatasetImages_DatasetClassId",
                table: "DatasetImages",
                column: "DatasetClassId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetImages_DatasetClassId1",
                table: "DatasetImages",
                column: "DatasetClassId1");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetItems_DatasetClassId",
                table: "DatasetItems",
                column: "DatasetClassId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetItems_DatasetColorId",
                table: "DatasetItems",
                column: "DatasetColorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatasetImages");

            migrationBuilder.DropTable(
                name: "DatasetItems");

            migrationBuilder.DropTable(
                name: "DatasetClasses");

            migrationBuilder.DropTable(
                name: "DatasetColors");
        }
    }
}
