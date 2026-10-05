namespace PodmanFUI.App

open System
open PodmanFUI.Domain
open PodmanFUI.Domain.Errors
open PodmanFUI.Domain.Models
open PodmanFUI.Infrastructure
open PodmanFUI.Presentation

module Program =

    [<EntryPoint>]
    let main _argv =
        // 1. Hiển thị banner khởi động ứng dụng
        PocRenderer.renderWelcomeBanner()

        // 2. Dò tìm đường dẫn Unix Domain Socket theo thứ tự ưu tiên
        match SocketDiscovery.discoverSocket() with
        | NotFound (attemptedPaths, hint) ->
            let err = SocketNotFound ("", attemptedPaths, hint)
            PocRenderer.renderErrorDialog err
            1
        | Discovered (socketPath, mode) ->
            // 3. Khởi tạo client kết nối Unix Domain Socket
            use client = new PodmanSocketClient(socketPath, mode)
            let socketClient = client :> IPodmanSocketClient

            // 4. Gửi truy vấn kiểm tra tới endpoint GET /v4.0.0/libpod/info
            let result =
                socketClient.GetSystemInfoAsync()
                |> Async.RunSynchronously

            // 5. Kết xuất dữ liệu nghiệm thu hoặc cảnh báo lỗi
            match result with
            | Ok systemInfo ->
                PocRenderer.renderSystemInfoTable systemInfo
                0
            | Error err ->
                PocRenderer.renderErrorDialog err
                1
