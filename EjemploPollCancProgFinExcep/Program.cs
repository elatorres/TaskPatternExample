using System;
using System.Threading;
using System.Threading.Tasks;
// #================================================================#
// #   Con Cancellation, ejecutamos la tarea, con Progress y Finish #
// #   El main hace polling si la tarea terminó o falló y espera el #
// #   resultado.                                                   # 
// #----------------------------------------------------------------#
// #   using var cts = new CancellationTokenSource();               #
// #   var myTask = new MagicNumberTask();                          #
// #   myTask.OnProgress += (percentage, message) =>                #
// #   myTask.OnFinishTask += (task, result, error) =>              #
// #   myTask.Execute(cts.Token);                                   #
// #   while (!myTask.IsCompleted && !myTask.IsFaulted)             #
// #   if (myTask.IsExecuting) WriteLine("It is still running..."); #
// #   int result = myTask.WaitForFinish();                         #
// #================================================================# 

namespace TaskPattern
{
    class Program
    {
        // Aplicación de Consola que demuestra el funcionamiento del patrón completo
        static async Task Main(string[] args)
        {
            // Comenzamos
            Console.WriteLine("🚀 Starting Main Program");
            // Hacemos la doble raya
            Console.WriteLine("=".PadRight(50, '='));

            // Creamos un cancellation token source. No lo usamos pero debe estar ahí.
            using var cts = new CancellationTokenSource();
            
            // Creamos nuestra tarea que devuelve un entero.
            // Este entero es la respuesta al sentido de la vida, el universo y t0do lo demás (42)
            var myTask = new MagicNumberTask();
            
            // Subscribirse al evento de informe de progreso
            myTask.OnProgress += (percentage, message) =>
            {
                Console.WriteLine($"[Progress] {percentage}%: {message}");
            };

            // Subscribirse al evento de informe de compleción
            myTask.OnFinishTask += (task, result, error) =>
            {
                if (error != null)
                    Console.WriteLine($"[Completion] Task failed: {error.Message}");
                else
                    Console.WriteLine($"[Completion] Task completed with result: {result}");
            };

            // Comenzar la tarea asincrónica (fire-and-forget) con manejo de eventos
            Console.WriteLine("\n📦 Starting background task...");
            // Le pasamos el cancelation token si un día lo queremos cancelar
            myTask.Execute(cts.Token);

            // El programa principal hace otras cosas mientras la tarea/proceso hace lo suyo.
            int loopCounter = 0;
            Console.WriteLine("\n🔄 Main thread doing work...");
            // El programa principal hace cosas esperando por el proceso.
            // Si el proceso termina (bien o mal) entonces el main va a buscar el resultado.
            while (!myTask.IsCompleted && !myTask.IsFaulted)
            {
                loopCounter++;
                
                // Simulamos algo de trabajo
                Console.WriteLine($"  Main loop iteration #{loopCounter} - Doing work...");
                await Task.Delay(300); // Simulate work
                
                // Verificar si la tarea sigue corriendo
                if (myTask.IsExecuting)
                {
                    Console.WriteLine($"  Main: Background task is still running... (loop #{loopCounter})");
                }
            }
            // Si terminó o falló entonces salgo del loop
            Console.WriteLine($"\n✅ Main loop finished after {loopCounter} iterations");
            Console.WriteLine($"📊 Background task status: IsCompleted={myTask.IsCompleted}, IsFaulted={myTask.IsFaulted}");

            // Obtenemos el resultado (sea lo que sea)
            try
            {
                // Por las dudas esepramos. Pero en este caso ya sé que terminó
                int result = myTask.WaitForFinish();
                Console.WriteLine("The Answer to the Ultimate Question of Life, the Universe, and Everything is ...");
                Console.WriteLine($"🎉 Final Result: {result}");
            }
            // Si me explotó el resultado
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting result: {ex.Message}");
            }
            
            // Aviso que terminé
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