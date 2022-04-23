// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using FileDbNs;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using J4JSoftware.XamlMapControl.Caching;

namespace MapControl.Caching;

/// <summary>
/// Image cache implementation based on FileDb, a free and simple No-SQL database by EzTools Software.
/// See http://www.eztools-software.com/tools/filedb/.
/// </summary>
public sealed class FileDbCache : IImageCache, IDisposable
{
    private const string KeyField = "Key";
    private const string ValueField = "Value";
    private const string ExpiresField = "Expires";

    private readonly FileDb _fileDb = new() { AutoFlush = true };

    public FileDbCache( string path )
    {
        if( string.IsNullOrEmpty( path ) )
            throw new ArgumentException( "The path argument must not be null or empty.", nameof( path ) );

        if( string.IsNullOrEmpty( Path.GetExtension( path ) ) )
            path = Path.Combine( path, "TileCache.fdb" );

        Open( path );
    }

    public Task<Tuple<byte[], DateTime>?> GetAsync( string key ) =>
        Task.Run( () =>
        {
            var record = GetRecordByKey( key );

            return record == null
                ? null
                : Tuple.Create( (byte[]) record[ 0 ], (DateTime) record[ 1 ] );
        } );

    public Task SetAsync( string key, byte[] buffer, DateTime expiration ) =>
        Task.Run( () => AddOrUpdateRecord( key, buffer, expiration ) );

    public void Dispose() => _fileDb.Dispose();

    public void Clean()
    {
        var deleted =
            _fileDb.DeleteRecords(
                new FilterExpression( ExpiresField, DateTime.UtcNow, ComparisonOperatorEnum.LessThan ) );

        if( deleted <= 0 )
            return;

        Debug.WriteLine( $"FileDbCache: Deleted {deleted} expired items" );
        _fileDb.Clean();
    }

    private void Open( string path )
    {
        try
        {
            _fileDb.Open( path );
            Debug.WriteLine( $"FileDbCache: Opened database {path}" );

            Clean();
        }
        catch
        {
            if( File.Exists( path ) )
                File.Delete( path );
            else
            {
                var dir = Path.GetDirectoryName( path );

                if( !string.IsNullOrEmpty( dir ) )
                    Directory.CreateDirectory( dir );
            }

            _fileDb.Create( path,
                            new[]
                            {
                                new Field( KeyField, DataTypeEnum.String ) { IsPrimaryKey = true },
                                new Field( ValueField, DataTypeEnum.Byte ) { IsArray = true },
                                new Field( ExpiresField, DataTypeEnum.DateTime )
                            } );

            Debug.WriteLine( $"FileDbCache: Created database {path}" );
        }
    }

    private Record? GetRecordByKey( string key )
    {
        try
        {
            return _fileDb.GetRecordByKey( key, new[] { ValueField, ExpiresField }, false );
        }
        catch( Exception ex )
        {
            Debug.WriteLine( $"FileDbCache.GetRecordByKey({key}): {ex.Message}" );
        }

        return null;
    }

    private void AddOrUpdateRecord( string key, byte[]? buffer, DateTime expiration )
    {
        var fieldValues = new FieldValues( 3 )
        {
            { ValueField, buffer ?? Array.Empty<byte>() }, { ExpiresField, expiration }
        };

        try
        {
            if( _fileDb.GetRecordByKey( key, Array.Empty<string>(), false ) != null )
                _fileDb.UpdateRecordByKey( key, fieldValues );
            else
            {
                fieldValues.Add( KeyField, key );
                _fileDb.AddRecord( fieldValues );
            }
        }
        catch( Exception ex )
        {
            Debug.WriteLine( $"FileDbCache.AddOrUpdateRecord({key}): {ex.Message}" );
        }
    }
}