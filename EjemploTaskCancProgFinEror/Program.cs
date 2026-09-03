using System;
using System.Threading;
using System.Threading.Tasks;
using TaskPattern;
// #================================================================#
// #   Hacemos tarea con cancellation Progress Finish y detección   #
// #   de errores                                                   #
// #----------------------------------------------------------------#
// #   var cts = new CancellationTokenSource();                     #
// #   var task = new DownloadTask();                               #
// #   task.OnProgress += (p, m) => ...                             #
// #   task.OnFinishTask += (task, result, error) => ...            #
// #   var myFuture=task.ExecuteAsync(cts.Token);                   #
// #   if (myFuture.IsCompleted) Continue...                        #
// #   if (task.IsFaulted) Console.WriteLine("[Main] Oh, No!");     #
// #================================================================#

internal class Program
{
    static async Task Main(string[] args)
    {
        // Uso con cancelación
        var cts = new CancellationTokenSource();
        // Instanciamos la clase, el progress, el onfinish
        var task = new DownloadTask();
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
        
        // Cancelar después de 2 segundos
        // cts.CancelAfter(2000);
        
        // Async/await way
        try
        {
            // invocamos con cancellation, espero a que termine,
            // pero no va a terminar, va a fallar
            var myFuture=task.ExecuteAsync(cts.Token);
            // Main hace algo más
            for (int i = 0; i <= 200; i += 10)
            {
                Console.WriteLine($"[Main ] Doing something else number {i}.");
                await Task.Delay(500); // Simular trabajo
                // Si el proceso terminó dejo de hacer mis cosas
                if (myFuture.IsCompleted)
                {
                    Console.WriteLine($"[Main] Process Completed, Main exiting loop!");
                    break;
                }
            }            
            Console.WriteLine("[Main] Something else completed!");
            // Verificamos si el proceso terminó correctamente
            if (myFuture.IsFaulted) Console.WriteLine("[Main] Oh, Fútbol! Something went wrong with my Process!");
            else Console.WriteLine("[Main] Main and Process completed!");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Task was cancelled!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

// Hacemos una clase que simula un trabajo derivada de
// AbstractTask con Cancellation Token y Main haciendo otras cosas
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
        
        // The Answer to the Ultimate Question of Life, the Universe, and Everything
        return 42;
    }
}
