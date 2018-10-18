using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ZipDsl
{
    class TextToken { public string Token {get;set;} }

    class Program
    {
        static void Main(string[] args)
        {
            var (success, exception) = 

            DataFuncs.ReadJsonArray<TextToken>()
                .Map(x => new {Category = "token", Text = x})
                .WriteJson()

            // DataFuncs.Copy()
                .FromFile("C:\\projects\\try.json")
                .ToZipPart(fileName: "try.json", creationDateTime: DateTime.Now)
                .ToZip(level: 3)
                // .ToFile("C:\\projects\\try2.json");
                .ToFile("C:\\projects\\try.zip");

            Console.WriteLine(success ? "Успех!" : $"Ошибка:\"{exception.Message}\", стек: {exception.StackTrace}");
        }
    }
    
    public static partial class DataFuncs
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

        public static Action<Stream, Stream> BeautifyJsonAndWrite()
        {
            return (inputStream, outputStream) =>
            {
                using(var reader = new StreamReader(inputStream, Encoding.UTF8))
                using(var writer = new StreamWriter(outputStream, Encoding.UTF8, 4000, true))
                {
                    var jsonReader = new JsonTextReader(reader);
                    var jsonWriter = new JsonTextWriter(writer);
                    var serializer = JsonSerializer.Create(new JsonSerializerSettings(){ Formatting = Formatting.Indented });
                    var obj = serializer.Deserialize(jsonReader);
                    serializer.Serialize(jsonWriter, obj);
                }
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
            catch(Exception ex)
            {
                return (false, ex);
            }
        }
    }
}
