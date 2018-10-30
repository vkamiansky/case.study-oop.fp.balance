using System;

namespace DslTestingGround.Components
{
    public static class DataFunctions
    {
        public static IDataSupplier FromFile(this IDataRelay transferData, string path) =>
            new FileReader(path, transferData);

        public static IDataRelay Copy() =>
            new DataCopier();

        public static (bool success, Exception exception) ToFile(this IDataSupplier useOutputStream, string path)
        {
            try
            {
                new FileWriter(useOutputStream).WriteData(path);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex);
            }
        }
    }
}
