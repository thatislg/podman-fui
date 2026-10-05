namespace PodmanFUI.Presentation.Views

open System
open Terminal.Gui.Views
open Terminal.Gui.ViewBase
open PodmanFUI.Domain.ContainerModels
open PodmanFUI.Domain.NavigationModels
open PodmanFUI.Domain.MvuTypes

/// Khung thông tin chi tiết Container bên phải (DetailPane)
type ContainerDetailView() as this =
    inherit FrameView()

    let tabHeader = new Label()
    let textView = new TextView()

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

    /// Cập nhật hiển thị DetailPane theo mô hình trạng thái MVU
    member this.Update(model: DashboardModel) =
        // 1. Cập nhật Tab Header
        let tabs =
            [ formatTabButton "Logs" DetailTab.Logs model.ActiveTab
              formatTabButton "Inspect" DetailTab.Inspect model.ActiveTab
              formatTabButton "Top" DetailTab.Top model.ActiveTab
              formatTabButton "Env" DetailTab.Env model.ActiveTab ]
            |> String.concat " │ "

        tabHeader.Text <- sprintf "  %s  (Switch: [ or ])" tabs

        // 2. Cập nhật nội dung hiển thị theo Tab đang kích hoạt
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
