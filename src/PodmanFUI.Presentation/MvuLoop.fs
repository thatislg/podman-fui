namespace PodmanFUI.Presentation

open System
open System.Threading.Tasks
open Terminal.Gui.App
open Terminal.Gui.Views
open Terminal.Gui.ViewBase
open Terminal.Gui.Input
open PodmanFUI.Domain
open PodmanFUI.Domain.Errors
open PodmanFUI.Domain.ContainerModels
open PodmanFUI.Domain.NavigationModels
open PodmanFUI.Domain.MvuTypes
open PodmanFUI.Infrastructure
open PodmanFUI.Presentation.Views
open PodmanFUI.Presentation.ResponsiveLayoutManager

module MvuLoop =

    /// Khởi tạo trạng thái thuần khiết ban đầu của Dashboard
    let init () : DashboardModel * DashboardCmd =
        let initialModel =
            { ActiveCategory = NavigationCategory.Containers
              ActiveTab = DetailTab.Inspect
              CurrentFocus = ActiveFocus.Sidebar
              Containers = []
              FilteredContainers = []
              SelectedIndex = 0
              SelectedContainerId = None
              DetailData = None
              IsFilterActive = false
              FilterQuery = ""
              ActiveModal = ModalState.Closed
              IsLoading = true
              ErrorMessage = None
              TerminalWidth = 100
              TerminalHeight = 30 }
        (initialModel, DashboardCmd.FetchContainers)

    /// Hàm cập nhật trạng thái thuần khiết (Pure Update Function)
    let rec update (msg: DashboardMsg) (model: DashboardModel) : DashboardModel * DashboardCmd =
        match msg with
        | RowSelected idx ->
            if model.FilteredContainers.IsEmpty then
                { model with SelectedIndex = 0; SelectedContainerId = None }, DashboardCmd.NoCmd
            else
                let safeIdx = min (model.FilteredContainers.Length - 1) (max 0 idx)
                let item = model.FilteredContainers.[safeIdx]
                let cmd = DashboardCmd.FetchDetail item.Id
                { model with SelectedIndex = safeIdx; SelectedContainerId = Some item.Id }, cmd

        | CategoryChanged cat ->
            if cat = NavigationCategory.Containers then
                let cmd =
                    match model.SelectedContainerId with
                    | Some id -> DashboardCmd.FetchDetail id
                    | None ->
                        match model.FilteredContainers with
                        | head :: _ -> DashboardCmd.FetchDetail head.Id
                        | [] -> DashboardCmd.FetchContainers
                { model with ActiveCategory = cat }, cmd
            else
                { model with ActiveCategory = cat; DetailData = None }, DashboardCmd.NoCmd

        | TabChanged tab ->
            { model with ActiveTab = tab }, DashboardCmd.NoCmd

        | ToggleFocus ->
            let newFocus =
                match model.CurrentFocus with
                | ActiveFocus.Sidebar -> ActiveFocus.DetailPane
                | ActiveFocus.DetailPane -> ActiveFocus.Sidebar
                | ActiveFocus.ModalDialog -> ActiveFocus.Sidebar
            { model with CurrentFocus = newFocus }, DashboardCmd.NoCmd

        | RequestAction (action, containerId) ->
            match action with
            | ContainerAction.ExecShell ->
                model, DashboardCmd.ExecuteShell containerId
            | _ ->
                model, DashboardCmd.PerformContainerAction (action, containerId)

        | ConfirmModal ->
            match model.ActiveModal with
            | ConfirmActionDialog (action, id, _) ->
                let m = { model with ActiveModal = Closed }
                m, DashboardCmd.PerformContainerAction (action, id)
            | FilterInputDialog ->
                let m = { model with ActiveModal = Closed; IsFilterActive = false }
                m, DashboardCmd.NoCmd
            | ErrorAlertDialog _
            | Closed ->
                { model with ActiveModal = Closed }, DashboardCmd.NoCmd

        | DismissModal ->
            { model with ActiveModal = Closed; IsFilterActive = false }, DashboardCmd.NoCmd

        | UpdateFilter query ->
            let filtered =
                if String.IsNullOrWhiteSpace query then
                    model.Containers
                else
                    let q = query.ToLowerInvariant()
                    model.Containers
                    |> List.filter (fun c ->
                        c.PrimaryName.ToLowerInvariant().Contains(q) ||
                        c.ShortId.ToLowerInvariant().Contains(q) ||
                        c.Image.ToLowerInvariant().Contains(q))
            let safeIdx = 0
            let newSelectedId =
                match filtered with
                | head :: _ -> Some head.Id
                | [] -> None
            let cmd =
                match newSelectedId with
                | Some id -> DashboardCmd.FetchDetail id
                | None -> DashboardCmd.NoCmd
            { model with FilterQuery = query; FilteredContainers = filtered; SelectedIndex = safeIdx; SelectedContainerId = newSelectedId }, cmd

        | ClearFilter ->
            let newSelectedId =
                match model.Containers with
                | head :: _ -> Some head.Id
                | [] -> None
            let cmd =
                match newSelectedId with
                | Some id -> DashboardCmd.FetchDetail id
                | None -> DashboardCmd.NoCmd
            { model with
                IsFilterActive = false
                FilterQuery = ""
                FilteredContainers = model.Containers
                SelectedIndex = 0
                SelectedContainerId = newSelectedId
                ActiveModal = Closed }, cmd

        | ContainersLoaded result ->
            match result with
            | Ok containers ->
                let filtered =
                    if String.IsNullOrWhiteSpace model.FilterQuery then
                        containers
                    else
                        let q = model.FilterQuery.ToLowerInvariant()
                        containers
                        |> List.filter (fun c ->
                            c.PrimaryName.ToLowerInvariant().Contains(q) ||
                            c.ShortId.ToLowerInvariant().Contains(q) ||
                            c.Image.ToLowerInvariant().Contains(q))

                let preservedIdx =
                    match model.SelectedContainerId with
                    | Some prevId ->
                        let found = filtered |> List.tryFindIndex (fun c -> c.Id = prevId)
                        defaultArg found 0
                    | None -> 0

                let safeIdx = if filtered.IsEmpty then 0 else min (filtered.Length - 1) (max 0 preservedIdx)
                let newSelectedId =
                    if not filtered.IsEmpty && safeIdx < filtered.Length then
                        Some filtered.[safeIdx].Id
                    else
                        None

                let cmd =
                    match newSelectedId with
                    | Some id -> DashboardCmd.FetchDetail id
                    | None -> DashboardCmd.NoCmd

                { model with
                    Containers = containers
                    FilteredContainers = filtered
                    SelectedIndex = safeIdx
                    SelectedContainerId = newSelectedId
                    IsLoading = false
                    ErrorMessage = None }, cmd

            | Error err ->
                { model with IsLoading = false; ErrorMessage = Some (sprintf "%A" err) }, DashboardCmd.NoCmd

        | DetailLoaded result ->
            match result with
            | Ok detail ->
                { model with DetailData = Some detail; IsLoading = false }, DashboardCmd.NoCmd
            | Error _ ->
                { model with DetailData = None; IsLoading = false }, DashboardCmd.NoCmd

        | ActionExecuted result ->
            match result with
            | Ok (_action, _id) ->
                // Nạp lại danh sách để đồng bộ thực tế
                model, DashboardCmd.FetchContainers
            | Error err ->
                let errorModal = ErrorAlertDialog ("Action Failed", sprintf "%A" err)
                { model with ActiveModal = errorModal }, DashboardCmd.NoCmd

        | TerminalResized (w, h) ->
            { model with TerminalWidth = w; TerminalHeight = h }, DashboardCmd.NoCmd

        | KeyPressed keyStr ->
            // 1. Nếu đang có hộp thoại Modal hiển thị
            match model.ActiveModal with
            | ConfirmActionDialog _ ->
                match keyStr with
                | "y" | "Y" | "Enter" ->
                    update ConfirmModal model
                | "n" | "N" | "Escape" | "Esc" ->
                    update DismissModal model
                | _ ->
                    model, DashboardCmd.NoCmd

            | FilterInputDialog ->
                match keyStr with
                | "Escape" | "Esc" ->
                    update ClearFilter model
                | "Enter" ->
                    update ConfirmModal model
                | _ ->
                    model, DashboardCmd.NoCmd

            | ErrorAlertDialog _ ->
                match keyStr with
                | "Escape" | "Esc" | "Enter" ->
                    update DismissModal model
                | _ ->
                    model, DashboardCmd.NoCmd

            | Closed ->
                // 2. Chế độ điều hướng thông thường
                match keyStr with
                | "1" -> update (CategoryChanged NavigationCategory.Pods) model
                | "2" -> update (CategoryChanged NavigationCategory.Containers) model
                | "3" -> update (CategoryChanged NavigationCategory.Images) model
                | "4" -> update (CategoryChanged NavigationCategory.Volumes) model
                | "5" -> update (CategoryChanged NavigationCategory.Networks) model
                | "Tab" -> update ToggleFocus model
                | "]" | "Right" | "CursorRight" ->
                    let nextTab =
                        match model.ActiveTab with
                        | DetailTab.Logs -> DetailTab.Inspect
                        | DetailTab.Inspect -> DetailTab.Top
                        | DetailTab.Top -> DetailTab.Env
                        | DetailTab.Env -> DetailTab.Logs
                    update (TabChanged nextTab) model
                | "[" | "Left" | "CursorLeft" ->
                    let prevTab =
                        match model.ActiveTab with
                        | DetailTab.Logs -> DetailTab.Env
                        | DetailTab.Inspect -> DetailTab.Logs
                        | DetailTab.Top -> DetailTab.Inspect
                        | DetailTab.Env -> DetailTab.Top
                    update (TabChanged prevTab) model
                | "j" | "Down" | "CursorDown" ->
                    if model.ActiveCategory = NavigationCategory.Containers && not model.FilteredContainers.IsEmpty then
                        let nextIdx = min (model.FilteredContainers.Length - 1) (model.SelectedIndex + 1)
                        update (RowSelected nextIdx) model
                    else
                        model, DashboardCmd.NoCmd
                | "k" | "Up" | "CursorUp" ->
                    if model.ActiveCategory = NavigationCategory.Containers && not model.FilteredContainers.IsEmpty then
                        let prevIdx = max 0 (model.SelectedIndex - 1)
                        update (RowSelected prevIdx) model
                    else
                        model, DashboardCmd.NoCmd
                | "s" ->
                    if model.ActiveCategory = NavigationCategory.Containers && not model.FilteredContainers.IsEmpty && model.SelectedIndex >= 0 && model.SelectedIndex < model.FilteredContainers.Length then
                        let c = model.FilteredContainers.[model.SelectedIndex]
                        match c.Status with
                        | ContainerStatus.Running ->
                            update (RequestAction (ContainerAction.Stop, c.Id)) model
                        | ContainerStatus.Paused ->
                            update (RequestAction (ContainerAction.Unpause, c.Id)) model
                        | ContainerStatus.Exited
                        | ContainerStatus.Created
                        | ContainerStatus.Restarting
                        | ContainerStatus.Dead ->
                            update (RequestAction (ContainerAction.Start, c.Id)) model
                    else
                        model, DashboardCmd.NoCmd
                | "r" ->
                    if model.ActiveCategory = NavigationCategory.Containers && not model.FilteredContainers.IsEmpty && model.SelectedIndex >= 0 && model.SelectedIndex < model.FilteredContainers.Length then
                        let c = model.FilteredContainers.[model.SelectedIndex]
                        update (RequestAction (ContainerAction.Restart, c.Id)) model
                    else
                        model, DashboardCmd.NoCmd
                | "p" ->
                    if model.ActiveCategory = NavigationCategory.Containers && not model.FilteredContainers.IsEmpty && model.SelectedIndex >= 0 && model.SelectedIndex < model.FilteredContainers.Length then
                        let c = model.FilteredContainers.[model.SelectedIndex]
                        match c.Status with
                        | ContainerStatus.Running ->
                            update (RequestAction (ContainerAction.Pause, c.Id)) model
                        | ContainerStatus.Paused ->
                            update (RequestAction (ContainerAction.Unpause, c.Id)) model
                        | _ ->
                            model, DashboardCmd.NoCmd
                    else
                        model, DashboardCmd.NoCmd
                | "d" ->
                    if model.ActiveCategory = NavigationCategory.Containers && not model.FilteredContainers.IsEmpty && model.SelectedIndex >= 0 && model.SelectedIndex < model.FilteredContainers.Length then
                        let c = model.FilteredContainers.[model.SelectedIndex]
                        let modal = ConfirmActionDialog (ContainerAction.Delete, c.Id, c.PrimaryName)
                        { model with ActiveModal = modal }, DashboardCmd.NoCmd
                    else
                        model, DashboardCmd.NoCmd
                | "e" ->
                    if model.ActiveCategory = NavigationCategory.Containers && not model.FilteredContainers.IsEmpty && model.SelectedIndex >= 0 && model.SelectedIndex < model.FilteredContainers.Length then
                        let c = model.FilteredContainers.[model.SelectedIndex]
                        update (RequestAction (ContainerAction.ExecShell, c.Id)) model
                    else
                        model, DashboardCmd.NoCmd
                | "/" ->
                    if model.ActiveCategory = NavigationCategory.Containers then
                        { model with IsFilterActive = true; ActiveModal = FilterInputDialog }, DashboardCmd.NoCmd
                    else
                        model, DashboardCmd.NoCmd
                | _ ->
                    model, DashboardCmd.NoCmd

    /// Host điều phối vòng lặp Terminal.Gui và tương tác bất đồng bộ
    type DashboardApp(containerService: IContainerService) =
        let app = Application.Create()
        let top = new Runnable()

        let topBar = new TopBarView()
        let sidebar = new SidebarView()
        let detailView = new ContainerDetailView()
        let footerView = new FooterView()
        let modalsView = new ModalsView()

        let mutable model = fst (init ())

        let render (m: DashboardModel) =
            let w = if app.Driver <> null && app.Driver.Cols > 0 then app.Driver.Cols else m.TerminalWidth
            let h = if app.Driver <> null && app.Driver.Rows > 0 then app.Driver.Rows else m.TerminalHeight
            let plan = ResponsiveLayoutManager.computeLayout w h m.CurrentFocus

            topBar.X <- Pos.Absolute(plan.TopBar.X)
            topBar.Y <- Pos.Absolute(plan.TopBar.Y)
            topBar.Width <- Dim.Absolute(plan.TopBar.Width)
            topBar.Height <- Dim.Absolute(plan.TopBar.Height)
            topBar.Visible <- plan.TopBar.IsVisible
            topBar.Update(m)

            footerView.X <- Pos.Absolute(plan.FooterBar.X)
            footerView.Y <- Pos.Absolute(plan.FooterBar.Y)
            footerView.Width <- Dim.Absolute(plan.FooterBar.Width)
            footerView.Height <- Dim.Absolute(plan.FooterBar.Height)
            footerView.Visible <- plan.FooterBar.IsVisible
            footerView.Update(m)

            sidebar.X <- Pos.Absolute(plan.Sidebar.X)
            sidebar.Y <- Pos.Absolute(plan.Sidebar.Y)
            sidebar.Width <- Dim.Absolute(plan.Sidebar.Width)
            sidebar.Height <- Dim.Absolute(plan.Sidebar.Height)
            sidebar.Visible <- plan.Sidebar.IsVisible
            sidebar.Update(m, plan.Mode)

            detailView.X <- Pos.Absolute(plan.DetailPane.X)
            detailView.Y <- Pos.Absolute(plan.DetailPane.Y)
            detailView.Width <- Dim.Absolute(plan.DetailPane.Width)
            detailView.Height <- Dim.Absolute(plan.DetailPane.Height)
            detailView.Visible <- plan.DetailPane.IsVisible
            detailView.Update(m)

            modalsView.Update(m)

            // Cập nhật Terminal.Gui focus dựa theo m.CurrentFocus
            match m.CurrentFocus with
            | ActiveFocus.Sidebar ->
                sidebar.SetFocus() |> ignore
            | ActiveFocus.DetailPane ->
                detailView.SetFocus() |> ignore
            | ActiveFocus.ModalDialog ->
                if modalsView.Visible && modalsView.InputField.Visible then
                    modalsView.InputField.SetFocus() |> ignore
                elif modalsView.Visible then
                    modalsView.SetFocus() |> ignore

        let rec dispatch (msg: DashboardMsg) =
            let (newModel, cmd) = update msg model
            model <- newModel
            render newModel
            executeCmd cmd

        and executeCmd (cmd: DashboardCmd) =
            match cmd with
            | DashboardCmd.NoCmd -> ()
            | DashboardCmd.Batch cmds ->
                for c in cmds do executeCmd c
            | DashboardCmd.FetchContainers ->
                Task.Run(fun () ->
                    async {
                        let! result = containerService.ListContainersAsync()
                        app.Invoke(fun () -> dispatch (ContainersLoaded result))
                    } |> Async.StartImmediate
                ) |> ignore
            | DashboardCmd.FetchDetail id ->
                Task.Run(fun () ->
                    async {
                        let! result = containerService.GetContainerDetailAsync(id)
                        app.Invoke(fun () -> dispatch (DetailLoaded result))
                    } |> Async.StartImmediate
                ) |> ignore
            | DashboardCmd.PerformContainerAction (action, id) ->
                Task.Run(fun () ->
                    async {
                        let! result = containerService.PerformActionAsync action id
                        app.Invoke(fun () -> dispatch (ActionExecuted (result |> Result.map (fun () -> (action, id)))))
                    } |> Async.StartImmediate
                ) |> ignore
            | DashboardCmd.ExecuteShell id ->
                let isRunning =
                    model.Containers
                    |> List.tryFind (fun c -> c.Id = id || c.ShortId = id)
                    |> Option.map (fun c -> c.Status = ContainerStatus.Running)
                    |> Option.defaultValue false

                if not isRunning then
                    let err = HttpFailure (409, "Container is not running. Interactive shell requires a running container.")
                    let errorModal = ErrorAlertDialog ("Cannot Execute Shell", sprintf "%A" err)
                    dispatch (DismissModal)
                    model <- { model with ActiveModal = errorModal }
                    render model
                else
                    Task.Run(fun () ->
                        let suspendFn () =
                            if app.Driver <> null then
                                app.Driver.Suspend()
                        let resumeFn () =
                            if app.Driver <> null then
                                app.Driver.Init()
                                app.Driver.Refresh()

                        let _ = ProcessExecutionService.runInteractiveShell id true (Some suspendFn) (Some resumeFn)
                        app.Invoke(fun () ->
                            executeCmd DashboardCmd.FetchContainers
                        )
                    ) |> ignore

        member this.Run() : int =
            try
                app.Init() |> ignore

                top.Add(topBar) |> ignore
                top.Add(sidebar) |> ignore
                top.Add(detailView) |> ignore
                top.Add(footerView) |> ignore
                top.Add(modalsView) |> ignore

                // Gắn kết sự kiện chuột (Mouse Event Listeners)
                topBar.CategoryClicked.Add(fun cat ->
                    dispatch (CategoryChanged cat)
                )

                detailView.TabClicked.Add(fun tab ->
                    dispatch (TabChanged tab)
                )

                sidebar.ListView.ValueChanged.Add(fun e ->
                    if e.NewValue.HasValue && model.ActiveCategory = NavigationCategory.Containers then
                        let newIdx = e.NewValue.Value
                        if newIdx <> model.SelectedIndex && newIdx >= 0 && newIdx < model.FilteredContainers.Length then
                            dispatch (RowSelected newIdx)
                )

                // Gắn kết bàn phím toàn cục (Global Keyboard Hook qua app.Keyboard.KeyDown)
                app.Keyboard.KeyDown.Add(fun (key: Key) ->
                    match model.ActiveModal with
                    | ConfirmActionDialog _ ->
                        let baseKey = key.NoShift.NoCtrl.NoAlt
                        if baseKey = Key.Y || baseKey = Key.Enter then
                            key.Handled <- true
                            dispatch ConfirmModal
                        elif baseKey = Key.N || baseKey = Key.Esc then
                            key.Handled <- true
                            dispatch DismissModal
                    | FilterInputDialog ->
                        let baseKey = key.NoShift.NoCtrl.NoAlt
                        if baseKey = Key.Enter then
                            key.Handled <- true
                            dispatch (UpdateFilter modalsView.InputField.Text)
                            dispatch ConfirmModal
                        elif baseKey = Key.Esc then
                            key.Handled <- true
                            dispatch ClearFilter
                        // Không đánh dấu Handled để TextField nhận phím gõ bình thường
                    | ErrorAlertDialog _ ->
                        let baseKey = key.NoShift.NoCtrl.NoAlt
                        if baseKey = Key.Enter || baseKey = Key.Esc then
                            key.Handled <- true
                            dispatch DismissModal
                    | Closed ->
                        let baseKey = key.NoShift.NoCtrl.NoAlt
                        if baseKey = Key.Q || key = Key.Q.WithCtrl || key = Key.C.WithCtrl then
                            key.Handled <- true
                            app.RequestStop(top)
                        elif baseKey = Key.D1 then
                            key.Handled <- true
                            dispatch (CategoryChanged NavigationCategory.Pods)
                        elif baseKey = Key.D2 then
                            key.Handled <- true
                            dispatch (CategoryChanged NavigationCategory.Containers)
                        elif baseKey = Key.D3 then
                            key.Handled <- true
                            dispatch (CategoryChanged NavigationCategory.Images)
                        elif baseKey = Key.D4 then
                            key.Handled <- true
                            dispatch (CategoryChanged NavigationCategory.Volumes)
                        elif baseKey = Key.D5 then
                            key.Handled <- true
                            dispatch (CategoryChanged NavigationCategory.Networks)
                        elif baseKey = Key.Tab then
                            key.Handled <- true
                            dispatch ToggleFocus
                        elif baseKey = Key.F1 then
                            key.Handled <- true
                            dispatch (TabChanged DetailTab.Logs)
                        elif baseKey = Key.F2 then
                            key.Handled <- true
                            dispatch (TabChanged DetailTab.Inspect)
                        elif baseKey = Key.F3 then
                            key.Handled <- true
                            dispatch (TabChanged DetailTab.Top)
                        elif baseKey = Key.F4 then
                            key.Handled <- true
                            dispatch (TabChanged DetailTab.Env)
                        elif baseKey = Key.CursorLeft || baseKey = Key(int '[') then
                            key.Handled <- true
                            dispatch (KeyPressed "[")
                        elif baseKey = Key.CursorRight || baseKey = Key(int ']') then
                            key.Handled <- true
                            dispatch (KeyPressed "]")
                        elif baseKey = Key.CursorUp || baseKey = Key.K then
                            key.Handled <- true
                            dispatch (KeyPressed "k")
                        elif baseKey = Key.CursorDown || baseKey = Key.J then
                            key.Handled <- true
                            dispatch (KeyPressed "j")
                        elif baseKey = Key.S then
                            key.Handled <- true
                            dispatch (KeyPressed "s")
                        elif baseKey = Key.R then
                            key.Handled <- true
                            dispatch (KeyPressed "r")
                        elif baseKey = Key.P then
                            key.Handled <- true
                            dispatch (KeyPressed "p")
                        elif baseKey = Key.D then
                            key.Handled <- true
                            dispatch (KeyPressed "d")
                        elif baseKey = Key.E then
                            key.Handled <- true
                            dispatch (KeyPressed "e")
                        elif baseKey = Key(int '/') then
                            key.Handled <- true
                            dispatch (KeyPressed "/")
                )

                // Gắn kết thay đổi kích thước terminal
                if app.Driver <> null then
                    app.Driver.SizeChanged.Add(fun _ ->
                        dispatch (TerminalResized (app.Driver.Cols, app.Driver.Rows))
                    )

                // Kích hoạt nạp dữ liệu ban đầu
                let initialCols = if app.Driver <> null && app.Driver.Cols > 0 then app.Driver.Cols else 100
                let initialRows = if app.Driver <> null && app.Driver.Rows > 0 then app.Driver.Rows else 30
                dispatch (TerminalResized (initialCols, initialRows))
                executeCmd DashboardCmd.FetchContainers

                app.Run(top) |> ignore
                0
            finally
                // Giải phóng hoàn toàn Terminal.Gui và khôi phục chế độ Terminal chuẩn
                try (app :> IDisposable).Dispose() with _ -> ()
                try top.Dispose() with _ -> ()
                try
                    // Tắt chuột SGR (?1006l), chuột mọi chuyển động (?1003l), chế độ dán (?2004l), thoát buffer phụ (?1049l), hiện con trỏ (?25h)
                    let resetSeq = "\u001B[?1006l\u001B[?1015l\u001B[?1003l\u001B[?1002l\u001B[?1001l\u001B[?1000l\u001B[?2004l\u001B[?1049l\u001B[?25h\u001B[0m"
                    Console.Write(resetSeq)
                    Console.Out.Flush()
                    Console.ResetColor()
                    Console.CursorVisible <- true
                with _ -> ()
