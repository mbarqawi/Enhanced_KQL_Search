using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Azure.Identity;
using CsvHelper;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Win32;

namespace KustoSearchApp;

public partial class MainWindow : Window
{
    private ICslQueryProvider? _queryProvider;
    private ICslAdminProvider? _adminProvider;
    private List<string> _allTables = new();
    private CancellationTokenSource? _cancellationTokenSource;
    
    private ObservableCollection<SearchTermItem> _searchTerms = new();
    private ObservableCollection<SearchResultItem> _searchResults = new();

    public MainWindow()
    {
        InitializeComponent();
        
        SearchTermsPanel.ItemsSource = _searchTerms;
        dgResults.ItemsSource = _searchResults;
        
        // Add first search term
        _searchTerms.Add(new SearchTermItem());
        
        // Set default dates with time
        txtStartDate.Text = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd HH:mm:ss");
        txtEndDate.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        LoadSettings();
        LogMessage("Application started. Ready to connect to Azure Data Explorer.");
    }

    private void LoadSettings()
    {
        try
        {
            txtClusterUrl.Text = Properties.Settings.Default.ClusterUrl;
            txtDatabase.Text = Properties.Settings.Default.Database;
            
            if (_searchTerms.Count > 0 && !string.IsNullOrEmpty(Properties.Settings.Default.SearchPhrase))
            {
                _searchTerms[0].Text = Properties.Settings.Default.SearchPhrase;
            }
            
            if (Properties.Settings.Default.StartDate != DateTime.MinValue)
                txtStartDate.Text = Properties.Settings.Default.StartDate.ToString("yyyy-MM-dd HH:mm:ss");
                
            if (Properties.Settings.Default.EndDate != DateTime.MinValue)
                txtEndDate.Text = Properties.Settings.Default.EndDate.ToString("yyyy-MM-dd HH:mm:ss");
        }
        catch { }
    }

    private void SaveSettings()
    {
        try
        {
            Properties.Settings.Default.ClusterUrl = txtClusterUrl.Text.Trim();
            Properties.Settings.Default.Database = txtDatabase.Text.Trim();
            
            if (_searchTerms.Count > 0)
            {
                Properties.Settings.Default.SearchPhrase = _searchTerms[0].Text;
            }
            
            if (TryParseDateTime(txtStartDate.Text, out DateTime startDate))
                Properties.Settings.Default.StartDate = startDate;
            else
                Properties.Settings.Default.StartDate = DateTime.Now.AddDays(-7);
                
            if (TryParseDateTime(txtEndDate.Text, out DateTime endDate))
                Properties.Settings.Default.EndDate = endDate;
            else
                Properties.Settings.Default.EndDate = DateTime.Now;

            // Save connection to history
            SaveConnectionToHistory(txtClusterUrl.Text.Trim(), txtDatabase.Text.Trim());
                
            Properties.Settings.Default.Save();
        }
        catch { }
    }

    private void SaveConnectionToHistory(string clusterUrl, string database)
    {
        if (string.IsNullOrEmpty(clusterUrl) || string.IsNullOrEmpty(database))
            return;

        var history = Properties.Settings.Default.ConnectionHistory ?? new StringCollection();
        string entry = $"{clusterUrl}|{database}";

        // Remove if already exists (to move it to top)
        if (history.Contains(entry))
            history.Remove(entry);

        // Insert at the beginning
        history.Insert(0, entry);

        // Keep only last 10 entries
        while (history.Count > 10)
            history.RemoveAt(history.Count - 1);

        Properties.Settings.Default.ConnectionHistory = history;
    }

