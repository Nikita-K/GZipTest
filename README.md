# GZipTest

## Architecture description

The program is based on the *Dataflow* model. To process the file, a pipeline is built from connected nodes that perform preassigned strategy. Each node works asynchronously and is linked by thread safe transfer channels.
The followings bellow are the nodes that were implemented in the project:
*  `NodeBase` - is a base(abstract) class containing basic logic for starting a node and handling any errors;
*  `ActionNode` - invokes a strategy for each received data packet;
*  `TransformNode` - invokes a strategy for each received data packet and sends the processed data to the linked node.

Implemented strategies: 
*  `UserDataReader` - reads blocks sized of `chunkSize` from the input stream. The default size of block is 1 MB;
*  `UserDataWriter` -  writes decompressed blocks to the output stream in the original source order;
*  `Compressor`/`Decompressor` - compresses/decompresses the data block;
*  `GZipDataReader` - reads blocks from the input stream in *CustomGZip* format;
*  `GZipDataWriter` - writes compressed blocks to the output stream in *CustomGZip* format.

### CustomGZip format description
**CustomGZip** file contains a special header. Each block is written in the following order:
1.  position in source data
2.  data length
2.  compressed data
