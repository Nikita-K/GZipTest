using GZipTest.Pipeline;
using System;
using System.Collections.Generic;

namespace GZipTest.Test.Helpers
{
    public class DataProvider
    {
        public static DataBlock[] PrepareDataBlocks(int blocksCount, int chunkSize)
        {
            var blocksList = new DataBlock[blocksCount + 1];
            for (int i = 0; i < blocksCount + 1; i++)
            {
                var rnd = new Random();
                var data = new byte[chunkSize];
                rnd.NextBytes(data);

                blocksList[i] = new DataBlock(i * chunkSize, data);
            }

            blocksList[blocksCount] = new DataBlock(long.MaxValue, new byte[0]);
            return blocksList;
        }
    }
}
