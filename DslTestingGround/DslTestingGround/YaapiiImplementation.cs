using System;
using Yaapii.Atoms;
using Yaapii.Atoms.IO;

namespace DslTestingGround.Yaapii
{
    public static class DataFunctions
    {
        public static Action<IOutput> FromFile(this Action<IInput, IOutput> transferData, string path) =>
            output => transferData(new InputOf(new Uri(path)), output);

        public static Action<IInput, IOutput> Copy() =>
            (input, output) => new LengthOf(new TeeInput(input, output)).Value();

        public static (bool success, Exception exception) ToFile(this Action<IOutput> useOutputStream, string path)
        {
            try
            {
                useOutputStream(new OutputTo(new Uri(path)));
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex);
            }
        }
    }
}
