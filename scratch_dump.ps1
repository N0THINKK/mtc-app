$connStr = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\AC90 Master Paper\ListHistry.mdb;"
$conn = New-Object System.Data.OleDb.OleDbConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT * FROM Problem"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    $row = ""
    for ($i = 0; $i -lt $reader.FieldCount; $i++) {
        $row += $reader.GetValue($i).ToString() + "`t"
    }
    Write-Output $row
}
$reader.Close()
$conn.Close()
