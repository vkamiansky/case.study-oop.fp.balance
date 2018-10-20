using System;

namespace DslTestingGround
{
    /// <summary>
    /// A token class used for deserialization
    /// </summary>
    class TextToken { public string Token { get; set; } }

    class Program
    {
        static void Main(string[] args)
        {
            // Seamless DSL usage with FP elements
            var (success, exception) =
                DataFunctions.ReadJsonArray<TextToken>()
                    .Map(x => new { Category = "token", Text = x })
                    .WriteJson()

                    // DataFunctions.Copy()
                    .FromFile("try.json")
                    .ToZipPart(fileName: "try.json", creationDateTime: DateTime.Now)
                    .ToZip(level: 3)
                    // .ToFile("try2.json");
                    .ToFile("try.zip");

            Console.WriteLine(success ? "Success!" : $"Exception:\"{exception.Message}\", tack trace: {exception.StackTrace}");

            // Trying to do the same thing OOP-style
            // var result = Strategy.Copy()
            //     .Transit(Transition.FromFile("try.json"))
            //     .Transit(Transition.WriteFile("try2.json"));

            // Console.WriteLine(result.IsSuccess ? "Success!" : $"Exception:\"{result.Exception.Message}\", tack trace: {result.Exception.StackTrace}");
        }
    }
}