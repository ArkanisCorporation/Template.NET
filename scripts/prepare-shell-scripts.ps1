<#
.SYNOPSIS
Prepares staged shell scripts before commit.

.DESCRIPTION
Marks staged *.sh files executable and blocks CRLF or mixed line endings.
#>

$ErrorActionPreference = "Stop"

# Only staged shell scripts need preparation.
$trackedShellScripts = @(git diff --cached --name-only --diff-filter=ACMR -- "*.sh")

if ($trackedShellScripts.Count -eq 0) {
    exit 0
}

# File mode must be changed in the Git index so the commit records +x.
foreach ($file in $trackedShellScripts) {
    git update-index --chmod=+x -- $file
}

$failedFiles = New-Object System.Collections.Generic.List[string]

# Git's staged EOL metadata catches CRLF before the file reaches CI.
foreach ($file in $trackedShellScripts) {
    $eolInfo = git ls-files --eol -- $file

    if ($eolInfo -match "^i/(crlf|mixed)\s") {
        $failedFiles.Add($file)
    }
}

if ($failedFiles.Count -gt 0) {
    Write-Host "Shell scripts must use LF line endings before commit:"

    foreach ($file in $failedFiles) {
        Write-Host "  - $file"
    }

    Write-Host "Convert the files to LF and stage them again."
    exit 1
}
