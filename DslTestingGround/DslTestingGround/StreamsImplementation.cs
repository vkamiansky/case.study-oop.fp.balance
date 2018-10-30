using System;
using System.IO;

namespace DslTestingGround.Streams
{
    public static partial class Data
    {
        public static Action<Stream> FromFile(this Action<Stream, Stream> transferData, string path)
        {
            return outputStream =>
            {
                using (var inputStream = File.Open(path, FileMode.Open))
                    transferData(inputStream, outputStream);
            };
        }

        public static Action<Stream, Stream> Copy()
        {
            return (inputStream, outputStream) => inputStream.CopyTo(outputStream);
        }

        public static (bool success, Exception exception) ToFile(this Action<Stream> useOutputStream, string path)
        {
            try
            {
                using (var outputStream = File.Open(path, FileMode.Create))
                    useOutputStream(outputStream);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex);
            }
        }
    }
}
