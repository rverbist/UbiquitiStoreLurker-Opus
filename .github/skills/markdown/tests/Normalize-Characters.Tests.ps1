BeforeAll {
    . $PSScriptRoot/../scripts/Normalize-Characters.ps1
}

Describe 'Normalize-Characters' -Tag 'Unit' {

    BeforeEach {
        $script:tempDir = New-Item -Path ([System.IO.Path]::GetTempPath()) `
            -Name "PesterTest_$([Guid]::NewGuid().ToString('N'))" -ItemType Directory
    }

    AfterEach {
        if (Test-Path $script:tempDir.FullName) {
            Remove-Item $script:tempDir.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Context 'When file contains an en-dash' {
        It 'Should replace en-dash with hyphen and report replacement count' {
            $enDash = [char]0x2013
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "word${enDash}word", [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-Characters -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)): 1 replacement\(s\) made"
            ([System.IO.File]::ReadAllText($file.FullName, [System.Text.UTF8Encoding]::new($false))) | Should -Not -Match [char]0x2013
        }
    }

    Context 'When file contains an em-dash' {
        It 'Should replace em-dash with hyphen' {
            $emDash = [char]0x2014
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "a${emDash}b", [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-Characters -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)):.*replacement\(s\) made"
            ([System.IO.File]::ReadAllText($file.FullName, [System.Text.UTF8Encoding]::new($false))) | Should -Not -Match [char]0x2014
        }
    }

    Context 'When file contains a non-breaking space' {
        It 'Should replace NBSP with a regular space' {
            $nbsp = [char]0x00A0
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "word${nbsp}word", [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-Characters -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)):.*replacement\(s\) made"
            ([System.IO.File]::ReadAllText($file.FullName, [System.Text.UTF8Encoding]::new($false))) | Should -Not -Match [char]0x00A0
        }
    }

    Context 'When file contains curly double quotes' {
        It 'Should replace curly quotes with straight quotes' {
            $leftQuote = [char]0x201C
            $rightQuote = [char]0x201D
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "${leftQuote}hello${rightQuote}", [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-Characters -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)):.*replacement\(s\) made"
            $result = [System.IO.File]::ReadAllText($file.FullName, [System.Text.UTF8Encoding]::new($false))
            $result | Should -Not -Match [char]0x201C
            $result | Should -Not -Match [char]0x201D
        }
    }

    Context 'When file has no special characters' {
        It 'Should report Nothing to update' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, '# Plain markdown file', [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-Characters -Path $script:tempDir.FullName
            $output | Should -Match "$([regex]::Escape($file.Name)): Nothing to update"
        }
    }

    Context 'When -PreserveDashes is specified' {
        It 'Should leave dashes unreplaced' {
            $enDash = [char]0x2013
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "word${enDash}word", [System.Text.UTF8Encoding]::new($false))
            Normalize-Characters -Path $script:tempDir.FullName -PreserveDashes
            $result = [System.IO.File]::ReadAllText($file.FullName, [System.Text.UTF8Encoding]::new($false))
            $result | Should -Match [char]0x2013
        }
    }

    Context 'When -Inverse is specified without any -Replace* switches' {
        It 'Should report Nothing to update (no active categories)' {
            $enDash = [char]0x2013
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "word${enDash}word", [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-Characters -Path $script:tempDir.FullName -Inverse
            $output | Should -Match 'Nothing to update \(no active categories\)'
        }
    }

    Context 'When -Inverse is specified with -ReplaceDashes' {
        It 'Should replace dashes only and leave quotes unchanged' {
            $enDash = [char]0x2013
            $leftQuote = [char]0x201C
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "word${enDash}${leftQuote}end", [System.Text.UTF8Encoding]::new($false))
            Normalize-Characters -Path $script:tempDir.FullName -Inverse -ReplaceDashes
            $result = [System.IO.File]::ReadAllText($file.FullName, [System.Text.UTF8Encoding]::new($false))
            $result | Should -Not -Match [char]0x2013
            $result | Should -Match [char]0x201C
        }
    }

    Context 'When -ReplaceDashes is used without -Inverse' {
        It 'Should throw an argument error' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, '# ok', [System.Text.UTF8Encoding]::new($false))
            { Normalize-Characters -Path $file.FullName -ReplaceDashes -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When -Backup is specified' {
        It 'Should create a .bak file alongside the original' {
            $enDash = [char]0x2013
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "word${enDash}word", [System.Text.UTF8Encoding]::new($false))
            Normalize-Characters -Path $script:tempDir.FullName -Backup
            (Test-Path "$($file.FullName).bak") | Should -BeTrue
        }
    }

    Context 'When -WhatIf is specified' {
        It 'Should not modify the file' {
            $enDash = [char]0x2013
            $content = "word${enDash}word"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            Normalize-Characters -Path $script:tempDir.FullName -WhatIf
            ([System.IO.File]::ReadAllText($file.FullName, [System.Text.UTF8Encoding]::new($false))) | Should -Be $content
        }
    }

    Context 'When path does not exist' {
        It 'Should throw when the path is not found' {
            $missing = Join-Path $script:tempDir.FullName 'nonexistent'
            { Normalize-Characters -Path $missing -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When path is a non-markdown file' {
        It 'Should throw for a non-.md extension' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.txt' -ItemType File
            { Normalize-Characters -Path $file.FullName -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When -Recurse is specified with a file path' {
        It 'Should warn that -Recurse is ignored' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, '# ok', [System.Text.UTF8Encoding]::new($false))
            { Normalize-Characters -Path $file.FullName -Recurse -WarningAction Stop } | Should -Throw
        }
    }

    Context 'When -Recurse is used on a directory' {
        It 'Should process markdown files in subdirectories' {
            $subDir = New-Item -Path $script:tempDir.FullName -Name 'sub' -ItemType Directory
            $file = New-Item -Path $subDir.FullName -Name 'nested.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, '# no special chars', [System.Text.UTF8Encoding]::new($false))
            $output = Normalize-Characters -Path $script:tempDir.FullName -Recurse
            $output | Should -Match 'nested\.md'
        }
    }
}
