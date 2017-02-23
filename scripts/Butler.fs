module Fake.Butler

open System.Net

let butler_linux_32 = "https://dl.itch.ovh/butler/linux-386/head/butler"
let butler_linux_64 = "https://dl.itch.ovh/butler/linux-amd64/head/butler"
let butler_mac = "https://dl.itch.ovh/butler/darwin-amd64/head/butler"
let butler_windows_32 = "https://dl.itch.ovh/butler/windows-386/head/butler.exe"
let butler_windows_64 = "https://dl.itch.ovh/butler/windows-amd64/head/butler.exe"

let butlerFileName =
    match EnvironmentHelper.isWindows with
        | true -> "butler.exe"
        | false -> "butler"

let butlerURL =
    if EnvironmentHelper.isLinux then match System.Environment.Is64BitOperatingSystem with
                                        | true -> butler_linux_64
                                        | false -> butler_linux_32
    else if EnvironmentHelper.isMacOS then butler_mac
    else if EnvironmentHelper.isWindows then match System.Environment.Is64BitOperatingSystem with
                                                | true -> butler_windows_64
                                                | false -> butler_windows_32
    else butler_windows_32

let DownloadButler (butlerFolder:string) =
    let wc = new WebClient()
    try
        wc.DownloadFile(butlerURL, butlerFolder + butlerFileName)
    with
    | _ -> (let SSLUri = new System.Uri(butlerURL)
            let uriBuilder = new System.UriBuilder(butlerURL)
            uriBuilder.Scheme <- System.Uri.UriSchemeHttp
            uriBuilder.Port <- -1
            let nonSSLURL = uriBuilder.ToString()
            trace ("Download failed, trying non-ssl url " + nonSSLURL)
            wc.DownloadFile(nonSSLURL, butlerFolder + butlerFileName))
    match EnvironmentHelper.isWindows with
        | false -> (let result = ProcessHelper.ExecProcess (fun info ->
                                    info.FileName <- "chmod"
                                    info.Arguments <- "u+x " + butlerFolder + butlerFileName) (System.TimeSpan.FromSeconds 10.0)
                   if result <> 0 then failwithf "Couldn't set executable permissions on bulter")
        | _ -> ()
    trace ("Butler downloaded to " + (butlerFolder + butlerFileName))

let PushBuild (butlerFolder:string) (buildFileOrDir:string) (target:string) (channel:string) (version:string) (fixPermissions:bool) =
    let butlerFullPath = butlerFolder + butlerFileName

    let flags =
        "" + (if fixPermissions then "--fix-permissions " else "")
        + ("--userversion=" + version + " ")

    ProcessHelper.redirectOutputToTrace = true |> ignore
    let result = ProcessHelper.ExecProcess (fun info -> 
                info.FileName <- butlerFullPath
                info.Arguments <- (sprintf "push %s%s %s:%s" flags buildFileOrDir target channel)) (System.TimeSpan.FromMinutes 5.0)
    if result <> 0 then failwithf "Butler push failed"
    result