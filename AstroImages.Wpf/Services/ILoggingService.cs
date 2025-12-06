namespace AstroImages.Wpf.Services
{
    public interface ILoggingService
    {
        void LogFolderOpen(string folderPath);
        void LogMetadataReading(string fileName, int totalFiles, int currentIndex);
        void LogFileOpened(string fileName);
        void LogFileMarked(string fileName, bool isMarked);
        void LogFullscreenToggle(bool isFullscreen);
        void LogError(string operation, string message, Exception? ex = null);
        void LogWarning(string operation, string message);
        void LogInfo(string message);
        void ClearLog();
        string GetLogContents();
    }
}
