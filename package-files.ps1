$major = (Get-Item .\Binaries\Release\turnt-ninja.exe).VersionInfo.FileMajorPart
$minor = (Get-Item .\Binaries\Release\turnt-ninja.exe).VersionInfo.FileMinorPart
$build = (Get-Item .\Binaries\Release\turnt-ninja.exe).VersionInfo.FileBuildPart
$name = 'turnt-ninja_v{0}_{1}_{2}' -f $major, $minor, $build
echo $name

& 'C:\Program Files\WinRAR\Rar.exe' a -r $name .\Binaries\Release/*.dll
Start-Sleep -Milliseconds 500
& 'C:\Program Files\WinRAR\Rar.exe' a -r $name .\Binaries\Release\turnt-ninja.exe
Start-Sleep -Milliseconds 500
& 'C:\Program Files\WinRAR\Rar.exe' a -r $name .\Binaries\Release\turnt-ninja.exe.config
Start-Sleep -Milliseconds 500
& 'C:\Program Files\WinRAR\Rar.exe' a -r $name .\Resources\
Start-Sleep -Milliseconds 500
& 'C:\Program Files\WinRAR\Rar.exe' a -r $name .\Licenses\
Start-Sleep -Milliseconds 500
& 'C:\Program Files\WinRAR\Rar.exe' a $name '.\Turnt Ninja.lnk'
Start-Sleep -Milliseconds 500