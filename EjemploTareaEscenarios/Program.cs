using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskPattern
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🧪 Testing m_Task Scenarios\n");
            Console.WriteLine("=".PadRight(50, '='));

            // Scenario 1: Execute() + WaitForFinishAsync() ✅
            await Scenario1();
            
            // Scenario 2: ExecuteAsync() + WaitForFinishAsync() ❌
            await Scenario2();
            
            // Scenario 3: Just WaitForFinishAsync() ✅
            await Scenario3();
            
            // Scenario 4: ExecuteAsync() + await the returned task ✅
            await Scenario4();
        }

        static async Task Scenario1()
        {
            Console.WriteLine("\n📌 Scenario 1: Execute() + WaitForFinishAsync()");
            var task = new MagicNumberTask();
            task.Execute();  // ✅ Sets m_Task
            
            await Task.Delay(100); // Let it start
            Console.WriteLine($"  m_Task is {(task.m_Task == null ? "null" : "NOT null")}");
            
            int result = await task.WaitForFinishAsync();  // ✅ Uses m_Task
            Console.WriteLine($"  Result: {result} ✅");
        }

        static async Task Scenario2()
        {
            Console.WriteLine("\n📌 Scenario 2: ExecuteAsync() + WaitForFinishAsync()");
            var task = new MagicNumberTask();
            _ = task.ExecuteAsync();  // ❌ Does NOT set m_Task
            
            await Task.Delay(100);
            Console.WriteLine($"  m_Task is {(task.m_Task == null ? "null" : "NOT null")}");
            
            try
            {
                int result = await task.WaitForFinishAsync();  // ❌ Calls ExecuteAsync() again
                Console.WriteLine($"  Result: {result}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"  ❌ Error: {ex.Message}");
            }
        }

        static async Task Scenario3()
        {
            Console.WriteLine("\n📌 Scenario 3: Just WaitForFinishAsync()");
            var task = new MagicNumberTask();  // m_Task is null
            Console.WriteLine($"  m_Task is {(task.m_Task == null ? "null" : "NOT null")}");
            
            int result = await task.WaitForFinishAsync();  // ✅ Starts new task
            Console.WriteLine($"  Result: {result} ✅");
        }

        static async Task Scenario4()
        {
            Console.WriteLine("\n📌 Scenario 4: ExecuteAsync() + await returned task");
            var task = new MagicNumberTask();
            var runningTask = task.ExecuteAsync();  // ❌ m_Task still null
            
            await Task.Delay(100);
            Console.WriteLine($"  m_Task is {(task.m_Task == null ? "null" : "NOT null")}");
            
            int result = await runningTask;  // ✅ Await the stored task directly
            Console.WriteLine($"  Result: {result} ✅");
        }
    }

    // Modified to expose m_Task for testing
    public class MagicNumberTask : AbstractTask<int>
    {
        // Expose m_Task for testing
        public object m_Task => typeof(AbstractTask<int>)
            .GetField("m_Task", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(this);

        protected override async Task<int> ProcessAsync(
            ProgressReport progressReport,
            CancellationToken cancellationToken)
        {
            for (int step = 1; step <= 3; step++)
            {
                await Task.Delay(200, cancellationToken);
                progressReport(step * 33, $"Step {step}/3");
            }
            return 42;
        }
    }
}
