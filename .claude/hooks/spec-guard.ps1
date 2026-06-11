# Constraint: specs and decision records are durable context. They change deliberately,
# never as collateral damage of an implementation task. Any write to docs/specs or
# docs/decisions requires explicit human approval.

$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$path = $payload.tool_input.file_path

if ($path -and $path -match '[\\/]docs[\\/](specs|decisions)[\\/]' -and $path -notmatch 'TEMPLATE\.md$') {
    @{
        hookSpecificOutput = @{
            hookEventName            = "PreToolUse"
            permissionDecision       = "ask"
            permissionDecisionReason = "This edits durable context ($path). Spec and decision changes must be deliberate - confirm before proceeding."
        }
    } | ConvertTo-Json -Compress | Write-Output
}

exit 0