    private void btnHistory_Click(object sender, RoutedEventArgs e)
    {
        var history = Properties.Settings.Default.ConnectionHistory;
        if (history == null || history.Count == 0)
        {
            MessageBox.Show("No previous connections found.", "Connection History", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Create context menu for history
        var contextMenu = new ContextMenu();
        contextMenu.Style = null; // Use default style

        foreach (string? entry in history)
        {
            if (string.IsNullOrEmpty(entry)) continue;

            var parts = entry.Split('|');
            if (parts.Length != 2) continue;

            var menuItem = new MenuItem
            {
                Header = $"{parts[1]} @ {parts[0]}",
                Tag = entry
            };
            menuItem.Click += HistoryMenuItem_Click;
            contextMenu.Items.Add(menuItem);
        }

        if (contextMenu.Items.Count > 0)
        {
            contextMenu.Items.Add(new Separator());
            var clearItem = new MenuItem { Header = "Clear History" };
            clearItem.Click += (s, args) =>
            {
                Properties.Settings.Default.ConnectionHistory?.Clear();
                Properties.Settings.Default.Save();
            };
            contextMenu.Items.Add(clearItem);
        }

        contextMenu.PlacementTarget = btnHistory;
        contextMenu.IsOpen = true;
    }

    private void HistoryMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is string entry)
        {
            var parts = entry.Split('|');
            if (parts.Length == 2)
            {
                txtClusterUrl.Text = parts[0];
                txtDatabase.Text = parts[1];
            }
        }
    }

    /// <summary>
    /// Tries to parse a date/time string in multiple formats.
    /// </summary>
    private bool TryParseDateTime(string input, out DateTime result)
    {
        string[] formats = new[]
        {
            // ISO 8601 formats with T separator
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm",
            // Space separator formats
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy/MM/dd HH:mm",
            "yyyy/MM/dd",
            "MM/dd/yyyy HH:mm:ss",
            "MM/dd/yyyy HH:mm",
            "MM/dd/yyyy",
            "dd-MM-yyyy HH:mm:ss",
            "dd-MM-yyyy HH:mm",
            "dd-MM-yyyy"
        };

        return DateTime.TryParseExact(input?.Trim() ?? "", formats, CultureInfo.InvariantCulture, 
            DateTimeStyles.None, out result);
    }

