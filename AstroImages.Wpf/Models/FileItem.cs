using System.Collections.Generic;
using System.ComponentModel;

namespace AstroImages.Wpf.Models
{
    public class FileItem : INotifyPropertyChanged
    {
        private Dictionary<string, string> _customKeywords = new Dictionary<string, string>();
        private Dictionary<string, string> _fitsKeywords = new Dictionary<string, string>();
        private Dictionary<string, string> _csvKeywords = new Dictionary<string, string>();
        private bool _isSelected = false;
        private double? _median;
        private double? _mean;

        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public double? Median 
        { 
            get => _median;
            set
            {
                if (_median != value)
                {
                    _median = value;
                    OnPropertyChanged(nameof(Median));
                }
            }
        }
        public double? Mean
        {
            get => _mean;
            set
            {
                if (_mean != value)
                {
                    _mean = value;
                    OnPropertyChanged(nameof(Mean));
                }
            }
        }
        public string Path { get; set; } = string.Empty;
        
        public bool IsSelected 
        { 
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                    
                    // Log the mark/unmark action
                    try
                    {
                        var loggingService = App.LoggingService;
                        loggingService?.LogFileMarked(Name, value);
                    }
                    catch
                    {
                        // Silently fail if logging service is not available
                    }
                }
            }
        }
        
        public Dictionary<string, string> CustomKeywords 
        { 
            get => _customKeywords;
            set
            {
                _customKeywords = value;
                OnPropertyChanged(nameof(CustomKeywords));
                
                // Notify for each custom keyword property that might be bound
                foreach (var key in value.Keys)
                {
                    OnPropertyChanged(key);
                }
            }
        }

        public Dictionary<string, string> FitsKeywords 
        { 
            get => _fitsKeywords;
            set
            {
                _fitsKeywords = value;
                OnPropertyChanged(nameof(FitsKeywords));
                
                // Notify for each FITS keyword property that might be bound
                foreach (var key in value.Keys)
                {
                    OnPropertyChanged(key);
                }
            }
        }

        public Dictionary<string, string> CsvKeywords
        {
            get => _csvKeywords;
            set
            {
                _csvKeywords = value;
                OnPropertyChanged(nameof(CsvKeywords));

                foreach (var key in value.Keys)
                    OnPropertyChanged(key);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}