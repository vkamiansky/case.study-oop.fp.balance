using System;
using System.IO;
using Yaapii.Atoms.IO;

namespace DslTestingGround
{
    public class ObjectWay
    {
        public void Run()
        {
            ReadFileToConsole(Path.GetFullPath("try.json"));
            ZipFile(Path.GetFullPath("try.json"), Path.GetFullPath("try.gzip"));
        }

        void ReadFileToConsole(String FilePath)
        {
            new LengthOf(
              new TeeInput(
                  new InputOf(new Uri(FilePath)),
                  new ConsoleOutput())
            ).Value();
        }

        void ZipFile(String FilePath, String ZipFilePath)
        {
            new LengthOf(
              new TeeInput(
                  new InputOf(new Uri(FilePath)),
                    new GZipOutput(new OutputTo(new Uri(ZipFilePath))))
            ).Value();
        }
    }
}
