using GZipTest.Dataflow;
using GZipTest.Utilities;

namespace GZipTest.Pipeline
{
    public class Compressor : IDataTransformer<DataBlock, DataBlock>
    {
        public DataBlock Process(DataBlock dataBlock)
        {
            return new DataBlock(dataBlock.Position, dataBlock.Data.Compress());
        }
    }
}
