using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Prefer static readonly fields over constant array arguments

namespace UniFiStoreWatcher.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nickname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PollIntervalMinSeconds = table.Column<int>(type: "int", nullable: false),
                    PollIntervalMaxSeconds = table.Column<int>(type: "int", nullable: false),
                    MaxRetryAttempts = table.Column<int>(type: "int", nullable: false),
                    RetryBaseDelaySeconds = table.Column<int>(type: "int", nullable: false),
                    MinDelayBetweenRequestsSeconds = table.Column<int>(type: "int", nullable: false),
                    VapidPublicKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VapidPrivateKey = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ImageUrls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalImagePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    PreviousState = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SubscribedEvents = table.Column<int>(type: "int", nullable: false),
                    NextPollDueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastPollAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastStateChangeAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PollCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    ConsecutiveErrors = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Endpoint = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    P256dh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Auth = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockChecks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RequestUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    DetectedState = table.Column<int>(type: "int", nullable: false),
                    ParserStrategy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ParserConfidence = table.Column<double>(type: "float", nullable: true),
                    ParserEvidence = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRetry = table.Column<bool>(type: "bit", nullable: false),
                    RetryAttempt = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockChecks_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    FromState = table.Column<int>(type: "int", nullable: false),
                    ToState = table.Column<int>(type: "int", nullable: false),
                    DetectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StockCheckId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransitions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransitions_StockChecks_StockCheckId",
                        column: x => x.StockCheckId,
                        principalTable: "StockChecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationConfigId = table.Column<int>(type: "int", nullable: false),
                    StockTransitionId = table.Column<int>(type: "int", nullable: false),
                    ProviderType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_NotificationConfigs_NotificationConfigId",
                        column: x => x.NotificationConfigId,
                        principalTable: "NotificationConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_StockTransitions_StockTransitionId",
                        column: x => x.StockTransitionId,
                        principalTable: "StockTransitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Id", "Email", "MaxRetryAttempts", "MinDelayBetweenRequestsSeconds", "Nickname", "Phone", "PollIntervalMaxSeconds", "PollIntervalMinSeconds", "RetryBaseDelaySeconds", "VapidPrivateKey", "VapidPublicKey" },
                values: new object[] { 1, null, 3, 5, "Stock Monitor", null, 90, 30, 2, null, null });

            migrationBuilder.InsertData(
                table: "NotificationConfigs",
                columns: new[] { "Id", "CreatedAtUtc", "DisplayName", "IsEnabled", "ProviderType", "SettingsJson", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Browser Push", true, "BrowserPush", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Email", false, "Email", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SMS (Twilio)", false, "Sms", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Microsoft Teams", false, "Teams", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Discord", false, "Discord", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "ConsecutiveErrors", "CreatedAtUtc", "CurrentState", "Description", "ErrorCount", "ImageUrl", "ImageUrls", "IsActive", "LastPollAtUtc", "LastStateChangeAtUtc", "LocalImagePath", "Name", "NextPollDueAtUtc", "PollCount", "PreviousState", "ProductCode", "SubscribedEvents", "UpdatedAtUtc", "Url" },
                values: new object[,]
                {
                    { 1, 0, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, 0, null, null, true, null, null, null, "UNAS Pro 4", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, 0, "UNAS-Pro-4", 1, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://eu.store.ui.com/eu/en/category/network-storage/products/unas-pro-4" },
                    { 2, 0, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, 0, null, null, true, null, null, null, "UNAS Pro 8", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, 0, "UNAS-Pro-8", 1, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://eu.store.ui.com/eu/en/category/network-storage/products/unas-pro-8" },
                    { 3, 0, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, 0, null, null, true, null, null, null, "UDB IoT", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, 0, "UDB-IoT", 1, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://eu.store.ui.com/eu/en/category/wifi-bridging/products/udb-iot" },
                    { 4, 0, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, 0, null, null, true, null, null, null, "UTR", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, 0, "UTR", 1, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://eu.store.ui.com/eu/en/category/wifi-special-devices/products/utr" },
                    { 5, 0, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, 0, null, null, true, null, null, null, "UVC G6 Edge Turret", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, 0, "UVC-G6-Edge-Turret", 1, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://eu.store.ui.com/eu/en/category/cameras-dome-turret/products/uvc-g6-edge-turret" },
                    { 6, 0, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, 0, null, null, true, null, null, null, "UVC G6 Edge Dome", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, 0, "UVC-G6-Edge-Dome", 1, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://eu.store.ui.com/eu/en/category/cameras-dome-turret/products/uvc-g6-edge-dome" },
                    { 7, 0, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, null, 0, null, null, true, null, null, null, "UACC HDD E-24TB", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, 0, "UACC-HDD-E-24TB", 1, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://eu.store.ui.com/eu/en/category/accessories-storage/collections/unifi-accessory-tech-hdd/products/uacc-hdd-e-24tb" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_NotificationConfigId",
                table: "NotificationLogs",
                column: "NotificationConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_StockTransitionId",
                table: "NotificationLogs",
                column: "StockTransitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_NextPollDueAtUtc",
                table: "Products",
                columns: new[] { "IsActive", "NextPollDueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductCode",
                table: "Products",
                column: "ProductCode",
                unique: true,
                filter: "[ProductCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Url",
                table: "Products",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_Endpoint",
                table: "PushSubscriptions",
                column: "Endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockChecks_CreatedAtUtc",
                table: "StockChecks",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockChecks_ProductId",
                table: "StockChecks",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransitions_DetectedAtUtc",
                table: "StockTransitions",
                column: "DetectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransitions_ProductId",
                table: "StockTransitions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransitions_StockCheckId",
                table: "StockTransitions",
                column: "StockCheckId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "NotificationLogs");

            migrationBuilder.DropTable(
                name: "PushSubscriptions");

            migrationBuilder.DropTable(
                name: "NotificationConfigs");

            migrationBuilder.DropTable(
                name: "StockTransitions");

            migrationBuilder.DropTable(
                name: "StockChecks");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
