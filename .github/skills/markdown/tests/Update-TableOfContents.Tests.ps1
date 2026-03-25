BeforeAll {
    . $PSScriptRoot/../scripts/Update-TableOfContents.ps1
}

Describe 'Update-TableOfContents' -Tag 'Unit' {

    BeforeEach {
        $script:tempDir = New-Item -Path ([System.IO.Path]::GetTempPath()) `
            -Name "PesterTest_$([Guid]::NewGuid().ToString('N'))" -ItemType Directory
    }

    AfterEach {
        if (Test-Path $script:tempDir.FullName) {
            Remove-Item $script:tempDir.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Context 'When a file has headings with nested sections' {
        It 'Should insert a TOC and report TOC refreshed' {
            $content = "# Main Heading`r`n`r`n## Section One`r`n`r`nContent One`r`n`r`n## Section Two`r`n`r`nContent Two"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            $output = Update-TableOfContents -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)): TOC refreshed"
            (Get-Content $file.FullName -Raw) | Should -Match 'toc:start'
        }
    }

    Context 'When an existing TOC is present' {
        It 'Should replace the old TOC with updated content' {
            $content = "# Main Heading`r`n<!-- toc:start -->`r`n- [Old](#old)`r`n<!-- toc:end -->`r`n`r`n## New Section`r`n`r`nContent"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            $output = Update-TableOfContents -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)): TOC refreshed"
            $result = Get-Content $file.FullName -Raw
            $result | Should -Not -Match '\[Old\]'
            $result | Should -Match 'New Section'
        }
    }

    Context 'When -Plain flag is specified' {
        It 'Should generate a plain list TOC without a details block' {
            $content = "# Main`r`n`r`n## Section A`r`n`r`nContent"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            Update-TableOfContents -Path $script:tempDir.FullName -Plain
            $result = Get-Content $file.FullName -Raw
            $result | Should -Not -Match '<details>'
            $result | Should -Match '\- \[Section A\]'
        }
    }

    Context 'When -OutputConsole is specified' {
        It 'Should write processed file content to stdout without the TOC refreshed message' {
            $content = "# Main`r`n`r`n## Sec`r`n`r`nText"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            $output = Update-TableOfContents -Path $file.FullName -OutputConsole
            ($output -join "`n") | Should -Match 'toc:start'
        }
    }

    Context 'When -OutputPath is specified' {
        It 'Should write the result to the alternate path' {
            $content = "# Main`r`n`r`n## Sec`r`n`r`nText"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            $outFile = Join-Path $script:tempDir.FullName 'output.md'
            Update-TableOfContents -Path $file.FullName -OutputPath $outFile
            (Get-Content $outFile -Raw) | Should -Match 'toc:start'
        }
    }

    Context 'When both -OutputPath and -OutputConsole are specified' {
        It 'Should throw a validation error' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, '# ok', [System.Text.UTF8Encoding]::new($false))
            $outFile = Join-Path $script:tempDir.FullName 'output.md'
            { Update-TableOfContents -Path $file.FullName -OutputPath $outFile -OutputConsole -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When no headings exist at the specified MinLevel' {
        It 'Should warn that no headings were found' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "# Heading`r`nNo subheadings", [System.Text.UTF8Encoding]::new($false))
            { Update-TableOfContents -Path $script:tempDir.FullName -MinLevel 2 -WarningAction Stop } | Should -Throw
        }
    }

    Context 'When MaxLevel is not greater than MinLevel' {
        It 'Should throw a terminating error for invalid level range' {
            $content = "# Main`r`n`r`n## Section`r`n`r`nContent"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            { Update-TableOfContents -Path $script:tempDir.FullName -MinLevel 2 -MaxLevel 1 -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When -WhatIf is specified' {
        It 'Should not modify the file' {
            $content = "# Main`r`n`r`n## Section`r`n`r`nContent"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            Update-TableOfContents -Path $script:tempDir.FullName -WhatIf
            (Get-Content $file.FullName -Raw) | Should -Be $content
        }
    }

    Context 'When path does not exist' {
        It 'Should throw when path is not found' {
            $missing = Join-Path $script:tempDir.FullName 'nonexistent'
            { Update-TableOfContents -Path $missing -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When path is a non-markdown file' {
        It 'Should throw for a non-.md extension' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.txt' -ItemType File
            { Update-TableOfContents -Path $file.FullName -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When -Recurse is specified with a file path' {
        It 'Should warn that -Recurse is ignored for single files' {
            $content = "# Main`r`n`r`n## Section`r`n`r`nContent"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            { Update-TableOfContents -Path $file.FullName -Recurse -WarningAction Stop } | Should -Throw
        }
    }

    Context 'When -Recurse is used on a directory' {
        It 'Should process markdown files in subdirectories' {
            $subDir = New-Item -Path $script:tempDir.FullName -Name 'sub' -ItemType Directory
            $file = New-Item -Path $subDir.FullName -Name 'nested.md' -ItemType File
            $content = "# Main`r`n`r`n## Section`r`n`r`nContent"
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            $output = Update-TableOfContents -Path $script:tempDir.FullName -Recurse
            $output | Should -Match 'nested\.md'
        }
    }
}
