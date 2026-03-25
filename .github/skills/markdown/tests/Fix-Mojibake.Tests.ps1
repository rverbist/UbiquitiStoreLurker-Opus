BeforeAll {
    . $PSScriptRoot/../scripts/Fix-Mojibake.ps1
}

Describe 'Fix-Mojibake' -Tag 'Unit' {

    BeforeEach {
        $script:tempDir = New-Item -Path ([System.IO.Path]::GetTempPath()) `
            -Name "PesterTest_$([Guid]::NewGuid().ToString('N'))" -ItemType Directory
    }

    AfterEach {
        if (Test-Path $script:tempDir.FullName) {
            Remove-Item $script:tempDir.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Context 'When mojibake em-dash sequence is present' {
        It 'Should replace mojibake bytes with proper em-dash and report replacement' {
            $mojibakeEmDash = [char]0x00E2 + [char]0x20AC + [char]0x201D
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "word${mojibakeEmDash}word", [System.Text.UTF8Encoding]::new($false))
            $output = Fix-Mojibake -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)):.*replacement\(s\) made"
            $result = [System.IO.File]::ReadAllText($file.FullName, [System.Text.UTF8Encoding]::new($false))
            $result | Should -Match [char]0x2014
        }
    }

    Context 'When mojibake en-dash sequence is present' {
        It 'Should replace mojibake bytes with proper en-dash' {
            $mojibakeEnDash = [char]0x00E2 + [char]0x20AC + [char]0x201C
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "word${mojibakeEnDash}word", [System.Text.UTF8Encoding]::new($false))
            $output = Fix-Mojibake -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)):.*replacement\(s\) made"
            $result = [System.IO.File]::ReadAllText($file.FullName, [System.Text.UTF8Encoding]::new($false))
            $result | Should -Match [char]0x2013
        }
    }

    Context 'When mojibake NBSP sequence is present' {
        It 'Should replace mojibake bytes with a regular space' {
            $mojibakeNbsp = [char]0x00C2 + [char]0x00A0
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "word${mojibakeNbsp}word", [System.Text.UTF8Encoding]::new($false))
            $output = Fix-Mojibake -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)):.*replacement\(s\) made"
        }
    }

    Context 'When no mojibake is present' {
        It 'Should report Nothing to update' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "# Clean heading`r`nNo mojibake here.", [System.Text.UTF8Encoding]::new($false))
            $output = Fix-Mojibake -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)): Nothing to update"
        }
    }

    Context 'When reporting replacement details' {
        It 'Should include count and parenthesised description in output' {
            $mojibakeEmDash = [char]0x00E2 + [char]0x20AC + [char]0x201D
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "a${mojibakeEmDash}b", [System.Text.UTF8Encoding]::new($false))
            $output = Fix-Mojibake -Path $script:tempDir.FullName
            $output | Should -Match '1 replacement\(s\) made \('
        }
    }

    Context 'When -WhatIf is specified' {
        It 'Should not modify the file' {
            $mojibakeEmDash = [char]0x00E2 + [char]0x20AC + [char]0x201D
            $content = "word${mojibakeEmDash}word"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            Fix-Mojibake -Path $script:tempDir.FullName -WhatIf
            ([System.IO.File]::ReadAllText($file.FullName, [System.Text.UTF8Encoding]::new($false))) | Should -Be $content
        }
    }

    Context 'When path does not exist' {
        It 'Should throw when path is not found' {
            $missing = Join-Path $script:tempDir.FullName 'nonexistent'
            { Fix-Mojibake -Path $missing -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When path is a non-markdown file' {
        It 'Should throw for a non-.md extension' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.txt' -ItemType File
            { Fix-Mojibake -Path $file.FullName -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When -Recurse is specified with a file path' {
        It 'Should warn that -Recurse is ignored' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, '# ok', [System.Text.UTF8Encoding]::new($false))
            { Fix-Mojibake -Path $file.FullName -Recurse -WarningAction Stop } | Should -Throw
        }
    }

    Context 'When -Recurse is used on a directory' {
        It 'Should process markdown files in subdirectories' {
            $subDir = New-Item -Path $script:tempDir.FullName -Name 'sub' -ItemType Directory
            $file = New-Item -Path $subDir.FullName -Name 'nested.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, '# Clean', [System.Text.UTF8Encoding]::new($false))
            $output = Fix-Mojibake -Path $script:tempDir.FullName -Recurse
            $output | Should -Match 'nested\.md'
        }
    }
}
