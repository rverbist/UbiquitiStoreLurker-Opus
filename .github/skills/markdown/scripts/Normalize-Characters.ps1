<#
.SYNOPSIS
    Normalizes special Unicode characters in markdown files to ASCII equivalents.

.DESCRIPTION
    Processes markdown files to replace special Unicode characters (curly quotes,
    em/en dashes, non-breaking spaces, arrows, math symbols, list markers, ellipsis)
    with their ASCII equivalents. Supports category-based inclusion/exclusion of
    replacements. Can also warn about problematic characters (surrogates, corrupted).

.PARAMETER Path
    The path to a markdown file (.md) or a directory containing markdown files.
    Accepts absolute or relative paths.

.PARAMETER Recurse
    When Path is a directory, processes markdown files in all subdirectories.
    By default, only markdown files in the specified directory are processed.

.PARAMETER Backup
    Creates a backup copy of each file before modification (.bak extension appended).

.PARAMETER Warn
    Detects and reports problematic characters (surrogates, corrupted, undefined)
    as warnings. Outputs file, line, column, character, and context.

.PARAMETER ErrorOnProblematic
    Detects and reports problematic characters (surrogates, corrupted, undefined)
    as non-terminating errors. Use with $ErrorActionPreference or -ErrorAction to
    control behavior. Outputs file, line, column, character, and context.

.PARAMETER Inverse
    Flips the default behavior from "replace all unless preserved" to
    "preserve all unless explicitly replaced".

.PARAMETER PreserveDashes
    Do not replace en dash, em dash, and non-breaking hyphen with regular hyphen.

.PARAMETER PreserveSpaces
    Do not replace non-breaking space with regular space.

.PARAMETER PreserveQuotes
    Do not replace curly quotes with straight quotes.

.PARAMETER PreserveArrows
    Do not replace arrow symbols with ASCII arrow notation.

.PARAMETER PreserveMath
    Do not replace math/comparison symbols with ASCII equivalents.

.PARAMETER PreserveLists
    Do not replace bullet/list markers with ASCII equivalents.

.PARAMETER PreserveEllipsis
    Do not replace ellipsis character with triple dots.

.PARAMETER ReplaceDashes
    Replace en dash, em dash, and non-breaking hyphen with regular hyphen.
    Only valid when -Inverse is specified.

.PARAMETER ReplaceSpaces
    Replace non-breaking space with regular space.
    Only valid when -Inverse is specified.

.PARAMETER ReplaceQuotes
    Replace curly quotes with straight quotes.
    Only valid when -Inverse is specified.

.PARAMETER ReplaceArrows
    Replace arrow symbols with ASCII arrow notation.
    Only valid when -Inverse is specified.

.PARAMETER ReplaceMath
    Replace math/comparison symbols with ASCII equivalents.
    Only valid when -Inverse is specified.

.PARAMETER ReplaceLists
    Replace bullet/list markers with ASCII equivalents.
    Only valid when -Inverse is specified.

.PARAMETER ReplaceEllipsis
    Replace ellipsis character with triple dots.
    Only valid when -Inverse is specified.

.EXAMPLE
    Normalize-Characters -Path .\docs\README.md
    Normalizes all special characters in a single markdown file.

.EXAMPLE
    Normalize-Characters -Path .\docs -Recurse -PreserveArrows
    Normalizes all special characters except arrows in all markdown files.

.EXAMPLE
    Normalize-Characters -Path .\docs -Inverse -ReplaceQuotes -ReplaceSpaces
    Only replaces curly quotes and non-breaking spaces, preserving everything else.

.EXAMPLE
    Normalize-Characters -Path .\docs -Recurse -Warn
    Scans for problematic characters (surrogates, undefined) and reports as warnings.

.EXAMPLE
    Normalize-Characters -Path .\docs -Recurse -ErrorOnProblematic -ErrorAction Stop
    Scans for problematic characters and stops on first error found.

