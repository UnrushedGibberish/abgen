$ErrorActionPreference = "Stop"

function Test-PortAvailable([int]$Port) {
    $listener = $null
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
        $listener.Start()
        return $true
    }
    catch {
        return $false
    }
    finally {
        if ($null -ne $listener) {
            $listener.Stop()
        }
    }
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $repositoryRoot ".env"
$values = @{}
$existingContent = ""

if (Test-Path -LiteralPath $envPath) {
    $existingContent = [System.IO.File]::ReadAllText($envPath)
    foreach ($line in ($existingContent -split "`r?`n")) {
        if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$') {
            $key = $Matches[1]
            $value = $Matches[2].Trim()
            if ($value.Length -ge 2 -and (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'")))) {
                $value = $value.Substring(1, $value.Length - 2)
            }
            $values[$key] = $value
        }
    }
}

$portKeys = @("ABGEN_API_PORT", "ABGEN_FRONTEND_PORT", "ABGEN_POSTGRES_PORT")
$workspacePath = [System.IO.Path]::GetFullPath($repositoryRoot).ToLowerInvariant()
$sha256 = [System.Security.Cryptography.SHA256]::Create()
try {
    $pathHash = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($workspacePath))
}
finally {
    $sha256.Dispose()
}
$projectHash = [System.BitConverter]::ToString($pathHash).Replace("-", "").ToLowerInvariant()
$projectName = "abgen-$($projectHash.Substring(0, 12))"
$assignedPorts = @{}
$usedPorts = @{}
foreach ($key in $portKeys) {
    if ($values.ContainsKey($key) -and -not [string]::IsNullOrWhiteSpace($values[$key])) {
        $port = 0
        if (-not [int]::TryParse([string]$values[$key], [ref]$port) -or $port -lt 1 -or $port -gt 65535) {
            throw "$key in .env must be a valid TCP port between 1 and 65535."
        }
        if ($usedPorts.ContainsKey($port)) {
            throw "$key and $($usedPorts[$port]) in .env cannot use the same port ($port)."
        }
        $assignedPorts[$key] = $port
        $usedPorts[$port] = $key
    }
}

if ($assignedPorts.Count -lt $portKeys.Count) {
    $workspacePath = [System.IO.Path]::GetFullPath($repositoryRoot).ToLowerInvariant()
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $pathHash = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($workspacePath))
    }
    finally {
        $sha256.Dispose()
    }

    $startingSlot = [System.BitConverter]::ToUInt32($pathHash, 0) % 1000
    $selectedPorts = $null
    for ($step = 0; $step -lt 1000; $step++) {
        $slot = ($startingSlot + $step) % 1000
        $candidatePorts = @{
            "ABGEN_API_PORT" = 5000 + $slot
            "ABGEN_FRONTEND_PORT" = 6000 + $slot
            "ABGEN_POSTGRES_PORT" = 15000 + $slot
        }
        $portsForSlot = @{}
        $slotIsAvailable = $true

        foreach ($key in $portKeys) {
            $port = if ($assignedPorts.ContainsKey($key)) { $assignedPorts[$key] } else { $candidatePorts[$key] }
            if ($portsForSlot.ContainsKey($port)) {
                $slotIsAvailable = $false
                break
            }
            $portsForSlot[$port] = $key
        }

        if (-not $slotIsAvailable) {
            continue
        }

        foreach ($key in $portKeys) {
            if (-not $assignedPorts.ContainsKey($key) -and -not (Test-PortAvailable $candidatePorts[$key])) {
                $slotIsAvailable = $false
                break
            }
        }

        if ($slotIsAvailable) {
            $selectedPorts = @{}
            foreach ($key in $portKeys) {
                $selectedPorts[$key] = if ($assignedPorts.ContainsKey($key)) { $assignedPorts[$key] } else { $candidatePorts[$key] }
            }
            break
        }
    }

    if ($null -eq $selectedPorts) {
        throw "Could not find a free set of local development ports. Set ABGEN_API_PORT, ABGEN_FRONTEND_PORT, and ABGEN_POSTGRES_PORT in .env."
    }
    $assignedPorts = $selectedPorts
}

