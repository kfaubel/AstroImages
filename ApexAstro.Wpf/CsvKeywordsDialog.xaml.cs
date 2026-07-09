using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using ApexAstro.Wpf.Services;

namespace ApexAstro.Wpf
{
    /// <summary>
    /// Dialog for selecting which session metadata columns to display as columns in the file list.
    /// Session metadata comes from ImageMetaData.csv and AcquisitionDetails.csv.
    /// </summary>
    public partial class CsvKeywordsDialog : Window
    {
        public ObservableCollection<CsvKeywordItem> KeywordItems { get; } = new ObservableCollection<CsvKeywordItem>();

        public CsvKeywordsDialog()
        {
            InitializeComponent();
            InitializeKeywords();
            KeywordCheckBoxes.ItemsSource = KeywordItems;
        }

        public CsvKeywordsDialog(IEnumerable<string> selectedKeywords) : this()
        {
            foreach (var item in KeywordItems)
                item.IsSelected = selectedKeywords.Contains(item.Keyword);
        }

        private void InitializeKeywords()
        {
            foreach (var (column, description) in CsvMetadataService.KnownColumns.OrderBy(k => k.Column))
                KeywordItems.Add(new CsvKeywordItem(column, description));
        }

        public List<string> GetSelectedKeywords()
            => KeywordItems.Where(i => i.IsSelected).Select(i => i.Keyword).ToList();

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in KeywordItems)
                item.IsSelected = false;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class CsvKeywordItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Keyword { get; }
        public string Description { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public CsvKeywordItem(string keyword, string description)
        {
            Keyword = keyword;
            Description = description;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
