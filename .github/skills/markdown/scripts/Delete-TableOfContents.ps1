<#
.SYNOPSIS
    Removes all Tables of Contents from markdown files.

.DESCRIPTION
    Scans a markdown file (or all markdown files in a directory) and removes every
    TOC block delimited by <!-- toc:start --> and <!-- toc:end -->, including the
    blank line that immediately precedes each block (if present).

.PARAMETER Path
    The path to a markdown file (.md) or a directory containing markdown files.
    Accepts absolute or relative paths.

.PARAMETER Recurse
    When Path is a directory, processes markdown files in all subdirectories.
    By default, only markdown files in the specified directory are processed.
    This parameter is ignored when Path is a file.

.PARAMETER OutputPath
    Optional output file path. If not specified, the input file is modified in-place.
    Mutually exclusive with -OutputConsole. Only valid when Path is a single file.

.PARAMETER OutputConsole
    Output the entire modified markdown to stdout instead of writing to a file.
    Mutually exclusive with -OutputPath. Only valid when Path is a single file.

.EXAMPLE
    Delete-TableOfContents -Path .\docs\README.md
    Removes all TOC blocks from the specified file.

.EXAMPLE
    Delete-TableOfContents -Path .\docs -Recurse
    Removes all TOC blocks from every markdown file under docs and its subdirectories.

.EXAMPLE
    Delete-TableOfContents -Path .\docs -WhatIf
    Shows what changes would be made without modifying any files.

.OUTPUTS
    Reports each file that was modified.

.NOTES
    Version: 1.0.0
    TOC markers: <!-- toc:start --> and <!-- toc:end -->
#>
function Delete-TableOfContents {
    [CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
    param(
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [Alias('Input', 'In', 'i', 'FullName')]
        [ValidateNotNullOrEmpty()]
        [string]$Path,

        [Parameter()]
        [switch]$Recurse,

        [Parameter()]
        [ValidateScript({
            if ($_ -and $PSBoundParameters.ContainsKey('OutputConsole')) {
                throw 'OutputPath and OutputConsole are mutually exclusive.'
            }
            $true
        })]
        [string]$OutputPath,

        [Parameter()]
        [ValidateScript({
            if ($_ -and $PSBoundParameters.ContainsKey('OutputPath')) {
                throw 'OutputConsole and OutputPath are mutually exclusive.'
            }
            $true
        })]
        [switch]$OutputConsole
    )

    begin {
        Write-Verbose 'Starting Delete-TableOfContents'

        # Remove all TOC blocks from a line list.
        # Returns the number of blocks removed.
        function Remove-AllTocBlocks {
            param([System.Collections.ArrayList]$Lines)

            $blocksRemoved = 0

            # Scan from the end so that index removal does not affect earlier positions.
            for ($i = $Lines.Count - 1; $i -ge 0; $i--) {
                if ($Lines[$i] -match '<!--\s*toc:end\s*-->') {
                    $endIndex = $i

                    # Walk backwards to find the matching toc:start
                    $startIndex = -1
                    for ($j = $endIndex - 1; $j -ge 0; $j--) {
                        if ($Lines[$j] -match '<!--\s*toc:start\s*-->') {
                            $startIndex = $j
                            break
                        }
                    }

                    if ($startIndex -lt 0) {
                        Write-Warning "Found <!-- toc:end --> at line $($endIndex + 1) without a matching <!-- toc:start -->. Skipping."
                        continue
                    }

                    # Also remove a blank line immediately preceding the block.
                    $removeFrom = $startIndex
                    if ($removeFrom -gt 0 -and $Lines[$removeFrom - 1] -match '^\s*$') {
                        $removeFrom--
                    }

                    $removeCount = $endIndex - $removeFrom + 1
                    $Lines.RemoveRange($removeFrom, $removeCount)
                    $blocksRemoved++
                    Write-Verbose "Removed TOC block ($removeCount lines, toc:start was at line $($startIndex + 1))"

                    # Restart search from just before the removed region.
                    $i = $removeFrom
                }
            }

            return $blocksRemoved
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
            if ($OutputPath -or $OutputConsole.IsPresent) {
                Write-Warning '-OutputPath and -OutputConsole are only valid when Path is a single file. Ignoring.'
            }
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
            Write-Verbose "Processing file: $($file.FullName)"

            $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
            $lines = [System.Collections.ArrayList]::new()
            $lines.AddRange(($content -split '\r?\n'))

            Write-Verbose "File has $($lines.Count) lines"

            $blocksFound = ($content | Select-String -Pattern '<!--\s*toc:start\s*-->' -AllMatches).Matches.Count

            if ($blocksFound -eq 0) {
                Write-Verbose "No TOC blocks found in $($file.Name) — skipping"
                continue
            }

            $shouldProcessMessage = "Remove $blocksFound TOC block(s) from '$($file.Name)'"
            if (-not $PSCmdlet.ShouldProcess($file.FullName, $shouldProcessMessage)) {
                continue
            }

            $removed = Remove-AllTocBlocks -Lines $lines

            $finalContent = $lines -join "`r`n"

            if ($markdownFiles.Count -eq 1 -and $OutputConsole.IsPresent) {
                Write-Output $finalContent
            } else {
                $targetPath = if ($markdownFiles.Count -eq 1 -and $OutputPath) { $OutputPath } else { $file.FullName }

                if ($PSCmdlet.ShouldProcess($targetPath, 'Save markdown file')) {
                    [System.IO.File]::WriteAllText($targetPath, $finalContent, [System.Text.UTF8Encoding]::new($false))
                    Write-Output "$($file.Name): removed $removed TOC block(s)"
                }
            }
        }
    }

    end {
        Write-Verbose 'Completed Delete-TableOfContents'
    }
}

# If running as a script, execute with provided arguments
if ($MyInvocation.InvocationName -ne '.') {
    if ($args.Count -gt 0) {
        Delete-TableOfContents @args
    } else {
        Get-Help Delete-TableOfContents -Detailed
    }
}
