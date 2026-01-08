using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace AstroImages.Wpf.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly List<string> _logEntries = new();
        private readonly object _lockObject = new();
        private readonly string _logFilePath;

        /// <summary>
        /// Event raised when a long-running operation exceeds the threshold (3 seconds)
        /// Allows UI to show progress dialogs
        /// </summary>
        public event EventHandler<(string operation, string target)>? LongOperationDetected;

        public LoggingService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AstroImages"
            );
            Directory.CreateDirectory(appDataPath);
            _logFilePath = Path.Combine(appDataPath, $"AstroImages_{DateTime.Now:yyyyMMdd}.log");
            
            // Load existing log entries from file if it exists
            try
            {
                if (File.Exists(_logFilePath))
                {
                    var lines = File.ReadAllLines(_logFilePath);
                    // Load only the last 1000 entries to avoid memory issues
                    var entriesToLoad = lines.Length > 1000 ? lines.Skip(lines.Length - 1000) : lines;
                    foreach (var line in entriesToLoad)
                    {
                        _logEntries.Add(line);
                    }
                }
            }
            catch
            {
                // Silently fail if we can't read the log file
            }
        }

        public void LogFolderOpen(string folderPath)
        {
            LogEntry("INFO", $"Folder opened: {folderPath}");
        }

        public void LogMetadataReading(string fileName, int totalFiles, int currentIndex)
        {
            LogEntry("INFO", $"Reading metadata: [{currentIndex}/{totalFiles}] {fileName}");
        }

        public void LogFileOpened(string fileName)
        {
            LogEntry("INFO", $"File opened: {fileName}");
        }

        public void LogFileMarked(string fileName, bool isMarked)
        {
            var action = isMarked ? "marked" : "unmarked";
            LogEntry("INFO", $"File {action}: {fileName}");
        }

        public void LogFullscreenToggle(bool isFullscreen)
        {
            var state = isFullscreen ? "entered" : "exited";
            LogEntry("INFO", $"Fullscreen {state}");
        }

        public void LogError(string operation, string message, Exception? ex = null)
        {
            var logMessage = $"ERROR in {operation}: {message}";
            if (ex != null)
            {
                logMessage += $" | Exception: {ex.GetType().Name} - {ex.Message}";
            }
            LogEntry("ERROR", logMessage);
        }

        public void LogWarning(string operation, string message)
        {
            LogEntry("WARNING", $"{operation}: {message}");
            
            // Trigger event for long operations (>3 second threshold)
            if (message.Contains(">3s threshold"))
            {
                // Extract the target name from the message (e.g., 'filename.fits' from "'filename.fits' took Xms (>3s threshold)")
                var targetMatch = Regex.Match(message, @"'([^']+)'");
                var target = targetMatch.Success ? targetMatch.Groups[1].Value : "Unknown";
                
                LongOperationDetected?.Invoke(this, (operation, target));
            }
        }

        public void LogInfo(string message)
        {
            LogEntry("INFO", message);
        }

        public void ClearLog()
        {
            lock (_lockObject)
            {
                _logEntries.Clear();
                
                // Truncate the log file
                try
                {
                    File.WriteAllText(_logFilePath, string.Empty);
                }
                catch
                {
                    // Silently fail if we can't truncate the log file
                }
            }
            LogEntry("INFO", "Log cleared");
        }

        public string GetLogContents()
        {
            lock (_lockObject)
            {
                return string.Join(Environment.NewLine, _logEntries);
            }
        }

        private void LogEntry(string level, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{level}] {message}";

            lock (_lockObject)
            {
                _logEntries.Add(logEntry);
                
                // Keep only last 1000 entries in memory
                if (_logEntries.Count > 1000)
                {
                    _logEntries.RemoveAt(0);
                }

                // Append to file (persistent across sessions)
                try
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
                catch
                {
                    // Silently fail if we can't write to the log file
                }
            }
        }
    }
}
