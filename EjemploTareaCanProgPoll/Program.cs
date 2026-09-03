using System;
using System.Threading;
using System.Threading.Tasks;

// #================================================================# 
// #   Tenemos Tarea con Cancellation, OnProgress y polling         #
// #----------------------------------------------------------------#
// #   using var cts = new CancellationTokenSource();               #
// #   var myTask = new MagicNumberTask();                          #
// #   myTask.OnProgress += (percentage, message) =>                #
// #   var executionTask = myTask.ExecuteAsync(cts.Token);          #
// #   while (!executionTask.IsCompleted)                           #
// #   int result = await executionTask;                            #
// #================================================================# 
// Creado en colaboración con DeepSeek
namespace TaskPattern
{
    class Program
    {
        // Aplicación de Consola que demuestra el funcionamiento del patrón
        // Usando Await en vez de Polling
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Starting Main Program (Await Version)");
            Console.WriteLine("=".PadRight(50, '='));

            using var cts = new CancellationTokenSource();
        
            var myTask = new MagicNumberTask();
        
            myTask.OnProgress += (percentage, message) =>
            {
                Console.WriteLine($"[Progress] {percentage}%: {message}");
            };

            // Start the task but don't await yet
            var executionTask = myTask.ExecuteAsync(cts.Token);

            // Do other work while task runs
            int loopCounter = 0;
            Console.WriteLine("\n🔄 Main thread doing work while task runs...");
        
            while (!executionTask.IsCompleted)
            {
                loopCounter++;
                Console.WriteLine($"  Main loop iteration #{loopCounter} - Doing work...");
                await Task.Delay(300);
            
                // Check if we should continue
                if (loopCounter > 20) break; // Safety valve
            }

            // Now await the result
            try
            {
                int result = await executionTask;
                Console.WriteLine($"\n✅ Main loop finished after {loopCounter} iterations");
                Console.WriteLine($"🎉 Final Result: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }

            Console.WriteLine("\n🏁 Program finished!");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            // Y nos fuimos ...
        }
    }


    // La implementación de nuestra tarea concreta (implementación de la abstracta)
    public class MagicNumberTask : AbstractTask<int>
    {
        // El método ProcessAsync ...
        protected override async Task<int> ProcessAsync(
            ProgressReport progressReport,
            CancellationToken cancellationToken)
        {
            // Informa del avance/comienzo
            progressReport(0, "Starting magic number generation...");
            // Inicializamos contadores
            int totalSteps = 10;
            int currentValue = 0;
            // El loop ...
            for (int step = 1; step <= totalSteps; step++)
            {
                // Verificar si nos cancelaron!!
                cancellationToken.ThrowIfCancellationRequested();

                // Simulamos trabajo!!
                await Task.Delay(500, cancellationToken);
                
                // Actualizamos los informes de actividad
                int percentage = (int)((double)step / totalSteps * 100);
                currentValue = step * 4 + 2; // Va a terminar en 42
                progressReport(percentage, $"Processing step {step}/{totalSteps}, current value: {currentValue}");
            }

            progressReport(100, "Magic number generated successfully!");
            
            // Devolvemos la respuesta al sentido de la vida, el universo y t0do lo demás (42)
            return currentValue;
        }
    }
}