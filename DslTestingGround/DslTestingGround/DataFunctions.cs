using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DslTestingGround
{
    public static partial class DataFunctions
    {
        public static Action<ZipOutputStream> ToZipPart(this Action<Stream> useStream, string fileName, DateTime creationDateTime)
        {
            return zipStream =>
            {
                var newEntry = new ZipEntry(fileName)
                {
                    DateTime = creationDateTime
                };
                zipStream.PutNextEntry(newEntry);
                useStream(zipStream);
                zipStream.CloseEntry();
            };
        }

        public static Action<Stream> ToZip(this Action<ZipOutputStream> useZipStream, int level)
        {
            return outputStream =>
            {
                using (var zipStream = new ZipOutputStream(outputStream))
                {
                    zipStream.SetLevel(level);
                    useZipStream(zipStream);

                    zipStream.IsStreamOwner = false;
                    zipStream.Close();
                    outputStream.Position = 0;
                }
            };
        }

        public static Action<Stream> FromFile(this Action<Stream, Stream> processStream, string path)
        {
            return outputStream =>
            {
                using (var inputStream = File.Open(path, FileMode.Open))
                {
                    processStream(inputStream, outputStream);
                }
            };
        }

        public static Action<Stream> FromString(this Action<Stream, Stream> processStream, string text)
        {
            return outputStream =>
            {
                using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
                {
                    processStream(inputStream, outputStream);
                }
            };
        }

        public static Action<Stream, Stream> Copy()
        {
            return (inputStream, outputStream) => inputStream.CopyTo(outputStream);
        }

        public static Action<Stream> ToJson<T>(this IEnumerable<T> source)
        {
            return outputStream =>
            {
                using (var streamWriter = new StreamWriter(outputStream, Encoding.UTF8, 4000, true))
                {
                    var jsonWriter = new JsonTextWriter(streamWriter);
                    jsonWriter.Formatting = Formatting.Indented;
                    jsonWriter.WriteStartArray();
                    foreach (var obj in source)
                    {
                        JToken.FromObject(obj).WriteTo(jsonWriter);
                    }
                    jsonWriter.WriteEndArray();
                }
            };
        }

        public static Func<Stream, IEnumerable<T>> ReadJsonArray<T>()
        {
            IEnumerable<T> readJson(Stream inputStream)
            {
                using (var streamReader = new StreamReader(inputStream, Encoding.UTF8))
                {
                    var jsonReader = new JsonTextReader(streamReader);
                    while (jsonReader.Read())
                    {
                        if (jsonReader.TokenType == JsonToken.StartArray)
                        {
                            continue;
                        }
                        if (jsonReader.TokenType == JsonToken.StartObject)
                        {
                            var obj = JObject.Load(jsonReader);
                            yield return obj.ToObject<T>();
                        }
                    }
                }
            }
            return readJson;
        }

        public static Func<Stream, IEnumerable<T2>> Map<T1, T2>(
            this Func<Stream, IEnumerable<T1>> readEntities,
            Func<T1, T2> bind) =>
             inputStream => readEntities(inputStream).Select(bind);

        public static Action<Stream, Stream> WriteJson<T>(this Func<Stream, IEnumerable<T>> readEntities)
        {
            return (inputStream, outputStream) =>
             {
                 readEntities(inputStream).ToJson()(outputStream);
             };
        }

        public static (bool success, Exception exception) ToFile(this Action<Stream> useStream, string path)
        {
            try
            {
                using (var outputStream = File.Open(path, FileMode.Create))
                {
                    useStream(outputStream);
                }
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex);
            }
        }
    }
}
