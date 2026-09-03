using System;
using System.Threading;
using System.Threading.Tasks;
using TaskPattern;
// #================================================================#
// #   Hacemos una clase que simula un trabajo derivada de          #
// #   AbstractTask con Cancellation Token                          #
// #----------------------------------------------------------------#
// #   var cts = new CancellationTokenSource();                     #
// #   var task = new DownloadTask();                               #
// #   task.OnProgress += (p, m) => ...                             #
// #   task.OnFinishTask += (task, result, error) =>...             #
// #   cts.CancelAfter(2000);                                       #
// #   await task.ExecuteAsync(cts.Token);                          #
// #================================================================#

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
        cts.CancelAfter(2000);
        
        // Async/await way
        try
        {
            // invocamos con cancellation, espero a que termine,
            // pero no va a terminar, va a fallar
            await task.ExecuteAsync(cts.Token);
            // nunca llega
            Console.WriteLine("Task completed!");
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