    private void LogMessage(string message)
    {
        Dispatcher.Invoke(() =>
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLog.AppendText($"[{timestamp}] {message}\n");
            txtLog.ScrollToEnd();
        });
    }

    private void UpdateStatus(string status)
    {
        Dispatcher.Invoke(() =>
        {
            txtStatus.Text = $"⚫ {status}";
        });
        LogMessage(status);
    }

    private async void btnConnect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            btnConnect.IsEnabled = false;
            UpdateStatus("Connecting...");

            string clusterUrl = txtClusterUrl.Text.Trim();
            string database = txtDatabase.Text.Trim();

            if (string.IsNullOrEmpty(clusterUrl))
            {
                MessageBox.Show("Please enter a cluster URL.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(database))
            {
                MessageBox.Show("Please enter a database name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var kcsb = new KustoConnectionStringBuilder(clusterUrl, database)
                .WithAadUserPromptAuthentication();

            _queryProvider?.Dispose();
            _adminProvider?.Dispose();

            _queryProvider = KustoClientFactory.CreateCslQueryProvider(kcsb);
            _adminProvider = KustoClientFactory.CreateCslAdminProvider(kcsb);

            SaveSettings();
            LogMessage($"Attempting connection to {clusterUrl}, Database: {database}");

            await Task.Run(() =>
            {
                var query = ".show database schema";
                using var reader = _queryProvider.ExecuteQuery(database, query, null);
            });

            UpdateStatus("Connected successfully!");
            await LoadTablesAsync();

            MessageBox.Show($"Successfully connected!\nDatabase: {database}\nTables found: {_allTables.Count}",
                "Connection Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            UpdateStatus("Connection failed.");
            LogMessage($"ERROR: {ex.Message}");
            MessageBox.Show($"Connection failed:\n{ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnConnect.IsEnabled = true;
        }
    }

    private async Task LoadTablesAsync()
    {
        UpdateStatus("Loading tables...");
        _allTables.Clear();

        string database = txtDatabase.Text.Trim();

        await Task.Run(() =>
        {
            var query = ".show tables";
            using var reader = _adminProvider!.ExecuteControlCommand(database, query, null);

            while (reader.Read())
            {
                string tableName = reader.GetString(0);
                _allTables.Add(tableName);
            }
        });

        Dispatcher.Invoke(() =>
        {
            txtTablesCount.Text = $"│ Tables: {_allTables.Count}";
            txtSelectedTables.Text = string.Join(",\n", _allTables);
        });

        UpdateStatus($"Loaded {_allTables.Count} tables.");
    }

    private async void btnSearch_Click(object sender, RoutedEventArgs e)
    {
        if (_queryProvider == null)
        {
            MessageBox.Show("Please connect to a cluster first.", "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selectedTables = ParseSelectedTables();
        if (selectedTables.Count == 0)
        {
            MessageBox.Show("Please select at least one table.", "No Tables", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var searchTerms = _searchTerms.Where(t => !string.IsNullOrWhiteSpace(t.Text)).Select(t => t.Text!.Trim()).ToList();
        if (searchTerms.Count == 0)
        {
            MessageBox.Show("Please enter at least one search term.", "No Search Terms", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate date format
        if (!TryParseDateTime(txtStartDate.Text, out DateTime startDate))
        {
            MessageBox.Show("Invalid Start Date format.\n\nExpected format: yyyy-MM-dd HH:mm:ss\nExample: 2026-01-13 09:30:00", 
                "Date Format Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtStartDate.Focus();
            return;
        }

        if (!TryParseDateTime(txtEndDate.Text, out DateTime endDate))
        {
            MessageBox.Show("Invalid End Date format.\n\nExpected format: yyyy-MM-dd HH:mm:ss\nExample: 2026-01-13 17:30:00", 
                "Date Format Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtEndDate.Focus();
            return;
        }

        if (startDate > endDate)
        {
            MessageBox.Show("Start date must be before end date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SaveSettings();

        try
        {
            btnSearch.Visibility = Visibility.Collapsed;
            btnStop.Visibility = Visibility.Visible;
            btnConnect.IsEnabled = false;
            _cancellationTokenSource = new CancellationTokenSource();

            _searchResults.Clear();
            progressBar.Value = 0;
            progressBar.Maximum = selectedTables.Count;

            UpdateStatus("Starting search...");
            LogMessage($"Searching {selectedTables.Count} table(s) for: {string.Join(", ", searchTerms)}");

            await SearchTablesAsync(selectedTables, searchTerms, startDate, endDate, _cancellationTokenSource.Token);

            UpdateStatus("Search completed!");
            MessageBox.Show("Search completed!", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            UpdateStatus("Search cancelled.");
        }
        catch (Exception ex)
        {
            UpdateStatus("Search failed.");
            LogMessage($"ERROR: {ex.Message}");
            MessageBox.Show($"Search failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnSearch.Visibility = Visibility.Visible;
            btnStop.Visibility = Visibility.Collapsed;
            btnConnect.IsEnabled = true;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private async Task SearchTablesAsync(List<string> tables, List<string> searchTerms, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        string database = txtDatabase.Text.Trim();
        int index = 0;

        foreach (var tableName in tables)
        {
            ct.ThrowIfCancellationRequested();
            index++;
            UpdateStatus($"Querying {index}/{tables.Count}: {tableName}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                string startDateStr = startDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
                string endDateStr = endDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

                string searchConditions = string.Join(" and ", searchTerms.Select(t => $"\"{t}\""));
                string query = $@"
                    {tableName}
                    | where TIMESTAMP between (datetime({startDateStr}) .. datetime({endDateStr}))
                    | search {searchConditions}
                    | count";

                long matchCount = await Task.Run(() =>
                {
                    using var reader = _queryProvider!.ExecuteQuery(database, query, null);
                    return reader.Read() ? reader.GetInt64(0) : 0;
                }, ct);

                stopwatch.Stop();

                Dispatcher.Invoke(() =>
                {
                    _searchResults.Add(new SearchResultItem
                    {
                        IsSelected = matchCount > 0,
                        TableName = tableName,
                        MatchCount = matchCount,
                        HasMatches = matchCount > 0 ? "Yes" : "No",
                        Duration = (int)stopwatch.ElapsedMilliseconds,
                        Error = ""
                    });
                    progressBar.Value = index;
                });

                LogMessage(matchCount > 0
                    ? $"✓ {tableName}: {matchCount} matches ({stopwatch.ElapsedMilliseconds}ms)"
                    : $"✗ {tableName}: No matches ({stopwatch.ElapsedMilliseconds}ms)");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                string errorMsg = ex.Message.Length > 80 ? ex.Message.Substring(0, 80) + "..." : ex.Message;

                Dispatcher.Invoke(() =>
                {
                    _searchResults.Add(new SearchResultItem
                    {
                        IsSelected = false,
                        TableName = tableName,
                        MatchCount = 0,
                        HasMatches = "Error",
                        Duration = (int)stopwatch.ElapsedMilliseconds,
                        Error = errorMsg
                    });
                    progressBar.Value = index;
                });

                LogMessage($"ERROR {tableName}: {ex.Message}");
            }

            await Task.Delay(300, ct);
        }
    }

    private void btnStop_Click(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        UpdateStatus("Cancelling...");
    }

    private void btnCopyKql_Click(object sender, RoutedEventArgs e)
    {
        var selectedTables = _searchResults.Where(r => r.IsSelected).Select(r => r.TableName).ToList();
        if (selectedTables.Count == 0)
        {
            MessageBox.Show("Select tables using checkboxes in the results grid.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var searchTerms = _searchTerms.Where(t => !string.IsNullOrWhiteSpace(t.Text)).Select(t => t.Text!.Trim()).ToList();
        
        // Parse dates from TextBox
        if (!TryParseDateTime(txtStartDate.Text, out DateTime startDate))
            startDate = DateTime.Now.AddDays(-7);
        if (!TryParseDateTime(txtEndDate.Text, out DateTime endDate))
            endDate = DateTime.Now;

        string startDateStr = startDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        string endDateStr = endDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

        var kql = new StringBuilder();
        kql.AppendLine("union");
        kql.AppendLine(string.Join(",\n", selectedTables.Select(t => $"    {t}")));
        kql.AppendLine($"| where TIMESTAMP between (datetime({startDateStr}) .. datetime({endDateStr}))");

        if (searchTerms.Count > 0)
        {
            string searchConditions = string.Join(" and ", searchTerms.Select(t => $"\"{t}\""));
            kql.AppendLine($"| search {searchConditions}");
        }

        Clipboard.SetText(kql.ToString());
        LogMessage($"KQL copied for {selectedTables.Count} table(s)");
        MessageBox.Show($"KQL query copied for {selectedTables.Count} table(s)!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnManageTables_Click(object sender, RoutedEventArgs e)
    {
        if (_allTables.Count == 0)
        {
            MessageBox.Show("Connect to a database first.", "No Tables", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var currentSelection = ParseSelectedTables();
        var dialog = new TableSelectionWindow(_allTables, currentSelection);
        dialog.Owner = this;

        if (dialog.ShowDialog() == true)
        {
            txtSelectedTables.Text = string.Join(",\n", dialog.SelectedTables);
            LogMessage($"Updated selection: {dialog.SelectedTables.Count} table(s)");
        }
    }

    private void btnClear_Click(object sender, RoutedEventArgs e)
    {
        _searchResults.Clear();
        txtLog.Clear();
        progressBar.Value = 0;
        UpdateStatus("Cleared.");
    }

    private void btnExport_Click(object sender, RoutedEventArgs e)
    {
        if (_searchResults.Count == 0)
        {
            MessageBox.Show("No results to export.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"KustoSearchResults_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var writer = new StreamWriter(dialog.FileName);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                csv.WriteRecords(_searchResults);

                LogMessage($"Exported to: {dialog.FileName}");
                MessageBox.Show("Export successful!", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void btnAddSearchTerm_Click(object sender, RoutedEventArgs e)
    {
        _searchTerms.Add(new SearchTermItem());
    }

    private void RemoveSearchTerm_Click(object sender, RoutedEventArgs e)
    {
        if (_searchTerms.Count <= 1)
        {
            MessageBox.Show("At least one search term is required.", "Cannot Remove", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (sender is Button btn && btn.Tag is SearchTermItem item)
        {
            _searchTerms.Remove(item);
        }
    }

    private List<string> ParseSelectedTables()
    {
        var text = txtSelectedTables.Text.Trim();
        if (string.IsNullOrEmpty(text)) return new List<string>();

        return text.Split(new[] { ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t) && _allTables.Contains(t, StringComparer.OrdinalIgnoreCase))
            .Distinct()
            .ToList();
    }
}

public class SearchTermItem : INotifyPropertyChanged
{
    private string? _text;
    public string? Text
    {
        get => _text;
        set { _text = value; OnPropertyChanged(nameof(Text)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class SearchResultItem : INotifyPropertyChanged
{
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
    }

    public string TableName { get; set; } = "";
    public long MatchCount { get; set; }
    public string HasMatches { get; set; } = "";
    public int Duration { get; set; }
    public string Error { get; set; } = "";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
