using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ApexAstro.Wpf.ViewModels
{
    /// <summary>
    /// ViewModel for the Histogram control
    /// Handles histogram data and display settings
    /// </summary>
    public class HistogramViewModel : INotifyPropertyChanged
    {
        private bool _isLinearScale = false;
        private bool _hasHistogramData = false;
        private int[]? _redHistogram;
        private int[]? _greenHistogram;
        private int[]? _blueHistogram;
        private int[]? _grayHistogram;
        private bool _isGrayscale = false;

        /// <summary>
        /// Event raised when histogram data changes
        /// </summary>
        public event Action? HistogramDataChanged;

        /// <summary>
        /// Gets or sets whether the scale is linear (true) or logarithmic (false)
        /// </summary>
        public bool IsLinearScale
        {
            get => _isLinearScale;
            set
            {
                if (_isLinearScale != value)
                {
                    _isLinearScale = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsLogarithmicScale));
                    HistogramDataChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the scale is logarithmic
        /// </summary>
        public bool IsLogarithmicScale
        {
            get => !_isLinearScale;
            set
            {
                if (_isLinearScale == value)
                {
                    _isLinearScale = !value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsLinearScale));
                    HistogramDataChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Gets whether histogram data is available
        /// </summary>
        public bool HasHistogramData
        {
            get => _hasHistogramData;
            private set
            {
                if (_hasHistogramData != value)
                {
                    _hasHistogramData = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the red channel histogram data
        /// </summary>
        public int[]? RedHistogram
        {
            get => _redHistogram;
            private set
            {
                _redHistogram = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the green channel histogram data
        /// </summary>
        public int[]? GreenHistogram
        {
            get => _greenHistogram;
            private set
            {
                _greenHistogram = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the blue channel histogram data
        /// </summary>
        public int[]? BlueHistogram
        {
            get => _blueHistogram;
            private set
            {
                _blueHistogram = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the grayscale histogram data
        /// </summary>
        public int[]? GrayHistogram
        {
            get => _grayHistogram;
            private set
            {
                _grayHistogram = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets whether the image is grayscale
        /// </summary>
        public bool IsGrayscale
        {
            get => _isGrayscale;
            private set
            {
                _isGrayscale = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Updates the histogram with RGB channel data
        /// </summary>
        public void UpdateHistogram(int[] redData, int[] greenData, int[] blueData)
        {
            RedHistogram = redData;
            GreenHistogram = greenData;
            BlueHistogram = blueData;
            GrayHistogram = null;
            IsGrayscale = false;
            HasHistogramData = redData.Length > 0 || greenData.Length > 0 || blueData.Length > 0;
            HistogramDataChanged?.Invoke();
        }

        /// <summary>
        /// Updates the histogram with grayscale data
        /// </summary>
        public void UpdateHistogram(int[] grayData)
        {
            RedHistogram = null;
            GreenHistogram = null;
            BlueHistogram = null;
            GrayHistogram = grayData;
            IsGrayscale = true;
            HasHistogramData = grayData.Length > 0;
            HistogramDataChanged?.Invoke();
        }

        /// <summary>
        /// Clears all histogram data
        /// </summary>
        public void ClearHistogram()
        {
            RedHistogram = null;
            GreenHistogram = null;
            BlueHistogram = null;
            GrayHistogram = null;
            IsGrayscale = false;
            HasHistogramData = false;
            HistogramDataChanged?.Invoke();
        }

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
