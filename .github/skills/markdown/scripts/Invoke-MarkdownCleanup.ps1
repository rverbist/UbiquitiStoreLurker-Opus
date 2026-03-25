<#
.SYNOPSIS
    Runs all markdown normalization and mojibake repair scripts on a folder.

.DESCRIPTION
    Executes the complete markdown cleanup pipeline including:
    - Fix-Mojibake
    - Normalize-Characters
    - Normalize-Whitespace
    - Normalize-TableSeparators
    - Normalize-CodeFences
    - Update-TableOfContents

.PARAMETER Path
    Target file (.md) or directory to process.

.PARAMETER Recurse
    Process subdirectories recursively.

.OUTPUTS
    System.String. Per-file result messages from each pipeline step.

.EXAMPLE
    .\Invoke-MarkdownCleanup.ps1 -Path .\docs -Recurse

.EXAMPLE
    .\Invoke-MarkdownCleanup.ps1 -Path .\docs -Recurse -WhatIf -Verbose
#>
function Invoke-MarkdownCleanup {
    [CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
    param(
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [Alias('Input', 'In', 'i', 'FullName')]
        [ValidateNotNullOrEmpty()]
        [string]$Path,

        [Parameter()]
        [switch]$Recurse
    )

    begin {
        $scriptsPath = $PSScriptRoot
        $scripts = @(
            'Fix-Mojibake.ps1',
            'Normalize-Characters.ps1',
            'Normalize-Whitespace.ps1',
            'Normalize-TableSeparators.ps1',
            'Normalize-CodeFences.ps1',
            'Update-TableOfContents.ps1'
        )
        foreach ($script in $scripts) {
            $scriptFullPath = Join-Path $scriptsPath $script
            if (-not (Test-Path $scriptFullPath)) {
                $errorRecord = [System.Management.Automation.ErrorRecord]::new(
                    [System.IO.FileNotFoundException]::new("Script not found: $scriptFullPath"),
                    'ScriptNotFound',
                    [System.Management.Automation.ErrorCategory]::ObjectNotFound,
                    $scriptFullPath
                )
                $PSCmdlet.ThrowTerminatingError($errorRecord)
            }
            . $scriptFullPath
        }
    }

    process {
        Write-Verbose "Starting markdown cleanup pipeline..."
        Write-Verbose "Target: $Path"
        Write-Verbose "Recurse: $($Recurse.IsPresent)"

        $commonParams = @{ Path = $Path }
        if ($Recurse.IsPresent) { $commonParams['Recurse'] = $true }
        if ($WhatIfPreference) { $commonParams['WhatIf'] = $true }
        if ($VerbosePreference -eq 'Continue') { $commonParams['Verbose'] = $true }

        $steps = @(
            @{ Function = 'Fix-Mojibake'; Description = 'Repairing encoding corruption' },
            @{ Function = 'Normalize-Characters'; Description = 'Normalizing special characters' },
            @{ Function = 'Normalize-Whitespace'; Description = 'Cleaning up whitespace' },
            @{ Function = 'Normalize-TableSeparators'; Description = 'Formatting table separators' },
            @{ Function = 'Normalize-CodeFences'; Description = 'Normalizing code fences' },
            @{ Function = 'Update-TableOfContents'; Description = 'Updating tables of contents' }
        )

        foreach ($step in $steps) {
            Write-Verbose "[$($step.Function)] $($step.Description)..."
            & $step.Function @commonParams
        }

        Write-Verbose 'Markdown cleanup pipeline complete.'
    }
}

if ($MyInvocation.InvocationName -ne '.') {
    if ($args.Count -gt 0) {
        Invoke-MarkdownCleanup @args
    } else {
        Get-Help Invoke-MarkdownCleanup -Detailed
    }
}
