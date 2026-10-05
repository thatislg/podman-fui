namespace PodmanFUI.Infrastructure

open System
open System.IO
open PodmanFUI.Domain.Models

module SocketDiscovery =

    /// Chuẩn hóa đường dẫn socket: loại bỏ tiền tố "unix://" nếu có
    let normalizePath (rawPath: string) : string =
        if String.IsNullOrWhiteSpace(rawPath) then
            ""
        else
            let trimmed = rawPath.Trim()
            if trimmed.StartsWith("unix://", StringComparison.OrdinalIgnoreCase) then
                trimmed.Substring(7)
            else
                trimmed

    /// Dò tìm tự động đường dẫn Unix Domain Socket của Podman theo thứ tự ưu tiên
    let discoverSocket () : SocketDiscoveryResult =
        let attemptedPaths = ResizeArray<string>()

        // 1. Kiểm tra biến môi trường CONTAINER_HOST hoặc PODMAN_SOCKET
        let envHost = 
            match Environment.GetEnvironmentVariable("CONTAINER_HOST") with
            | v when not (String.IsNullOrWhiteSpace(v)) -> Some v
            | _ -> 
                match Environment.GetEnvironmentVariable("PODMAN_SOCKET") with
                | v when not (String.IsNullOrWhiteSpace(v)) -> Some v
                | _ -> None

        match envHost with
        | Some rawEnvPath ->
            let customPath = normalizePath rawEnvPath
            attemptedPaths.Add(customPath)
            if File.Exists(customPath) then
                Discovered(customPath, Custom)
            else
                NotFound(Seq.toList attemptedPaths, sprintf "Đường dẫn socket chỉ định trong biến môi trường không tồn tại: %s" customPath)
        | None ->
            // 2. Kiểm tra Rootless Socket của người dùng hiện tại
            let xdgRuntime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR")
            let rootlessPathCandidates =
                [ if not (String.IsNullOrWhiteSpace(xdgRuntime)) then
                      yield Path.Combine(xdgRuntime, "podman", "podman.sock")
                  // Thử tìm trong /run/user/<uid>/podman/podman.sock nếu có
                  if Directory.Exists("/run/user") then
                      for userDir in Directory.GetDirectories("/run/user") do
                          yield Path.Combine(userDir, "podman", "podman.sock")
                  yield "/run/user/1000/podman/podman.sock" ]
                |> List.distinct

            let foundRootless =
                rootlessPathCandidates
                |> List.tryFind (fun p ->
                    attemptedPaths.Add(p)
                    File.Exists(p))

            match foundRootless with
            | Some validRootlessPath ->
                Discovered(validRootlessPath, Rootless)
            | None ->
                // 3. Kiểm tra Rootful Socket của toàn hệ thống
                let rootfulPath = "/run/podman/podman.sock"
                attemptedPaths.Add(rootfulPath)
                if File.Exists(rootfulPath) then
                    Discovered(rootfulPath, Rootful)
                else
                    NotFound(
                        Seq.toList attemptedPaths,
                        "Dịch vụ Podman Socket chưa được kích hoạt. Hãy chạy lệnh: systemctl --user enable --now podman.socket"
                    )
