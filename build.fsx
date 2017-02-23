// include Fake lib
#r @"packages/build/FAKE/tools/FakeLib.dll"
#load "scripts/Butler.fs"
open Fake
open System.Net

// Properties
let mode = getBuildParamOrDefault "mode" "Release"

let parentDir = "../"

// Build directories
let buildDirBase = "./bin/"
let buildDir = buildDirBase + mode + "/"
let appName = "turnt_ninja"
let appPath = buildDir + appName + ".exe"

// Substructio properties
let substructioRepo = @"https://github.com/opcon/Substructio"
let substructioBranch = "develop"
let substructioFolder = "Substructio"
let substructioDir = parentDir + substructioFolder + "/"
let substructioBuildDir = substructioDir + buildDirBase + mode + "/"
let contentDirDeployName = "Content"
let licenseDirDeployName = "Licenses"
let dllDirDeployName = "Dependencies"

// KickStart properties
let kickstartArchive = @"https://my.mixtape.moe/boqboe.zip"
let kickstartArchiveNoSSL = @"http://my.mixtape.moe/boqboe.zip"
let kickstartArchivePath = "./kickstart.zip"

// More directories

let tempDirBase = "./tmp/"
let deployDir = "./deploy/"
let squirrelDeployDir = deployDir + "squirrel/"

// Get directory postfix
let postFix = match mode.ToLower() with
                | "release" -> ""
                | _ -> "-" + mode.ToLower()

let versionString = 
    lazy
    let ver = VersionHelper.GetAssemblyVersion appPath
    sprintf "%i.%i.%i%s" ver.Major ver.Minor ver.Build postFix 

let deployName = 
    lazy 
    sprintf "%s-%s" appName versionString.Value

// Get temp directory name
let tempDirName = lazy 
                    tempDirBase + appName + "/"

let deployKickstartName = lazy
                            deployName.Value + "-kickstart"

let deployZipName = lazy
                        deployName.Value + ".zip"
let deployZipPath = lazy
                        deployDir + deployZipName.Value
let deployKickstartPath = lazy
                            deployDir + deployKickstartName.Value + ".zip"
let tempKickstartPath = lazy
                            tempDirBase + deployKickstartName.Value + "/"
let macAppName = "Turnt Ninja"
let macAppFolder = macAppName + ".app"
let macAppDeployName = lazy
                        macAppFolder + "-" + versionString.Value + ".zip"
let macAppDeployPath = lazy
                        deployDir + macAppDeployName.Value
let tempMacAppPath = lazy
                        tempDirBase + "mac/" + macAppFolder + "/"
let tempMacAppContents = lazy
                            tempMacAppPath.Value + "Contents/"
let tempMacAppResources = lazy
                            tempMacAppContents.Value + "Resources/"
let tempMacAppMacOS = lazy
                        tempMacAppContents.Value + "MacOS/"
let macIconOrigPath = lazy
                        tempMacAppMacOS.Value + "Content/Images/icon.icns"

// Info.plist mac file
let macInfoFile = """<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>CFBundleDevelopmentRegion</key>
	<string>en</string>
	<key>CFBundleExecutable</key>
	<string>turntninja</string>
	<key>CFBundleIconFile</key>
	<string>icon.icns</string>
	<key>CFBundleIdentifier</key>
	<string>org.ptrk.turntninja</string>
	<key>CFBundleInfoDictionaryVersion</key>
	<string>6.0</string>
	<key>CFBundleName</key>
	<string>Turnt Ninja</string>
	<key>CFBundlePackageType</key>
	<string>APPL</string>
	<key>CFBundleShortVersionString</key>
	<string>1.0</string>
	<key>CFBundleSignature</key>
	<string>????</string>
	<key>CFBundleVersion</key>
	<string>1.0</string>
	<key>LSMinimumSystemVersion</key>
	<string>10.7.0</string>
	<key>LSUIElement</key>
	<false/>
	<key>NSAppTransportSecurity</key>
	<dict>
		<key>NSAllowsArbitraryLoads</key>
		<true/>
	</dict>
	<key>NSHumanReadableCopyright</key>
	<string>� 2017 PTRK</string>
	<key>NSPrincipalClass</key>
	<string>NSApplication</string>
</dict>
</plist>"""

