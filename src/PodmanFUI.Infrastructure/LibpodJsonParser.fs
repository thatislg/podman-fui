namespace PodmanFUI.Infrastructure

open System
open System.Text.Json
open PodmanFUI.Domain.Errors
open PodmanFUI.Domain.Models
open PodmanFUI.Domain.ContainerModels

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
            Error (DeserializationError (sprintf "Failed to parse JSON response from Podman API: %s" ex.Message))
        | ex ->
            Error (DeserializationError (sprintf "Unexpected error while parsing Podman API data: %s" ex.Message))

    /// Phân giải danh sách Container tóm tắt từ GET /v4.0.0/libpod/containers/json?all=true
    let parseContainerSummaryList (rawJson: string) : Result<ContainerSummary list, ConnectionError> =
        try
            use doc = JsonDocument.Parse(rawJson)
            let root = doc.RootElement
            if root.ValueKind <> JsonValueKind.Array then
                Ok []
            else
                let containers =
                    [ for elem in root.EnumerateArray() do
                        let id = tryGetString elem "Id" ""
                        let shortId = if id.Length > 12 then id.Substring(0, 12) else id
                        let names =
                            match elem.TryGetProperty("Names") with
                            | true, n when n.ValueKind = JsonValueKind.Array ->
                                [ for nameElem in n.EnumerateArray() do
                                    if nameElem.ValueKind = JsonValueKind.String then
                                        yield nameElem.GetString().TrimStart('/') ]
                            | _ -> []
                        let primaryName =
                            match names with
                            | head :: _ -> head
                            | [] -> shortId
                        let image = tryGetString elem "Image" (tryGetString elem "ImageName" "unknown")
                        let stateStr = tryGetString elem "State" "unknown"
                        let status =
                            match stateStr.ToLowerInvariant() with
                            | "running" -> ContainerStatus.Running
                            | "paused" -> ContainerStatus.Paused
                            | "restarting" -> ContainerStatus.Restarting
                            | "exited" | "stopped" -> ContainerStatus.Exited
                            | "created" | "configured" | "initialized" -> ContainerStatus.Created
                            | "dead" | "removing" -> ContainerStatus.Dead
                            | _ -> ContainerStatus.Exited
                        let statusText =
                            let s = tryGetString elem "Status" ""
                            if String.IsNullOrWhiteSpace(s) then
                                match status with
                                | ContainerStatus.Running -> "Running"
                                | ContainerStatus.Exited -> "Exited"
                                | ContainerStatus.Paused -> "Paused"
                                | ContainerStatus.Created -> "Created"
                                | ContainerStatus.Restarting -> "Restarting"
                                | ContainerStatus.Dead -> "Dead"
                            else s
                        let createdDto =
                            match elem.TryGetProperty("Created") with
                            | true, p when p.ValueKind = JsonValueKind.String ->
                                match DateTimeOffset.TryParse(p.GetString()) with
                                | true, dto -> dto
                                | _ -> DateTimeOffset.UtcNow
                            | true, p when p.ValueKind = JsonValueKind.Number ->
                                try
                                    DateTimeOffset.FromUnixTimeSeconds(p.GetInt64())
                                with _ -> DateTimeOffset.UtcNow
                            | _ -> DateTimeOffset.UtcNow
                        let ports =
                            match elem.TryGetProperty("Ports") with
                            | true, p when p.ValueKind = JsonValueKind.Array ->
                                [ for portElem in p.EnumerateArray() do
                                    let hostIp = tryGetString portElem "host_ip" (tryGetString portElem "HostIp" "")
                                    let hostPort =
                                        match portElem.TryGetProperty("host_port") with
                                        | true, hp when hp.ValueKind = JsonValueKind.Number -> hp.GetInt32()
                                        | _ ->
                                            match portElem.TryGetProperty("HostPort") with
                                            | true, hp when hp.ValueKind = JsonValueKind.Number -> hp.GetInt32()
                                            | true, hp when hp.ValueKind = JsonValueKind.String ->
                                                match Int32.TryParse(hp.GetString()) with
                                                | true, v -> v
                                                | _ -> 0
                                            | _ -> 0
                                    let containerPort =
                                        match portElem.TryGetProperty("container_port") with
                                        | true, cp when cp.ValueKind = JsonValueKind.Number -> cp.GetInt32()
                                        | _ ->
                                            match portElem.TryGetProperty("ContainerPort") with
                                            | true, cp when cp.ValueKind = JsonValueKind.Number -> cp.GetInt32()
                                            | _ -> 0
                                    let protocol = tryGetString portElem "protocol" (tryGetString portElem "Protocol" "tcp")
                                    yield { HostIp = hostIp; HostPort = hostPort; ContainerPort = containerPort; Protocol = protocol } ]
                            | _ -> []
                        let podName =
                            let p = tryGetString elem "PodName" (tryGetString elem "Pod" "")
                            if String.IsNullOrWhiteSpace(p) then None else Some p
                        let isQuadlet =
                            match elem.TryGetProperty("Labels") with
                            | true, labels when labels.ValueKind = JsonValueKind.Object ->
                                labels.EnumerateObject()
                                |> Seq.exists (fun prop ->
                                    prop.Name.StartsWith("io.podman.quadlet", StringComparison.OrdinalIgnoreCase) ||
                                    prop.Name.StartsWith("systemd.unit", StringComparison.OrdinalIgnoreCase))
                            | _ -> false
                        yield {
                            Id = id
                            ShortId = shortId
                            Names = names
                            PrimaryName = primaryName
                            Image = image
                            Status = status
                            StatusText = statusText
                            Created = createdDto
                            Ports = ports
                            PodName = podName
                            IsQuadletManaged = isQuadlet
                        } ]
                Ok containers
        with
        | :? JsonException as ex ->
            Error (DeserializationError (sprintf "Failed to parse container list JSON: %s" ex.Message))
        | ex ->
            Error (DeserializationError (sprintf "Unexpected error while parsing container list: %s" ex.Message))

    /// Phân giải dữ liệu Inspect chi tiết từ GET /v4.0.0/libpod/containers/{name}/json
    let parseContainerDetail (rawJson: string) : Result<ContainerDetail, ConnectionError> =
        try
            use doc = JsonDocument.Parse(rawJson)
            let root = doc.RootElement
            let id = tryGetString root "Id" ""
            let rawName = tryGetString root "Name" ""
            let name = rawName.TrimStart('/')
            let image = tryGetString root "ImageName" (tryGetString root "Image" "unknown")
            let createdDto =
                match root.TryGetProperty("Created") with
                | true, p when p.ValueKind = JsonValueKind.String ->
                    match DateTimeOffset.TryParse(p.GetString()) with
                    | true, dto -> dto
                    | _ -> DateTimeOffset.UtcNow
                | _ -> DateTimeOffset.UtcNow

            let stateElem =
                match root.TryGetProperty("State") with
                | true, s when s.ValueKind = JsonValueKind.Object -> s
                | _ -> root

            let stateStatus = tryGetString stateElem "Status" "unknown"
            let pid =
                match stateElem.TryGetProperty("Pid") with
                | true, p when p.ValueKind = JsonValueKind.Number -> p.GetInt32()
                | _ -> 0
            let exitCode =
                match stateElem.TryGetProperty("ExitCode") with
                | true, e when e.ValueKind = JsonValueKind.Number -> e.GetInt32()
                | _ -> 0

            let parseDateOption (elem: JsonElement) (propName: string) : DateTimeOffset option =
                match elem.TryGetProperty(propName) with
                | true, p when p.ValueKind = JsonValueKind.String ->
                    let s = p.GetString()
                    if String.IsNullOrWhiteSpace(s) || s.StartsWith("0001-01-01") then None
                    else
                        match DateTimeOffset.TryParse(s) with
                        | true, dto -> Some dto
                        | _ -> None
                | _ -> None

            let startedAt = parseDateOption stateElem "StartedAt"
            let finishedAt = parseDateOption stateElem "FinishedAt"

            let mounts =
                match root.TryGetProperty("Mounts") with
                | true, m when m.ValueKind = JsonValueKind.Array ->
                    [ for item in m.EnumerateArray() do
                        let src = tryGetString item "Source" ""
                        let dst = tryGetString item "Destination" ""
                        if not (String.IsNullOrEmpty(src)) || not (String.IsNullOrEmpty(dst)) then
                            yield (src, dst) ]
                | _ -> []

            let configElem =
                match root.TryGetProperty("Config") with
                | true, c when c.ValueKind = JsonValueKind.Object -> c
                | _ -> root

            let env = tryGetStringList configElem "Env"

            let labels =
                match configElem.TryGetProperty("Labels") with
                | true, l when l.ValueKind = JsonValueKind.Object ->
                    [ for prop in l.EnumerateObject() do
                        yield (prop.Name, prop.Value.GetString()) ]
                    |> Map.ofList
                | _ -> Map.empty

            let ports =
                let list = ResizeArray<PortMapping>()
                match root.TryGetProperty("NetworkSettings") with
                | true, ns when ns.ValueKind = JsonValueKind.Object ->
                    match ns.TryGetProperty("Ports") with
                    | true, p when p.ValueKind = JsonValueKind.Object ->
                        for portProp in p.EnumerateObject() do
                            let portProto = portProp.Name
                            let parts = portProto.Split('/')
                            let containerPort = if parts.Length > 0 then match Int32.TryParse(parts.[0]) with true, cp -> cp | _ -> 0 else 0
                            let proto = if parts.Length > 1 then parts.[1] else "tcp"
                            if portProp.Value.ValueKind = JsonValueKind.Array then
                                for b in portProp.Value.EnumerateArray() do
                                    let hostIp = tryGetString b "HostIp" ""
                                    let hostPort =
                                        match b.TryGetProperty("HostPort") with
                                        | true, hp when hp.ValueKind = JsonValueKind.String ->
                                            match Int32.TryParse(hp.GetString()) with true, v -> v | _ -> 0
                                        | true, hp when hp.ValueKind = JsonValueKind.Number -> hp.GetInt32()
                                        | _ -> 0
                                    list.Add({ HostIp = hostIp; HostPort = hostPort; ContainerPort = containerPort; Protocol = proto })
                    | _ -> ()
                | _ -> ()
                list |> Seq.toList

            let detail: ContainerDetail =
                { Id = id
                  Name = name
                  Image = image
                  Created = createdDto
                  State = stateStatus
                  Pid = pid
                  StartedAt = startedAt
                  FinishedAt = finishedAt
                  ExitCode = exitCode
                  Ports = ports
                  Mounts = mounts
                  Env = env
                  Labels = labels }

            Ok detail
        with
        | :? JsonException as ex ->
            Error (DeserializationError (sprintf "Failed to parse container inspect JSON: %s" ex.Message))
        | ex ->
            Error (DeserializationError (sprintf "Unexpected error while parsing container inspect: %s" ex.Message))

