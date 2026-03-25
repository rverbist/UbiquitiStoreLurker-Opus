<#
.SYNOPSIS
    Fixes double-encoded UTF-8 (mojibake) characters in markdown files.

.DESCRIPTION
    Replaces common mojibake sequences with their correct Unicode or ASCII equivalents.
    Handles patterns like em-dash, en-dash, quotes, multiplication signs, non-breaking spaces,
    corrupted citations, arrows, and other common double-encoding issues.

.PARAMETER Path
    The path to a markdown file (.md) or a directory containing markdown files.
    Accepts absolute or relative paths.

.PARAMETER Recurse
    When Path is a directory, processes markdown files in all subdirectories.
    By default, only markdown files in the specified directory are processed.
    This parameter is ignored when Path is a file.

.EXAMPLE
    Fix-Mojibake -Path .\docs\README.md
    Fixes mojibake characters in a single markdown file.

.EXAMPLE
    Fix-Mojibake -Path .\docs -Recurse
    Fixes mojibake characters in all markdown files in docs and subdirectories.

.EXAMPLE
    Fix-Mojibake -Path .\docs -WhatIf
    Shows what changes would be made without actually modifying files.

.OUTPUTS
    Reports the number of replacements made per file.

.NOTES
    Author: AICMS Team
    Version: 2.0.0
    Updated: 2026-02-11 - Enhanced with comprehensive mojibake pattern coverage
