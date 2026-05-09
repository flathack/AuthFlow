using AutoLogin.App.Models;
using Microsoft.Data.Sqlite;

namespace AutoLogin.App.Services.Storage;

public sealed class SqliteLoginEntryRepository : ILoginEntryRepository
{
    private readonly string _connectionString;

    public SqliteLoginEntryRepository(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS LoginEntries (
                Id TEXT NOT NULL PRIMARY KEY,
                DisplayName TEXT NOT NULL,
                StartUrl TEXT NOT NULL,
                Username TEXT NOT NULL,
                EncryptedPassword TEXT NOT NULL,
                EncryptedTotpSecret TEXT NULL,
                AutomationProfileId TEXT NOT NULL,
                AutoSubmit INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync();

        var migrationCommand = connection.CreateCommand();
        migrationCommand.CommandText = "ALTER TABLE LoginEntries ADD COLUMN EncryptedTotpSecret TEXT NULL;";

        try
        {
            await migrationCommand.ExecuteNonQueryAsync();
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 1 && exception.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    public async Task<IReadOnlyList<LoginEntry>> GetAllAsync()
    {
        var entries = new List<LoginEntry>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, DisplayName, StartUrl, Username, EncryptedPassword, EncryptedTotpSecret, AutomationProfileId, AutoSubmit, CreatedAt, UpdatedAt
            FROM LoginEntries
            ORDER BY DisplayName COLLATE NOCASE;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new LoginEntry
            {
                Id = Guid.Parse(reader.GetString(0)),
                DisplayName = reader.GetString(1),
                StartUrl = reader.GetString(2),
                Username = reader.GetString(3),
                EncryptedPassword = reader.GetString(4),
                EncryptedTotpSecret = reader.IsDBNull(5) ? null : reader.GetString(5),
                AutomationProfileId = reader.GetString(6),
                AutoSubmit = reader.GetInt64(7) == 1,
                CreatedAt = DateTimeOffset.Parse(reader.GetString(8)),
                UpdatedAt = DateTimeOffset.Parse(reader.GetString(9))
            });
        }

        return entries;
    }

    public async Task SaveAsync(LoginEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO LoginEntries (Id, DisplayName, StartUrl, Username, EncryptedPassword, EncryptedTotpSecret, AutomationProfileId, AutoSubmit, CreatedAt, UpdatedAt)
            VALUES ($id, $displayName, $startUrl, $username, $encryptedPassword, $encryptedTotpSecret, $automationProfileId, $autoSubmit, $createdAt, $updatedAt)
            ON CONFLICT(Id) DO UPDATE SET
                DisplayName = excluded.DisplayName,
                StartUrl = excluded.StartUrl,
                Username = excluded.Username,
                EncryptedPassword = excluded.EncryptedPassword,
                EncryptedTotpSecret = excluded.EncryptedTotpSecret,
                AutomationProfileId = excluded.AutomationProfileId,
                AutoSubmit = excluded.AutoSubmit,
                CreatedAt = excluded.CreatedAt,
                UpdatedAt = excluded.UpdatedAt;
            """;

        command.Parameters.AddWithValue("$id", entry.Id.ToString());
        command.Parameters.AddWithValue("$displayName", entry.DisplayName);
        command.Parameters.AddWithValue("$startUrl", entry.StartUrl);
        command.Parameters.AddWithValue("$username", entry.Username);
        command.Parameters.AddWithValue("$encryptedPassword", entry.EncryptedPassword);
        command.Parameters.AddWithValue("$encryptedTotpSecret", (object?)entry.EncryptedTotpSecret ?? DBNull.Value);
        command.Parameters.AddWithValue("$automationProfileId", entry.AutomationProfileId);
        command.Parameters.AddWithValue("$autoSubmit", entry.AutoSubmit ? 1 : 0);
        command.Parameters.AddWithValue("$createdAt", entry.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updatedAt", entry.UpdatedAt.ToString("O"));

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM LoginEntries WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        await command.ExecuteNonQueryAsync();
    }
}
