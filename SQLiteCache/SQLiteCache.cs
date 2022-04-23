// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace J4JSoftware.XamlMapControl.Caching
{
    /// <summary>
    /// Image cache implementation based on SqLite.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public sealed class SQLiteCache : IImageCache, IDisposable
    {
        private readonly SQLiteConnection _connection;

        public SQLiteCache(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("The path argument must not be null or empty.", nameof(path));

            if (string.IsNullOrEmpty(Path.GetExtension(path)))
                path = Path.Combine(path, "TileCache.sqlite");

            _connection = Open(Path.GetFullPath(path));

            Clean();
        }

        private static SQLiteConnection Open(string path)
        {
            var connection = new SQLiteConnection("Data Source=" + path);
            connection.Open();

            using var command = new SQLiteCommand(
                "create table if not exists items (key text primary key, expiration integer, buffer blob)",
                connection );

            command.ExecuteNonQuery();

            Debug.WriteLine($"SQLiteCache: Opened database {path}");

            return connection;
        }

        public void Dispose() => _connection.Dispose();

        public void Clean()
        {
            using var command = new SQLiteCommand("delete from items where expiration < @exp", _connection);

            command.Parameters.AddWithValue("@exp", DateTime.UtcNow.Ticks);
            command.ExecuteNonQuery();
#if DEBUG
            using var command2 = new SQLiteCommand("select changes()", _connection);

            var deleted = (long)command2.ExecuteScalar();
            if (deleted > 0)
                Debug.WriteLine($"SQLiteCache: Deleted {deleted} expired items");
#endif
        }

        public async Task<Tuple<byte[], DateTime>?> GetAsync(string key)
        {
            try
            {
                await using var command = GetItemCommand(key);
                var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                    return Tuple.Create((byte[])reader["buffer"], new DateTime((long)reader["expiration"]));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SQLiteCache.GetAsync({key}): {ex.Message}");
            }

            return null;
        }

        public async Task SetAsync(string key, byte[]? buffer, DateTime expiration)
        {
            try
            {
                await using var command = SetItemCommand(key, buffer, expiration);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SQLiteCache.SetAsync({key}): {ex.Message}");
            }
        }

        private SQLiteCommand RemoveItemCommand(string key)
        {
            var command = new SQLiteCommand("delete from items where key = @key", _connection);
            command.Parameters.AddWithValue("@key", key);

            return command;
        }

        private SQLiteCommand GetItemCommand(string key)
        {
            var command = new SQLiteCommand("select expiration, buffer from items where key = @key", _connection);
            command.Parameters.AddWithValue("@key", key);
            return command;
        }

        private SQLiteCommand SetItemCommand(string key, byte[]? buffer, DateTime expiration)
        {
            var command = new SQLiteCommand("insert or replace into items (key, expiration, buffer) values (@key, @exp, @buf)", _connection);
            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@exp", expiration.Ticks);
            command.Parameters.AddWithValue("@buf", buffer ?? Array.Empty<byte>());

            return command;
        }
    }
}
