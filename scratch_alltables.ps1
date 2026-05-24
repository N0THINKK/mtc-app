$dir = "C:\AC90 Master Paper"
$mdbs = Get-ChildItem -Path $dir -Filter "*.mdb"
foreach ($mdb in $mdbs) {
    $connStr = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + $mdb.FullName + ";"
    try {
        $conn = New-Object System.Data.OleDb.OleDbConnection($connStr)
        $conn.Open()
        $schema = $conn.GetSchema("Tables")
        $tables = @()
        $schema.Rows | ForEach-Object { if ($_.TABLE_TYPE -eq "TABLE") { $tables += $_.TABLE_NAME } }
        Write-Output ("File: " + $mdb.Name + " -> Tables: " + ($tables -join ", "))
        $conn.Close()
    } catch {
        # ignore
    }
}
