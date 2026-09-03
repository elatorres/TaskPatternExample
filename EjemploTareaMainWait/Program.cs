using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskPattern
{
    class Program
    {
        // #================================================================#
        // #  Este ejemplo invoca a la tarea, mientras el main hace sus     #
        // #  cosas. Luego Main espera a que la tarea termine de resultado  #
        // #----------------------------------------------------------------#
        // #  MyTask= new ConcreteTask();                                   #
        // #  runningTask = myTask.ExecuteAsync();                          #
        // #  int result = await runningTask;                               #
        // #================================================================#
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Starting Program");
            Console.WriteLine("=".PadRight(40, '='));

            var myTask = new MagicNumberTask();
            
            // ✅ START the task and STORE the running task reference
            Console.WriteLine("\n📦 Starting background task...");
            var runningTask = myTask.ExecuteAsync();  // ← This returns Task<int>

            // Main thread does other work
            Console.WriteLine("🔄 Main thread doing work...");
            for (int i = 1; i <= 3; i++)
            {
                await Task.Delay(400);
                Console.WriteLine($"  Main iteration #{i} - Working...");
            }

            // ✅ AWAIT the stored task directly - NO WaitForFinishAsync needed!
            Console.WriteLine("\n⏳ Waiting for background task to finish...");
            int result = await runningTask;  // ← Just await the task!
            
            Console.WriteLine($"\n🎉 Final Result: {result}");
            Console.WriteLine("🏁 Program finished!");
        }
    }

    public class MagicNumberTask : AbstractTask<int>
    {
        protected override async Task<int> ProcessAsync(
            ProgressReport progressReport,
            CancellationToken cancellationToken)
        {
            for (int step = 1; step <= 5; step++)
            {
                await Task.Delay(500, cancellationToken);
                int percentage = step * 20;
                Console.WriteLine($"ProcessAsync:   percentage {percentage} complete");
            }
            return 42;
        }
    }
}