// Itch.io Linux toml
let itchLinuxConfig = @"[[actions]]
name = ""Play""
path = ""turntninja"""

let itchMacConfig = @"[[actions]]
name = ""Play""
path = ""Turnt Ninja.app"""

// Tool names
let squirrelToolName = "Squirrel.exe"
let ILMergeToolName = "ILRepack.exe"

// MSBuild location for VS2017 RC (Community)
let msbuild2017Location = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin"

let ZipFolderWithWorkingDir (workingDir:string) (folderPath:string) (zipPath:string) =
    let dInfo = new System.IO.DirectoryInfo(folderPath)
    Zip workingDir zipPath [ for f in dInfo.EnumerateFiles("*", System.IO.SearchOption.AllDirectories) do yield f.FullName]

let ZipFolder (folderPath:string) (zipPath:string) = ZipFolderWithWorkingDir folderPath folderPath zipPath

let pushAppVeyorArtifact (appVeyorFileName:string) (localPath:string) =
    AppVeyor.PushArtifact (fun p ->
    {p with
        FileName = appVeyorFileName
        Path = localPath
    })


// Targets
Target "Clean" (fun _ -> 
    try
        CleanDir buildDir
    with
    | _ -> ()
)

Target "CleanSubstructio" (fun _ ->
        CleanDir substructioBuildDir
)

Target "CleanAll" (fun _ ->
    try
        CleanDir buildDirBase
    with
    | _ -> ()
)

Target "CheckSubstructioExists" (fun _ ->
        if not (directoryExists substructioDir) then
            trace "Substructio does not exist, cloning Substructio"
            Git.Repository.cloneSingleBranch parentDir substructioRepo substructioBranch substructioFolder
        else
            trace "Substructio folder already exists"
)

Target "RestoreSubstructioPackages" (fun _ ->
        Paket.Restore (fun p ->
        { p with
            WorkingDir = substructioDir})
)

Target "BuildSubstructio" (fun _ ->
        "Building Substructio, in " + mode + " configuration" |> trace
        !! (substructioDir + "src/**/*.csproj")
            |>
            match mode.ToLower() with
                | "release" -> MSBuildRelease "" "Build"
                | _ -> MSBuildDebug "" "Build"
            |> Log "AppBuild-Output:"
)

Target "Build" (fun _ ->
        "Building Turnt Ninja in " + mode + " configuration" |> trace
        !! "src/**/*.csproj"
            |>
            match mode.ToLower() with
                | "release" -> MSBuildRelease buildDir "Build"
                | _ -> MSBuildDebug buildDir "Build"
            |> Log "AppBuild-Output: "
)

Target "CleanTemp" (fun _ ->
    CleanDir tempDirBase
)

Target "CleanDeploy" (fun _ ->
    CleanDir deployDir
)

Target "CopyToTemp" (fun _ ->
    ensureDirectory deployDir
    ensureDirectory tempDirBase

    let mainFiles = !! (sprintf "%s*.dll" buildDir) ++ (sprintf "%s*.config" buildDir) ++ (sprintf "%s*.exe" buildDir) -- (sprintf "%s*vshost*" buildDir)

    CopyFiles tempDirName.Value mainFiles
    CopyDir (tempDirName.Value + contentDirDeployName) "src/TurntNinja/Content/" (fun x -> true)
    CopyDir (tempDirName.Value + licenseDirDeployName) "docs/licenses" (fun x-> true)

    let filesToMove = !! (sprintf "%s*.dll" tempDirName.Value) ++ (sprintf "%s*.dll.config" tempDirName.Value)
    
    Copy (tempDirName.Value + dllDirDeployName) filesToMove
    
    DeleteFiles filesToMove
)

