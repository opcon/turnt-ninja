// include Fake lib
#r @"packages/build/FAKE/tools/FakeLib.dll"
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

// MonoKickStart properties
let monoKickStartRepo = @"https://github.com/MonoGame/MonoKickstart"
let monoKickStartArchive = @"https://github.com/MonoGame/MonoKickstart/archive/master.zip"

// More directories

let tempDirBase = "tmp/"
let deployDir = "./deploy/"
let squirrelDeployDir = deployDir + "squirrel/"

// Get directory postfix
let postFix = match mode.ToLower() with
                | "release" -> ""
                | _ -> "-" + mode.ToLower()

let deployName = 
    lazy 
    let ver = VersionHelper.GetAssemblyVersion appPath
    sprintf "%s-%i.%i.%i%s" appName ver.Major ver.Minor ver.Build postFix 

// Get temp directory name
let tempDirName = lazy 
                    tempDirBase + deployName.Value + "/"

let tempDirZipName = lazy 
                        tempDirBase + deployName.Value + "-zip/"

let tempMergedDirName = lazy
                           tempDirBase + deployName.Value + "-merged/"

let deployZipName = lazy
                        deployName.Value + ".zip"
let deployZipPath = lazy
                        deployDir + deployZipName.Value
let deployZipMergedName = lazy
                             deployName.Value + "-merged.zip"
let deployZipMergedPath = lazy
                             deployDir + deployZipMergedName.Value

// Tool names
let squirrelToolName = "Squirrel.exe"
let ILMergeToolName = "ILRepack.exe"

// MSBuild location for VS2017 RC (Community)
let msbuild2017Location = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin"


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

    let mainFiles = !! (sprintf "%s*.dll" buildDir) ++ (sprintf "%s*.config" buildDir) ++ (sprintf "%s*.exe" buildDir) -- (sprintf "%s*vshost*" buildDir)

    CopyFiles tempDirName.Value mainFiles
    CopyDir (tempDirName.Value + contentDirDeployName) "src/TurntNinja/Content/" (fun x -> true)
    CopyDir (tempDirName.Value + licenseDirDeployName) "docs/licenses" (fun x-> true)
)

Target "DeployZip" (fun _ ->
    CopyDir tempDirZipName.Value tempDirName.Value (fun x -> true)
    
    let filesToMove = !! (sprintf "%s*.dll" tempDirZipName.Value) ++ (sprintf "%s*.dll.config" tempDirZipName.Value)
    
    Copy (tempDirZipName.Value + dllDirDeployName) filesToMove
    
    DeleteFiles filesToMove

    let dInfo = new System.IO.DirectoryInfo(tempDirZipName.Value)
    Zip tempDirZipName.Value deployZipPath.Value [ for f in dInfo.EnumerateFiles("*", System.IO.SearchOption.AllDirectories) do yield f.FullName]
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
                    WorkingDir = tempDirZipName.Value
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

Target "DeployMerged" (fun _ ->
    //ensureDirectory tempMergedDirName.Value
    let libraries =  [for x in (!! (tempDirName.Value + "*.dll") -- "**/freetype6.dll") do yield x |> filename |> combinePaths tempDirName.Value ]
    // NuGet.Squirrel.dll is an already-merged assembly.
    // To stop ILRepack from complaining about missing references, we need to add copies of
    // NuGet.Squirrel.dll to the directory we're searching, but named the dlls we need to reference.
    CopyFile (tempDirName.Value + "Microsoft.Data.OData.dll") (tempDirName.Value + "NuGet.Squirrel.dll")
    CopyFile (tempDirName.Value + "Microsoft.Data.Services.Client.dll") (tempDirName.Value + "NuGet.Squirrel.dll")
    let searchDir = [tempDirName.Value; "/usr/lib/mono/4.5/Facades/"; "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5/Facades"]
    ILMerge (fun p ->
            {p with
                ToolPath = findToolInSubPath ILMergeToolName ""
                TargetKind = TargetKind.WinExe
                Libraries = libraries
                SearchDirectories = searchDir
            })
        (tempMergedDirName.Value + appName + ".exe")
        (tempDirName.Value + appName + ".exe")

    CopyDir (tempMergedDirName.Value + contentDirDeployName) (tempDirName.Value + contentDirDeployName) (fun x -> true)
    CopyDir (tempMergedDirName.Value + licenseDirDeployName) (tempDirName.Value + licenseDirDeployName) (fun x-> true)
    CopyFile (tempMergedDirName.Value + "freetype6.dll") (tempDirName.Value + "freetype6.dll")

    let dInfo = new System.IO.DirectoryInfo(tempMergedDirName.Value)
    Zip tempMergedDirName.Value deployZipMergedPath.Value [ for f in dInfo.EnumerateFiles("*", System.IO.SearchOption.AllDirectories) do yield f.FullName]
)
Target "DeployKickStart" (fun _ ->
    ensureDirectory tempDirName.Value
    // Download mono kickstart
    let wc = new WebClient()
    wc.DownloadFile(monoKickStartArchive, (tempDirName.Value + "kickstart.zip"))
)

Target "Deploy" (fun _ -> ())
Target "DeployAll" (fun _ -> ())

Target "PushArtifacts" (fun _ ->
    match buildServer with
    | BuildServer.AppVeyor ->
        AppVeyor.PushArtifact (fun p ->
            {p with
                FileName = deployName.Value + "-appveyor.zip"
                Path = deployZipPath.Value
            })
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

// Dependencies
"Clean"
    ==> "Build"
    ==> "Default"

"SetupMSBuildPath"
    ==> "Build"

"CleanSubstructio"
    ==> "RestoreSubstructioPackages"
    ==> "BuildSubstructio"

"RestoreSubstructioPackages"
    ==> "BuildSubstructio"

"BuildSubstructio"
    ==> "Build"

"CheckSubstructioExists"
    ==> "CleanSubstructio"

"CleanTemp"
    ==> "CopyToTemp"

"CopyToTemp"
    ==> "DeployZip"

"DeployZip"
    ==> "PushArtifacts"

"CopyToTemp"
    ==> "DeployMerged"

"DeployZip"
    ==> "DeploySquirrel"

"DeployZip"
    ==> "Deploy"
    
"DeployZip"
    ==> "DeployAll"

"DeployMerged"
    ==> "DeployAll"

"DeploySquirrel"
    ==> "DeployAll"

// CopyToTemp conditional target to ensure that we are built
"Build"
    =?> ("CopyToTemp", not (fileExists appPath))

// start build
RunTargetOrDefault "Default"
