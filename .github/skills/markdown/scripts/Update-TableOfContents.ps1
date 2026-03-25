<#
.SYNOPSIS
    Adds nested Tables of Contents to markdown headings.

.DESCRIPTION
    Scans a markdown file for headings and inserts a TOC after each heading at the
    specified MinLevel. The TOC lists child headings up to MaxLevel depth. Existing
    TOCs (between <!-- toc:start --> and <!-- toc:end -->) are replaced.

.PARAMETER Path
    The path to a markdown file (.md) or a directory containing markdown files.
    Accepts absolute or relative paths.

.PARAMETER Recurse
    When Path is a directory, processes markdown files in all subdirectories.
    By default, only markdown files in the specified directory are processed.
    This parameter is ignored when Path is a file.

.PARAMETER MinLevel
    Heading level (1-6) at which to insert TOCs. Default is 1 (H1).

.PARAMETER MaxLevel
    Maximum heading level (greater than MinLevel, up to 6) to include in TOCs. Default is 6.

.PARAMETER OutputPath
    Optional output file path. If not specified, the input file is modified in-place.
    Mutually exclusive with -OutputConsole. Only valid when Path is a single file.

.PARAMETER OutputConsole
    Output the entire modified markdown to stdout instead of writing to a file.
    Mutually exclusive with -OutputPath. Only valid when Path is a single file.

.PARAMETER Plain
    Output TOC as a plain nested list without collapsible details/summary wrapper.
    TOC markers are still included.

.EXAMPLE
    Update-TableOfContents -Path .\docs\README.md -MinLevel 2 -MaxLevel 4
    Adds TOCs after each H2 heading, including H3 and H4 children.

.EXAMPLE
    Update-TableOfContents -Path .\docs -Recurse
    Adds TOCs to all markdown files in docs and subdirectories.

.EXAMPLE
    Update-TableOfContents -Path .\docs\guide.md -WhatIf
    Shows what changes would be made without modifying the file.

.OUTPUTS
    Reports the files that were processed.

.NOTES
    Version: 1.1.0
    TOC markers: <!-- toc:start --> and <!-- toc:end -->
