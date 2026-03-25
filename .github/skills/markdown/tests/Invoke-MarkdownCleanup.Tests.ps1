BeforeAll {
    . $PSScriptRoot/../scripts/Invoke-MarkdownCleanup.ps1
}

Describe 'Invoke-MarkdownCleanup' -Tag 'Unit' {

    BeforeEach {
        $script:tempDir = New-Item -Path ([System.IO.Path]::GetTempPath()) `
            -Name "PesterTest_$([Guid]::NewGuid().ToString('N'))" -ItemType Directory
    }

    AfterEach {
        if (Test-Path $script:tempDir.FullName) {
            Remove-Item $script:tempDir.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Context 'When processing a markdown file with mojibake' {
        It 'Should clean the file through the full pipeline without throwing' {
            $mojibakeEmDash = [char]0x00E2 + [char]0x20AC + [char]0x201D
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, "word${mojibakeEmDash}word", [System.Text.UTF8Encoding]::new($false))
            { Invoke-MarkdownCleanup -Path $script:tempDir.FullName } | Should -Not -Throw
            $result = [System.IO.File]::ReadAllText($file.FullName, [System.Text.UTF8Encoding]::new($false))
            $result | Should -Match [char]0x2014
        }
    }

    Context 'When -WhatIf is specified' {
        It 'Should not modify the file' {
            $mojibakeEmDash = [char]0x00E2 + [char]0x20AC + [char]0x201D
            $content = "word${mojibakeEmDash}word"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            Invoke-MarkdownCleanup -Path $script:tempDir.FullName -WhatIf
            ([System.IO.File]::ReadAllText($file.FullName, [System.Text.UTF8Encoding]::new($false))) | Should -Be $content
        }
    }

    Context 'When path does not exist' {
        It 'Should throw when path is not found' {
            $missing = Join-Path $script:tempDir.FullName 'nonexistent'
            { Invoke-MarkdownCleanup -Path $missing -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When path is a non-markdown file' {
        It 'Should throw for a non-.md extension' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.txt' -ItemType File
            { Invoke-MarkdownCleanup -Path $file.FullName -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'When -Recurse is used on a directory' {
        It 'Should process markdown files in subdirectories without throwing' {
            $subDir = New-Item -Path $script:tempDir.FullName -Name 'sub' -ItemType Directory
            $file = New-Item -Path $subDir.FullName -Name 'nested.md' -ItemType File
            [System.IO.File]::WriteAllText($file.FullName, '# clean', [System.Text.UTF8Encoding]::new($false))
            { Invoke-MarkdownCleanup -Path $script:tempDir.FullName -Recurse } | Should -Not -Throw
        }
    }
}