Target "DownloadKickstart" (fun _ ->
    ensureDirectory (tempKickstartPath.Value)

    trace ("Downloading kickstart from " + kickstartArchive)

    // Download mono kickstart
    let wc = new WebClient()
    try
        wc.DownloadFile(kickstartArchive, kickstartArchivePath)
    with
    | _ -> (trace ("Download failed, using non-ssl url " + kickstartArchiveNoSSL)
            wc.DownloadFile(kickstartArchiveNoSSL, kickstartArchivePath))
)

Target "DeployZip" (fun _ ->
    let dInfo = new System.IO.DirectoryInfo(tempDirName.Value)
    Zip tempDirName.Value deployZipPath.Value [ for f in dInfo.EnumerateFiles("*", System.IO.SearchOption.AllDirectories) do yield f.FullName]
    //ArchiveHelper.Tar.GZip.CompressWithDefaults (directoryInfo artifactTempDir) (fileInfo (deployDir + deployName + ".tar.gz")) (dInfo.EnumerateFiles("*", System.IO.SearchOption.AllDirectories)) 
)

Target "DeploySquirrel" (fun _ ->
    ensureDirectory squirrelDeployDir
    let info = System.Diagnostics.FileVersionInfo.GetVersionInfo appPath
    let packagePath = sprintf "%s%s.%s%s.nupkg" deployDir appName info.FileVersion postFix
    // Pack nuget package
    NuGet (fun p ->
            {p with
                    Authors = ["Patrick Yates"]
                    Description = "A music game"
                    Project = "Turnt Ninja"
                    Version = info.FileVersion + postFix
                    ReleaseNotes = "https://github.com/opcon/turnt-ninja"
                    Files = [
                                ("**/*.*", Some @"lib/net45", None)
                            ]
                    OutputPath = deployDir
                    WorkingDir = tempDirName.Value
            })
            "src/TurntNinja/TurntNinja.nuspec"

    let squirrelPath = findToolInSubPath squirrelToolName ""
    // Create squirrel package
    Squirrel.SquirrelPack (fun p -> 
        {p with
            ReleaseDir = squirrelDeployDir
            ToolPath = squirrelPath
        })
        packagePath
)

Target "DeployKickstart" (fun _ ->
    trace "Copying build files to kickstart folder"
    CopyDir (tempKickstartPath.Value + appName) tempDirName.Value (fun x -> true)

    trace "Unzipping kickstart over top"
    Unzip (tempKickstartPath.Value + appName) kickstartArchivePath

    trace "Zipping it all up"
    ZipFolder tempKickstartPath.Value deployKickstartPath.Value
)

Target "DeployMacApp" (fun _ ->
    ensureDirectory tempMacAppContents.Value
    ensureDirectory tempMacAppMacOS.Value
    ensureDirectory tempMacAppResources.Value

    trace "Copying kickstart folder to mac app folder"
    CopyDir tempMacAppMacOS.Value (tempKickstartPath.Value + appName) (fun x -> true)

    trace "Copying icon to mac app folder"
    CopyFile tempMacAppResources.Value macIconOrigPath.Value

    trace "Writing Info.plist file"
    WriteStringToFile false (tempMacAppContents.Value + "Info.plist") macInfoFile

    trace "Zipping it all up"
    ZipFolderWithWorkingDir (tempDirBase + "mac/") tempMacAppPath.Value macAppDeployPath.Value
)

Target "Deploy" (fun _ -> ())
Target "DeployAll" (fun _ -> ())

Target "PushArtifacts" (fun _ ->
    match buildServer with
    | BuildServer.AppVeyor ->
        pushAppVeyorArtifact (deployName.Value + "-appveyor.zip") deployZipPath.Value
        pushAppVeyorArtifact macAppDeployName.Value macAppDeployPath.Value
        pushAppVeyorArtifact (deployKickstartName.Value + ".zip") deployKickstartPath.Value
    | _ -> ()
)