#>
function Fix-Mojibake {
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
        Write-Verbose 'Starting mojibake fix process'

        # Build mojibake patterns using Unicode character codes to avoid script corruption
        # Pattern format: @{ Pattern = 'corrupted text'; Replacement = 'correct text'; IsRegex = $bool }
        $script:replacements = @(
            # Complex multibyte corruptions (CJK-like citation patterns) - regex
            @{ 
                Pattern = "$([char]0x00E3)$([char]0x20AC)$([char]0x0090)\d+$([char]0x00E2)$([char]0x20AC)\s+L\d+-L\d+$([char]0x00E3)$([char]0x20AC)$([char]0x2018)"
                Replacement = ''
                IsRegex = $true
                Description = 'Corrupted citation markers'
            }
            
            # Double-mojibake multiplication sign: Ã— (Ã + em-dash) -> ×
            @{ 
                Pattern = "$([char]0x00C3)$([char]0x2014)"
                Replacement = "$([char]0x00D7)"
                IsRegex = $false
                Description = 'Multiplication sign (double-mojibake)'
            }
            
            # Alternative em-dash mojibake: â€" (variant 1)
            @{ 
                Pattern = "$([char]0x00E2)$([char]0x20AC)$([char]0x201C)"
                Replacement = "$([char]0x2014)"
                IsRegex = $false
                Description = 'Em-dash alt 1'
            }
            
            # Alternative em-dash mojibake: â€" (variant 2)
            @{ 
                Pattern = "$([char]0x00C3)$([char]0x00A2)$([char]0x00E2)$([char]0x0080)$([char]0x201C)"
                Replacement = "$([char]0x2014)"
                IsRegex = $false
                Description = 'Em-dash alt 2'
            }
            
            # Em-dash mojibake: â€" -> —
            @{ 
                Pattern = "$([char]0x00E2)$([char]0x20AC)$([char]0x201D)"
                Replacement = "$([char]0x2014)"
                IsRegex = $false
                Description = 'Em-dash'
            }
            
            # En-dash mojibake: â€' -> –
            @{ 
                Pattern = "$([char]0x00E2)$([char]0x20AC)$([char]0x2018)"
                Replacement = "$([char]0x2013)"
                IsRegex = $false
                Description = 'En-dash'
            }
            
            # Right single quote mojibake: â€™ -> '
            @{ 
                Pattern = "$([char]0x00E2)$([char]0x20AC)$([char]0x2122)"
                Replacement = "$([char]0x2019)"
                IsRegex = $false
                Description = 'Right single quote'
            }
            
            # Left double quote mojibake: â€œ -> "
            @{ 
                Pattern = "$([char]0x00E2)$([char]0x20AC)$([char]0x0153)"
                Replacement = "$([char]0x201C)"
                IsRegex = $false
                Description = 'Left double quote'
            }
            
            # Right double quote mojibake: â€ -> "
            @{ 
                Pattern = "$([char]0x00E2)$([char]0x20AC)$([char]0x009D)"
                Replacement = "$([char]0x201D)"
                IsRegex = $false
                Description = 'Right double quote'
            }
            
            # Multiplication sign mojibake: Ã— -> ×
            @{ 
                Pattern = "$([char]0x00C3)$([char]0x0097)"
                Replacement = "$([char]0x00D7)"
                IsRegex = $false
                Description = 'Multiplication sign'
            }
            
            # Non-breaking space before regular space: Â  -> (single space)
            @{ 
                Pattern = "$([char]0x00C2)$([char]0x00A0) "
                Replacement = ' '
                IsRegex = $false
                Description = 'NBSP before space'
            }
            
            # Standalone Â before digit (regex): Â2 -> (space)2
            @{ 
                Pattern = "$([char]0x00C2)(\d)"
                Replacement = ' $1'
                IsRegex = $true
                Description = 'Standalone NBSP before digit'
            }
            
            # Standalone Â in other contexts
            @{ 
                Pattern = "$([char]0x00C2)"
                Replacement = ' '
                IsRegex = $false
                Description = 'Standalone NBSP'
            }
            
            # Arrow right mojibake: â†' -> →
            @{ 
                Pattern = "$([char]0x00E2)$([char]0x2020)$([char]0x2019)"
                Replacement = "$([char]0x2192)"
                IsRegex = $false
                Description = 'Right arrow'
            }
            
            # Checkmark mojibake: âœ… -> ✅
            @{ 
                Pattern = "$([char]0x00E2)$([char]0x0153)$([char]0x0160)"
                Replacement = "$([char]0x2705)"
                IsRegex = $false
                Description = 'Checkmark'
            }
            
            # Ellipsis mojibake: â€¦ -> …
            @{ 
                Pattern = "$([char]0x00E2)$([char]0x20AC)$([char]0x00A6)"
                Replacement = "$([char]0x2026)"
                IsRegex = $false
                Description = 'Ellipsis'
            }
            
            # Right-pointing angle quote: â€º -> ›
            @{ 
                Pattern = "$([char]0x00E2)$([char]0x20AC)$([char]0x00BA)"
                Replacement = "$([char]0x203A)"
                IsRegex = $false
                Description = 'Right angle quote'
            }
            
            # Left-pointing angle quote: â€¹ -> ‹
            @{ 
                Pattern = "$([char]0x00E2)$([char]0x20AC)$([char]0x00B9)"
                Replacement = "$([char]0x2039)"
                IsRegex = $false
                Description = 'Left angle quote'
            }
            
            # Latin a with grave: Ã  -> à
            @{ 
                Pattern = "$([char]0x00C3)$([char]0x00A0)"
                Replacement = "$([char]0x00E0)"
                IsRegex = $false
                Description = 'a with grave'
            }
            
            # Latin e with acute: Ã© -> é
            @{ 
                Pattern = "$([char]0x00C3)$([char]0x00A9)"
                Replacement = "$([char]0x00E9)"
                IsRegex = $false
                Description = 'e with acute'
            }
            
            # Latin e with grave: Ã¨ -> è
            @{ 
                Pattern = "$([char]0x00C3)$([char]0x00A8)"
                Replacement = "$([char]0x00E8)"
                IsRegex = $false
                Description = 'e with grave'
            }
        )
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
            $content = [System.IO.File]::ReadAllText($file.FullName, [System.Text.UTF8Encoding]::new($false))
            $changes = 0
            $replacementDetails = @()

            foreach ($r in $script:replacements) {
                if ($r.IsRegex) {
                    # Use regex replacement
                    $matches = [regex]::Matches($content, $r.Pattern)
                    if ($matches.Count -gt 0) {
                        $content = [regex]::Replace($content, $r.Pattern, $r.Replacement)
                        $changes += $matches.Count
                        $replacementDetails += "$($r.Description): $($matches.Count)"
                        Write-Verbose "  $($r.Description) (regex): $($matches.Count) match(es)"
                    }
                } else {
                    # Use literal string replacement
                    $pattern = [regex]::Escape($r.Pattern)
                    $matches = [regex]::Matches($content, $pattern)
                    if ($matches.Count -gt 0) {
                        $content = $content.Replace($r.Pattern, $r.Replacement)
                        $changes += $matches.Count
                        $replacementDetails += "$($r.Description): $($matches.Count)"
                        Write-Verbose "  $($r.Description): $($matches.Count) replacement(s)"
                    }
                }
            }

            if ($changes -gt 0) {
                $shouldProcessMessage = "Fix $changes mojibake sequence(s): $($replacementDetails -join ', ')"
                if ($PSCmdlet.ShouldProcess($file.FullName, $shouldProcessMessage)) {
                    [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
                    Write-Output "$($file.Name): $changes replacement(s) made ($($replacementDetails -join '; '))"
                }
            } else {
                Write-Output "$($file.Name): Nothing to update"
            }
        }
    }

    end {
        Write-Verbose 'Mojibake fix process completed'
    }
}

# If running as a script, execute with provided arguments
if ($MyInvocation.InvocationName -ne '.') {
    if ($args.Count -gt 0) {
        Fix-Mojibake @args
    } else {
        Get-Help Fix-Mojibake -Detailed
    }
}
