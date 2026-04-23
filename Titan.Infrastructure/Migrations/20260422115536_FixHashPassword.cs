using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Titan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixHashPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Description", "DisplayOrder", "ImageUrl", "IsActive", "IsDeleted", "Name", "NameAr", "ParentId", "Slug", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000010"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, 1, null, true, false, "Men", "رجال", null, "men", null },
                    { new Guid("00000000-0000-0000-0000-000000000011"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, 2, null, true, false, "Women", "نساء", null, "women", null },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, 3, null, true, false, "Accessories", "إكسسوارات", null, "accessories", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "BanReason", "CreatedAt", "DeletedAt", "Email", "FirstName", "IsActive", "IsBanned", "IsDeleted", "LastLoginAt", "LastName", "PasswordHash", "Phone", "PreferredLanguage", "ProfileImageUrl", "Role", "UpdatedAt" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "admin@titan.com", "TITAN", true, false, false, null, "Admin", "$2a$11$lpvPoxI0PIm9Fov9K/UIs.S59M7xKx9.V2Y8.R.u/yY/F/F/F/F/F", "", "en", null, "Admin", null });
        }
    }
}
