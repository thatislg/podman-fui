namespace PodmanFUI.Infrastructure

open System
open System.Diagnostics
open PodmanFUI.Domain.Errors

/// Dịch vụ điều phối tiến trình con hệ điều hành và bọc an toàn phiên Interactive Shell PTY
module ProcessExecutionService =

    let private saveTermios () : string option =
        try
            let psi = ProcessStartInfo("stty", "-g")
            psi.RedirectStandardOutput <- true
            psi.UseShellExecute <- false
            use p = Process.Start(psi)
            let output = p.StandardOutput.ReadToEnd().Trim()
            p.WaitForExit()
            if p.ExitCode = 0 && not (String.IsNullOrWhiteSpace(output)) then
                Some output
            else
                None
        with _ ->
            None

    let private restoreTermios (saved: string option) =
        try
            match saved with
            | Some sttyState ->
                let psi = ProcessStartInfo("stty", sttyState)
                psi.UseShellExecute <- false
                use p = Process.Start(psi)
                p.WaitForExit()
            | None ->
                let psi = ProcessStartInfo("stty", "sane")
                psi.UseShellExecute <- false
                use p = Process.Start(psi)
                p.WaitForExit()
        with _ ->
            ()

    let private executeProcess (containerId: string) (shellCmd: string) : int =
        let psi = ProcessStartInfo("podman")
        psi.ArgumentList.Add("exec")
        psi.ArgumentList.Add("-it")
        psi.ArgumentList.Add(containerId)
        psi.ArgumentList.Add(shellCmd)
        psi.UseShellExecute <- false
        psi.RedirectStandardInput <- false
        psi.RedirectStandardOutput <- false
        psi.RedirectStandardError <- false

        use proc = Process.Start(psi)
        proc.WaitForExit()
        proc.ExitCode

    /// Khởi chạy phiên shell tương tác (PTY) vào trong Container với cơ chế hoàn nguyên an toàn
    let runInteractiveShell
        (containerId: string)
        (isRunning: bool)
        (suspendDriver: (unit -> unit) option)
        (resumeDriver: (unit -> unit) option)
        : Result<int, ConnectionError> =

        // 1. Kiểm tra trạng thái hoạt động của Container
        if not isRunning then
            Error (HttpFailure (409, "Container is not running. Interactive shell requires a running container."))
        else
            // 2. Lưu trạng thái termios hiện tại
            let savedState = saveTermios ()

            // 3. Tạm dừng giao diện TUI nếu có hàm suspend
            suspendDriver |> Option.iter (fun f -> f ())

            try
                // 4. Xóa màn hình và bật con trỏ terminal máy host
                try
                    Console.Write("\u001b[?25h")
                    Console.Clear()
                with _ -> ()

                printfn "=== Entering interactive shell for container %s ===" (if containerId.Length > 12 then containerId.Substring(0, 12) else containerId)
                printfn "Type 'exit' to return to Podman-FUI Dashboard.\n"

                // 5. Thử chạy /bin/sh trước
                let exitCode = executeProcess containerId "/bin/sh"

                // 6. Nếu lỗi 127 (không tìm thấy shell /bin/sh), thử /bin/bash
                let finalExitCode =
                    if exitCode = 127 then
                        printfn "/bin/sh not found, trying /bin/bash..."
                        executeProcess containerId "/bin/bash"
                    else
                        exitCode

                Ok finalExitCode
            finally
                // 7. Khối tối cao dọn dẹp và khôi phục terminal
                restoreTermios savedState
                resumeDriver |> Option.iter (fun f -> f ())
