$connStr = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\AC90 Master Paper\DataAbnormal.mdb;"
try {
    $conn = New-Object System.Data.OleDb.OleDbConnection($connStr)
    $conn.Open()
    $schema = $conn.GetSchema("Tables")
    $schema.Rows | ForEach-Object { if ($_.TABLE_TYPE -eq "TABLE") { Write-Output $_.TABLE_NAME } }
    $conn.Close()
} catch {
    Write-Output "ACE failed. Trying JET..."
    $connStr2 = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=C:\AC90 Master Paper\DataAbnormal.mdb;"
    try {
        $conn2 = New-Object System.Data.OleDb.OleDbConnection($connStr2)
        $conn2.Open()
        $schema2 = $conn2.GetSchema("Tables")
        $schema2.Rows | ForEach-Object { if ($_.TABLE_TYPE -eq "TABLE") { Write-Output $_.TABLE_NAME } }
        $conn2.Close()
    } catch {
        Write-Output "JET also failed: $_"
    }
}
