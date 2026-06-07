# Fix contract statuses
$base = "http://localhost:5001"

Write-Host "Logging in..." -ForegroundColor Cyan
$loginResponse = Invoke-RestMethod -Uri "$base/api/Auth/login" -Method POST `
    -ContentType "application/json" `
    -Body '{"username":"admin","password":"Admin123!"}'
$token = $loginResponse.token
$headers = @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" }
Write-Host "  OK" -ForegroundColor Green

# Get all contracts to see current state
$contracts = Invoke-RestMethod -Uri "$base/api/contracts" -Method GET -Headers $headers
Write-Host "Found $($contracts.Count) contracts:" -ForegroundColor Cyan
$contracts | ForEach-Object { Write-Host "  id=$($_.id) status=$($_.status) clientId=$($_.clientId)" }

# Patch each one
$statuses = @{ 1="Active"; 2="Expired"; 3="Active"; 4="Active"; 5="OnHold" }

foreach ($entry in $statuses.GetEnumerator()) {
    $id = $entry.Key
    $status = $entry.Value
    try {
        $body = '{"status":"' + $status + '"}'
        $result = Invoke-RestMethod -Uri "$base/api/contracts/$id/status" -Method PATCH `
            -Headers $headers -Body $body
        Write-Host "  Contract $id -> $status OK" -ForegroundColor Green
    } catch {
        Write-Host "  Contract $id -> $status FAILED: $_" -ForegroundColor Red
    }
}

Write-Host "`nDone! Refresh http://localhost:5000" -ForegroundColor Yellow
