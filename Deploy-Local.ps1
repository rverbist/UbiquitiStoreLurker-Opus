#Requires -Version 7.0
<#
.SYNOPSIS
    Deploys UniFiStoreWatcher locally using Docker Compose.

.DESCRIPTION
    Pulls the latest code from Git, rebuilds the Docker image, restarts the
    containers, and prunes dangling images. Supports -WhatIf and -Confirm.

.EXAMPLE
    .\Deploy-Local.ps1

.EXAMPLE
    .\Deploy-Local.ps1 -WhatIf

.NOTES
    Requires Docker and Git to be available on PATH.
#>
[CmdletBinding(SupportsShouldProcess)]
param()

$ErrorActionPreference = 'Stop'

function Write-Step {
    <#
    .SYNOPSIS
        Writes a formatted section header to the host.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Message
    )

    Write-Host ''
    Write-Host " === $Message ===" -ForegroundColor Cyan
}

function Assert-LastExitCode {
    <#
    .SYNOPSIS
        Throws a terminating error if the last native command exited non-zero.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Step
    )

    if ($LASTEXITCODE -ne 0) {
        Write-Host ''
        Write-Host " [FAILED] $Step exited with code $LASTEXITCODE" -ForegroundColor Red

        $errorRecord = [System.Management.Automation.ErrorRecord]::new(
            [System.Exception]::new("'$Step' failed with exit code $LASTEXITCODE"),
            'NativeCommandFailed',
            [System.Management.Automation.ErrorCategory]::OperationStopped,
            $Step
        )
        $PSCmdlet.ThrowTerminatingError($errorRecord)
    }
}

Write-Step -Message 'Retrieving latest version from Git'
if ($PSCmdlet.ShouldProcess('local repository', 'git pull')) {
    git pull
    Assert-LastExitCode -Step 'git pull'
}

Write-Step -Message 'Building Docker image'
if ($PSCmdlet.ShouldProcess('Docker image', 'docker compose build')) {
    docker compose build
    Assert-LastExitCode -Step 'docker compose build'
}

Write-Step -Message 'Stopping running containers'
if ($PSCmdlet.ShouldProcess('running containers', 'docker compose down')) {
    docker compose down
    Assert-LastExitCode -Step 'docker compose down'
}

Write-Step -Message 'Starting containers'
if ($PSCmdlet.ShouldProcess('containers', 'docker compose up -d')) {
    docker compose up -d
    Assert-LastExitCode -Step 'docker compose up'
}

Write-Step -Message 'Pruning dangling images'
if ($PSCmdlet.ShouldProcess('dangling Docker images', 'docker image prune -f')) {
    docker image prune -f
    Assert-LastExitCode -Step 'docker image prune'
}

Write-Host ''
Write-Host ' === Deploy complete ===' -ForegroundColor Green
