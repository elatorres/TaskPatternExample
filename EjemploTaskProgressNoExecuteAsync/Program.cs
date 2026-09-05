using System;
using System.Threading;
using System.Threading.Tasks;
using TaskPattern;
// #================================================================#
// #   El ejemplo crea a la tarea, con OnProgress y OnFinishTask    #
// #   mientras el main hace otras cosas. Cuando termina de hacer   #
// #   sus cosas, allí hace await y entonces se dispara el proceso  #
// #   y espera que termine para devolver el valor.                 #
// #----------------------------------------------------------------#
// #   var task = new DownloadTask();                               #
// #   task.OnProgress += (p, m) => ...                             #
// #   task.OnFinishTask += (task, result, error) => ...            #
// #   await task.ExecuteAsync();                                   #
// #================================================================#

internal class Program
{
    static async Task Main(string[] args)
    {
        // Uso
        // Instanciamos la clase, el progress, el onfinish  
        var task = new DownloadTask(); // Pero no se dispara aquí
        task.OnProgress += (p, m) => Console.WriteLine($"[Progress] {p}%: {m}");
        task.OnFinishTask += (task, result, error) =>
        {
            if (error is OperationCanceledException)
                Console.WriteLine("[Completion] Task was cancelled!");
            else if (error != null)
                Console.WriteLine($"[Completion] Task failed: {error.Message}");
            else
                Console.WriteLine($"[Completion] Task completed with result: {result}");
        };

        try
        {
            // espero a que termine
            Console.WriteLine("[Main] waiting fo Task!");
            // Lazy Initialization ...
            int result = await task.WaitForFinishAsync();
            Console.WriteLine("Task completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}


// Hacemos una clase que simula un trabajo derivada de AbstractTask Simple
public class DownloadTask : AbstractTask<int>
{
    // Implementamos el ProcessAsync con progress y cancellation
    protected override async Task<int> ProcessAsync(ProgressReport progress, CancellationToken token)
    {
        // de diez en diez
        for (int i = 0; i <= 100; i += 10)
        {
            // si lo cancelan genera excepción
            token.ThrowIfCancellationRequested();
            await Task.Delay(500, token); // Simular trabajo
            progress(i, $"Downloading... {i}%");
        }
        return 42;
    }
}



