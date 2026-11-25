param(
    [string]$GameDir = 'D:\SteamLibrary\steamapps\common\Super Fantasy Kingdom',
    [string]$ProfileDir = "$PSScriptRoot/../.analysis/profile"
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/..").Path
$core = (Resolve-Path "$ProfileDir/BepInEx/core").Path
$managed = "$GameDir/Super Fantasy Kingdom_Data/Managed"
[void][Reflection.Assembly]::LoadFrom("$core/Mono.Cecil.dll")
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory($managed)
$resolver.AddSearchDirectory($core)
$params = [Mono.Cecil.ReaderParameters]::new()
$params.AssemblyResolver = $resolver
$manifest = Get-Content "$root/thunderstore/manifest.json" -Raw | ConvertFrom-Json
$z = [IO.Compression.ZipFile]::OpenRead("$root/dist/CursedRelics-$($manifest.version_number).zip")
$bytes = [IO.MemoryStream]::new()
$entry = $z.GetEntry('plugins/CursedRelics.dll').Open()
try { $entry.CopyTo($bytes) } finally { $entry.Dispose(); $z.Dispose() }
$bytes.Position = 0
$dll = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($bytes,$params)
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$managed/Assembly-CSharp.dll",$params)
function Assert($value,$description) {
    if (!$value) {throw $description}
    "PASS $description"
}
function Code($type,$method) {
    ($game.MainModule.GetType($type).Methods | Where-Object Name -eq $method).Body.Instructions
}
try {
    $refs = @($dll.MainModule.GetMemberReferences() | Where-Object { $_.DeclaringType.Scope.Name -match '^(Unity|Assembly-CSharp|0Harmony|BepInEx|Newtonsoft)' })
    foreach ($ref in $refs) { if (!$ref.Resolve()) {throw "Unresolved member $ref"} }
    Assert ($refs.Count -gt 0) "$($refs.Count) game, engine and loader member references resolve"
    $wall = @(Code 'SuperFantasyKingdom.WallManager' 'WallGetsHit')
    $stores = @($wall | Where-Object { $_.OpCode.Name -eq 'stfld' -and $_.Operand.Name -eq 'm_Hitpoints' })
    Assert ($stores.Count -eq 1 -and $stores[0].Previous.Operand.Name -eq 'Max') 'one clamped incoming wall HP write'
    $signal = $wall | Where-Object { $_.Operand.Name -eq 'OnWallDamageTaken' } | Select-Object -First 1
    Assert ($signal.Offset -gt $stores[0].Offset) 'wall hit observers run before repair listeners'
    $tick = @(Code 'SuperFantasyKingdom.Status.BurnStatus' 'OnTick')
    Assert (@($tick | Where-Object { $_.OpCode.Name -eq 'stfld' -and $_.Operand.Name -eq 'damageAmount' }).Count -eq 1) 'one final Burn tick damage write'
    $cards = @(Code 'SuperFantasyKingdom.UI.UIOverlayMerchant' 'GenerateRelics')
    Assert (@($cards | Where-Object { $_.Operand.Name -eq 'GetEnumerator' }).Count -eq 1 -and $cards.Operand.Name -contains 'CanAddRelic') 'native card rendering follows eligible draw'
    foreach ($method in 'AddHitpoints','SetAdditionalHitpoints','AddToHitpointMultiplier') {
        Assert ((Code 'SuperFantasyKingdom.WallManager' $method).Operand.Name -notcontains 'WallGetsHit') "$method uses a separate wall-capacity path"
    }
    Assert ((Code 'SuperFantasyKingdom.GameDataManager' 'LoadRelics').Operand.Name -contains 'SpawnRelic') 'Continue uses native SpawnRelic'
    Assert ((Code 'SuperFantasyKingdom.GameDataManager' 'SaveRelics').Operand.Name -contains 'get_name') 'checkpoint stores the relic prefab name'
    Assert (@($dll.MainModule.Resources | Where-Object Name -like 'gkj_CR_*.png').Count -eq 10) 'ten embedded relic icons'
    Assert (@($dll.MainModule.Resources | Where-Object Name -like 'frame.*.png').Count -eq 4) 'pixel border, icon texture, ribbon and curse seal'
} finally { $game.Dispose(); $dll.Dispose(); $bytes.Dispose(); $resolver.Dispose() }
