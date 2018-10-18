using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ZipDsl
{
    public static partial class DataFuncs
    {
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
            return(inputStream, outputStream) =>
            {
                readEntities(inputStream).ToJson()(outputStream);
            };
        }
    }
}
