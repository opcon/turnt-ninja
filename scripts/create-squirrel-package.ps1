# create release file
.\package-files-new.ps1

# load release notes
$rel = Get-Content .\release-notes.txt -Raw

# do directory stuff
$basedir = '.\SquirrelReleases\{0}_{1}_{2}_{3}' -f $ENV:major, $ENV:minor, $ENV:build, $ENV:private
$dirname = '{0}\lib\net45' -f $basedir
md -Force $dirname
& 'C:\Program Files\WinRAR\UnRAR.exe' x -o+ $env:name $dirname
pushd $basedir
..\..\.nuget\NuGet.exe spec -a '.\lib\net45\turnt-ninja.exe' -f
# update spec info
$xml = [xml](Get-Content 'turnt-ninja.nuspec')

$xml.package.metadata.licenseUrl = "https://github.com/opcon/turnt-ninja/blob/master/LICENSE"
$xml.package.metadata.projectUrl = "https://github.com/opcon/turnt-ninja"
$xml.package.metadata.releaseNotes = "$rel"

# delete some entries
$remove = $xml.package.metadata.dependencies
$xml.package.metadata.RemoveChild($remove)

$remove = $xml.SelectSingleNode("//tags")
$xml.package.metadata.RemoveChild($remove)

$remove = $xml.SelectSingleNode("//iconUrl")
$xml.package.metadata.RemoveChild($remove)

# save spec file
$xml.Save(("$pwd\turnt-ninja.nuspec"))
..\..\.nuget\NuGet.exe pack 'turnt-ninja.nuspec'
popd

# squirrel releasify
$packageFile = "$basedir\turnt-ninja.$ENV:major.$ENV:minor.$ENV:build.$ENV:private.nupkg"
$arguments = "--releasify $packageFile"
Start-Process '.\packages\squirrel.windows.1.4.0\tools\Squirrel.exe' -ArgumentList $arguments -Wait