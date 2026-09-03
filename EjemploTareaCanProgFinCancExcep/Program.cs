using System;
using System.Threading;
using System.Threading.Tasks;
// #================================================================#
// #   Tenemos Tarea con Cancellation, OnProgress, OnFinish y       #
// #   cancela y atrapa excepción                                   #
// #----------------------------------------------------------------#
// #   using var cts = new CancellationTokenSource();               #
// #   var myTask = new MagicNumberTask();                          #
// #   myTask.OnProgress += (p, m) => ...                           #
// #   myTask.OnFinishTask += (task, result, error) => ...          #
// #   var task = myTask.ExecuteAsync(cts.Token);                   #
// #   cts.Cancel();                                                #
// #   try int result = await task;                                 #
// #   catch (OperationCanceledException)                           #
// #================================================================#
// Creado en colaboración con DeepSeek
namespace TaskPattern
{
    class Program
    {
        // Aplicación de Consola que demuestra el funcionamiento del patrón
        // con cancellation token
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Starting with Cancellation");
            Console.WriteLine("=".PadRight(50, '='));

            using var cts = new CancellationTokenSource();
            var myTask = new MagicNumberTask();
        
            myTask.OnProgress += (p, m) => Console.WriteLine($"[Progress] {p}%: {m}");
            myTask.OnFinishTask += (task, result, error) =>
            {
                if (error is OperationCanceledException)
                    Console.WriteLine("[Completion] Task was cancelled!");
                else if (error != null)
                    Console.WriteLine($"[Completion] Task failed: {error.Message}");
                else
                    Console.WriteLine($"[Completion] Task completed with result: {result}");
            };

            // Start the task
            var task = myTask.ExecuteAsync(cts.Token);

            // Cancel after 2 seconds
            Console.WriteLine("\n⏰ Cancelling task in 2 seconds...");
            await Task.Delay(2000);
            cts.Cancel();

            try
            {
                int result = await task;
                Console.WriteLine($"Result: {result}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("✅ Task was successfully cancelled!");
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