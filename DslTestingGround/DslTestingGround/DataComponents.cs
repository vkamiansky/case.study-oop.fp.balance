using System;
using System.IO;

namespace DslTestingGround
{
    public interface IDataSupplier
    {
        void SupplyTo(Stream outputStream);
    }

    public interface IDataRelay
    {
        void DoRelay(Stream inputStream, Stream outputStream);
    }

    public class DataCopier : IDataRelay
    {
        public void DoRelay(Stream inputStream, Stream outputStream)
        {
            inputStream.CopyTo(outputStream);
        }
    }

    public class FileReader : IDataSupplier
    {
        private IDataRelay _dataRelay;

        private string _filePath;

        public FileReader(string filePath, IDataRelay dataRelay)
        {
            _dataRelay = dataRelay;
            _filePath = filePath;
        }

        public void SupplyTo(Stream outputStream)
        {
            using (var inputStream = File.Open(_filePath, FileMode.Open))
                _dataRelay.DoRelay(inputStream, outputStream);
        }
    }

    public class FileWriter
    {
        private IDataSupplier _dataSupplier;

        public FileWriter(IDataSupplier dataSupplier)
        {
            _dataSupplier = dataSupplier;
        }

        public Result WriteData(string filePath)
        {
            try
            {
                using (var outputStream = File.Open(filePath, FileMode.Create))
                    _dataSupplier.SupplyTo(outputStream);

                return new Result(true, null);
            }
            catch (Exception ex)
            {
                return new Result(false, ex);
            }
        }
    }
}