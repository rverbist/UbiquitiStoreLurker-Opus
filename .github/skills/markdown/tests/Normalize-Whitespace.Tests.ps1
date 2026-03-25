BeforeAll {
    . $PSScriptRoot/../scripts/Normalize-Whitespace.ps1
}

Describe 'Normalize-Whitespace' -Tag 'Unit' {

    BeforeEach {
        $script:tempDir = New-Item -Path ([System.IO.Path]::GetTempPath()) `
            -Name "PesterTest_$([Guid]::NewGuid().ToString('N'))" -ItemType Directory
    }

    AfterEach {
        if (Test-Path $script:tempDir.FullName) {
            Remove-Item $script:tempDir.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Context 'When trailing whitespace is present' {
        It 'Should trim trailing whitespace and report Normalized' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "# Heading   `r`nContent   ", [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-Whitespace -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)): Normalized"
            (Get-Content $file.FullName -Raw) | Should -Not -Match ' +\r\n'
        }
    }

    Context 'When consecutive blank lines are present' {
        It 'Should collapse to a single blank line and report Normalized' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "Line one`r`n`r`n`r`nLine two", [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-Whitespace -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)): Normalized"
        }
    }

    Context 'When a blank line appears adjacent to a heading' {
        It 'Should preserve the blank line next to the heading' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "# Heading`r`n`r`nContent", [System.Text.UTF8Encoding]::new($false))
            Normalize-Whitespace -Path $script:tempDir.FullName
            $result = Get-Content $file.FullName -Raw
            $result | Should -Match '# Heading\r\n\r\nContent'
        }
    }

    Context 'When the file is already normalized' {
        It 'Should produce no output on the second run' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "# Heading`r`n`r`nContent   ", [System.Text.UTF8Encoding]::new($false))
            Normalize-Whitespace -Path $script:tempDir.FullName | Out-Null
            $output = Normalize-Whitespace -Path $script:tempDir.FullName
            $output | Should -BeNullOrEmpty
        }
    }

    Context 'When the file is empty' {
        It 'Should produce no output' {
            New-Item -Path $script:tempDir.FullName -Name 'empty.md' -ItemType File | Out-Null
            $output = Normalize-Whitespace -Path $script:tempDir.FullName
            $output | Should -BeNullOrEmpty
        }
    }

    Context 'When a fenced code block contains blank lines' {
        It 'Should preserve blank lines inside the fenced block' {
            $fence = '```'
            $content = "# Heading`r`n${fence}powershell`r`n`r`n# comment`r`n${fence}"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            Normalize-Whitespace -Path $script:tempDir.FullName
            $result = Get-Content $file.FullName -Raw
            $result | Should -Match "powershell`r`n`r`n# comment"
        }
    }

    Context 'When there is an unclosed code fence' {
        It 'Should write a warning about the unclosed fence' {
            $fence = '```'
            $content = "${fence}powershell`r`n# unclosed"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            { Normalize-Whitespace -Path $script:tempDir.FullName -WarningAction Stop } | Should -Throw
        }
    }

    Context 'When -WhatIf is specified' {
        It 'Should not modify the file content' {
            $content = "Line   `r`n"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            Normalize-Whitespace -Path $script:tempDir.FullName -WhatIf
            (Get-Content $file.FullName -Raw) | Should -Be $content
        }
    }

    Context 'When path does not exist' {
        It 'Should throw when the path is not found' {
            $missing = Join-Path $script:tempDir.FullName 'nonexistent'
            { Normalize-Whitespace -Path $missing -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When path is a non-markdown file' {
        It 'Should throw for a non-.md extension' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.txt' -ItemType File
            { Normalize-Whitespace -Path $file.FullName -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When -Recurse is specified with a file path' {
        It 'Should warn that -Recurse is ignored' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, '# ok', [System.Text.UTF8Encoding]::new($false))
            { Normalize-Whitespace -Path $file.FullName -Recurse -WarningAction Stop } | Should -Throw
        }
    }

    Context 'When -Recurse is used on a directory' {
        It 'Should process markdown files in subdirectories' {
            $subDir = New-Item -Path $script:tempDir.FullName -Name 'sub' -ItemType Directory
            $file = New-Item -Path $subDir.FullName -Name 'nested.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "Trailing   `r`n", [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-Whitespace -Path $script:tempDir.FullName -Recurse
            $output | Should -Match 'nested\.md'
        }
    }
}
