# GLMS Demo Data Seeder - contracts + service requests only
# Run: Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
#      .\seed-data.ps1

$base = "http://localhost:5001"

# 1. LOGIN
Write-Host "Logging in..." -ForegroundColor Cyan
$loginResponse = Invoke-RestMethod -Uri "$base/api/Auth/login" -Method POST `
    -ContentType "application/json" `
    -Body '{"username":"admin","password":"Admin123!"}'
$token = $loginResponse.token
$headers = @{ Authorization = "Bearer $token" }
Write-Host "  OK - token received" -ForegroundColor Green

# 2. GET EXISTING CLIENTS
Write-Host "Fetching existing clients..." -ForegroundColor Cyan
$clients = Invoke-RestMethod -Uri "$base/api/clients" -Method GET -Headers $headers
Write-Host "  Found $($clients.Count) clients:" -ForegroundColor Green
$clients | ForEach-Object { Write-Host "    id=$($_.id)  $($_.name)" }

# Use first 4 client IDs for contracts
$ids = $clients | Select-Object -First 4 -ExpandProperty id
$id1 = $ids[0]; $id2 = $ids[1]; $id3 = $ids[2]; $id4 = $ids[3]

# 3. CREATE CONTRACTS using curl.exe (handles multipart reliably)
Write-Host "Creating contracts..." -ForegroundColor Cyan

function New-Contract($clientId, $start, $end, $level, $type) {
    $result = curl.exe -s -X POST "$base/api/contracts" `
        -H "Authorization: Bearer $token" `
        -F "clientId=$clientId" `
        -F "startDate=$start" `
        -F "endDate=$end" `
        -F "serviceLevel=$level" `
        -F "contractType=$type" | ConvertFrom-Json
    return $result.id
}

$k1 = New-Contract $id1 "2025-01-15" "2026-01-14" "Premium"  "Local"
$k2 = New-Contract $id1 "2024-06-01" "2025-05-31" "Standard" "Local"
$k3 = New-Contract $id2 "2025-03-01" "2026-02-28" "Basic"    "Local"
$k4 = New-Contract $id3 "2025-07-01" "2026-06-30" "Premium"  "International"
$k5 = New-Contract $id4 "2025-09-01" "2026-08-31" "Standard" "Local"
Write-Host "  Created contracts: $k1, $k2, $k3, $k4, $k5" -ForegroundColor Green

# 4. PATCH CONTRACT STATUSES
Write-Host "Setting contract statuses..." -ForegroundColor Cyan
$null = curl.exe -s -X PATCH "$base/api/contracts/$k1/status" -H "Authorization: Bearer $token" -H "Content-Type: application/json" -d '{"status":"Active"}'
$null = curl.exe -s -X PATCH "$base/api/contracts/$k2/status" -H "Authorization: Bearer $token" -H "Content-Type: application/json" -d '{"status":"Expired"}'
$null = curl.exe -s -X PATCH "$base/api/contracts/$k3/status" -H "Authorization: Bearer $token" -H "Content-Type: application/json" -d '{"status":"Active"}'
$null = curl.exe -s -X PATCH "$base/api/contracts/$k4/status" -H "Authorization: Bearer $token" -H "Content-Type: application/json" -d '{"status":"Active"}'
$null = curl.exe -s -X PATCH "$base/api/contracts/$k5/status" -H "Authorization: Bearer $token" -H "Content-Type: application/json" -d '{"status":"OnHold"}'
Write-Host "  Statuses set" -ForegroundColor Green

# 5. CREATE SERVICE REQUESTS
Write-Host "Creating service requests..." -ForegroundColor Cyan

function New-SR($contractId, $desc, $costUSD, $status) {
    $body = "{`"contractId`":$contractId,`"description`":`"$desc`",`"costUSD`":$costUSD,`"status`":`"$status`"}"
    $r = Invoke-RestMethod -Uri "$base/api/servicerequests" -Method POST `
        -Headers $headers -ContentType "application/json" -Body $body
    Write-Host "    id=$($r.id) $desc" -ForegroundColor DarkGreen
}

New-SR $k1 "Pothole repair on N1 highway 15km stretch"        5000 "Pending"
New-SR $k1 "Road marking renewal Johannesburg CBD"            3200 "InProgress"
New-SR $k1 "Traffic light maintenance Q2 2025"                1800 "Completed"
New-SR $k3 "Storm drain clearing Cape Town southern suburbs"  2500 "Pending"
New-SR $k3 "Bridge structural inspection report"              4100 "Pending"
New-SR $k4 "Port cargo logistics shipment tracking"           9800 "InProgress"
New-SR $k4 "Fleet vehicle GPS installation 40 units"          6500 "Pending"
New-SR $k5 "Rural road grading Limpopo northern region"       3700 "Pending"

Write-Host "`nDone! Refresh http://localhost:5000" -ForegroundColor Yellow
