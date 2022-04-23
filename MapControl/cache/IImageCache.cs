using System;
using System.Threading.Tasks;

namespace J4JSoftware.XamlMapControl.Caching;

public interface IImageCache
{
    Task<Tuple<byte[], DateTime>?> GetAsync(string key);

    Task SetAsync(string key, byte[] buffer, DateTime expiration);
}
