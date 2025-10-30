using System.Collections.ObjectModel;  // For ObservableCollection<T>
using System.Windows;                   // For Window base class and WPF framework
using System.Windows.Controls;          // For UI controls like Button, TextBox

namespace AstroImages.Wpf
{
    /// <summary>
    /// Dialog window for configuring custom keywords that are extracted from filenames.
    /// 
    /// Custom keywords are used to parse structured filenames like those created by NINA:
    /// "2025-10-16_23-42-23_R_RMS_0.75_HFR_2.26_Stars_2029_100_10.00s_-9.60C_0052.fits"
    /// 
    /// Users can define keywords like "RMS", "HFR", "Stars" to extract specific values
    /// that will be displayed as columns in the main file list.
    /// 
    /// This dialog allows users to:
    /// - Add new keywords to parse
    /// - Remove existing keywords
    /// - See a live list of currently configured keywords
    /// </summary>
    public partial class CustomKeywordsDialog : Window
    {
        /// <summary>
        /// Observable collection of keyword strings that the user has configured.
        /// 
        /// ObservableCollection automatically notifies the UI when items are added/removed,
        /// so the ListBox in the dialog updates immediately when keywords change.
        /// 
        /// The collection is read-only (only get accessor) but items can still be
        /// added/removed from the collection itself.
        /// </summary>
        public ObservableCollection<string> Keywords { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Default constructor - creates an empty dialog ready for user input.
        /// Sets up the UI components and wires up event handlers.
        /// </summary>
        public CustomKeywordsDialog()
        {
            // Initialize UI components from XAML markup
            InitializeComponent();
            
            // Connect the ListBox to our Keywords collection for data binding
            // This makes the ListBox automatically show all items in the Keywords collection
            KeywordListBox.ItemsSource = Keywords;
            
            // Set up keyboard shortcut: pressing Enter in the text box adds the keyword
            // This provides better user experience than requiring mouse clicks
            NewKeywordTextBox.KeyDown += NewKeywordTextBox_KeyDown;
        }

        /// <summary>
        /// Constructor that initializes the dialog with existing keywords.
        /// This is used when editing the current keyword configuration rather than starting fresh.
        /// 
        /// Constructor chaining (: this()) ensures the default constructor runs first,
        /// setting up all the UI components before we populate the keywords.
        /// </summary>
        /// <param name="existingKeywords">Collection of keywords currently configured in the application</param>
        public CustomKeywordsDialog(IEnumerable<string> existingKeywords) : this()
        {
            // Populate the Keywords collection with existing values
            // The UI will automatically update because Keywords is an ObservableCollection
            foreach (var keyword in existingKeywords)
            {
                Keywords.Add(keyword);
            }
        }

        /// <summary>
        /// Event handler for keyboard input in the new keyword text box.
        /// Provides keyboard shortcut for adding keywords - user can press Enter instead of clicking Add button.
        /// 
        /// This improves user experience by allowing rapid keyword entry without mouse interaction.
        /// </summary>
        /// <param name="sender">The TextBox that received the keyboard input</param>
        /// <param name="e">Event arguments containing information about which key was pressed</param>
        private void NewKeywordTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Check if the user pressed the Enter key
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                // Simulate clicking the Add button by calling its event handler
                // This provides consistent behavior whether user clicks button or presses Enter
                AddKeyword_Click(sender, e);
            }
        }

        /// <summary>
        /// Event handler for the Add Keyword button click.
        /// Takes the text from the input box and adds it to the Keywords collection if valid.
        /// 
        /// Validation rules:
        /// - Keyword must not be empty or just whitespace
        /// - Keyword must not already exist in the collection (no duplicates)
        /// 
        /// After successful addition, clears the text box and keeps focus for easy entry of more keywords.
        /// </summary>
        /// <param name="sender">The button or control that triggered this action</param>
        /// <param name="e">Event arguments (not used in this handler)</param>
        private void AddKeyword_Click(object sender, RoutedEventArgs e)
        {
            // Get the text from the input box and remove leading/trailing whitespace
            var keyword = NewKeywordTextBox.Text.Trim();
            
            // Validate the keyword: must not be empty and must not already exist
            if (!string.IsNullOrEmpty(keyword) && !Keywords.Contains(keyword))
            {
                // Add to the observable collection (UI updates automatically)
                Keywords.Add(keyword);
                
                // Clear the text box for the next keyword entry
                NewKeywordTextBox.Clear();
                
                // Keep focus on the text box so user can immediately type the next keyword
                NewKeywordTextBox.Focus();
            }
        }

        /// <summary>
        /// Event handler for Remove Keyword button clicks.
        /// Each keyword in the list has its own Remove button, and this handler determines
        /// which keyword to remove based on the button's Tag property.
        /// 
        /// Pattern matching with 'is' keyword:
        /// - Checks if sender is a Button AND casts it to 'button' variable
        /// - Checks if button.Tag is a string AND casts it to 'keywordToRemove' variable
        /// - Only executes the code block if both conditions are true
        /// </summary>
        /// <param name="sender">The Remove button that was clicked</param>
        /// <param name="e">Event arguments for the click event</param>
        private void RemoveKeyword_Click(object sender, RoutedEventArgs e)
        {
            // Use pattern matching to safely check types and extract values
            // This is equivalent to: if (sender is Button && ((Button)sender).Tag is string)
            if (sender is System.Windows.Controls.Button button && button.Tag is string keywordToRemove)
            {
                // Remove the keyword from the observable collection
                // The UI will automatically update to reflect the removal
                Keywords.Remove(keywordToRemove);
            }
        }

        /// <summary>
        /// Event handler for the OK button click.
        /// Indicates that the user wants to save their keyword configuration changes.
        /// The calling code can access the Keywords collection to get the final list.
        /// </summary>
        /// <param name="sender">The OK button</param>
        /// <param name="e">Event arguments for the click event</param>
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // Set DialogResult to true to indicate user accepted the changes
            DialogResult = true;
            
            // Close the dialog - calling code can now read the Keywords collection
            Close();
        }

        /// <summary>
        /// Event handler for the Cancel button click.
        /// Indicates that the user wants to discard their changes and keep the original keywords.
        /// </summary>
        /// <param name="sender">The Cancel button</param>
        /// <param name="e">Event arguments for the click event</param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Set DialogResult to false to indicate user cancelled
            DialogResult = false;
            
            // Close the dialog - calling code should ignore any changes to Keywords collection
            Close();
        }
    } // End of CustomKeywordsDialog class
} // End of AstroImages.Wpf namespace