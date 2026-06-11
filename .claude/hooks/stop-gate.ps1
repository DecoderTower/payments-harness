# Feedback loop: an agent cannot declare work done while the build or tests are red.
# Runs on the Stop event. Exit 2 blocks the stop and feeds the failure back to the agent.

$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json

# A previous stop was already blocked by this hook; let the agent stop to avoid a loop.
if ($payload.stop_hook_active) { exit 0 }

# Nothing to gate until the implementation exists.
if (-not (Get-ChildItem -Path "src", "tests" -Filter *.csproj -Recurse -ErrorAction SilentlyContinue)) { exit 0 }

$build = dotnet build --nologo -v q 2>&1
if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine("Stop gate: dotnet build failed.`n$($build | Out-String)")
    exit 2
}

$test = dotnet test --nologo -v q 2>&1
if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine("Stop gate: dotnet test failed.`n$($test | Out-String)")
    exit 2
}

exit 0
