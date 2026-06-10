<#
.SYNOPSIS
Marks tracked shell scripts executable.

.DESCRIPTION
Updates the Git index executable bit for every tracked *.sh file in the repository.
#>

$ErrorActionPreference = "Stop"

$repoRoot = git rev-parse --show-toplevel
Set-Location -LiteralPath $repoRoot

$trackedShellScripts = @(git ls-files -- "*.sh")

if ($trackedShellScripts.Count -eq 0) {
    exit 0
}

foreach ($file in $trackedShellScripts) {
    git update-index --chmod=+x -- $file
}

Write-Host "Marked tracked shell scripts executable in the Git index:"

foreach ($file in $trackedShellScripts) {
    Write-Host "  - $file"
}
