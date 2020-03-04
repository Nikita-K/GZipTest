using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using GZipTest.Pipeline;

namespace GZipTest
{
    class Program
    {
        private static CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private static Arguments _parsedArgs;
        private static readonly string _help = @"Usage: 
    GZipTest.exe compression_mode path_to_input_file output_file

Args description:
    compression_mode        Operation is performed with the input file. Values: Compress, Decompress.
    path_to_input_file      Path to the file to be compressed/decompressed.
    output_file_name        Name of file with result of the performed operation. Overwrites if already exists.";

        static int Main(string[] args)
        {
            try
            {
                _parsedArgs = Parse(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($@"Error: 
    {ex.Message}");
                Console.WriteLine();
                Console.WriteLine(_help);
                return 1;
            }

            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs cancelArgs) => { _tokenSource.Cancel(); };

            try
            {
                return RunGzipPipeline(_parsedArgs);
            }
            catch (IOException ioEx)
            {
                Console.WriteLine("IO problems. Please check input/output arguments.");
                Console.WriteLine(ioEx.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($@"Fail to {_parsedArgs.Mode} input file:
    {ex.Message}");
            }

            return 1;
        }

        static int RunGzipPipeline(Arguments parsedArgs)
        {
            using (var inputStream = new FileStream(_parsedArgs.PathToInpuFile, FileMode.Open, FileAccess.Read))
            using (var outputStream = new FileStream(_parsedArgs.PathToOutputFile, FileMode.Create, FileAccess.Write))
            {
                using (var gzipPipeline = new GZipPipeline(_parsedArgs.Mode, inputStream, outputStream))
                {
                    gzipPipeline.Process(_tokenSource.Token);
                    if (gzipPipeline.Exception != null)
                    {
                        throw gzipPipeline.Exception;
                    }

                    Console.WriteLine("Successfully сompleted.");
                }
            }

            return 0;
        }

        static Arguments Parse(string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid arguments number");
            }

            CompressionMode opType;
            try
            {
                opType = (CompressionMode)Enum.Parse(typeof(CompressionMode), args[0], true);
            }
            catch (Exception)
            {
                throw new ArgumentException($"Invalid operation type: {args[0]}");
            }

            string pathToInputFile = args[1];
            if (!File.Exists(pathToInputFile))
            {
                throw new ArgumentException($"Input file does not exist: {pathToInputFile}");
            }

            string pathToOutputFile = args[2];
            string outDir = Path.GetDirectoryName(pathToOutputFile);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
            {
                throw new ArgumentException($"Output directory does not exist: {pathToOutputFile}");
            }

            return new Arguments(opType, pathToInputFile, pathToOutputFile);
        }

        public struct Arguments
        {
            public CompressionMode Mode { get; }
            public string PathToInpuFile { get; }
            public string PathToOutputFile { get; }

            public Arguments(CompressionMode mode, string pathToInpuFile, string pathToOutputFile)
            {
                Mode = mode;
                PathToInpuFile = pathToInpuFile;
                PathToOutputFile = pathToOutputFile;
            }
        }
    }
}
