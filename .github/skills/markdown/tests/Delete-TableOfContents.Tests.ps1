BeforeAll {
    . $PSScriptRoot/../scripts/Delete-TableOfContents.ps1
}

Describe 'Delete-TableOfContents' -Tag 'Unit' {

    BeforeEach {
        $script:tempDir = New-Item -Path ([System.IO.Path]::GetTempPath()) `
            -Name "PesterTest_$([Guid]::NewGuid().ToString('N'))" -ItemType Directory
    }

    AfterEach {
        if (Test-Path $script:tempDir.FullName) {
            Remove-Item $script:tempDir.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Context 'When a TOC block is present' {
        It 'Should remove the TOC block and report removed count' {
            $content = "# Heading`r`n<!-- toc:start -->`r`n- [Item](#item)`r`n<!-- toc:end -->`r`nContent"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            $output = Delete-TableOfContents -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)): removed 1 TOC block\(s\)"
            (Get-Content $file.FullName -Raw) | Should -Not -Match 'toc:start'
        }
    }

    Context 'When multiple TOC blocks are present' {
        It 'Should remove all TOC blocks and report total count' {
            $toc = "<!-- toc:start -->`r`n- [Item](#item)`r`n<!-- toc:end -->"
            $content = "# H1`r`n${toc}`r`n## H2`r`n${toc}`r`nEnd"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            $output = Delete-TableOfContents -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)): removed 2 TOC block\(s\)"
        }
    }

    Context 'When there is no TOC block' {
        It 'Should not report any removed blocks' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "# Heading`r`nContent", [System.Text.UTF8Encoding]::new($false))
            $output = Delete-TableOfContents -Path $script:tempDir.FullName
            $output | Should -Not -Match 'removed \d+ TOC'
        }
    }

    Context 'When an orphan toc:end marker is present' {
        It 'Should warn about the orphan end marker' {
            $content = "# Heading`r`n<!-- toc:end -->`r`nContent"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            { Delete-TableOfContents -Path $script:tempDir.FullName -WarningAction Stop } | Should -Throw
        }
    }

    Context 'When -OutputConsole is specified' {
        It 'Should write processed file content to stdout' {
            $content = "# Heading`r`n<!-- toc:start -->`r`n- [Item](#item)`r`n<!-- toc:end -->`r`nContent"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            $output = Delete-TableOfContents -Path $file.FullName -OutputConsole
            ($output -join "`n") | Should -Match 'Content'
            ($output -join "`n") | Should -Not -Match 'toc:start'
        }
    }

    Context 'When -OutputPath is specified' {
        It 'Should write the result to the alternate file' {
            $content = "# Heading`r`n<!-- toc:start -->`r`n- [Item](#item)`r`n<!-- toc:end -->`r`nContent"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            $outFile = Join-Path $script:tempDir.FullName 'output.md'
            Delete-TableOfContents -Path $file.FullName -OutputPath $outFile
            (Get-Content $outFile -Raw) | Should -Not -Match 'toc:start'
        }
    }

    Context 'When both -OutputPath and -OutputConsole are specified' {
        It 'Should throw a validation error' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, '# ok', [System.Text.UTF8Encoding]::new($false))
            $outFile = Join-Path $script:tempDir.FullName 'output.md'
            { Delete-TableOfContents -Path $file.FullName -OutputPath $outFile -OutputConsole -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When -OutputConsole is used with a directory path' {
        It 'Should warn that output flags are only valid for single files' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, '# ok', [System.Text.UTF8Encoding]::new($false))
            { Delete-TableOfContents -Path $script:tempDir.FullName -OutputConsole -WarningAction Stop } | Should -Throw
        }
    }

    Context 'When -WhatIf is specified' {
        It 'Should not modify the file' {
            $content = "# Heading`r`n<!-- toc:start -->`r`n- [Item](#item)`r`n<!-- toc:end -->`r`nContent"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            Delete-TableOfContents -Path $script:tempDir.FullName -WhatIf
            (Get-Content $file.FullName -Raw) | Should -Be $content
        }
    }

    Context 'When path does not exist' {
        It 'Should throw when path is not found' {
            $missing = Join-Path $script:tempDir.FullName 'nonexistent'
            { Delete-TableOfContents -Path $missing -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When path is a non-markdown file' {
        It 'Should throw for a non-.md extension' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.txt' -ItemType File
            { Delete-TableOfContents -Path $file.FullName -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When -Recurse is used on a directory' {
        It 'Should process markdown files in subdirectories' {
            $subDir = New-Item -Path $script:tempDir.FullName -Name 'sub' -ItemType Directory
            $file = New-Item -Path $subDir.FullName -Name 'nested.md' -ItemType File
            $content = "# H`r`n<!-- toc:start -->`r`n- [item](#item)`r`n<!-- toc:end -->`r`nText"
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            $output = Delete-TableOfContents -Path $script:tempDir.FullName -Recurse
            $output | Should -Match 'nested\.md.*removed'
        }
    }
}
