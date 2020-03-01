using GZipTest.Dataflow;
using System;
using Xunit;
using Moq;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest.Test.DataflowTests
{
    public class ActionNodeTests
    {
        [Fact]
        public void ConstructorShouldThrowExceptionIfParametersAreInvalid()
        {
            Assert.Throws<ArgumentNullException>(() => {
                new ActionNode<int>(null);
            });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void ShouldCallConsumer(int callsNumber)
        {
            var expectDataList = new List<int>();
            var actualDataList = new List<int>();

            var consumerMock = new Mock<IDataConsumer<int>>();
            consumerMock.Setup(m => m.Process(It.IsAny<int>())).Callback(
                (int x) => {
                    actualDataList.Add(x);
            });
            var actionNode = new ActionNode<int>(consumerMock.Object);

            actionNode.Start();
            for (int i = 0; i < callsNumber; i++)
            {
                expectDataList.Add(i);
                actionNode.Post(i);
            }

            actionNode.Complete();
            actionNode.Wait();
            Assert.Equal(expectDataList, actualDataList);
        }

        [Fact]
        public void ShouldCatchExceptionAndStop()
        {
            var expectException = new Exception("test");
            var consumerMock = new Mock<IDataConsumer<int>>();
            consumerMock.Setup(m => m.Process(It.IsAny<int>())).Throws(expectException);
            var actionNode = new ActionNode<int>(consumerMock.Object);

            actionNode.Start();
            actionNode.Post(default(int));

            actionNode.Wait();

            Assert.Equal(expectException, actionNode.Exception);
        }

        [Fact]
        public void PostMethodShouldReturnFalseIfNodeIsDown()
        {
            var consumerMock = new Mock<IDataConsumer<int>>();
            var actionNode = new ActionNode<int>(consumerMock.Object);

            actionNode.Start();

            actionNode.Complete();
            actionNode.Wait();

            Assert.False(actionNode.Post(default(int)));
        }

        [Fact]
        public void ShouldStopAfterCancelling()
        {
            var consumerMock = new Mock<IDataConsumer<int>>();
            var actionNode = new ActionNode<int>(consumerMock.Object);
            var cts = new CancellationTokenSource();

            actionNode.Start(cts.Token);

            cts.Cancel();
            actionNode.Wait();

            Assert.True(actionNode.Exception is OperationCanceledException);
        }
    }
}
