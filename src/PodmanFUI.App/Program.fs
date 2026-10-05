namespace PodmanFUI.App

open System
open PodmanFUI.Domain
open PodmanFUI.Domain.Errors
open PodmanFUI.Domain.Models
open PodmanFUI.Infrastructure
open PodmanFUI.Presentation
open PodmanFUI.Presentation.MvuLoop

module Program =

    [<EntryPoint>]
    let main argv =
        try
            let isPoc = argv |> Array.exists (fun arg -> arg = "--poc" || arg = "--diagnose")

            // 1. Dò tìm đường dẫn Unix Domain Socket theo thứ tự ưu tiên
            match SocketDiscovery.discoverSocket() with
            | NotFound (attemptedPaths, hint) ->
                let err = SocketNotFound ("", attemptedPaths, hint)
                PocRenderer.renderErrorDialog err
                1
            | Discovered (socketPath, mode) ->
                if isPoc then
                    // Chế độ chẩn đoán nhanh Socket từ Milestone 1
                    PocRenderer.renderWelcomeBanner()
                    use client = new PodmanSocketClient(socketPath, mode)
                    let socketClient = client :> IPodmanSocketClient
                    let result =
                        socketClient.GetSystemInfoAsync()
                        |> Async.RunSynchronously

                    match result with
                    | Ok systemInfo ->
                        PocRenderer.renderSystemInfoTable systemInfo
                        0
                    | Error err ->
                        PocRenderer.renderErrorDialog err
                        1
                else
                    // Chế độ Dashboard TUI tương tác chính của Milestone 2
                    use client = new ContainerApiClient(socketPath)
                    let service = client :> IContainerService
                    let dashboardApp = new DashboardApp(service)
                    dashboardApp.Run()
        finally
            // Đảm bảo terminal luôn được khôi phục trạng thái chuẩn khi tiến trình kết thúc
            try
                let resetSeq = "\u001B[?1006l\u001B[?1015l\u001B[?1003l\u001B[?1002l\u001B[?1001l\u001B[?1000l\u001B[?2004l\u001B[?1049l\u001B[?25h\u001B[0m"
                Console.Write(resetSeq)
                Console.Out.Flush()
                Console.ResetColor()
                Console.CursorVisible <- true
            with _ -> ()
