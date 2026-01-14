using System.Windows;
using System.Windows.Input;

namespace KustoSearchApp;

/// <summary>
/// Represents a table item for display in the ListBox with fuzzy match support.
/// </summary>
public class TableItem
{
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public List<int> MatchedIndices { get; set; } = new();

    public override string ToString() => Name;
}

public partial class TableSelectionWindow : Window
{
    private List<string> _allTables;
    private List<string> _availableTables;
    private List<string> _selectedTables;

    public List<string> SelectedTables => _selectedTables.ToList();

    public TableSelectionWindow(List<string> allTables, List<string>? preSelectedTables = null)
    {
        InitializeComponent();

        _allTables = allTables.OrderBy(t => t).ToList();
        _selectedTables = (preSelectedTables ?? new List<string>()).OrderBy(t => t).ToList();
        _availableTables = _allTables.Except(_selectedTables).OrderBy(t => t).ToList();

        RefreshLists();
    }

    private string GetFilterText(System.Windows.Controls.TextBox textBox)
    {
        return textBox.Text?.Trim() ?? "";
    }

    private void RefreshLists()
    {
        string availableFilter = GetFilterText(txtFilterAvailable);
        string selectedFilter = GetFilterText(txtFilterSelected);

        // Filter available tables with fuzzy matching
        List<TableItem> filteredAvailable;
        if (string.IsNullOrWhiteSpace(availableFilter))
        {
            filteredAvailable = _availableTables
                .Select(t => new TableItem { Name = t, Score = 0 })
                .OrderBy(t => t.Name)
                .ToList();
        }
        else
        {
            filteredAvailable = _availableTables
                .Select(t =>
                {
                    var (isMatch, indices, score) = FuzzyMatcher.FuzzyMatch(t, availableFilter);
                    return new { Name = t, IsMatch = isMatch, Indices = indices, Score = score };
                })
                .Where(x => x.IsMatch)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Name)
                .Select(x => new TableItem { Name = x.Name, Score = x.Score, MatchedIndices = x.Indices })
                .ToList();
        }

        lstAvailable.ItemsSource = filteredAvailable;
        lblAvailableCount.Text = $"{filteredAvailable.Count} of {_availableTables.Count} tables";

        // Filter selected tables with fuzzy matching
        List<TableItem> filteredSelected;
        if (string.IsNullOrWhiteSpace(selectedFilter))
        {
            filteredSelected = _selectedTables
                .Select(t => new TableItem { Name = t, Score = 0 })
                .OrderBy(t => t.Name)
                .ToList();
        }
        else
        {
            filteredSelected = _selectedTables
                .Select(t =>
                {
                    var (isMatch, indices, score) = FuzzyMatcher.FuzzyMatch(t, selectedFilter);
                    return new { Name = t, IsMatch = isMatch, Indices = indices, Score = score };
                })
                .Where(x => x.IsMatch)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Name)
                .Select(x => new TableItem { Name = x.Name, Score = x.Score, MatchedIndices = x.Indices })
                .ToList();
        }

        lstSelected.ItemsSource = filteredSelected;
        lblSelectedCount.Text = $"{filteredSelected.Count} of {_selectedTables.Count} selected";
    }

    private void TxtFilterAvailable_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        RefreshLists();
    }

    private void TxtFilterSelected_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        RefreshLists();
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        AddSelected();
    }

    private void LstAvailable_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        AddSelected();
    }

    private void AddSelected()
    {
        var itemsToMove = lstAvailable.SelectedItems.Cast<TableItem>().Select(t => t.Name).ToList();
        if (itemsToMove.Count == 0) return;

        foreach (var item in itemsToMove)
        {
            _availableTables.Remove(item);
            _selectedTables.Add(item);
        }

        _availableTables.Sort();
        _selectedTables.Sort();
        RefreshLists();
    }

    private void BtnAddAll_Click(object sender, RoutedEventArgs e)
    {
        var itemsToMove = lstAvailable.Items.Cast<TableItem>().Select(t => t.Name).ToList();
        if (itemsToMove.Count == 0) return;

        foreach (var item in itemsToMove)
        {
            _availableTables.Remove(item);
            _selectedTables.Add(item);
        }

        _availableTables.Sort();
        _selectedTables.Sort();
        RefreshLists();
    }

    private void BtnRemove_Click(object sender, RoutedEventArgs e)
    {
        RemoveSelected();
    }

    private void LstSelected_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        RemoveSelected();
    }

    private void RemoveSelected()
    {
        var itemsToMove = lstSelected.SelectedItems.Cast<TableItem>().Select(t => t.Name).ToList();
        if (itemsToMove.Count == 0) return;

        foreach (var item in itemsToMove)
        {
            _selectedTables.Remove(item);
            _availableTables.Add(item);
        }

        _availableTables.Sort();
        _selectedTables.Sort();
        RefreshLists();
    }

    private void BtnRemoveAll_Click(object sender, RoutedEventArgs e)
    {
        var itemsToMove = lstSelected.Items.Cast<TableItem>().Select(t => t.Name).ToList();
        if (itemsToMove.Count == 0) return;

        foreach (var item in itemsToMove)
        {
            _selectedTables.Remove(item);
            _availableTables.Add(item);
        }

        _availableTables.Sort();
        _selectedTables.Sort();
        RefreshLists();
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
