namespace AstroImages.Wpf.Services
{
    public interface IListViewColumnService
    {
        void UpdateListViewColumns();
        void AutoResizeColumns();
        void UpdateFileColumnWidth();
        void ResetAllColumnWidths();
        void RecalculateColumnWidthsFromData();
        double CalculateTotalColumnsWidth();
        void AdjustSplitterForOptimalWidth();
    }
}
