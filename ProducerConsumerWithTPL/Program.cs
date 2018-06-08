using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ProducerConsumerWithTPL
{
    class Program
    {
		// BlockingCollection creates a list of type ConcurrentBag with max size of ten positions
        // If a producer try to add one more item It will wait until some consumer grab a value and release a position
		static BlockingCollection<int> messages = 
			new BlockingCollection<int>(new ConcurrentBag<int>(), 10);
        
        // Necessary object to stop thread execution properly when requested
		static CancellationTokenSource cts = new CancellationTokenSource();

        static Random random = new Random();

        static void Main(string[] args)
        {
            Task.Factory.StartNew(RunProducerConsumer);
            Console.ReadKey();
            cts.Cancel();         
        }

        static void RunProducerConsumer()
        {
            var producer1 = Task.Factory.StartNew(RunProducer);
            var producer2 = Task.Factory.StartNew(RunProducer);
            var consumer = Task.Factory.StartNew(RunConsumer);

            try
            {
                Task.WaitAll(new[] { producer1, producer2, consumer }, cts.Token);
            }
            // Exception handling with multithreading. If an error occurs a AggregateException is raised
            catch (AggregateException ae)
            {
				ae.Handle(e => 
				{
					Console.WriteLine($"Oops! Something wrong happened. Please see errors below:");
					foreach (var error in ae.InnerExceptions)
					{
						Console.WriteLine($"  - {error.Message}");
					}
					return true;
				});
			}
        }

        static void RunProducer()
        {
            while (true)
            {
				// Command sent to thread which are running this method -> stop here
                cts.Token.ThrowIfCancellationRequested();
                int i = random.Next(100);

                //Adding value to buffer
                messages.Add(i);
                Console.WriteLine($"Producing {i} from {Task.CurrentId}");
                Thread.Sleep(random.Next(100));
            }
        }

        static void RunConsumer()
        {
			// The method 'GetConsumingEnumerable()' gets a value from buffer
            foreach (var item in messages.GetConsumingEnumerable())
            {
                cts.Token.ThrowIfCancellationRequested();
                Console.WriteLine($"Consuming {item}\t");
                Thread.Sleep(random.Next(1000));
            }
        }
    }
}
