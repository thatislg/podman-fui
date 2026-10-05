namespace PodmanFUI.Presentation

open Spectre.Console
open PodmanFUI.Domain.Errors
open PodmanFUI.Domain.Models

module PocRenderer =

    /// Hiển thị thanh tiêu đề ứng dụng và thông tin bản PoC
    let renderWelcomeBanner () : unit =
        AnsiConsole.WriteLine()
        let rule = new Rule("[bold cyan1]podman-FUI[/] - [grey]F# Modern Terminal User Interface for Podman[/]")
        rule.Border <- BoxBorder.Rounded
        rule.Style <- Style(Theme.borderColor)
        AnsiConsole.Write(rule)
        AnsiConsole.WriteLine()

    /// Hiển thị bảng thông tin hệ thống dạng Spectre Rounded Table theo đặc tả Mục 6.2
    let renderSystemInfoTable (info: SystemInfo) : unit =
        let table = new Table()
        table.Title <- TableTitle("[bold cyan1]Podman Engine Environment Validation (Milestone 1 PoC)[/]")
        table.Border <- TableBorder.Rounded
        table.BorderColor(Theme.borderColor) |> ignore

        let col1 = new TableColumn("[grey]Property Name[/]")
        col1.PadRight(2) |> ignore
        col1.NoWrap <- true
        table.AddColumn(col1) |> ignore

        let col2 = new TableColumn("[grey]Detected Value[/]")
        table.AddColumn(col2) |> ignore

        // Hàng 1: Podman Version
        table.AddRow(
            "[italic grey]Podman Version[/]",
            sprintf "[bold white]%s[/] [grey](API: %s, Go: %s)[/]" info.Version.Version info.Version.ApiVersion info.Version.GoVersion
        ) |> ignore

        // Hàng 2: Target OS / Arch
        table.AddRow(
            "[italic grey]Target OS / Arch[/]",
            sprintf "[bold white]%s / %s[/]" info.Host.OS info.Host.Arch
        ) |> ignore

        // Hàng 3: Linux Kernel
        table.AddRow(
            "[italic grey]Linux Kernel[/]",
            sprintf "[bold white]%s[/]" info.Host.Kernel
        ) |> ignore

        // Hàng 4: Cgroup Version
        table.AddRow(
            "[italic grey]Cgroup Version[/]",
            sprintf "[bold white]%s[/] [grey](Manager: %s)[/]" info.Host.Cgroup.Version info.Host.Cgroup.Manager
        ) |> ignore

        // Hàng 5: Cgroup Controllers
        let controllersStr =
            if info.Host.Cgroup.Controllers.IsEmpty then
                "[italic grey]none[/]"
            else
                String.concat ", " info.Host.Cgroup.Controllers

        table.AddRow(
            "[italic grey]Cgroup Controllers[/]",
            sprintf "[bold white]%s[/]" controllersStr
        ) |> ignore

        // Hàng 6: Storage Driver
        table.AddRow(
            "[italic grey]Storage Driver[/]",
            sprintf "[bold white]%s[/] [grey](GraphRoot: %s)[/]" info.Host.StorageDriver info.Host.GraphRoot
        ) |> ignore

        // Hàng 7: Execution Mode
        let modeMarkup =
            match info.SocketMode with
            | Rootless -> "[bold green]✓ Rootless (Unprivileged user mode)[/]"
            | Rootful -> "[bold yellow]! Rootful (System root mode)[/]"
            | Custom -> "[bold cyan1]⚙ Custom (Environment variable mode)[/]"

        table.AddRow(
            "[italic grey]Execution Mode[/]",
            modeMarkup
        ) |> ignore

        // Hàng 8: Socket Endpoint
        table.AddRow(
            "[italic grey]Socket Endpoint[/]",
            sprintf "[bold white]%s[/]" info.SocketPath
        ) |> ignore

        AnsiConsole.Write(table)
        AnsiConsole.WriteLine()
        AnsiConsole.MarkupLine("[bold green]✓ Milestone 1 Technical Validation Succeeded (DoD Achieved)[/]")
        AnsiConsole.WriteLine()

    /// Hiển thị cảnh báo lỗi theo mã lỗi và kịch bản Ma trận lỗi (Mục 5)
    let renderErrorDialog (error: ConnectionError) : unit =
        let (errCode, errMsg, hint) =
            match error with
            | SocketNotFound (path, attempted, advice) ->
                let attemptedList = String.concat "\n  - " attempted
                ("ERR_SOCK_404",
                 sprintf "Podman Socket not found at: %s\nAttempted paths:\n  - %s" path attemptedList,
                 advice)
            | AccessDenied (path, msg) ->
                ("ERR_SOCK_403",
                 sprintf "Access denied to socket at: %s" path,
                 sprintf "%s. Please verify user permissions." msg)
            | HttpFailure (statusCode, reason) ->
                ("ERR_HTTP_500",
                 sprintf "Podman Engine returned HTTP %d" statusCode,
                 sprintf "Reason: %s" reason)
            | Timeout msg ->
                ("ERR_CONN_TIMEOUT",
                 "Connection to Podman Socket timed out (10s)",
                 msg)
            | DeserializationError msg ->
                ("ERR_JSON_PARSE",
                 "Failed to deserialize response from Podman API",
                 msg)

        let panelContent =
            sprintf "[bold red][[%s]][/] %s\n\n[bold yellow]Remediation:[/] %s" errCode (Markup.Escape(errMsg)) (Markup.Escape(hint))

        let panel = new Panel(panelContent)
        panel.Header <- PanelHeader("[bold red] Podman Connection Failure [/]")
        panel.Border <- BoxBorder.Rounded
        panel.BorderColor(Theme.failureColor) |> ignore
        panel.Padding <- Padding(1, 1, 1, 1)

        AnsiConsole.WriteLine()
        AnsiConsole.Write(panel)
        AnsiConsole.WriteLine()
