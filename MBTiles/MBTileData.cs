// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace J4JSoftware.XamlMapControl.MBTiles;

public sealed class MbTileData : IDisposable
{
    private readonly SQLiteConnection _connection;

    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();

    private MbTileData(string file)
    {
        _connection = new SQLiteConnection("Data Source=" + Path.GetFullPath(file));
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    public static async Task<MbTileData> CreateAsync(string file)
    {
        var tileData = new MbTileData(file);

        await tileData.OpenAsync();
        await tileData.ReadMetadataAsync();

        return tileData;
    }

    private async Task OpenAsync()
    {
        await _connection.OpenAsync();

        await using (var command = new SQLiteCommand("create table if not exists metadata (name string, value string)", _connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        await using (var command = new SQLiteCommand("create table if not exists tiles (zoom_level integer, tile_column integer, tile_row integer, tile_data blob)", _connection))
        {
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task ReadMetadataAsync()
    {
        try
        {
            await using var command = new SQLiteCommand("select * from metadata", _connection);
            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Metadata[(string)reader["name"]] = (string)reader["value"];
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MBTileData: {ex.Message}");
        }
    }

    public async Task WriteMetadataAsync()
    {
        try
        {
            await using var command =
                new SQLiteCommand( "insert or replace into metadata (name, value) values (@n, @v)", _connection );

            foreach (var keyValue in Metadata)
            {
                command.Parameters.AddWithValue("@n", keyValue.Key);
                command.Parameters.AddWithValue("@v", keyValue.Value);

                await command.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MBTileData: {ex.Message}");
        }
    }

    public async Task<byte[]?> ReadImageBufferAsync(int x, int y, int zoomLevel)
    {
        byte[]? imageBuffer = null;

        try
        {
            await using var command =
                new SQLiteCommand(
                    "select tile_data from tiles where zoom_level=@z and tile_column=@x and tile_row=@y",
                    _connection );

            command.Parameters.AddWithValue("@z", zoomLevel);
            command.Parameters.AddWithValue("@x", x);
            command.Parameters.AddWithValue("@y", (1 << zoomLevel) - y - 1);

            imageBuffer = await command.ExecuteScalarAsync() as byte[];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MBTileData: {zoomLevel}/{x}/{y}: {ex.Message}");
        }

        return imageBuffer;
    }

    public async Task WriteImageBufferAsync(int x, int y, int zoomLevel, byte[] imageBuffer)
    {
        try
        {
            await using var command =
                new SQLiteCommand(
                    "insert or replace into tiles (zoom_level, tile_column, tile_row, tile_data) values (@z, @x, @y, @b)",
                    _connection );

            command.Parameters.AddWithValue("@z", zoomLevel);
            command.Parameters.AddWithValue("@x", x);
            command.Parameters.AddWithValue("@y", (1 << zoomLevel) - y - 1);
            command.Parameters.AddWithValue("@b", imageBuffer);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MBTileData: {zoomLevel}/{x}/{y}: {ex.Message}");
        }
    }
}