# GZipTest

## Architecture description

The program is based on the *Dataflow* model. 
The main idea is to split the global task into several subtasks. 
These subtasks are named *Nodes*. 
Nodes do useful work asynchronously and are linked by thread safe data transfer channels. 
The followings bellow are the nodes that were implemented in the project:

*  `NodeBase` - is a base(abstract) class containing basic logic for starting a node and handling any errors;
*  `UserDataReader` - reads blocks sized of `chunkSize` from the data stream and put them in an output queue. The default size of block is 1 MB;
*  `UserDataWriter` -  gets compressed blocks from input queue and writes them to output stream in original source order;
*  `GZipNode` - gets data blocks from input queue, compresses or decompresses data and put them to output queue. Contains `CustomThreadPool` for parallel processing of blocks;
*  `GZipDataReader` - reads blocks from data stream in *CustomGZip* format and put them in an output queue;
*  `GZipDataWriter` - gets compressed blocks from input queue and writes them to output stream in *CustomGZip* format;
*  `StatusNode` - collects statuses from input queue and prints summury status once per second. Can be disabled in the `App.config` file.

Depending on `CompressionMode`, the program builds and runs pipeline containing nodes connected via `BlockingQueue`.

### CustomGZip format description
**CustomGZip** file contains a special header. Each block is written accordingly with the following order:
1.  offset in source data
2.  data length
2.  compressed data
