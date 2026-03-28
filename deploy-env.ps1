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
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string]$Source,

        [Parameter(Position = 1)]
        [string]$Destination
    )

    process {
        if (-not (Test-Path -LiteralPath $Source)) {
            Write-Warning "Source file not found, skipping: $Source"
            return
        }

        $resolvedDestination = if ($Destination) { $Destination } else { "$remoteBase/$(Split-Path -Leaf $Source)" }

        if ($PSCmdlet.ShouldProcess($resolvedDestination, "Copy '$Source'")) {
            Write-Verbose "scp '$Source' '$resolvedDestination'"
            scp $Source $resolvedDestination
            if ($LASTEXITCODE -ne 0) {
                $PSCmdlet.ThrowTerminatingError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Exception]::new("scp failed (exit code $LASTEXITCODE): $Source -> $resolvedDestination"),
                        'ScpFailed',
                        [System.Management.Automation.ErrorCategory]::ConnectionError,
                        $Source
                    )
                )
            }
        }
    }
}

function Invoke-OnRemote {
    <#
    .SYNOPSIS
        Runs a command on the remote host inside the app directory via ssh.
    .PARAMETER Command
        The shell command to execute remotely.
    #>
    [CmdletBinding(SupportsShouldProcess = $true)]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string]$Command
    )

    process {
        $remoteCommand = "cd /opt/docker/apps/$AppName && $Command"

        if ($PSCmdlet.ShouldProcess($RemoteHost, $remoteCommand)) {
            Write-Verbose "ssh $RemoteHost `"$remoteCommand`""
            ssh $RemoteHost $remoteCommand
            if ($LASTEXITCODE -ne 0) {
                $PSCmdlet.ThrowTerminatingError(
                    [System.Management.Automation.ErrorRecord]::new(
                        [System.Exception]::new("Remote command failed (exit code $LASTEXITCODE): $Command"),
                        'RemoteCommandFailed',
                        [System.Management.Automation.ErrorCategory]::InvalidResult,
                        $Command
                    )
                )
            }
        }
    }
}

Copy-ToRemote '.env' -Verbose
Copy-ToRemote '.env.Production' -Verbose

Invoke-OnRemote 'git pull' -Verbose
Invoke-OnRemote 'docker compose build' -Verbose
Invoke-OnRemote 'docker compose down' -Verbose
Invoke-OnRemote 'docker compose up -d' -Verbose

Invoke-OnRemote 'docker image prune -f' -Verbose
