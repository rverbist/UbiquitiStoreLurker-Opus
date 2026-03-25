<#
.SYNOPSIS
    Normalizes code fence formatting in Markdown files.

.DESCRIPTION
    Scans for markdown code fences and verifies they are balanced (every opening
    fence has a corresponding closing fence). Also performs cleanup:
    - If language is omitted on opening fence, replaces with "```code"
    - Trims trailing whitespace from fence lines

    Opening fence: ``` optionally followed by language (no spaces), then optional whitespace
    Closing fence: ``` followed by optional whitespace only

.PARAMETER Path
    The path to a markdown file (.md) or a directory containing markdown files.
    Accepts absolute or relative paths.

.PARAMETER Recurse
    When Path is a directory, processes markdown files in all subdirectories.
    By default, only markdown files in the specified directory are processed.
    This parameter is ignored when Path is a file.

.EXAMPLE
    Normalize-CodeFences -Path .\docs\README.md
    Normalizes code fences in a single markdown file.

.EXAMPLE
    Normalize-CodeFences -Path .\docs -Recurse
    Normalizes code fences in all markdown files in docs and subdirectories.

.EXAMPLE
    Normalize-CodeFences -Path .\docs -WhatIf
    Shows what changes would be made without actually modifying files.

.OUTPUTS
    Reports the files that were processed.

.NOTES
    Author: AICMS Team
    Version: 1.0.0
#>
function Normalize-CodeFences {
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
        Write-Verbose 'Starting code fence normalization process'
        Set-StrictMode -Version Latest

        # Opening fence: ``` optionally followed by language (no spaces), then optional whitespace
        $openingFenceRegex = '^```(\S*)\s*$'
        # Closing fence: just ``` followed by optional whitespace
        $closingFenceRegex = '^```\s*$'

        $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    }

    process {
        # Resolve the path
        $resolvedPath = $null
        try {
            $resolvedPath = Resolve-Path -Path $Path -ErrorAction Stop
        }
        catch {
            $errorRecord = [System.Management.Automation.ErrorRecord]::new(
                [System.IO.FileNotFoundException]::new("Path not found: $Path"),
                'PathNotFound',
                [System.Management.Automation.ErrorCategory]::ObjectNotFound,
                $Path
            )
            $PSCmdlet.WriteError($errorRecord)
            return
        }

        # Get list of markdown files
        $markdownFiles = @()
        $pathItem = Get-Item -Path $resolvedPath

        if ($pathItem.PSIsContainer) {
            Write-Verbose "Processing directory: $resolvedPath"
            if ($Recurse.IsPresent) {
                Write-Verbose 'Recursing into subdirectories'
                $markdownFiles = @(Get-ChildItem -Path $resolvedPath -Filter '*.md' -Recurse -File)
            }
            else {
                $markdownFiles = @(Get-ChildItem -Path $resolvedPath -Filter '*.md' -File)
            }
            if ($markdownFiles.Count -eq 0) {
                Write-Warning "No markdown files found in directory: $resolvedPath"
                return
            }
        }
        elseif ($pathItem.Extension -eq '.md') {
            Write-Verbose "Processing single file: $resolvedPath"
            if ($Recurse.IsPresent) {
                Write-Warning '-Recurse parameter is ignored when Path is a file'
            }
            $markdownFiles = @($pathItem)
        }
        else {
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

            $raw = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
            if ([string]::IsNullOrEmpty($raw)) {
                Write-Verbose "File is empty: $($file.FullName)"
                continue
            }

            $lines = $raw -split "\r?\n"

            $output = [System.Collections.Generic.List[string]]::new()
            $inFence = $false
            $fenceStartLine = 0
            $changeCount = 0

            for ($i = 0; $i -lt $lines.Count; $i++) {
                $line = $lines[$i]
                $lineNum = $i + 1

                if (-not $inFence) {
                    # Check for opening fence
                    if ($line -match $openingFenceRegex) {
                        $language = $Matches[1]
                        $inFence = $true
                        $fenceStartLine = $lineNum

                        # Determine the normalized line
                        if ([string]::IsNullOrEmpty($language)) {
                            # No language specified, add default
                            $normalizedLine = '```code'
                            if ($line -ne $normalizedLine) {
                                Write-Verbose "Line ${lineNum}: Adding default language 'code'"
                                $changeCount++
                            }
                        }
                        else {
                            # Has language, just ensure no trailing whitespace
                            $normalizedLine = '```' + $language
                            if ($line -ne $normalizedLine) {
                                Write-Verbose "Line ${lineNum}: Trimming trailing whitespace from opening fence"
                                $changeCount++
                            }
                        }
                        $output.Add($normalizedLine)
                    }
                    else {
                        $output.Add($line)
                    }
                }
                else {
                    # Inside fence, check for closing
                    if ($line -match $closingFenceRegex) {
                        $inFence = $false
                        $normalizedLine = '```'
                        if ($line -ne $normalizedLine) {
                            Write-Verbose "Line ${lineNum}: Trimming trailing whitespace from closing fence"
                            $changeCount++
                        }
                        $output.Add($normalizedLine)
                    }
                    else {
                        # Content inside fence, preserve as-is
                        $output.Add($line)
                    }
                }
            }

            # Check for unclosed fence
            if ($inFence) {
                Write-Warning "Unclosed code fence in '$($file.FullName)' starting at line $fenceStartLine"
            }

            if ($changeCount -eq 0) {
                Write-Output "$($file.Name): Nothing to update"
                continue
            }

            $normalized = $output -join "`r`n"

            if ($PSCmdlet.ShouldProcess($file.FullName, "Normalize $changeCount code fence(s)")) {
                [System.IO.File]::WriteAllText($file.FullName, $normalized, $utf8NoBom)
                Write-Output "$($file.Name): $changeCount change(s) made"
            }
        }
    }

    end {
        Write-Verbose 'Code fence normalization process completed'
    }
}

# If running as a script, execute with provided arguments
if ($MyInvocation.InvocationName -ne '.') {
    if ($args.Count -gt 0) {
        Normalize-CodeFences @args
    }
    else {
        Get-Help Normalize-CodeFences -Detailed
    }
}
