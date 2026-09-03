using System;
using System.Threading;
using System.Threading.Tasks;
// #=================================================================#
// # Clase abstracta para implementar control de tareas asincrónicas #
// #-----------------------------------------------------------------#
// # Creado en colaboración con DeepSeek                             #
// #=================================================================#
namespace TaskPattern
{
    // Método Delegado para notificación de terminación incluyendo caso de excepción
    public delegate void FinishTask<TResult>(AbstractTask<TResult> abstractTask, TResult result, Exception error);
    // Método Delegado para informe de progreso
    public delegate void ProgressReport(int percentage, string message);
    
    // La clase abstracta de la que derivar
    public abstract class AbstractTask<TResult>
    {
        // Gestión de estado y atributos privados
        private Task<TResult> m_Task = null;  // cambiado de Task<TResult> a Task
        private CancellationTokenSource m_Cts = null; // fuente de cancellation tokens
        private readonly object m_Lock = new object(); // Objeto para lock de exclusión mutua
        private bool m_IsExecuting = false; // La tarea no s4 está ejecutando
        private bool m_IsCompleted = false; // La tarea no está completada
        private Exception m_ExecutionError = null; // Hubo algún error
        private TResult m_Result = default; // El resultado a devolver

        // Eventos Públicos
        public event FinishTask<TResult> OnFinishTask;
        public event ProgressReport OnProgress;

        // Propiedades Públicas (Alias)
        public bool IsExecuting => m_IsExecuting;
        public bool IsCompleted => m_IsCompleted;
        public bool IsFaulted => m_ExecutionError != null;
        public Exception ExecutionError => m_ExecutionError;
        public TResult Result => m_Result;
        
        // #================================================================# 
        // #   Método de Ejecución Principal asincrónico y concurrente      #
        // #   ExecuteAsync: Recibe como parámetro el cancellation token    # 
        // #   Verifica la no reentrantabilidad de la tarea (tiene una      # 
        // #   sola instancia de ejecución. Inicializa las variables de     #
        // #   estado y crea un linked cancellation token. FinishTaskAsync  #
        // #   invoca al método concreto (implementación del abstracto)     #
        // #   espera a que termine, luego pone las variables para          #
        // #   indicarlo, ejecuta la delegada para indicar la terminación   #
        // #   y devuelve el resultado.                                     #
        // #================================================================#
        public async Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            // Región crítica para ejecución Thread-safe
            // Sólo un proceso a la vez
            lock (m_Lock)
            {
                // Si ya se está ejecutando, generar excepción
                if (m_IsExecuting)
                    throw new InvalidOperationException("Task is already executing.");
                
                // Si ya terminó y no hay error
                if (m_IsCompleted && m_ExecutionError == null)
                    throw new InvalidOperationException("Task already completed successfully. Create a new instance for re-execution.");
                
                // Si no se está ejecutando, preparamos para ejecutar.
                m_IsExecuting = true;
                m_IsCompleted = false;
                m_ExecutionError = null;
                m_Result = default;
                // Creamos el Linked Cancellation Token
                m_Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            try
            {
                // Ejecutar el trabajo asincrónico, devuelve resultado
                m_Result = await FinishTaskAsync(m_Cts.Token).ConfigureAwait(false);
                
                // Marcarlo como completado
                lock (m_Lock)
                {
                    m_IsExecuting = false;
                    m_IsCompleted = true;
                }
                // Dispara el evento de terminado sin error
                OnFinishTask?.Invoke(this, m_Result, null);
                
                // Devuelve el resultado del proceso (algo debe devolver)
                return m_Result;
            }
            // Si hay excepción
            catch (Exception ex)
            {
                // Gestionar los errores
                lock (m_Lock)
                {
                    // Excepción, no está ejecutando por excepción
                    m_IsExecuting = false;
                    m_ExecutionError = ex;
                }
                // Dispara el evento de terminado CON error
                OnFinishTask?.Invoke(this, default, ex);
                throw;
            }
            finally
            {
                // Disponer del generador de cancellation tokens
                m_Cts?.Dispose();
                m_Cts = null;
            }
        }
                
        // #================================================================# 
        // #   Wrapper Sincrónico para compatibilidad con programación      #
        // #   sincrónica, tipo Fire-and-forget con resultado via evento.   #
        // #   Execute: Simplemente un wrapper para ejecutar ExecuteAsync   #
        // #================================================================#        
        public void Execute(CancellationToken cancellationToken = default)
        {
            // Store the actual task
            // Corregido - ContinueWith devuelve Task, y m_Task es Task
            m_Task = ExecuteAsync(cancellationToken);
            
            // Fire-and-forget con gestión de errores
            _ = m_Task.ContinueWith(t =>
            {
                // Registrar o manejar las excepciones no observadas (asincrónicas)
                if (t.IsFaulted && t.Exception != null)
                {
                    // ToDo: Se debería hacer un log de esto o comunicar de alguna forma. REVISAR!!
                    Console.WriteLine($"Unhandled exception: {t.Exception.GetBaseException().Message}");
                }
            });
        }
        
