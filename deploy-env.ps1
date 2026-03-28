#Requires -Version 7.0
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$AppName = 'UnifiStoreWatcher',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$RemoteHost = 'rverbist@proxmox'
)

function Copy-ToRemote {
    <#
    .SYNOPSIS
        Copies a local file to a remote destination via scp, skipping missing sources.
    .PARAMETER Source
        Path to the local source file.
    .PARAMETER Destination
        Remote destination path (scp format: user@host:/path/file).
        If omitted, copies to $remoteBase using the source file's original name.
    .OUTPUTS
        None
    #>
    [CmdletBinding(SupportsShouldProcess = $true)]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Source,

        [Parameter()]
        [string]$Destination
    )

    process {
        if (-not (Test-Path -LiteralPath $Source)) {
            Write-Warning "Source file not found, skipping: $Source"
            return
        }

        $resolvedDestination = if ($Destination) { $Destination } else { "$remoteBase/$(Split-Path -Leaf $Source)" }

        if ($PSCmdlet.ShouldProcess($resolvedDestination, "Copy '$Source'")) {
            Write-Verbose "Copying '$Source' -> '$resolvedDestination'"
            scp $Source $resolvedDestination
        }
    }
}

$remoteBase = "${RemoteHost}:/opt/docker/apps/$AppName"

$filesToDeploy = @(
    '.env.Production'
    '.env'
    'compose.yaml'
    'docker-compose.yaml'
    'Dockerfile'
)

foreach ($file in $filesToDeploy) {
    Copy-ToRemote -Source $file
}
