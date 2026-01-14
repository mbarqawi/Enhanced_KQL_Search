# QUICK START GUIDE

## Run the Application Now

```bash
cd KustoSearchApp
dotnet run
```

The application window will open immediately.

## First Time Setup (30 seconds)

1. **Enter Cluster URL**
   - Example: `https://wawsweu.kusto.windows.net`

2. **Enter Database Name**
   - Example: `MyDatabase`

3. **Click "Connect"** (or use 📋 to select a previous connection)
   - Sign in with your Azure AD credentials when prompted
   - Wait for "Connected successfully!" message

## Perform Your First Search (1 minute)

1. **Add Search Terms**: Enter `error` (or any text you want to find)
   - Click "+ Add Search Term" to add more terms (AND logic)

2. **Start Date**: Enter date/time (defaults to 7 days ago)

3. **End Date**: Enter date/time (defaults to now)

4. **Select Tables** (optional): Click "Select Tables" to choose specific tables

5. **Click "Search"**

6. **Watch Progress**
   - Status bar shows current table being queried
   - Progress bar fills as search continues
   - Log window shows real-time activity

7. **View Results**
   - Data grid shows all tables
   - "Has Matches" column shows Yes/No
   - "Match Count" shows number of records found
   - "Query Duration" shows milliseconds per query

## Export Results

Click **"Export to CSV"** button to save results to a file.

---

## Sample First Search

**Scenario**: Find all error messages from the last week

```
Cluster URL:    https://yourcluster.kusto.windows.net
Database:       YourDatabase
Search Phrase:  error
Start Date:     2024-12-01 00:00:00
End Date:       2024-12-08 23:59:59
```

Click "Search All Tables" and wait for completion.

---

## Troubleshooting

**"Please connect to a cluster first"**
- Click the "Connect" button first

**"Connection failed"**
- Verify cluster URL is correct
- Check your Azure AD credentials
- Ensure you have access to the database

**Authentication popup doesn't appear**
- Check your pop-up blocker settings
- Try restarting the application

**Slow searches**
- Narrow your date range
- This is normal for large databases (100+ tables)

---

## What Happens During Search?

1. Application queries each table sequentially
2. Uses KQL query to search for your phrase
3. Counts matching records per table
4. Displays results in real-time
5. Logs all activity to the log window

## Expected Performance

- **Small databases (10 tables)**: ~10 seconds
- **Medium databases (50 tables)**: ~30 seconds  
- **Large databases (100 tables)**: ~1 minute
- **Very large (500+ tables)**: Several minutes

---

## Next Steps

- Read **EXAMPLES.md** for common usage scenarios
- Read **README.md** for detailed documentation
- Read **BUILD.md** for compilation options

---

## Support

Check the **Log Window** (bottom of screen) for detailed error messages and activity tracking.

All operations are logged with timestamps for troubleshooting.
