using System.Collections.Generic;
using System.ComponentModel;

namespace AstroImages.Wpf.Models
{
    public class FileItem : INotifyPropertyChanged
    {
        private Dictionary<string, string> _customKeywords = new Dictionary<string, string>();
        private Dictionary<string, string> _fitsKeywords = new Dictionary<string, string>();
        private bool _isSelected = false;

        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Path { get; set; } = string.Empty;
        
        public bool IsSelected 
        { 
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
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

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}