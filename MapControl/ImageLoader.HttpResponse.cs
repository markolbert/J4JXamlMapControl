using System;

namespace J4JSoftware.XamlMapControl;

public static partial class ImageLoader
{
    internal class HttpResponse
    {
        public byte[]? Buffer { get; }
        public TimeSpan? MaxAge { get; }

        public HttpResponse(byte[] buffer, TimeSpan? maxAge)
        {
            Buffer = buffer;
            MaxAge = maxAge;
        }
    }
}