#>
function Update-TableOfContents {
    [CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
    param(
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [Alias('Input', 'In', 'i', 'FullName')]
        [ValidateNotNullOrEmpty()]
        [string]$Path,

        [Parameter()]
        [switch]$Recurse,

        [Parameter()]
        [ValidateRange(1, 6)]
        [int]$MinLevel = 1,

        [Parameter()]
        [ValidateRange(1, 6)]
        [int]$MaxLevel = 6,

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
        [switch]$OutputConsole,

        [Parameter()]
        [switch]$Plain
    )

    begin {
        Write-Verbose 'Starting Update-TableOfContents'

        # GitHub-style slug generation (matches VS Code markdown-languageservice)
        # Algorithm: trim -> lowercase -> remove non-allowed chars -> replace spaces with hyphens
        # Allowed chars: letters (\p{L}), marks (\p{M}), numbers (\p{N}), connector punct (\p{Pc}), space, hyphen
        function ConvertTo-Slug {
            param([string]$Text)
            # 1. Trim leading/trailing whitespace
            $slug = $Text.Trim()
            # 2. Convert to lowercase
            $slug = $slug.ToLowerInvariant()
            # 3. Remove HTML tags
            $slug = $slug -replace '<[^>]+>', ''
            # 4. Remove characters not allowed by GitHub's algorithm
            # Keep: letters, combining marks, numbers, connector punctuation (underscore), space, hyphen
            $slug = $slug -replace '[^\p{L}\p{M}\p{N}\p{Pc}\s-]', ''
            # 5. Replace each whitespace character with a hyphen (preserves multiple spaces as multiple hyphens)
            $slug = $slug -replace '\s', '-'
            return $slug
        }

        # Build heading tree from lines
        function Build-HeadingTree {
            param([string[]]$Lines)

            $headings = [System.Collections.ArrayList]::new()
            $lineNumber = 0

            foreach ($line in $Lines) {
                $lineNumber++
                if ($line -match '^(#{1,6})\s+(.+)$') {
                    $level = $Matches[1].Length
                    $title = $Matches[2].Trim()
                    $slug = ConvertTo-Slug -Text $title

                    $node = [PSCustomObject]@{
                        Level    = $level
                        Title    = $title
                        Slug     = $slug
                        Index    = 0
                        Parent   = $null
                        Children = [System.Collections.ArrayList]::new()
                        Line     = $lineNumber
                    }
                    [void]$headings.Add($node)
                }
            }

            # Build parent-child relationships and set index
            $stack = [System.Collections.Stack]::new()

            foreach ($node in $headings) {
                # Pop stack until we find a parent (lower level) or stack is empty
                while ($stack.Count -gt 0 -and $stack.Peek().Level -ge $node.Level) {
                    [void]$stack.Pop()
                }

                if ($stack.Count -gt 0) {
                    $parent = $stack.Peek()
                    $node.Parent = $parent
                    $node.Index = $parent.Children.Count
                    [void]$parent.Children.Add($node)
                }
                else {
                    # Top-level heading (no parent at this level)
                    $node.Index = ($headings | Where-Object { $_.Level -eq $node.Level -and $null -eq $_.Parent }).Count - 1
                }

                $stack.Push($node)
            }

            return $headings
        }

        # Generate TOC markdown for a node's children
        function Build-TocContent {
            param(
                [PSCustomObject]$Node,
                [int]$MaxLevel,
                [int]$BaseIndent = 0,
                [int]$ParentLevel = 0
            )

            $lines = [System.Collections.ArrayList]::new()

            foreach ($child in $Node.Children) {
                if ($child.Level -le $MaxLevel) {
                    # Detect level skip (e.g., H4 directly under H2)
                    $expectedLevel = $ParentLevel + 1
                    if ($ParentLevel -gt 0 -and $child.Level -gt $expectedLevel) {
                        Write-Warning "Heading level skip: '$($child.Title)' (H$($child.Level)) at line $($child.Line) is child of H$ParentLevel. Treating as depth $($BaseIndent)."
                    }

                    $indent = '  ' * $BaseIndent
                    [void]$lines.Add("$indent- [$($child.Title)](#$($child.Slug))")

                    if ($child.Children.Count -gt 0) {
                        $childLines = Build-TocContent -Node $child -MaxLevel $MaxLevel -BaseIndent ($BaseIndent + 1) -ParentLevel $child.Level
                        foreach ($cl in $childLines) {
                            [void]$lines.Add($cl)
                        }
                    }
                }
            }

            return $lines
        }

        # Generate full TOC block
        function Build-TocBlock {
            param(
                [PSCustomObject]$Node,
                [int]$MaxLevel,
                [switch]$Plain
            )

            $tocLines = Build-TocContent -Node $Node -MaxLevel $MaxLevel -ParentLevel $Node.Level

            if ($tocLines.Count -eq 0) {
                return $null
            }

            $block = [System.Collections.ArrayList]::new()
            [void]$block.Add('')
            [void]$block.Add('<!-- toc:start -->')
            
            if (-not $Plain) {
                [void]$block.Add('<details>')
                [void]$block.Add('  <summary>')
                [void]$block.Add('   <strong>Table of Contents</strong> Click to open')
                [void]$block.Add('  </summary>')
                [void]$block.Add('')
            }
            else {
                [void]$block.Add('')
            }
            
            foreach ($tocLine in $tocLines) {
                [void]$block.Add($tocLine)
            }
            
            if (-not $Plain) {
                [void]$block.Add('')
                [void]$block.Add('</details>')
            }
            else {
                [void]$block.Add('')
            }
            
            [void]$block.Add('<!-- toc:end -->')

            return $block
        }

        # Remove existing TOC after a heading line
        function Remove-ExistingToc {
            param(
                [System.Collections.ArrayList]$Lines,
                [int]$HeadingLineIndex
            )

            $startIndex = -1
            $endIndex = -1

            # Look for TOC start within a few lines after the heading
            for ($i = $HeadingLineIndex + 1; $i -lt [Math]::Min($HeadingLineIndex + 5, $Lines.Count); $i++) {
                if ($Lines[$i] -match '^\s*$') {
                    continue
                }
                if ($Lines[$i] -match '<!--\s*toc:start\s*-->') {
                    $startIndex = $i
                    break
                }
                else {
                    break
                }
            }

            if ($startIndex -ge 0) {
                for ($i = $startIndex; $i -lt $Lines.Count; $i++) {
                    if ($Lines[$i] -match '<!--\s*toc:end\s*-->') {
                        $endIndex = $i
                        break
                    }
                }

                if ($endIndex -ge 0) {
                    # Also remove preceding empty line if present
                    if ($startIndex -gt 0 -and $Lines[$startIndex - 1] -match '^\s*$') {
                        $startIndex--
                    }
                    $removeCount = $endIndex - $startIndex + 1
                    $Lines.RemoveRange($startIndex, $removeCount)
                    return $removeCount
                }
            }

            return 0
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

            # Build heading tree
            $headings = Build-HeadingTree -Lines $lines.ToArray()
            Write-Verbose "Found $($headings.Count) headings"

            # Validate MaxLevel > MinLevel
            if ($MaxLevel -le $MinLevel) {
                $errorRecord = [System.Management.Automation.ErrorRecord]::new(
                    [System.ArgumentException]::new("MaxLevel ($MaxLevel) must be greater than MinLevel ($MinLevel)"),
                    'InvalidLevelRange',
                    [System.Management.Automation.ErrorCategory]::InvalidArgument,
                    $null
                )
                $PSCmdlet.ThrowTerminatingError($errorRecord)
            }

            # Get nodes at MinLevel, sorted by descending line number
            $targetNodes = $headings | Where-Object { $_.Level -eq $MinLevel } | Sort-Object -Property Line -Descending
            Write-Verbose "Found $($targetNodes.Count) headings at level $MinLevel"

            if ($targetNodes.Count -eq 0) {
                Write-Warning "No headings found at level $MinLevel in $($file.FullName)"
                continue
            }

            $modified = $false

            foreach ($node in $targetNodes) {
                Write-Verbose "Processing heading '$($node.Title)' at line $($node.Line)"

                # Zero-based index for the heading line
                $headingIndex = $node.Line - 1

                # Remove any existing TOC
                $removedLines = Remove-ExistingToc -Lines $lines -HeadingLineIndex $headingIndex
                if ($removedLines -gt 0) {
                    Write-Verbose "Removed existing TOC ($removedLines lines) after '$($node.Title)'"
                }

                # Rebuild tree since lines changed (children line numbers are now stale, but we only need structure)
                if ($removedLines -gt 0) {
                    $headings = Build-HeadingTree -Lines $lines.ToArray()
                    $node = $headings | Where-Object { $_.Title -eq $node.Title -and $_.Level -eq $MinLevel } |
                        Sort-Object -Property Line -Descending |
                        Select-Object -First 1
                    $headingIndex = $node.Line - 1
                }

                # Build TOC block
                $tocBlock = Build-TocBlock -Node $node -MaxLevel $MaxLevel -Plain:$Plain

                if ($null -eq $tocBlock -or $tocBlock.Count -eq 0) {
                    Write-Verbose "No children to add TOC for '$($node.Title)'"
                    continue
                }

                $shouldProcessMessage = "Add TOC with $($tocBlock.Count - 4) list items after heading '$($node.Title)' at line $($node.Line)"
                if ($PSCmdlet.ShouldProcess($file.FullName, $shouldProcessMessage)) {
                    # Insert TOC after the heading line
                    $insertIndex = $headingIndex + 1
                    for ($i = 0; $i -lt $tocBlock.Count; $i++) {
                        $lines.Insert($insertIndex + $i, $tocBlock[$i])
                    }
                    $modified = $true
                    Write-Verbose "Inserted TOC after '$($node.Title)'"
                }
            }

            $finalContent = $lines -join "`r`n"

            # Handle output options (only for single file mode)
            if ($markdownFiles.Count -eq 1 -and $OutputConsole.IsPresent) {
                Write-Output $finalContent
            } elseif ($modified -or $PSCmdlet.ShouldProcess($file.FullName, 'Write changes to file')) {
                $targetPath = if ($markdownFiles.Count -eq 1 -and $OutputPath) { $OutputPath } else { $file.FullName }

                if ($PSCmdlet.ShouldProcess($targetPath, 'Save markdown file')) {
                    [System.IO.File]::WriteAllText($targetPath, $finalContent, [System.Text.UTF8Encoding]::new($false))
                    Write-Output "$($file.Name): TOC refreshed"
                }
            }
        }
    }

    end {
        Write-Verbose 'Completed Update-TableOfContents'
    }
}

# If running as a script, execute with provided arguments
if ($MyInvocation.InvocationName -ne '.') {
    if ($args.Count -gt 0) {
        Update-TableOfContents @args
    } else {
        Get-Help Update-TableOfContents -Detailed
    }
}