Target "SetupMSBuildPath" (fun _ ->
    if (directoryExists msbuild2017Location) then
        trace (sprintf "Found 2017 MSBuild directory at %s, setting MSBuild build parameter to this location" msbuild2017Location)
        setBuildParam "MSBuild" msbuild2017Location
)

Target "Default" (fun _ ->
    ()
)

Target "DownloadButler" (fun _ ->
    Butler.DownloadButler "./"
)

let pushWinCI = 
    lazy 
    Butler.PushBuild "./" tempDirName.Value "opcon/turnt-ninja" "win-ci" versionString.Value true |> string |> trace

let pushLinuxCI = 
    lazy 
    WriteStringToFile false (tempKickstartPath.Value + appName + "/.itch.toml") itchLinuxConfig
    Butler.PushBuild "./" (tempKickstartPath.Value + appName) "opcon/turnt-ninja" "linux-ci" versionString.Value true |> string |> trace

let pushMacCI = 
    lazy
    WriteStringToFile false (tempDirBase + "mac/" + ".itch.toml") itchMacConfig
    Butler.PushBuild "./" (tempDirBase + "mac") "opcon/turnt-ninja" "mac-ci" versionString.Value true |> string |> trace

Target "PushItchCI" (fun _ ->
    match mode.ToLower() with
    | "release" ->
        match buildServer with
        | BuildServer.AppVeyor ->
            pushWinCI.Value
        | BuildServer.Travis ->
            match EnvironmentHelper.isMacOS with
            | true ->
                pushMacCI.Value
            | false ->
                pushLinuxCI.Value
        | BuildServer.LocalBuild ->
            pushWinCI.Value
            pushLinuxCI.Value
            pushMacCI.Value
        | _ -> ()
    | _ -> ()
)

Target "StampAssembly" (fun _ ->
    let stamp = sprintf "Head:%s Sha:%s" (Git.Information.getBranchName "./") (Git.Information.getCurrentHash())
    AssemblyInfoFile.UpdateAttributes ("src/TurntNinja/Properties/AssemblyInfo.cs") [AssemblyInfoFile.Attribute.InformationalVersion stamp]
)

Target "PushArtifactsAndItchBuilds" (fun _ ->
    ()
)

// Dependencies
"StampAssembly"
    ==> "Build"

"Clean"
    ==> "Build"
    ==> "Default"

"SetupMSBuildPath"
    ==> "Build"

"CleanSubstructio"
    ==> "RestoreSubstructioPackages"
    ==> "BuildSubstructio"

"BuildSubstructio"
    ==> "Build"

"CheckSubstructioExists"
    ==> "CleanSubstructio"

"CleanTemp"
    ==> "CopyToTemp"

"CopyToTemp"
    ==> "DeployZip"
    ==> "Deploy"
    ==> "DeployAll"

"DeployZip"
    ==> "DeploySquirrel"

"CopyToTemp"
    ==> "DeployKickstart"

"DeployKickstart"
    ==> "DeployMacApp"

"DeployAll"
    ==> "PushArtifacts"

"DeployKickstart"
    ==> "DeployAll"

"DeployMacApp"
    ==> "DeployAll"

"DeployAll"
    ==> "PushItchCI"

"DownloadButler"
    =?> ("PushItchCI", not (fileExists ("./" + Butler.butlerFileName)))

"PushItchCI"
    ==> "PushArtifactsAndItchBuilds"

"PushArtifacts"
    ==> "PushArtifactsAndItchBuilds"

// CopyToTemp conditional target to ensure that we are built
"Build"
    =?> ("CopyToTemp", not (fileExists appPath))

// Conditional target for downloading the kickstart archive
"DownloadKickstart"
    =?> ("DeployKickstart", not (fileExists kickstartArchivePath))

// start build
RunTargetOrDefault "Default"
