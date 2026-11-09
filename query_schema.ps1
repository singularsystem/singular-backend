$connString = "Server=localhost\SQLEXPRESS;Database=IPAAG_AEE;Trusted_Connection=True;TrustServerCertificate=True;"
$query = @"
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME IN ('RegularTeacherEvaluationDocuments', 'Students', 'BaseDocuments')
ORDER BY TABLE_NAME, COLUMN_NAME;
"@

$connection = New-Object System.Data.SqlClient.SqlConnection($connString)
$command = New-Object System.Data.SqlClient.SqlCommand($query, $connection)
$connection.Open()
$adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
$dataset = New-Object System.Data.DataSet
$adapter.Fill($dataset) | Out-Null
$connection.Close()
$dataset.Tables[0] | Format-Table -AutoSize
