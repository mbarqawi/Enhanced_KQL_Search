# Example Usage Scenarios

## Scenario 1: Search for Errors in the Last 24 Hours

**Goal**: Find all tables containing error messages in the last day.

**Steps**:
1. Connect to cluster: `https://yourcluster.kusto.windows.net`
2. Database: `YourDatabase`
3. Search Phrase: `error`
4. Start Date: Yesterday at current time
5. End Date: Today at current time
6. Click "Search All Tables"

**Expected Result**: Lists all tables with error occurrences and match counts.

---

## Scenario 2: Search for Specific User Activity

**Goal**: Find tables containing activity for a specific user email.

**Steps**:
1. Connect to cluster
2. Search Phrase: `user@example.com`
3. Start Date: `2024-12-01 00:00:00`
4. End Date: `2024-12-08 23:59:59`
5. Click "Search All Tables"

**Expected Result**: Shows which tables contain the user's email address.

---

## Scenario 3: Search for Transaction IDs

**Goal**: Locate all tables containing a specific transaction ID for debugging.

**Steps**:
1. Connect to cluster
2. Search Phrase: `TXN-123456789`
3. Set appropriate date range
4. Click "Search All Tables"
5. Click "Export to CSV" to save results

**Expected Result**: CSV file with all tables containing the transaction ID.

---

## Scenario 4: Security Audit - Search for IP Addresses

**Goal**: Find all tables logging activity from a specific IP address.

**Steps**:
1. Connect to cluster
2. Search Phrase: `192.168.1.100`
3. Start Date: Beginning of audit period
4. End Date: End of audit period
5. Click "Search All Tables"

**Expected Result**: Comprehensive list of tables with IP address occurrences.

---

## Scenario 5: Troubleshooting - Search for Exception Messages

**Goal**: Find tables containing a specific exception type.

**Steps**:
1. Connect to cluster
2. Search Phrase: `NullReferenceException`
3. Set date range around the incident time
4. Click "Search All Tables"
5. Review results to identify affected components

**Expected Result**: Identifies which services/tables logged the exception.

---

## Tips for Effective Searches

### Use Specific Search Phrases
- ✅ Good: `"OrderProcessingException"`
- ❌ Less effective: `"error"`

### Optimize Date Ranges
- Narrow date ranges = faster queries
- Use UTC timestamps for consistency
- Account for timezone differences

### Interpreting Results

**Match Count = 0**: No occurrences found
**Match Count > 0**: Number of matching records
**Error Column**: Query failed (table may not have expected schema)

### Common Search Patterns

| Search Goal | Example Phrase |
|-------------|----------------|
| User activity | `john.doe@company.com` |
| Error codes | `ERR-500` |
| GUIDs/IDs | `a1b2c3d4-e5f6-7890` |
| IP addresses | `10.0.0.1` |
| Exceptions | `TimeoutException` |
| Keywords | `payment failed` |

---

## Advanced: Modifying Search Logic

The default search uses KQL `| search "phrase"` which searches all string columns.

### For More Precise Queries

Edit `MainWindow.xaml.cs` to customize the query:

```csharp
// Current (broad search):
string query = $@"
    {tableName}
    | where ingestion_time() between (datetime({startDateStr}) .. datetime({endDateStr}))
    | search ""{searchPhrase}""
    | count";

// Example: Search specific column
string query = $@"
    {tableName}
    | where ingestion_time() between (datetime({startDateStr}) .. datetime({endDateStr}))
    | where Message contains ""{searchPhrase}""
    | count";
```

### Changing Time Column

Replace `ingestion_time()` with your table's timestamp column:

```csharp
| where Timestamp between (datetime({startDateStr}) .. datetime({endDateStr}))
```

---

## Export and Analysis

After searching:
1. Click "Export to CSV"
2. Open in Excel or data analysis tool
3. Filter/sort by "Match Count" to prioritize tables
4. Focus on tables with errors for troubleshooting

---

## Performance Expectations

| Tables | Approximate Time |
|--------|------------------|
| 10 | ~10 seconds |
| 50 | ~30 seconds |
| 100 | ~1 minute |
| 500 | ~5 minutes |

*Note: Times vary based on cluster load, data volume, and query complexity*
