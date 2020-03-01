using GZipTest.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GZipTest.Test.UtilitiesTests
{
    public class BlockingQueueTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void ShouldThrowExceptionIfSizeMaxHasInvalidValue(int sizeMax)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new BlockingQueue<int>(sizeMax);
            });
        }

        [Fact]
        public void TryDenqueueShouldReturnFalseWhenQueueIsEmpty()
        {
            var queue = new BlockingQueue<int>();
            Assert.False(queue.TryDequeue(out int item));
        }

        [Fact]
        public void TryEnqueueShouldReturnFalseWhenQueueIsFull()
        {
            var queue = new BlockingQueue<int>(1);
            Assert.True(queue.TryEnqueue(1));
            Assert.False(queue.TryEnqueue(1));
        }

        [Fact]
        public void ShouldDequeueElementAfterItAddedToEmptyQueue()
        {
            var expectItem = 10;
            var queue = new BlockingQueue<int>();
            var dequeueTask = Task.Run(() =>
            {
                queue.Dequeue(out int item);
                return item;
            });

            Thread.Sleep(100);
            queue.Enqueue(expectItem);
            var actualItem = dequeueTask.Result;

            Assert.Equal(expectItem, actualItem);
        }

        [Fact]
        public void ShouldEnqueToFullQueueAfterDequeue()
        {
            var expectItem = 10;
            var queue = new BlockingQueue<int>(1);
            queue.Enqueue(-1);

            var enqueueTask = Task.Run(() =>
            {
                queue.Enqueue(expectItem);
            });

            Thread.Sleep(100);
            queue.Dequeue(out int item);
            enqueueTask.Wait();

            queue.Dequeue(out int actualItem);
            Assert.Equal(expectItem, actualItem);
        }

        [Fact]
        public void StressTestForEnqueueDequeueOperations()
        {
            int expectItem = 111;
            int sizeMax = 500;
            int enqueueTasksCount = 5;
            int expectItemsCount = sizeMax * enqueueTasksCount;
            var queue = new BlockingQueue<int>(sizeMax);
            var semaphre = new SemaphoreSlim(0, expectItemsCount);

            var equeueTasks = new List<Task>();
            for (int i = 0; i < enqueueTasksCount; i++)
            {
                equeueTasks.Add(Task.Run(() =>
                {
                    semaphre.Wait();
                    for (int j = 0; j < sizeMax; j++)
                    {
                        queue.Enqueue(expectItem);
                    }
                }));
            }

            var dequeueTask = Task.Run(() => {
                var items = new List<int>();
                for (int i = 0; i < expectItemsCount; i++)
                {
                    queue.Dequeue(out int item);
                    items.Add(item);
                }

                return items;
            });

            semaphre.Release(expectItemsCount);
            Task.WaitAll(equeueTasks.ToArray());
            var actualItems = dequeueTask.Result;

            Assert.Equal(expectItemsCount, actualItems.Count);
            Assert.True(actualItems.Find(x => x != expectItem) == 0);
        }
    }
}
