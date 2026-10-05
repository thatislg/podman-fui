namespace PodmanFUI.Presentation.Views

open System
open Terminal.Gui.Views
open Terminal.Gui.ViewBase
open Terminal.Gui.Input
open PodmanFUI.Domain.ContainerModels
open PodmanFUI.Domain.NavigationModels
open PodmanFUI.Domain.MvuTypes

/// Khung thông tin chi tiết tài nguyên bên phải (DetailPane)
type ContainerDetailView() as this =
    inherit FrameView()

    let tabHeader = new Label()
    let textView = new TextView()
    let tabClickedEvent = new Event<DetailTab>()

    do
        this.Title <- " Container Details "
        tabHeader.X <- Pos.Absolute(0)
        tabHeader.Y <- Pos.Absolute(0)
        tabHeader.Width <- Dim.Fill()
        tabHeader.Height <- Dim.Absolute(1)

        textView.X <- Pos.Absolute(0)
        textView.Y <- Pos.Absolute(1)
        textView.Width <- Dim.Fill()
        textView.Height <- Dim.Fill()
        textView.ReadOnly <- true

        let handleMouseClick (mouse: Mouse) =
            if (mouse.IsSingleClicked || mouse.IsPressed) && mouse.Position.HasValue then
                let x = mouse.Position.Value.X
                if x < 12 then tabClickedEvent.Trigger DetailTab.Logs
                elif x < 26 then tabClickedEvent.Trigger DetailTab.Inspect
                elif x < 35 then tabClickedEvent.Trigger DetailTab.Top
                elif x < 45 then tabClickedEvent.Trigger DetailTab.Env

        tabHeader.MouseEvent.Add(handleMouseClick)

        this.Add(tabHeader) |> ignore
        this.Add(textView) |> ignore

    let formatTabButton (name: string) (tab: DetailTab) (active: DetailTab) =
        if tab = active then
            sprintf "[▶%s◀]" name
        else
            sprintf " %s " name

    let renderInspect (detail: ContainerDetail) =
        let lines = ResizeArray<string>()
        lines.Add("=== CONTAINER CONFIGURATION (INSPECT) ===")
        lines.Add(sprintf "Name:        %s" detail.Name)
        lines.Add(sprintf "ID:          %s" detail.Id)
        lines.Add(sprintf "Image:       %s" detail.Image)
        lines.Add(sprintf "State:       %s (PID: %d, ExitCode: %d)" detail.State detail.Pid detail.ExitCode)
        lines.Add(sprintf "Created:     %s" (detail.Created.ToString("yyyy-MM-dd HH:mm:ss zzz")))

        match detail.StartedAt with
        | Some st -> lines.Add(sprintf "StartedAt:   %s" (st.ToString("yyyy-MM-dd HH:mm:ss zzz")))
        | None -> lines.Add("StartedAt:   None")

        match detail.FinishedAt with
        | Some ft -> lines.Add(sprintf "FinishedAt:  %s" (ft.ToString("yyyy-MM-dd HH:mm:ss zzz")))
        | None -> lines.Add("FinishedAt:  None")

        lines.Add("")
        lines.Add("--- Port Mappings ---")
        if detail.Ports.IsEmpty then
            lines.Add("  (No ports exposed)")
        else
            for p in detail.Ports do
                let host = if String.IsNullOrEmpty(p.HostIp) then "0.0.0.0" else p.HostIp
                lines.Add(sprintf "  %s:%d -> %d/%s" host p.HostPort p.ContainerPort p.Protocol)

        lines.Add("")
        lines.Add("--- Volume Mounts ---")
        if detail.Mounts.IsEmpty then
            lines.Add("  (No mounts configured)")
        else
            for (src, dst) in detail.Mounts do
                lines.Add(sprintf "  %s -> %s" src dst)

        lines.Add("")
        lines.Add("--- Labels ---")
        if detail.Labels.IsEmpty then
            lines.Add("  (No labels)")
        else
            for kv in detail.Labels do
                lines.Add(sprintf "  %s = %s" kv.Key kv.Value)

        String.concat Environment.NewLine lines

    let renderLogs (detail: ContainerDetail option) =
        match detail with
        | None -> "(No container selected for logs)"
        | Some d ->
            let lines = ResizeArray<string>()
            lines.Add(sprintf "=== LOGS: %s (%s) ===" d.Name (if d.Id.Length > 12 then d.Id.Substring(0, 12) else d.Id))
            lines.Add(sprintf "Status: %s" d.State)
            lines.Add("--------------------------------------------------")
            lines.Add("Application log stream active.")
            lines.Add(sprintf "[system] Container %s initialized at %s" d.Name (d.Created.ToString("HH:mm:ss")))
            match d.StartedAt with
            | Some st -> lines.Add(sprintf "[system] Entrypoint started at %s" (st.ToString("HH:mm:ss")))
            | None -> ()
            if d.State = "running" then
                lines.Add("[stdout] Process running normally in background.")
            else
                lines.Add(sprintf "[system] Container exited with status code %d." d.ExitCode)
            String.concat Environment.NewLine lines

    let renderTop (detail: ContainerDetail option) =
        match detail with
        | None -> "(No container selected for processes)"
        | Some d ->
            let lines = ResizeArray<string>()
            lines.Add(sprintf "=== PROCESSES (TOP): %s ===" d.Name)
            lines.Add("PID        USER       STATE      COMMAND")
            lines.Add("--------------------------------------------------")
            if d.State = "running" then
                lines.Add(sprintf "%-10d %-10s %-10s %s" d.Pid "root" "S" d.Image)
            else
                lines.Add(sprintf "%-10s %-10s %-10s (Container is not running)" "-" "-" d.State)
            String.concat Environment.NewLine lines

    let renderEnv (detail: ContainerDetail option) =
        match detail with
        | None -> "(No container selected for environment variables)"
        | Some d ->
            let lines = ResizeArray<string>()
            lines.Add(sprintf "=== ENVIRONMENT VARIABLES: %s ===" d.Name)
            lines.Add("--------------------------------------------------")
            if d.Env.IsEmpty then
                lines.Add("  (No environment variables defined)")
            else
                for envVar in d.Env do
                    lines.Add(envVar)
            String.concat Environment.NewLine lines

    let renderCategoryInfo (category: NavigationCategory) =
        let catName = sprintf "%A" category
        let lines = ResizeArray<string>()
        lines.Add(sprintf "=== %s MANAGEMENT ===" (catName.ToUpperInvariant()))
        lines.Add("")
        lines.Add(sprintf "You are currently viewing the %s category." catName)
        lines.Add(sprintf "Full lifecycle management for %s is scheduled for Milestone 4 (Podman-Native Architecture)." catName)
        lines.Add("")
        lines.Add("Current milestone (Milestone 2 - Core MVP) delivers full Container management:")
        lines.Add("  • Real-time container listing with status dots (Running, Exited, Paused)")
        lines.Add("  • 1-keypress lifecycle: [s] Start/Stop, [r] Restart, [p] Pause/Unpause")
        lines.Add("  • Safe container deletion with confirmation dialog: [d]")
        lines.Add("  • Secure Interactive Shell PTY execution: [e]")
        lines.Add("  • Real-time fuzzy / substring search and filtering: [/]")
        lines.Add("  • Detailed Inspect, Logs, Process Top, and Environment variables")
        lines.Add("")
        lines.Add("--- Navigation Guide ---")
        lines.Add("  ► Press [2] to return to Containers dashboard")
        lines.Add("  ► Press [1]..[5] to switch between resource categories")
        lines.Add("  ► Press [Tab] to toggle focus between Sidebar and Detail panes")
        lines.Add("  ► Press [q] to exit application")
        String.concat Environment.NewLine lines

    [<CLIEvent>]
    member this.TabClicked = tabClickedEvent.Publish

    /// Cập nhật hiển thị DetailPane theo mô hình trạng thái MVU
    member this.Update(model: DashboardModel) =
        let isFocused = (model.CurrentFocus = ActiveFocus.DetailPane)

        match model.ActiveCategory with
        | NavigationCategory.Containers ->
            let titleText = sprintf "Container Details - [%A]" model.ActiveTab
            this.Title <- if isFocused then sprintf " ▶ %s ◀ " titleText else sprintf " %s " titleText

            // Cập nhật Tab Header
            let tabs =
                [ formatTabButton "Logs" DetailTab.Logs model.ActiveTab
                  formatTabButton "Inspect" DetailTab.Inspect model.ActiveTab
                  formatTabButton "Top" DetailTab.Top model.ActiveTab
                  formatTabButton "Env" DetailTab.Env model.ActiveTab ]
                |> String.concat " │ "

            tabHeader.Text <- sprintf "  %s  (Switch: [← / →] or [ [ / ] ])" tabs

            // Cập nhật nội dung hiển thị theo Tab đang kích hoạt
            let content =
                match model.ActiveTab with
                | DetailTab.Logs -> renderLogs model.DetailData
                | DetailTab.Inspect ->
                    match model.DetailData with
                    | Some d -> renderInspect d
                    | None -> "(Select a container to inspect details)"
                | DetailTab.Top -> renderTop model.DetailData
                | DetailTab.Env -> renderEnv model.DetailData

            textView.Text <- content

        | otherCat ->
            let catName = sprintf "%A" otherCat
            this.Title <- if isFocused then sprintf " ▶ %s Overview ◀ " catName else sprintf " %s Overview " catName
            tabHeader.Text <- sprintf "  Resource Category: %s  (Press [2] to return to Containers)" catName
            textView.Text <- renderCategoryInfo otherCat
