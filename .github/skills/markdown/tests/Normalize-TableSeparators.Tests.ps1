BeforeAll {
    . $PSScriptRoot/../scripts/Normalize-TableSeparators.ps1
}

Describe 'Normalize-TableSeparators' -Tag 'Unit' {

    BeforeEach {
        $script:tempDir = New-Item -Path ([System.IO.Path]::GetTempPath()) `
            -Name "PesterTest_$([Guid]::NewGuid().ToString('N'))" -ItemType Directory
    }

    AfterEach {
        if (Test-Path $script:tempDir.FullName) {
            Remove-Item $script:tempDir.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Context 'When a separator line lacks proper spacing' {
        It 'Should reformat the separator and report 1 line(s) updated' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "| A | B |`r`n|------|------|`r`n| 1 | 2 |", [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-TableSeparators -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)): 1 line\(s\) updated"
        }
    }

    Context 'When a separator line has alignment colons' {
        It 'Should preserve colons while reformatting and report 1 line(s) updated' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "| A | B |`r`n|:----:|-----:|`r`n| 1 | 2 |", [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-TableSeparators -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)): 1 line\(s\) updated"
            $result = Get-Content $file.FullName -Raw
            $result | Should -Match ':\s+-+\s+:'
            $result | Should -Match '-+\s+:'
        }
    }

    Context 'When separator lines are already formatted' {
        It 'Should report Nothing to update' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "| A | B |`r`n| ---- | ---- |`r`n| 1 | 2 |", [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-TableSeparators -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)): Nothing to update"
        }
    }

    Context 'When multiple separator lines are present' {
        It 'Should report the total count of updated lines' {
            $content = "| A | B |`r`n|------|------|`r`n| 1 | 2 |`r`n`r`n| C | D |`r`n|------|------|`r`n| 3 | 4 |"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-TableSeparators -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)): 2 line\(s\) updated"
        }
    }

    Context 'When the file has no separator lines' {
        It 'Should report Nothing to update' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "# Heading`r`nNo tables here.", [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-TableSeparators -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)): Nothing to update"
        }
    }

    Context 'When -WhatIf is specified' {
        It 'Should not modify the file' {
            $content = "| A | B |`r`n|------|------|`r`n| 1 | 2 |"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            Normalize-TableSeparators -Path $script:tempDir.FullName -WhatIf
            (Get-Content $file.FullName -Raw) | Should -Be $content
        }
    }

    Context 'When path does not exist' {
        It 'Should throw when the path is not found' {
            $missing = Join-Path $script:tempDir.FullName 'nonexistent'
            { Normalize-TableSeparators -Path $missing -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When path is a non-markdown file' {
        It 'Should throw for a non-.md extension' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.txt' -ItemType File
            { Normalize-TableSeparators -Path $file.FullName -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When -Recurse is specified with a file path' {
        It 'Should warn that -Recurse is ignored' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, '# ok', [System.Text.UTF8Encoding]::new($false))
            { Normalize-TableSeparators -Path $file.FullName -Recurse -WarningAction Stop } | Should -Throw
        }
    }

    Context 'When -Recurse is used on a directory' {
        It 'Should process markdown files in subdirectories' {
            $subDir = New-Item -Path $script:tempDir.FullName -Name 'sub' -ItemType Directory
            $file = New-Item -Path $subDir.FullName -Name 'nested.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "| A |`r`n|---|`r`n| 1 |", [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-TableSeparators -Path $script:tempDir.FullName -Recurse
            $output | Should -Match 'nested\.md'
        }
    }
}
