using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskPattern
{
    // #================================================================#        
    // #   Este ejemplo invoca a la tarea, mientras el main simplemente #
    // #   hace await de int result = await runningTask;                #
    // #   No hay polling, reporte de terminación ni sincronización     #
    // #   Y recibe el resultado. Hay reporte de avance                 #
    // #----------------------------------------------------------------#
    // #   var myTask = new MagicNumberTask();                          #
    // #   myTask.OnProgress += ...                                     #
    // #   var runningTask = myTask.ExecuteAsync();                     #
    // #   int result = await runningTask;                              #
    // #================================================================#
    class Program
    {
        static async Task Main(string[] args)
        {
            // Avisamos que empezamos
            Console.WriteLine("🚀 Starting Program");
            // Doble raya
            Console.WriteLine("=".PadRight(40, '='));
            // Instanciamos la clase concreta
            var myTask = new MagicNumberTask();
            // Registramos la delegada de progreso
            myTask.OnProgress += (percentage, message) =>
            {
                Console.WriteLine($"  [Progress] {percentage}%: {message}");
            };
            
            // Comenzamos la tarea y guardamos la referencia a la tarea en ejecución
            Console.WriteLine("\n📦 Starting background task...");
            var runningTask = myTask.ExecuteAsync();  // ← devuelve promesa Task<int>

            // El Main hace otras cosas
            Console.WriteLine("🔄 Main thread doing work...");
            for (int i = 1; i <= 3; i++)
            {
                await Task.Delay(400);
                Console.WriteLine($"  Main iteration #{i} - Working...");
            }

            // espera por la tarea runningTask directamente - NO necesita WaitForFinishAsync!
            Console.WriteLine("\n⏳ Waiting for background task to finish...");
            int result = await runningTask;  // ← Just await the task!
            // Mostramos el resultado y ya
            Console.WriteLine("The Answer to the Ultimate Question of Life, the Universe, and Everything is ...");
            Console.WriteLine($"\n🎉 Final Result: {result}");
            Console.WriteLine("🏁 Program finished!");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            // Y nos fuimos ...
        } // Fin Main 
    } // Fin CLass Program
    
    // Implementamos la clase abstracta
    public class MagicNumberTask : AbstractTask<int>
    {
        // Sólo es necesario ProcessAsync
        protected override async Task<int> ProcessAsync(
            ProgressReport progressReport,
            CancellationToken cancellationToken)
        {
            // Indica que comienza
            progressReport(0, "Starting...");
            // Simula trabajo y avance
            for (int step = 1; step <= 5; step++)
            {
                await Task.Delay(500, cancellationToken);
                int percentage = step * 20;
                // Indica el avance
                progressReport(percentage, $"Step {step}/5 complete");
            }
            // Avisa que termina
            progressReport(100, "Done!");
            // Devolvemos la respuesta al sentido de la vida, el universo y t0do lo demás (42)
            return 42;
        }
    }
}