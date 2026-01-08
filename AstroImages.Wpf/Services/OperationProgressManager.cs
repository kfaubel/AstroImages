using System;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;

namespace AstroImages.Wpf.Services
{
    /// <summary>
    /// Manages showing progress dialogs for long-running file system operations
    /// Shows a dialog if operation takes longer than the specified threshold
    /// </summary>
    public class OperationProgressManager
    {
        private const int DefaultThresholdMs = 3000; // 3 seconds
        private readonly Func<Window>? _getParentWindow;
        private readonly int _thresholdMs;

        public OperationProgressManager(Func<Window>? getParentWindow = null, int thresholdMs = DefaultThresholdMs)
        {
            _getParentWindow = getParentWindow;
            _thresholdMs = thresholdMs;
        }

        /// <summary>
        /// Executes an async operation and shows a progress dialog if it takes longer than the threshold
        /// </summary>
        public async Task<T> ExecuteWithProgressAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            string operationTarget)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationTask = operation();
            
            // Wait for either the operation to complete or the threshold to be exceeded
            var completedTask = await Task.WhenAny(
                operationTask,
                Task.Delay(_thresholdMs)
            );

            stopwatch.Stop();

            // If operation is already done, return the result
            if (completedTask == operationTask)
            {
                return await operationTask;
            }

            // Operation is still running - show progress dialog
            LoadingWindow? loadingWindow = null;
            try
            {
                // Create and show loading window
                var message = $"{operationName}: {operationTarget}";
                loadingWindow = new LoadingWindow(message);
                
                var parentWindow = _getParentWindow?.Invoke();
                if (parentWindow != null)
                {
                    loadingWindow.Owner = parentWindow;
                }
                
                loadingWindow.Show();

                // Wait for operation to complete
                var result = await operationTask;
                stopwatch.Stop();

                // Log the operation timing
                App.LoggingService?.LogWarning(operationName, 
                    $"'{operationTarget}' took {stopwatch.ElapsedMilliseconds}ms (>3s threshold)");

                return result;
            }
            finally
            {
                loadingWindow?.Close();
            }
        }

        /// <summary>
        /// Executes a synchronous operation and shows a progress dialog if it takes longer than the threshold
        /// </summary>
        public T ExecuteWithProgress<T>(
            Func<T> operation,
            string operationName,
            string operationTarget)
        {
            return ExecuteWithProgressAsync(
                async () => await Task.Run(operation),
                operationName,
                operationTarget
            ).GetAwaiter().GetResult();
        }
    }
}