        // #================================================================#        
        // #   Espera sincrónica Legacy.                                    #
        // #   WaitForFinish: Simplemente espera por la ejecución de        #
        // #   ExecuteAsync y su resultado                                  #
        // #================================================================#
        public TResult WaitForFinish()
        {
            if (m_Task != null)
            {
                // Para Fire-and-Forget, necesitamos obtener el resultado de la tarea original.
                // Almacenamos el resultado en m_Result, así que podemos simplemente esperar y devolverlo.
                // Esta es una llamada bloqueante para mantener la compatibilidad con programación sincrónica
                return m_Task.Result;
            }
            
            // Alternativa: si no se llamó a Execute, usar la versión asíncrona
            return ExecuteAsync().Result;
        }
        
        // #================================================================#        
        // #   Espera asincrónica por el resultado                          #
        // #   WaitForFinishAsync: Simplemente espera por la ejecución de   #
        // #   ExecuteAsync y su resultado                                  #
        // #================================================================#
        public async Task<TResult> WaitForFinishAsync()
        {
            if (m_Task != null)
            {
                // Esperar por la tarea existente a que complete, y devolver el resultado
                return await m_Task.ConfigureAwait(false);
            }
            
            // Si no hay tarea iniciada, comenzar una ahora y esperar
            return await ExecuteAsync().ConfigureAwait(false);
        }
        
        // #================================================================#        
        // #   Método de ejecución de la Tarea de Trabajo asincrónica con   #
        // #   cancellationEspera asincrónica por el resultado              #
        // #   FinishTaskAsync: Recibe el cancellation token, reporta el    #
        // #   inicio de la tarea, y dispara el ProcessAsync que es el      #
        // #   método concreto implementado, espera por la ejecución de     #
        // #   este, y reporta el final del proceso y devuelve el resultado #
        // #================================================================#        
        private async Task<TResult> FinishTaskAsync(CancellationToken cancellationToken)
        {
            // Informar del progreso inicial (Nada... que comenzó...)
            ReportProgress(0, "Starting...");
            
            try
            {
                // LLamar al proceso asincrónico con soporte para información de avance/progreso.
                // Y recibir la promesa del resultado
                var result = await ProcessAsync(
                    (percentage, message) => ReportProgress(percentage, message),
                    cancellationToken
                ).ConfigureAwait(false);
                
                // Informar que terminó. Es un poco redundante si el proceso también
                // informa cada vez. Pero si no informa este sí informa siempre.
                ReportProgress(100, "Complete!");
                return result;
            }
            catch (OperationCanceledException)
            {
                // Informar que canceló
                ReportProgress(0, "Cancelled!");
                throw;
            }
            catch (Exception ex)
            {
                // Informar que falló
                // Informar del error
                // ToDo: DESARROLLAR MAS ESTO
                ReportProgress(0, $"Error: {ex.Message}");
                throw;
            }
        } // Ends FinishTaskAsync
        
        // #================================================================#
        // #   Helper de reporte de avance                                  #
        // #   ReportProgress: es invocado por la implementación de         #
        // #   ProcessAsync, para informar el porcentaje de avance. Recibe  #
        // #   el porcentaje como un entero, y un mensaje como string.      #
        // #   Invoca a la función delegada creada para este fin            #
        // #================================================================#
        private void ReportProgress(int percentage, string message)
        {
            OnProgress?.Invoke(
                Math.Clamp(percentage, 0, 100),
                message ?? string.Empty
            );
        }
        
        // #================================================================#
        // #   Método de Procesamiento Asincrónica Abstracto.               #
        // #   ProcessAsync: es el método abstracto que debe ser            #
        // #   implementado para hacer el trabajo a realizar. Recibe la     #
        // #   delegada del progressReport y el cancellation token.         #
        // #   ProcessAsync, para informar el porcentaje de avance. Recibe  #
        // #   el porcentaje como un entero, y un mensaje como string.      #
        // #   Invoca a la función delegada creada para este fin. Debe      #
        // #   invocar a ReportProgress para informar su avance, si está    #
        // #   definida.                                                    #
        // #================================================================#
        protected abstract Task<TResult> ProcessAsync(
            ProgressReport progressReport,
            CancellationToken cancellationToken
        );
        
        // Opcional: 
        // 
        // #================================================================#
        // #   Proceso sincrónico legacy para programación sincrónica.      #
        // #   Marcar como obsoleto para fomentar la migración a asíncrono. #
        // #   Process: Es para promover el uso de programación asyncrónica #
        // #================================================================#
        [Obsolete("Use ProcessAsync instead")]
        protected virtual void Process()
        {
            throw new NotImplementedException("Use ProcessAsync instead");
        }
    } // Ends public abstract class AbstractTask 
}
