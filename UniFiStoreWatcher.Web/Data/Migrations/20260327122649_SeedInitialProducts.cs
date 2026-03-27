using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Prefer static readonly fields over constant array arguments

namespace UniFiStoreWatcher.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "ConsecutiveErrors", "CreatedAtUtc", "CurrentState", "Description", "ErrorCount", "ImageUrl", "ImageUrls", "IsActive", "LastPollAtUtc", "LastStateChangeAtUtc", "LocalImagePath", "Name", "NextPollDueAtUtc", "PollCount", "PreviousState", "ProductCode", "SubscribedEvents", "UpdatedAtUtc", "Url" },
                values: new object[,]
                {
                    { 1, 0, 621355968000000000L, 0, null, 0, null, null, true, null, null, null, "UNAS Pro 4", 621355968000000000L, 0, 0, "UNAS-Pro-4", 1, 621355968000000000L, "https://eu.store.ui.com/eu/en/category/network-storage/products/unas-pro-4" },
                    { 2, 0, 621355968000000000L, 0, null, 0, null, null, true, null, null, null, "UNAS Pro 8", 621355968000000000L, 0, 0, "UNAS-Pro-8", 1, 621355968000000000L, "https://eu.store.ui.com/eu/en/category/network-storage/products/unas-pro-8" },
                    { 3, 0, 621355968000000000L, 0, null, 0, null, null, true, null, null, null, "UDB IoT", 621355968000000000L, 0, 0, "UDB-IoT", 1, 621355968000000000L, "https://eu.store.ui.com/eu/en/category/wifi-bridging/products/udb-iot" },
                    { 4, 0, 621355968000000000L, 0, null, 0, null, null, true, null, null, null, "UTR", 621355968000000000L, 0, 0, "UTR", 1, 621355968000000000L, "https://eu.store.ui.com/eu/en/category/wifi-special-devices/products/utr" },
                    { 5, 0, 621355968000000000L, 0, null, 0, null, null, true, null, null, null, "UVC G6 Edge Turret", 621355968000000000L, 0, 0, "UVC-G6-Edge-Turret", 1, 621355968000000000L, "https://eu.store.ui.com/eu/en/category/cameras-dome-turret/products/uvc-g6-edge-turret" },
                    { 6, 0, 621355968000000000L, 0, null, 0, null, null, true, null, null, null, "UVC G6 Edge Dome", 621355968000000000L, 0, 0, "UVC-G6-Edge-Dome", 1, 621355968000000000L, "https://eu.store.ui.com/eu/en/category/cameras-dome-turret/products/uvc-g6-edge-dome" },
                    { 7, 0, 621355968000000000L, 0, null, 0, null, null, true, null, null, null, "UACC HDD E-24TB", 621355968000000000L, 0, 0, "UACC-HDD-E-24TB", 1, 621355968000000000L, "https://eu.store.ui.com/eu/en/category/accessories-storage/collections/unifi-accessory-tech-hdd/products/uacc-hdd-e-24tb" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
