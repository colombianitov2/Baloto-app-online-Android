$ErrorActionPreference='Stop'
$obj=Join-Path $PSScriptRoot 'obj'
[IO.Directory]::CreateDirectory($obj) | Out-Null
$source=[IO.File]::ReadAllText((Join-Path (Split-Path $PSScriptRoot -Parent) 'DatosBaloto.cs'))
# Only redirect Windows storage for isolated tests; the Android app links the original file.
$source=$source.Replace('Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)','Environment.GetEnvironmentVariable("BALOTO_TEST_DATA")')
[IO.File]::WriteAllText((Join-Path $obj 'DatosBaloto.Isolated.cs'),$source,[Text.UTF8Encoding]::new($false))
dotnet run --project (Join-Path $PSScriptRoot 'Baloto.CoreChecks.csproj') -- (Join-Path $obj ('test-'+[guid]::NewGuid().ToString('N')))
if ($LASTEXITCODE -ne 0) { throw 'Core checks failed.' }
