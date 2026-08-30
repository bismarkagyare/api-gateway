# Verifies the gateway stack end-to-end.
# Assumes Redis, both mocks, and the gateway are running (see gateway-run skill).
param(
    [string]$ApiKey = 'test-key-1',
    [string]$Gateway = 'http://localhost:5136',
    [string]$Products = 'http://localhost:5099',
    [string]$Orders = 'http://localhost:5101'
)

function Test-Endpoint {
    param([string]$Name, [string]$Url, [hashtable]$Headers = @{})

    try {
        $r = Invoke-WebRequest -Uri $Url -Headers $Headers -UseBasicParsing -TimeoutSec 10
        $verdict = if ($r.StatusCode -ge 200 -and $r.StatusCode -lt 300) { 'PASS' } else { 'FAIL' }
        Write-Host ("[{0}] {1} -> {2}" -f $verdict, $Name, $r.StatusCode)

        if ($Headers.ContainsKey('X-API-Key') -and $r.Headers.ContainsKey('X-RateLimit-Remaining')) {
            Write-Host ("       rate-limit remaining: {0} (limit {1}, reset {2})" -f `
                $r.Headers['X-RateLimit-Remaining'], $r.Headers['X-RateLimit-Limit'], $r.Headers['X-RateLimit-Reset'])
        }
    }
    catch {
        $code = $_.Exception.Response.StatusCode.value__
        Write-Host ("[FAIL] {0} -> {1} ({2})" -f $Name, $code, $_.Exception.Message)
    }
}

$keyHeaders = @{ 'X-API-Key' = $ApiKey }

Write-Host "== Mocks (direct) =="
Test-Endpoint 'products mock' "$Products/products"
Test-Endpoint 'orders mock'   "$Orders/orders"

Write-Host "== Gateway (authenticated) =="
Test-Endpoint 'health'         "$Gateway/health" $keyHeaders
Test-Endpoint 'proxy/products' "$Gateway/proxy/products" $keyHeaders
Test-Endpoint 'proxy/orders'   "$Gateway/proxy/orders" $keyHeaders

Write-Host "== Rate-limit headers (watch remaining decrement) =="
1..3 | ForEach-Object {
    Test-Endpoint "proxy/products #$_" "$Gateway/proxy/products" $keyHeaders
}
