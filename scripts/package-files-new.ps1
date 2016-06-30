$ENV:major = (Get-Item .\Binaries\Release\turnt-ninja.exe).VersionInfo.FileMajorPart
$ENV:minor = (Get-Item .\Binaries\Release\turnt-ninja.exe).VersionInfo.FileMinorPart
$ENV:build = (Get-Item .\Binaries\Release\turnt-ninja.exe).VersionInfo.FileBuildPart
$ENV:private = (Get-Item .\Binaries\Release\turnt-ninja.exe).VersionInfo.FilePrivatePart
$ENV:name = 'turnt-ninja_v{0}_{1}_{2}' -f $ENV:major, $ENV:minor, $ENV:build
echo $ENV:name

& 'C:\Program Files\WinRAR\Rar.exe' a -ep -r $ENV:name .\Binaries\Release/*.dll
Start-Sleep -Milliseconds 500
& 'C:\Program Files\WinRAR\Rar.exe' a -ep -r $ENV:name .\Binaries\Release\turnt-ninja.exe
Start-Sleep -Milliseconds 500
& 'C:\Program Files\WinRAR\Rar.exe' a -ep -r $ENV:name .\Binaries\Release\turnt-ninja.exe.config
Start-Sleep -Milliseconds 500
& 'C:\Program Files\WinRAR\Rar.exe' a -r $ENV:name .\Resources\
Start-Sleep -Milliseconds 500
& 'C:\Program Files\WinRAR\Rar.exe' a -r $ENV:name .\Licenses\
Start-Sleep -Milliseconds 500