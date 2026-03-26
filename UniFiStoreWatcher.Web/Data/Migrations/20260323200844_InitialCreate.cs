using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Prefer static readonly fields over constant array arguments

namespace UniFiStoreWatcher.Web.Data.Migrations
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nickname = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    PollIntervalMinSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    PollIntervalMaxSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxRetryAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    RetryBaseDelaySeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    MinDelayBetweenRequestsSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    VapidPublicKey = table.Column<string>(type: "TEXT", nullable: true),
                    VapidPrivateKey = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProviderType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SettingsJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    ImageUrls = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentState = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousState = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SubscribedEvents = table.Column<int>(type: "INTEGER", nullable: false),
                    NextPollDueAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    LastPollAtUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    LastStateChangeAtUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    PollCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsecutiveErrors = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockChecks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    HttpMethod = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    RequestUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    HttpStatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    DetectedState = table.Column<int>(type: "INTEGER", nullable: false),
                    ParserStrategy = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ParserConfidence = table.Column<double>(type: "REAL", nullable: true),
                    ParserEvidence = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    IsRetry = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetryAttempt = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromState = table.Column<int>(type: "INTEGER", nullable: false),
                    ToState = table.Column<int>(type: "INTEGER", nullable: false),
                    DetectedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    StockCheckId = table.Column<long>(type: "INTEGER", nullable: false)
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
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NotificationConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    StockTransitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProviderType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    SentAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
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
                    { 1, 621355968000000000L, "Browser Push", true, "BrowserPush", null, 621355968000000000L },
                    { 2, 621355968000000000L, "Email", false, "Email", null, 621355968000000000L },
                    { 3, 621355968000000000L, "SMS (Twilio)", false, "Sms", null, 621355968000000000L },
                    { 4, 621355968000000000L, "Microsoft Teams", false, "Teams", null, 621355968000000000L },
                    { 5, 621355968000000000L, "Discord", false, "Discord", null, 621355968000000000L }
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
