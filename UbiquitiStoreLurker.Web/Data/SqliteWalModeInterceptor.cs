using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace UbiquitiStoreLurker.Web.Data;

// Enables WAL journal mode and sets a busy timeout on every new SQLite connection.
public sealed class SqliteWalModeInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        if (connection is SqliteConnection sqliteConnection)
        {
            using var cmd = sqliteConnection.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
            cmd.ExecuteNonQuery();
        }
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (connection is SqliteConnection sqliteConnection)
        {
            await using var cmd = sqliteConnection.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}


