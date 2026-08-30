# Loads the sample API keys into Redis as hashes.
# Requires: redis-cli on PATH and a running Redis server.
param(
    [string]$RedisCli = 'redis-cli'
)

$keys = @(
    @{ Key = 'test-key-1'; Plan = 'premium'; MaxRequests = 100; WindowSeconds = 60; IsActive = 'true' },
    @{ Key = 'test-key-2'; Plan = 'free'; MaxRequests = 10; WindowSeconds = 60; IsActive = 'true' }
)

foreach ($k in $keys) {
    & $RedisCli HSET "apikey:meta:$($k.Key)" plan $k.Plan maxRequests $k.MaxRequests windowSeconds $k.WindowSeconds isActive $k.IsActive | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "OK  apikey:meta:$($k.Key) (plan=$($k.Plan), maxRequests=$($k.MaxRequests))"
    }
    else {
        Write-Host "ERR failed to load $($k.Key) - is Redis running?"
    }
}
