using GZipTest.Dataflow;
using System.Collections.Generic;
using Moq;
using System;
using Xunit;
using System.Threading;

namespace GZipTest.Test.DataflowTests
{
    public class TransformNodeTests
    {
        private Mock<IDataTransformer<int, int>> _transformerMock;
        private ITargetNode<int> _defaultTarget;

        public TransformNodeTests()
        {
            _transformerMock = new Mock<IDataTransformer<int, int>>();

            var mockTarget = new Mock<ITargetNode<int>>();
            mockTarget.Setup(m => m.Post(It.IsAny<int>())).Returns(true);
            _defaultTarget = mockTarget.Object;
        }

        [Fact]
        public void ConstructorShouldThrowExceptionIfParametersAreInvalid()
        {
            Assert.Throws<ArgumentNullException>(() => {
                new TransformNode<int, int>(null);
            });
        }

        [Fact]
        public void CompleteMethodShouldThrowExceptionIfNodeIsNotRunningYet()
        {
            var transformNode = new TransformNode<int, int>(_transformerMock.Object);

            Assert.Throws<InvalidOperationException>(() => transformNode.Complete());
            Assert.Throws<InvalidOperationException>(() => transformNode.Wait());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void ShouldCallTransformer(int callsNumber)
        {
            var expectDataList = new List<int>();
            var actualDataList = new List<int>();

            _transformerMock.Setup(m => m.Process(It.IsAny<int>())).Callback(
                (int x) => {
                    actualDataList.Add(x);
                });
            var transformNode = new TransformNode<int, int>(_transformerMock.Object);
            transformNode.LinkTo(_defaultTarget);

            transformNode.Start();
            for (int i = 0; i < callsNumber; i++)
            {
                expectDataList.Add(i);
                transformNode.Post(i);
            }

            transformNode.Complete();
            transformNode.Wait();
            Assert.Equal(expectDataList, actualDataList);
        }

        [Fact]
        public void ShouldCatchExceptionAndStop()
        {
            var expectException = new Exception("test");
            _transformerMock.Setup(m => m.Process(It.IsAny<int>())).Throws(expectException);
            var transformNode = new TransformNode<int, int>(_transformerMock.Object);
            transformNode.LinkTo(_defaultTarget);

            transformNode.Start();
            transformNode.Post(default(int));

            transformNode.Wait();

            Assert.Equal(expectException, transformNode.Exception);
        }

        [Fact]
        public void PostMethodShouldReturnFalseIfNodeIsDown()
        {
            var transformNode = new TransformNode<int, int>(_transformerMock.Object);
            transformNode.LinkTo(_defaultTarget);

            transformNode.Start();

            transformNode.Complete();
            transformNode.Wait();

            Assert.False(transformNode.Post(default(int)));
        }

        [Fact]
        public void ShouldStopAfterCancelling()
        {
            var transformNode = new TransformNode<int, int>(_transformerMock.Object);
            transformNode.LinkTo(_defaultTarget);
            var cts = new CancellationTokenSource();

            transformNode.Start(cts.Token);

            cts.Cancel();
            transformNode.Wait();

            Assert.True(transformNode.Exception is OperationCanceledException);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void ShouldSendDataToTarget(int callsNumber)
        {
            var expectDataList = new List<int>();
            var actualDataList = new List<int>();

            _transformerMock.Setup(m => m.Process(It.IsAny<int>())).Returns((int x) => { return x; });

            var targetMock = new Mock<ITargetNode<int>>();
            targetMock.Setup(m => m.Post(It.IsAny<int>()))
                .Callback((int x) => { actualDataList.Add(x); }).Returns(true);

            var transformNode = new TransformNode<int, int>(_transformerMock.Object);
            transformNode.LinkTo(targetMock.Object);
            transformNode.Start();

            for (int i = 0; i < callsNumber; i++)
            {
                transformNode.Post(i);
                expectDataList.Add(i);
            }

            transformNode.Complete();
            transformNode.Wait();

            Assert.Equal(expectDataList, actualDataList);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void ShouldProcessDataInMultipleThreads(int threadsCount)
        {
            var threadsIdSet = new HashSet<int>();
            var semaphore = new Semaphore(0, threadsCount);
            _transformerMock.Setup(m => m.Process(It.IsAny<int>())).Callback(() =>
            {
                lock (threadsIdSet)
                    threadsIdSet.Add(Thread.CurrentThread.ManagedThreadId);

                semaphore.WaitOne();
            });

            var transformNode = new TransformNode<int, int>(_transformerMock.Object, 
                new NodeOptions { MaxDegreeOfParallelism = (uint)threadsCount });
            transformNode.LinkTo(_defaultTarget);

            transformNode.Start();
            for (int i = 0; i < threadsCount; i++)
            {
                transformNode.Post(default(int));
            }

            Thread.Sleep(100);
            semaphore.Release(threadsCount);

            transformNode.Complete();
            transformNode.Wait();

            Assert.Equal(threadsCount, threadsIdSet.Count);
        }
    }
}
