// include Fake lib
#r @"packages/build/FAKE/tools/FakeLib.dll"
open Fake

// Properties
let mode = getBuildParamOrDefault "mode" "Release"
let parentDir = "../"
let buildDirBase = "./bin/"
let buildDir = buildDirBase + mode + "/"
let substructioRepo = @"https://github.com/opcon/Substructio"
let substructioBranch = "develop"
let substructioFolder = "Substructio"
let substructioDir = parentDir + substructioFolder
let substructioBuildDir = substructioDir + buildDirBase + mode + "/"

// Targets
Target "Clean" (fun _ -> 
        CleanDir buildDir
)

Target "CleanSubstructio" (fun _ ->
        CleanDir substructioBuildDir
)

Target "CleanAll" (fun _ ->
        CleanDir buildDirBase
)

Target "CheckSubstructioExists" (fun _ ->
        if not (directoryExists substructioDir) then
            trace "Substructio does not exist, cloning Substructio"
            Git.Repository.cloneSingleBranch parentDir substructioRepo substructioBranch substructioFolder
        else
            trace "Substructio folder already exists"
)

Target "BuildSubstructio" (fun _ ->
        "Building Substructio, in " + mode + " configuration" |> trace
        !! (substructioDir + "**/*.csproj")
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

Target "Default" (fun _ ->
    ()
)

// Dependencies
"Clean"
    ==> "Build"
    ==> "Default"

"CleanSubstructio"
    ==> "BuildSubstructio"

"BuildSubstructio"
    ==> "Build"

"CheckSubstructioExists"
    ==> "CleanSubstructio"

// start build
RunTargetOrDefault "Default"