.EXAMPLE
    Normalize-Characters -Path .\docs -Recurse -ErrorOnProblematic -ErrorAction Continue
    Scans for problematic characters, reports all as errors, continues processing.

.EXAMPLE
    Normalize-Characters -Path .\docs\README.md -Backup -WhatIf
    Shows what changes would be made without modifying files.

.OUTPUTS
    Reports the number of replacements made per file.

.NOTES
    Author: AICMS Team
    Version: 1.1.0
#>
function Normalize-Characters {
    [CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
    param(
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [Alias('Input', 'In', 'i', 'FullName')]
        [ValidateNotNullOrEmpty()]
        [string]$Path,

        [Parameter()]
        [switch]$Recurse,

        [Parameter()]
        [switch]$Backup,

        [Parameter()]
        [switch]$Inverse,

        [Parameter()]
        [switch]$Warn,

        [Parameter()]
        [switch]$ErrorOnProblematic,

        # Preserve switches (default mode: replace all)
        [Parameter()]
        [switch]$PreserveDashes,

        [Parameter()]
        [switch]$PreserveSpaces,

        [Parameter()]
        [switch]$PreserveQuotes,

        [Parameter()]
        [switch]$PreserveArrows,

        [Parameter()]
        [switch]$PreserveMath,

        [Parameter()]
        [switch]$PreserveLists,

        [Parameter()]
        [switch]$PreserveEllipsis,

        # Replace switches (inverse mode: preserve all)
        [Parameter()]
        [switch]$ReplaceDashes,

        [Parameter()]
        [switch]$ReplaceSpaces,

        [Parameter()]
        [switch]$ReplaceQuotes,

        [Parameter()]
        [switch]$ReplaceArrows,

        [Parameter()]
        [switch]$ReplaceMath,

        [Parameter()]
        [switch]$ReplaceLists,

        [Parameter()]
        [switch]$ReplaceEllipsis
    )

    begin {
        Write-Verbose 'Starting ASCII markdown conversion process'

        #region Replacement Categories Definition
        # Centralized definition of all replacement categories and their character mappings
        $script:ReplacementCategories = @{
            Dashes = @{
                Name = 'Dashes'
                Description = 'En dash, em dash, non-breaking hyphen, Unicode hyphen to regular hyphen'
                Replacements = @(
                    @{ From = [char]0x2013; To = '-'; Name = 'En dash' }
                    @{ From = [char]0x2014; To = '-'; Name = 'Em dash' }
                    @{ From = [char]0x2011; To = '-'; Name = 'Non-breaking hyphen' }
                    @{ From = [char]0x2010; To = '-'; Name = 'Unicode hyphen' }
                )
            }
            Spaces = @{
                Name = 'Spaces'
                Description = 'Non-breaking space and narrow non-breaking space to regular space'
                Replacements = @(
                    @{ From = [char]0x00A0; To = ' '; Name = 'Non-breaking space' }
                    @{ From = [char]0x202F; To = ' '; Name = 'Narrow non-breaking space' }
                )
            }
            Quotes = @{
                Name = 'Quotes'
                Description = 'Curly quotes to straight quotes'
                Replacements = @(
                    @{ From = [char]0x201C; To = '"'; Name = 'Left double quote' }
                    @{ From = [char]0x201D; To = '"'; Name = 'Right double quote' }
                    @{ From = [char]0x2018; To = "'"; Name = 'Left single quote' }
                    @{ From = [char]0x2019; To = "'"; Name = 'Right single quote' }
                )
            }
            Arrows = @{
                Name = 'Arrows'
                Description = 'Arrow symbols to ASCII notation'
                Replacements = @(
                    @{ From = [char]0x2192; To = '->'; Name = 'Right arrow' }
                    @{ From = [char]0x2190; To = '<-'; Name = 'Left arrow' }
                    @{ From = [char]0x2194; To = '<->'; Name = 'Left-right arrow' }
                    @{ From = [char]0x21D2; To = '=>'; Name = 'Double right arrow' }
                    @{ From = [char]0x21D0; To = '<='; Name = 'Double left arrow' }
                    @{ From = [char]0x21D4; To = '<=>'; Name = 'Double left-right arrow' }
                    @{ From = [char]0x2191; To = '^'; Name = 'Up arrow' }
                    @{ From = [char]0x2193; To = 'v'; Name = 'Down arrow' }
                )
            }
            Math = @{
                Name = 'Math'
                Description = 'Math and comparison symbols to ASCII notation'
                Replacements = @(
                    @{ From = [char]0x2264; To = '<='; Name = 'Less than or equal' }
                    @{ From = [char]0x2265; To = '>='; Name = 'Greater than or equal' }
                    @{ From = [char]0x2260; To = '!='; Name = 'Not equal' }
                    @{ From = [char]0x2261; To = '=='; Name = 'Identical/congruent' }
                    @{ From = [char]0x2262; To = '!=='; Name = 'Not identical' }
                    @{ From = [char]0x226A; To = '<<'; Name = 'Much less than' }
                    @{ From = [char]0x226B; To = '>>'; Name = 'Much greater than' }
                    @{ From = [char]0x2248; To = '~='; Name = 'Approximately equal' }
                    @{ From = [char]0x00D7; To = 'x'; Name = 'Multiplication sign' }
                    @{ From = [char]0x00B1; To = '+/-'; Name = 'Plus-minus sign' }
                )
            }
            Lists = @{
                Name = 'Lists'
                Description = 'Bullet and list markers to ASCII'
                Replacements = @(
                    @{ From = [char]0x2022; To = '-'; Name = 'Bullet' }
                    @{ From = [char]0x2023; To = '>'; Name = 'Triangular bullet' }
                    @{ From = [char]0x2043; To = '-'; Name = 'Hyphen bullet' }
                    @{ From = [char]0x25E6; To = 'o'; Name = 'White bullet' }
                    @{ From = [char]0x25AA; To = '-'; Name = 'Black small square' }
                    @{ From = [char]0x25AB; To = '-'; Name = 'White small square' }
                )
            }
            Ellipsis = @{
                Name = 'Ellipsis'
                Description = 'Ellipsis to triple dots'
                Replacements = @(
                    @{ From = [char]0x2026; To = '...'; Name = 'Ellipsis' }
                )
            }
        }
        #endregion

        #region Parameter Validation
        $preserveSwitches = @(
            $PreserveDashes.IsPresent,
            $PreserveSpaces.IsPresent,
            $PreserveQuotes.IsPresent,
            $PreserveArrows.IsPresent,
            $PreserveMath.IsPresent,
            $PreserveLists.IsPresent,
            $PreserveEllipsis.IsPresent
        )
        $replaceSwitches = @(
            $ReplaceDashes.IsPresent,
            $ReplaceSpaces.IsPresent,
            $ReplaceQuotes.IsPresent,
            $ReplaceArrows.IsPresent,
            $ReplaceMath.IsPresent,
            $ReplaceLists.IsPresent,
            $ReplaceEllipsis.IsPresent
        )

        $anyPreserveUsed = $preserveSwitches -contains $true
        $anyReplaceUsed = $replaceSwitches -contains $true

        if (-not $Inverse.IsPresent -and $anyReplaceUsed) {
            $errorRecord = [System.Management.Automation.ErrorRecord]::new(
                [System.ArgumentException]::new(
                    'Replace switches (-Replace*) are only valid when -Inverse is specified. ' +
                    'Default behavior is to replace all; use -Preserve* switches to exclude categories.'
                ),
                'InvalidSwitchCombination',
                [System.Management.Automation.ErrorCategory]::InvalidArgument,
                $null
            )
            $PSCmdlet.ThrowTerminatingError($errorRecord)
        }

        if ($Inverse.IsPresent -and $anyPreserveUsed) {
            $errorRecord = [System.Management.Automation.ErrorRecord]::new(
                [System.ArgumentException]::new(
                    'Preserve switches (-Preserve*) are not valid when -Inverse is specified. ' +
                    'With -Inverse, default behavior is to preserve all; use -Replace* switches to include categories.'
                ),
                'InvalidSwitchCombination',
                [System.Management.Automation.ErrorCategory]::InvalidArgument,
                $null
            )
            $PSCmdlet.ThrowTerminatingError($errorRecord)
        }
        #endregion

        #region Determine Active Categories
        $activeCategories = @{}

        if ($Inverse.IsPresent) {
            # Inverse mode: only replace if explicitly enabled
            if ($ReplaceDashes.IsPresent) { $activeCategories['Dashes'] = $true }
            if ($ReplaceSpaces.IsPresent) { $activeCategories['Spaces'] = $true }
            if ($ReplaceQuotes.IsPresent) { $activeCategories['Quotes'] = $true }
            if ($ReplaceArrows.IsPresent) { $activeCategories['Arrows'] = $true }
            if ($ReplaceMath.IsPresent) { $activeCategories['Math'] = $true }
            if ($ReplaceLists.IsPresent) { $activeCategories['Lists'] = $true }
            if ($ReplaceEllipsis.IsPresent) { $activeCategories['Ellipsis'] = $true }
        } else {
            # Default mode: replace all unless preserved
            if (-not $PreserveDashes.IsPresent) { $activeCategories['Dashes'] = $true }
            if (-not $PreserveSpaces.IsPresent) { $activeCategories['Spaces'] = $true }
            if (-not $PreserveQuotes.IsPresent) { $activeCategories['Quotes'] = $true }
            if (-not $PreserveArrows.IsPresent) { $activeCategories['Arrows'] = $true }
            if (-not $PreserveMath.IsPresent) { $activeCategories['Math'] = $true }
            if (-not $PreserveLists.IsPresent) { $activeCategories['Lists'] = $true }
            if (-not $PreserveEllipsis.IsPresent) { $activeCategories['Ellipsis'] = $true }
        }

        Write-Verbose "Active replacement categories: $($activeCategories.Keys -join ', ')"
        #endregion

        #region Build Active Replacements List
        $script:activeReplacements = @()
        foreach ($categoryName in $activeCategories.Keys) {
            $category = $script:ReplacementCategories[$categoryName]
            foreach ($replacement in $category.Replacements) {
                $script:activeReplacements += @{
                    From = $replacement.From
                    To = $replacement.To
                    Name = $replacement.Name
                    Category = $categoryName
                }
            }
        }

        if ($script:activeReplacements.Count -eq 0) {
            Write-Warning 'No replacement categories are active. No changes will be made.'
        }
        #endregion
    }

    process {
        #region Resolve Path and Get Files
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
        #endregion

        #region Process Each File
        foreach ($file in $markdownFiles) {
            Write-Verbose "Examining file: $($file.FullName)"

            $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8

            #region Detect problematic characters (Warn/ErrorOnProblematic)
            if ($Warn.IsPresent -or $ErrorOnProblematic.IsPresent) {
                $problematicCount = 0
                for ($i = 0; $i -lt $content.Length; $i++) {
                    $char = $content[$i]
                    $code = [int]$char
                    $isProblematic = $false
                    $reason = ''

                    # Check for surrogate pairs (invalid when alone)
                    if ($code -ge 0xD800 -and $code -le 0xDBFF) {
                        # High surrogate - check if followed by low surrogate
                        if ($i + 1 -ge $content.Length -or
                            [int]$content[$i + 1] -lt 0xDC00 -or
                            [int]$content[$i + 1] -gt 0xDFFF) {
                            $isProblematic = $true
                            $reason = 'Orphan high surrogate'
                        }
                    } elseif ($code -ge 0xDC00 -and $code -le 0xDFFF) {
                        # Low surrogate without preceding high surrogate
                        if ($i -eq 0 -or
                            [int]$content[$i - 1] -lt 0xD800 -or
                            [int]$content[$i - 1] -gt 0xDBFF) {
                            $isProblematic = $true
                            $reason = 'Orphan low surrogate'
                        }
                    }

                    # Check for replacement character (often indicates encoding issues)
                    if ($code -eq 0xFFFD) {
                        $isProblematic = $true
                        $reason = 'Replacement character (encoding error)'
                    }

                    # Check for null character
                    if ($code -eq 0x0000) {
                        $isProblematic = $true
                        $reason = 'Null character'
                    }

                    # Check for BOM in middle of file
                    if ($code -eq 0xFEFF -and $i -gt 0) {
                        $isProblematic = $true
                        $reason = 'BOM in middle of file'
                    }

                    # Check for private use area characters
                    if (($code -ge 0xE000 -and $code -le 0xF8FF) -or
                        ($code -ge 0xF0000 -and $code -le 0xFFFFD) -or
                        ($code -ge 0x100000 -and $code -le 0x10FFFD)) {
                        $isProblematic = $true
                        $reason = 'Private use area character'
                    }

                    # Check for non-characters
                    if (($code -ge 0xFDD0 -and $code -le 0xFDEF) -or
                        ($code -band 0xFFFF) -eq 0xFFFE -or
                        ($code -band 0xFFFF) -eq 0xFFFF) {
                        $isProblematic = $true
                        $reason = 'Unicode non-character'
                    }

                    # Check for C0 control characters (except tab, LF, CR)
                    if ($code -lt 0x0020 -and $code -ne 0x0009 -and $code -ne 0x000A -and $code -ne 0x000D) {
                        $isProblematic = $true
                        $reason = 'Control character'
                    }

                    # Check for C1 control characters
                    if ($code -ge 0x0080 -and $code -le 0x009F) {
                        $isProblematic = $true
                        $reason = 'C1 control character'
                    }

                    if ($isProblematic) {
                        $problematicCount++

                        # Calculate line and column
                        $lineNum = 1
                        $colNum = 1
                        for ($j = 0; $j -lt $i; $j++) {
                            if ($content[$j] -eq "`n") {
                                $lineNum++
                                $colNum = 1
                            } else {
                                $colNum++
                            }
                        }

                        # Get context (up to 20 chars before and after)
                        $contextStart = [Math]::Max(0, $i - 20)
                        $contextEnd = [Math]::Min($content.Length, $i + 1 + 20)
                        $beforeContext = $content.Substring($contextStart, $i - $contextStart)
                        $afterContext = $content.Substring($i + 1, $contextEnd - $i - 1)

                        # Clean up context for display
                        $beforeContext = $beforeContext -replace "`r`n", '␍␊' -replace "`n", '␊' -replace "`r", '␍'
                        $afterContext = $afterContext -replace "`r`n", '␍␊' -replace "`n", '␊' -replace "`r", '␍'

                        # Format character display (show as box if not printable)
                        $charDisplay = if ($code -lt 0x20 -or ($code -ge 0x7F -and $code -le 0x9F)) { '�' } else { $char }

                        $message = "$($file.FullName):$lineNum`:$colNum`: $reason - Char: '$charDisplay' (U+$($code.ToString('X4')))"
                        $contextMessage = "  Context: ${beforeContext}[${charDisplay}]${afterContext}"

                        if ($ErrorOnProblematic.IsPresent) {
                            $errorRecord = [System.Management.Automation.ErrorRecord]::new(
                                [System.Exception]::new("$message`n$contextMessage"),
                                'ProblematicCharacter',
                                [System.Management.Automation.ErrorCategory]::InvalidData,
                                $file.FullName
                            )
                            $PSCmdlet.WriteError($errorRecord)
                        } else {
                            Write-Warning $message
                            Write-Warning $contextMessage
                        }
                    }
                }

                if ($problematicCount -gt 0) {
                    Write-Output "$($file.Name): $problematicCount problematic character(s) found"
                } else {
                    Write-Verbose "$($file.Name): No problematic characters found"
                }
            }
            #endregion

            if ($script:activeReplacements.Count -eq 0) {
                if (-not $Warn.IsPresent -and -not $ErrorOnProblematic.IsPresent) {
                    Write-Output "$($file.Name): Nothing to update (no active categories)"
                }
                continue
            }

            $totalReplacements = 0
            $changes = @()

            # Process each replacement
            foreach ($replacement in $script:activeReplacements) {
                $fromChar = [string]$replacement.From
                $toStr = $replacement.To

                # Find all occurrences for verbose output
                $index = 0
                while (($foundIndex = $content.IndexOf($fromChar, $index)) -ge 0) {
                    # Calculate line and column
                    $lineNum = 1
                    $colNum = 1
                    for ($i = 0; $i -lt $foundIndex; $i++) {
                        if ($content[$i] -eq "`n") {
                            $lineNum++
                            $colNum = 1
                        } else {
                            $colNum++
                        }
                    }

                    # Get context (up to 20 chars before and after)
                    $contextStart = [Math]::Max(0, $foundIndex - 20)
                    $contextEnd = [Math]::Min($content.Length, $foundIndex + 1 + 20)
                    $beforeContext = $content.Substring($contextStart, $foundIndex - $contextStart)
                    $afterContext = $content.Substring($foundIndex + 1, $contextEnd - $foundIndex - 1)

                    # Clean up context for display (replace newlines)
                    $beforeContext = $beforeContext -replace "`r`n", '␍␊' -replace "`n", '␊' -replace "`r", '␍'
                    $afterContext = $afterContext -replace "`r`n", '␍␊' -replace "`n", '␊' -replace "`r", '␍'

                    $changes += [PSCustomObject]@{
                        Line = $lineNum
                        Column = $colNum
                        Name = $replacement.Name
                        Category = $replacement.Category
                        From = $fromChar
                        To = $toStr
                        Context = "${beforeContext}[${fromChar}]${afterContext}"
                    }

                    $totalReplacements++
                    $index = $foundIndex + 1
                }

                # Perform the replacement
                $content = $content.Replace($fromChar, $toStr)
            }

            if ($totalReplacements -gt 0) {
                # Output verbose information about changes
                foreach ($change in $changes) {
                    Write-Verbose "Line $($change.Line), Col $($change.Column): $($change.Name) ($($change.Category))"
                    Write-Verbose "  Context: $($change.Context)"
                    Write-Verbose "  '$($change.From)' -> '$($change.To)'"
                }

                $shouldProcessMessage = "Replace $totalReplacements special character(s)"
                if ($PSCmdlet.ShouldProcess($file.FullName, $shouldProcessMessage)) {
                    # Create backup if requested
                    if ($Backup.IsPresent) {
                        $backupPath = "$($file.FullName).bak"
                        Write-Verbose "Creating backup: $backupPath"
                        Copy-Item -Path $file.FullName -Destination $backupPath -Force
                    }

                    # Write the modified content
                    # Use WriteAllText to preserve exact content without BOM issues
                    [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
                    Write-Output "$($file.Name): $totalReplacements replacement(s) made"
                }
            } else {
                Write-Output "$($file.Name): Nothing to update"
            }
        }
        #endregion
    }

    end {
        Write-Verbose 'ASCII markdown conversion process completed'
    }
}

# If running as a script, execute with provided arguments
if ($MyInvocation.InvocationName -ne '.') {
    if ($args.Count -gt 0) {
        Normalize-Characters @args
    } else {
        Get-Help Normalize-Characters -Detailed
    }
}
