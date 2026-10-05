namespace PodmanFUI.Infrastructure

open System
open System.Text.Json
open PodmanFUI.Domain.Errors
open PodmanFUI.Domain.Models

module LibpodJsonParser =

    let private tryGetString (elem: JsonElement) (propName: string) (defaultVal: string) : string =
        match elem.TryGetProperty(propName) with
        | true, prop when prop.ValueKind = JsonValueKind.String -> prop.GetString()
        | _ -> defaultVal

    let private tryGetStringList (elem: JsonElement) (propName: string) : string list =
        match elem.TryGetProperty(propName) with
        | true, prop when prop.ValueKind = JsonValueKind.Array ->
            [ for item in prop.EnumerateArray() do
                if item.ValueKind = JsonValueKind.String then
                    yield item.GetString() ]
        | _ -> []

    /// Phân giải chuỗi JSON từ endpoint /v4.0.0/libpod/info thành cấu trúc SystemInfo
    let parseSystemInfo (rawJson: string) (socketPath: string) (mode: SocketMode) : Result<SystemInfo, ConnectionError> =
        try
            use doc = JsonDocument.Parse(rawJson)
            let root = doc.RootElement

            // 1. Bóc tách nhánh "host"
            let hostElem = 
                match root.TryGetProperty("host") with
                | true, h when h.ValueKind = JsonValueKind.Object -> h
                | _ -> root

            let cgroupVersion = tryGetString hostElem "cgroupVersion" "unknown"
            let cgroupManager = tryGetString hostElem "cgroupManager" "unknown"
            let cgroupControllers = tryGetStringList hostElem "cgroupControllers"

            let cgroupInfo: CgroupInfo =
                { Version = cgroupVersion
                  Manager = cgroupManager
                  Controllers = cgroupControllers }

            let arch = tryGetString hostElem "arch" "unknown"
            let os = tryGetString hostElem "os" "linux"
            let kernel = tryGetString hostElem "kernel" "unknown"

            let conmonVersion =
                match hostElem.TryGetProperty("conmon") with
                | true, c when c.ValueKind = JsonValueKind.Object ->
                    tryGetString c "version" "unknown"
                | _ -> "unknown"

            // 2. Bóc tách nhánh "store"
            let storeElem =
                match root.TryGetProperty("store") with
                | true, s when s.ValueKind = JsonValueKind.Object -> s
                | _ -> root

            let graphDriverName = tryGetString storeElem "graphDriverName" "overlay"
            let graphRoot = tryGetString storeElem "graphRoot" ""

            let isRootless =
                match mode with
                | Rootless -> true
                | Rootful -> false
                | Custom ->
                    if String.IsNullOrWhiteSpace(graphRoot) then
                        true
                    else
                        not (graphRoot.StartsWith("/var/lib/containers", StringComparison.OrdinalIgnoreCase))

            let hostInfo: HostInfo =
                { Arch = arch
                  OS = os
                  Kernel = kernel
                  Cgroup = cgroupInfo
                  StorageDriver = graphDriverName
                  GraphRoot = graphRoot
                  Rootless = isRootless
                  ConmonVersion = conmonVersion }

            // 3. Bóc tách nhánh "version"
            let versionElem =
                match root.TryGetProperty("version") with
                | true, v when v.ValueKind = JsonValueKind.Object -> v
                | _ -> root

            let versionStr = tryGetString versionElem "Version" "unknown"
            let apiVersionStr = tryGetString versionElem "APIVersion" "unknown"
            let goVersionStr = tryGetString versionElem "GoVersion" "unknown"
            let gitCommitStr = tryGetString versionElem "GitCommit" "unknown"
            let buildTimeStr = tryGetString versionElem "BuildTime" "unknown"

            let versionInfo: VersionInfo =
                { Version = versionStr
                  ApiVersion = apiVersionStr
                  GoVersion = goVersionStr
                  GitCommit = gitCommitStr
                  BuiltTime = buildTimeStr }

            let systemInfo: SystemInfo =
                { Host = hostInfo
                  Version = versionInfo
                  SocketPath = socketPath
                  SocketMode = mode }

            Ok systemInfo
        with
        | :? JsonException as ex ->
            Error (DeserializationError (sprintf "Lỗi giải mã cấu trúc JSON từ Podman API: %s" ex.Message))
        | ex ->
            Error (DeserializationError (sprintf "Lỗi không xác định khi phân giải dữ liệu: %s" ex.Message))
