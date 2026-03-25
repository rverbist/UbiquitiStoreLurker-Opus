<#
.SYNOPSIS
    Formats markdown table separator lines by adding spaces around dashes.

.DESCRIPTION
    Processes markdown files to format table separator lines (the lines with pipes, 
    dashes, and optional colons for alignment). The first and last dash within each 
    column are replaced with spaces, keeping the total line length unchanged.

    Examples:
    - |------|------|  becomes  | ---- | ---- |
    - |:----:|-----:|  becomes  |: -- :| --- :|

.PARAMETER Path
    The path to a markdown file (.md) or a directory containing markdown files.
    Accepts absolute or relative paths.

.PARAMETER Recurse
    When Path is a directory, processes markdown files in all subdirectories.
    By default, only markdown files in the specified directory are processed.
    This parameter is ignored when Path is a file.

.EXAMPLE
    Normalize-TableSeparators -Path .\docs\README.md
    Formats table separators in a single markdown file.

.EXAMPLE
    Normalize-TableSeparators -Path .\docs -Verbose
    Formats all markdown files in the docs directory with detailed output.

.EXAMPLE
    Normalize-TableSeparators -Path .\docs -WhatIf
    Shows what changes would be made without actually modifying files.

.OUTPUTS
    Reports the number of lines updated per file.

.NOTES
    Author: AICMS Team
    Version: 1.0.0
#>
function Normalize-TableSeparators {
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
        Write-Verbose 'Starting markdown table separator formatting process'

        # Pattern to match markdown table separator lines
        # Matches lines that consist of: optional leading whitespace, | followed by cells containing dashes, colons, and spaces
        # Leading whitespace is allowed to support indented tables (e.g. tables inside list items)
        $tableSeparatorPattern = '^\s*\|(?:\s*:?-+:?\s*\|)+$'
        $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    }

    process {
        # Resolve the path (handles relative paths)
        $resolvedPath = $null
        try {
            $resolvedPath = Resolve-Path -Path $Path -ErrorAction Stop
        } catch {
            $errorRecord = [System.Management.Automation.ErrorRecord]::new(
                [System.IO.FileNotFoundException]::new("Path not found: $Path"),
                'PathNotFound',
                [System.Management.Automation.ErrorCategory]::ObjectNotFound,
                $Path
            )
            $PSCmdlet.WriteError($errorRecord)
            return
        }

        # Get list of markdown files to process
        $markdownFiles = @()
        $pathItem = Get-Item -Path $resolvedPath

        if ($pathItem.PSIsContainer) {
            Write-Verbose "Processing directory: $resolvedPath"
            if ($Recurse.IsPresent) {
                Write-Verbose 'Recursing into subdirectories'
                $markdownFiles = @(Get-ChildItem -Path $resolvedPath -Filter '*.md' -Recurse -File)
            } else {
                $markdownFiles = @(Get-ChildItem -Path $resolvedPath -Filter '*.md' -File)
            }
            if ($markdownFiles.Count -eq 0) {
                Write-Warning "No markdown files found in directory: $resolvedPath"
                return
            }
        } elseif ($pathItem.Extension -eq '.md') {
            Write-Verbose "Processing single file: $resolvedPath"
            if ($Recurse.IsPresent) {
                Write-Warning '-Recurse parameter is ignored when Path is a file'
            }
            $markdownFiles = @($pathItem)
        } else {
            $errorRecord = [System.Management.Automation.ErrorRecord]::new(
                [System.ArgumentException]::new("Path must be a directory or a markdown file (.md): $Path"),
                'InvalidFileType',
                [System.Management.Automation.ErrorCategory]::InvalidArgument,
                $Path
            )
            $PSCmdlet.WriteError($errorRecord)
            return
        }

        foreach ($file in $markdownFiles) {
            Write-Verbose "Examining file: $($file.FullName)"
            $lines = Get-Content -Path $file.FullName -Encoding UTF8
            $updatedLines = @()
            $changeCount = 0
            $changes = @()

            for ($i = 0; $i -lt $lines.Count; $i++) {
                $line = $lines[$i]
                $lineNumber = $i + 1

                if ($line -match $tableSeparatorPattern) {
                    $newLine = Format-TableSeparatorLine -Line $line

                    if ($newLine -ne $line) {
                        $changeCount++
                        $changes += [PSCustomObject]@{
                            LineNumber = $lineNumber
                            Original   = $line
                            New        = $newLine
                        }
                        $updatedLines += $newLine
                    } else {
                        $updatedLines += $line
                    }
                } else {
                    $updatedLines += $line
                }
            }

            if ($changeCount -gt 0) {
                # Output verbose information about changes
                foreach ($change in $changes) {
                    Write-Verbose "Line $($change.LineNumber):"
                    Write-Verbose "  Original: $($change.Original)"
                    Write-Verbose "  New:      $($change.New)"
                }

                $shouldProcessMessage = "Update $changeCount table separator line(s)"
                if ($PSCmdlet.ShouldProcess($file.FullName, $shouldProcessMessage)) {
                    [System.IO.File]::WriteAllText($file.FullName, ($updatedLines -join "`r`n"), $utf8NoBom)
                    Write-Output "$($file.Name): $changeCount line(s) updated"
                }
            } else {
                Write-Output "$($file.Name): Nothing to update"
            }
        }
    }

    end {
        Write-Verbose 'Markdown table separator formatting process completed'
    }
}

function Format-TableSeparatorLine {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Line
    )

    # Split the line by pipe, process each cell, and rejoin
    # The line starts and ends with |, so first and last elements after split are empty
    $parts = $Line -split '\|'
    $formattedParts = @()

    for ($i = 0; $i -lt $parts.Count; $i++) {
        $cell = $parts[$i]

        # Skip empty parts (before first | and after last |)
        if ($i -eq 0 -or $i -eq ($parts.Count - 1)) {
            $formattedParts += $cell
            continue
        }

        # Check if cell contains dashes (is a separator cell)
        # Matches: optional spaces, optional colon, one or more dashes, optional colon, optional spaces
        if ($cell -match '^(\s*)(:?)(-+)(:?)(\s*)$') {
            $leadingColon = $Matches[2]
            $trailingColon = $Matches[4]

            $totalLength = $cell.Length

            # Target structure: <space> [colon] <dashes> [colon] <space>
            # Calculate overhead: 1 space each side, plus colons if present
            $colonOverhead = $leadingColon.Length + $trailingColon.Length
            $spaceOverhead = 2  # 1 space on each side
            $overhead = $colonOverhead + $spaceOverhead

            # Calculate required dash count to maintain cell width
            $requiredDashes = $totalLength - $overhead

            if ($requiredDashes -ge 1) {
                # Build standardized cell: space + colon? + dashes + colon? + space
                $newCell = ' '
                if ($leadingColon) { $newCell += $leadingColon }
                $newCell += '-' * $requiredDashes
                if ($trailingColon) { $newCell += $trailingColon }
                $newCell += ' '

                $formattedParts += $newCell
            } else {
                # Can't fit at least 1 dash with proper spacing, keep original
                $formattedParts += $cell
            }
        } else {
            # Not a standard separator cell, keep as-is
            $formattedParts += $cell
        }
    }

    return $formattedParts -join '|'
}

# If running as a script, execute with provided arguments
if ($MyInvocation.InvocationName -ne '.') {
    if ($args.Count -gt 0) {
        Normalize-TableSeparators @args
    } else {
        Get-Help Normalize-TableSeparators -Detailed
    }
}
