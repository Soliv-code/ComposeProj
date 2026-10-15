[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

# Формируем имя файла с датой и временем (как в твоих логах)
$timestamp = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$OutputPath = ".\Docs\ProjectStructure_$timestamp.md"

if (-not (Test-Path '.\Docs')) {
    New-Item -ItemType Directory -Path '.\Docs' | Out-Null
}

# Папки, которые игнорируем
$ExcludeDirs = @('bin', 'obj', '.git', '.vs', 'TestResults', 'Logs', 'Screenshots', 'node_modules')

# Расширения, которые игнорируем
$ExcludeExts = @('.png', '.jpg', '.jpeg', '.gif', '.bmp', '.dll', '.pdb', '.exe', '.user', '.tmp', '.log', '.diag', '.cache', '.props', '.targets')

# Точные имена файлов
$ExcludeNames = @('global.json', '.treerc', 'tree_gen.cmd', 'tree_gen_new.cmd', 'generate_structure.cmd', 'generate_structure.ps1')

# ШАБЛОНЫ имён файлов (поддерживают * и ?)
# ВАЖНО: ProjectStructure_*.md скрывает старые выгрузки из дерева!
$ExcludePatterns = @('Readme_*.md', 'Roadmap_*.md', 'ProjectStructure_*.md')

function Test-IsExcluded {
    param([string]$FileName)
    
    if ($ExcludeNames -contains $FileName) { return $true }
    
    foreach ($pattern in $ExcludePatterns) {
        if ($FileName -like $pattern) { return $true }
    }
    
    return $false
}

function Get-CleanTree {
    param([string]$Path, [string]$Prefix)
    
    $items = Get-ChildItem -Path $Path -Force | Where-Object {
        if ($_.PSIsContainer) { 
            return $ExcludeDirs -notcontains $_.Name 
        }
        if (Test-IsExcluded -FileName $_.Name) { 
            return $false 
        }
        return $ExcludeExts -notcontains $_.Extension
    } | Sort-Object { if ($_.PSIsContainer) { 0 } else { 1 } }, Name

    $count = $items.Count
    for ($i = 0; $i -lt $count; $i++) {
        $item = $items[$i]
        $isLast = ($i -eq $count - 1)
        $connector = if ($isLast) { '└── ' } else { '├── ' }
        
        Write-Output "$Prefix$connector$($item.Name)"
        
        if ($item.PSIsContainer) {
            $childPrefix = if ($isLast) { "$Prefix    " } else { "$Prefix│   " }
            Get-CleanTree -Path $item.FullName -Prefix $childPrefix
        }
    }
}

'```text' | Out-File -FilePath $OutputPath -Encoding utf8
Get-CleanTree -Path '.' -Prefix '' | Out-File -FilePath $OutputPath -Encoding utf8 -Append
'```' | Out-File -FilePath $OutputPath -Encoding utf8 -Append

Write-Host '💹 SUCCESS: Structure saved to' $OutputPath -ForegroundColor Green