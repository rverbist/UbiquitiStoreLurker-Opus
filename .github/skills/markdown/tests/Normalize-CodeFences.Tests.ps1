BeforeAll {
    . $PSScriptRoot/../scripts/Normalize-CodeFences.ps1
}

Describe 'Normalize-CodeFences' -Tag 'Unit' {

    BeforeEach {
        $script:tempDir = New-Item -Path ([System.IO.Path]::GetTempPath()) `
            -Name "PesterTest_$([Guid]::NewGuid().ToString('N'))" -ItemType Directory
    }

    AfterEach {
        if (Test-Path $script:tempDir.FullName) {
            Remove-Item $script:tempDir.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Context 'When fence has no language tag' {
        It 'Should add "code" as default language' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            Set-Content -Path $file.FullName -Value "``````" -Encoding UTF8 -NoNewline

            Normalize-CodeFences -Path $script:tempDir.FullName

            $result = Get-Content -Path $file.FullName -Raw
            $result | Should -Match '```code'
        }

        It 'Should report one change in output' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            Set-Content -Path $file.FullName -Value "``````" -Encoding UTF8 -NoNewline

            $output = Normalize-CodeFences -Path $script:tempDir.FullName

            $output | Should -Match '1 change\(s\) made'
        }
    }

    Context 'When fence language has trailing whitespace' {
        It 'Should trim trailing whitespace from language tag' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            Set-Content -Path $file.FullName -Value "``````powershell  " -Encoding UTF8 -NoNewline

            Normalize-CodeFences -Path $script:tempDir.FullName

            $result = Get-Content -Path $file.FullName -Raw
            $result | Should -Match '```powershell'
            $result | Should -Not -Match '```powershell  '
        }

        It 'Should count the trimmed fence as a change' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            Set-Content -Path $file.FullName -Value "``````powershell  " -Encoding UTF8 -NoNewline

            $output = Normalize-CodeFences -Path $script:tempDir.FullName

            $output | Should -Match '1 change\(s\) made'
        }
    }

    Context 'When closing fence has trailing whitespace' {
        It 'Should strip trailing whitespace from closing fence' {
            $content = "``````powershell`nsome code`n``````   "
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline

            Normalize-CodeFences -Path $script:tempDir.FullName

            $result = Get-Content -Path $file.FullName -Raw
            # Closing fence should be exactly ``` without trailing spaces
            $result | Should -Not -Match '```   '
        }
    }

    Context 'When file is already normalized' {
        It 'Should output "Nothing to update"' {
            $content = "``````powershell`nsome code`n``````"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline

            $output = Normalize-CodeFences -Path $script:tempDir.FullName

            $output | Should -Match 'Nothing to update'
        }

        It 'Should not modify file content' {
            $content = "``````powershell`nsome code`n``````"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
            $before = Get-Content -Path $file.FullName -Raw

            Normalize-CodeFences -Path $script:tempDir.FullName

            $after = Get-Content -Path $file.FullName -Raw
            $after | Should -Be $before
        }
    }

    Context 'When file has an unclosed code fence' {
        It 'Should emit a warning containing "Unclosed"' {
            $content = "``````powershell`nsome code without closing fence"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline

            { Normalize-CodeFences -Path $script:tempDir.FullName -WarningAction Stop } |
                Should -Throw '*Unclosed*'
        }
    }

    Context 'When multiple fences need normalization' {
        It 'Should count all changed fences' {
            $content = "``````" + "`n" + "code`n" + "``````" + "`n" +
                       "``````" + "`n" + "more code`n" + "``````"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline

            $output = Normalize-CodeFences -Path $script:tempDir.FullName

            $output | Should -Match '2 change\(s\) made'
        }
    }

    Context 'When -WhatIf is used' {
        It 'Should not modify the file' {
            $content = "``````"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
            $before = Get-Content -Path $file.FullName -Raw

            Normalize-CodeFences -Path $script:tempDir.FullName -WhatIf

            $after = Get-Content -Path $file.FullName -Raw
            $after | Should -Be $before
        }
    }

    Context 'When path points to a non-markdown file' {
        It 'Should emit an InvalidFileType error' {
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.txt' -ItemType File

            { Normalize-CodeFences -Path $file.FullName -ErrorAction Stop } |
                Should -Throw
        }
    }

    Context 'When path does not exist' {
        It 'Should emit a PathNotFound error' {
            $missing = Join-Path $script:tempDir.FullName 'nonexistent.md'

            { Normalize-CodeFences -Path $missing -ErrorAction Stop } |
                Should -Throw
        }
    }

    Context 'When -Recurse is used with a file path' {
        It 'Should emit a warning that -Recurse is ignored for files' {
            $content = "``````powershell`ncode`n``````"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline

            { Normalize-CodeFences -Path $file.FullName -Recurse -WarningAction Stop } |
                Should -Throw
        }
    }

    Context 'When -Recurse is used with a directory' {
        It 'Should process markdown files in subdirectories' {
            $subDir = New-Item -Path $script:tempDir.FullName -Name 'sub' -ItemType Directory
            $file = New-Item -Path $subDir.FullName -Name 'nested.md' -ItemType File
            Set-Content -Path $file.FullName -Value "``````" -Encoding UTF8 -NoNewline

            $output = Normalize-CodeFences -Path $script:tempDir.FullName -Recurse

            $output | Should -Match 'nested.md'
        }
    }

    Context 'When file language is already "code" (no change)' {
        It 'Should report nothing to update for already-normalized code fence' {
            $content = "``````code`ndata`n``````"
            $file = New-Item -Path $script:tempDir.FullName -Name 'test.md' -ItemType File
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline

            $output = Normalize-CodeFences -Path $script:tempDir.FullName

            $output | Should -Match 'Nothing to update'
        }
    }
}