$password = if ($env:POSTGRES_PASSWORD) {
    $env:POSTGRES_PASSWORD
}
elseif ($values.ContainsKey("POSTGRES_PASSWORD") -and -not [string]::IsNullOrEmpty($values["POSTGRES_PASSWORD"])) {
    $values["POSTGRES_PASSWORD"]
}
else {
    "development-only"
}
if ($password.Contains(';') -or $password.Contains('"')) {
    $password = '"' + $password.Replace('"', '""') + '"'
}

$generatedValues = [ordered]@{
    "ABGEN_API_PORT" = [string]$assignedPorts["ABGEN_API_PORT"]
    "ABGEN_FRONTEND_PORT" = [string]$assignedPorts["ABGEN_FRONTEND_PORT"]
    "ABGEN_POSTGRES_PORT" = [string]$assignedPorts["ABGEN_POSTGRES_PORT"]
    "POSTGRES_PASSWORD" = $password
    "COMPOSE_PROJECT_NAME" = $projectName
    "ConnectionStrings__Postgres" = "Host=127.0.0.1;Port=$($assignedPorts['ABGEN_POSTGRES_PORT']);Database=abgen;Username=abgen;Password=$password"
    "Cors__AllowedOrigins__0" = "http://127.0.0.1:$($assignedPorts['ABGEN_FRONTEND_PORT'])"
    "Cors__AllowedOrigins__1" = "http://localhost:$($assignedPorts['ABGEN_FRONTEND_PORT'])"
    "CORS_ALLOWED_ORIGIN_0" = "http://127.0.0.1:$($assignedPorts['ABGEN_FRONTEND_PORT'])"
    "CORS_ALLOWED_ORIGIN_1" = "http://localhost:$($assignedPorts['ABGEN_FRONTEND_PORT'])"
}
$settingsPath = Join-Path $repositoryRoot ".vscode\settings.json"
$settings = [ordered]@{
    "abgen.localApiPort" = $assignedPorts["ABGEN_API_PORT"]
    "abgen.localFrontendPort" = $assignedPorts["ABGEN_FRONTEND_PORT"]
    "abgen.localPostgresPort" = $assignedPorts["ABGEN_POSTGRES_PORT"]
}
$settingsJson = "{`n  `"abgen.localApiPort`": $($settings['abgen.localApiPort']),`n  `"abgen.localFrontendPort`": $($settings['abgen.localFrontendPort']),`n  `"abgen.localPostgresPort`": $($settings['abgen.localPostgresPort'])`n}`n"
[System.IO.File]::WriteAllText($settingsPath, $settingsJson, [System.Text.UTF8Encoding]::new($false))

$linesToWrite = @()
$writtenKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $generatedValues.GetEnumerator()) {
    $key = $entry.Key
    $linesToWrite += "$key=$($entry.Value)"
    [void]$writtenKeys.Add($key)
}

$retainedLines = @()
foreach ($existingLine in ($existingContent -split "`r?`n")) {
    if ($existingLine -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=') {
        $existingKey = $Matches[1]
        if (-not $writtenKeys.Contains($existingKey)) {
            $retainedLines += $existingLine
        }
    }
    elseif (-not [string]::IsNullOrWhiteSpace($existingLine)) {
        $retainedLines += $existingLine
    }
}
if (-not (Test-Path -LiteralPath $envPath)) {
    $retainedLines = @("# Generated by scripts/setup-local-dev.ps1; ignored by Git.") + $retainedLines
}
$content = ($retainedLines + $linesToWrite) -join [System.Environment]::NewLine
if ($content.Length -gt 0) {
    $content += [System.Environment]::NewLine
}
[System.IO.File]::WriteAllText($envPath, $content, [System.Text.UTF8Encoding]::new($false))

Write-Host "Local development ports: API $($assignedPorts['ABGEN_API_PORT']), Vite $($assignedPorts['ABGEN_FRONTEND_PORT']), PostgreSQL $($assignedPorts['ABGEN_POSTGRES_PORT'])."
