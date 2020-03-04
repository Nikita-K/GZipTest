using GZipTest.Dataflow;
using System;
using System.Collections.Generic;
using System.IO;

namespace GZipTest.Pipeline.IO
{
    public class UserDataWriter : IDataConsumer<DataBlock>
    {
        private uint _currentPosition;
        private readonly BinaryWriter _outputStream;
        private readonly Dictionary<long, DataBlock> _positionToDataDict;

        public UserDataWriter(BinaryWriter outputStream)
        {
            _ = outputStream ?? throw new ArgumentNullException($@"Parameter {nameof(outputStream)} cannot be null.");

            _outputStream = outputStream;
            _positionToDataDict = new Dictionary<long, DataBlock>();
        }

        public bool IsCompleted => _positionToDataDict.Count == 0;

        public void Process(DataBlock dataBlock)
        {
            if(dataBlock.Position < _currentPosition)
            {
                throw new InvalidDataException($"Duplicate packet position: {_currentPosition}");
            }

            if (_currentPosition != dataBlock.Position)
            {
                _positionToDataDict[dataBlock.Position] = dataBlock;
                return;
            }

            _outputStream.Write(dataBlock.Data);
            _currentPosition++;

            while (_positionToDataDict.ContainsKey(_currentPosition))
            {
                dataBlock = _positionToDataDict[_currentPosition];

                _outputStream.Write(dataBlock.Data);

                _positionToDataDict.Remove(_currentPosition);
                _currentPosition++;
            }
        }
    }
}
