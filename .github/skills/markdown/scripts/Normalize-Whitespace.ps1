<#
.SYNOPSIS
    Normalizes whitespace in Markdown files.

.DESCRIPTION
    Normalizes whitespace by:
    - Trimming trailing whitespace from every line
    - Removing whitespace-only lines (except directly adjacent to headings and anchors)
    - Collapsing multiple consecutive blank lines into single blank lines
    - Preserving content inside fenced code blocks (``` or ~~~)

    Blank lines are preserved directly adjacent to:
    - Markdown headings (ATX: #..######)
    - Anchor-only lines of the form: <a id="..."></a>

    Inline anchors (e.g., in list items) do NOT count.

    The output is written as UTF-8 (no BOM) with CRLF line endings and ends with exactly one newline.
    Empty files are left untouched.

.PARAMETER Path
    The path to a markdown file (.md) or a directory containing markdown files.
    Accepts absolute or relative paths.

.PARAMETER Recurse
    When Path is a directory, processes markdown files in all subdirectories.
    By default, only markdown files in the specified directory are processed.
    This parameter is ignored when Path is a file.

.EXAMPLE
    Normalize-Whitespace -Path .\docs\README.md
    Normalizes whitespace in a single markdown file.

.EXAMPLE
    Normalize-Whitespace -Path .\docs -Recurse
    Normalizes whitespace in all markdown files in docs and subdirectories.

.EXAMPLE
    Normalize-Whitespace -Path .\docs -WhatIf
    Shows what changes would be made without actually modifying files.

.OUTPUTS
    Reports the files that were processed.

.NOTES
    Author: AICMS Team
    Version: 1.2.0
    Designed for repo anchor conventions created by scripts/AddAnchors.ps1.
#>
function Normalize-Whitespace {
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
        Write-Verbose 'Starting whitespace normalization process'
        Set-StrictMode -Version Latest

        $headingRegex = '^\s{0,3}#{1,6}\s+\S'
        $anchorOnlyRegex = '^\s*<a\s+id="[^"]+"\s*></a>\s*$'
        $fenceRegex = '^\s*(```|~~~)'
        $blankRegex = '^\s*$'

        $utf8NoBom = [System.Text.UTF8Encoding]::new($false)

        function Test-IsProtectedLine {
            param([Parameter(Mandatory = $true)][AllowEmptyString()][string] $Line)
            return ($Line -match $headingRegex) -or ($Line -match $anchorOnlyRegex)
        }

        function Test-IsFenceLine {
            param([Parameter(Mandatory = $true)][AllowEmptyString()][string] $Line)
            return $Line -match $fenceRegex
        }
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

            $raw = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8

            # Skip empty files
            if ([string]::IsNullOrEmpty($raw)) {
                Write-Verbose "File is empty: $($file.FullName)"
                continue
            }

            # Split into lines (preserve trailing empty element if present).
            $lines = $raw -split "\r?\n", -1

            # PASS 0: Trim trailing whitespace from all lines
            $lines = @($lines | ForEach-Object {
                $_.TrimEnd()
            })

            # PASS 1: Replace whitespace-only lines with empty lines
            $lines = @($lines | ForEach-Object {
                if ($_ -match $blankRegex) {
                    ''
                } else {
                    $_
                }
            })

            $output = [System.Collections.Generic.List[string]]::new()
            $inFence = $false
            $pendingBlankCount = 0
            $prevIsProtected = $false

            # PASS 2: Collapse multiple blank lines and preserve structure
            foreach ($line in $lines) {
                # Inside code fence: preserve as-is.
                if ($inFence) {
                    $output.Add($line)
                    if (Test-IsFenceLine -Line $line) {
                        $inFence = $false
                    }
                    continue
                }

                # Fence start (simple toggle)
                if (Test-IsFenceLine -Line $line) {
                    if ($pendingBlankCount -gt 0) {
                        if ($prevIsProtected) {
                            $output.Add('')
                        }
                        $pendingBlankCount = 0
                    }

                    $output.Add($line)
                    $inFence = $true
                    $prevIsProtected = $false
                    continue
                }

                # Blank line (outside fence)
                if ($line -match $blankRegex) {
                    $pendingBlankCount++
                    continue
                }

                # Non-blank line (outside fence)
                $curIsProtected = Test-IsProtectedLine -Line $line

                if ($pendingBlankCount -gt 0) {
                    if ($prevIsProtected -or $curIsProtected) {
                        # Keep exactly one blank line adjacent to protected lines.
                        $output.Add('')
                    }
                    $pendingBlankCount = 0
                }

                $output.Add($line)
                $prevIsProtected = $curIsProtected
            }

            if ($inFence) {
                Write-Warning "Unclosed code fence detected in '$($file.FullName)'. Output may be unexpected."
            }

            # PASS 3: Remove all trailing blank lines
            while ($output.Count -gt 0 -and ($output[$output.Count - 1] -match $blankRegex)) {
                $output.RemoveAt($output.Count - 1)
            }

            # PASS 4: Ensure file ends with <non-whitespace><newline>
            if ($output.Count -gt 0) {
                $normalized = $output -join "`r`n"
                if (-not $normalized.EndsWith("`r`n")) {
                    $normalized += "`r`n"
                }
            } else {
                # Should not happen due to earlier empty check, but be safe
                continue
            }

            if ($PSCmdlet.ShouldProcess($file.FullName, 'Normalize whitespace')) {
                [System.IO.File]::WriteAllText($file.FullName, $normalized, $utf8NoBom)
                Write-Output "$($file.Name): Normalized"
            }
        }
    }

    end {
        Write-Verbose 'Whitespace normalization process completed'
    }
}

# If running as a script, execute with provided arguments
if ($MyInvocation.InvocationName -ne '.') {
    if ($args.Count -gt 0) {
        Normalize-Whitespace @args
    } else {
        Get-Help Normalize-Whitespace -Detailed
    }
}
