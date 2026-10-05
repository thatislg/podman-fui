namespace PodmanFUI.Presentation.Views

open System
open System.Collections.ObjectModel
open Terminal.Gui.Views
open Terminal.Gui.ViewBase
open PodmanFUI.Domain.ContainerModels
open PodmanFUI.Domain.NavigationModels
open PodmanFUI.Domain.MvuTypes
open PodmanFUI.Presentation.ResponsiveLayoutManager

/// Khung danh sách tài nguyên bên trái (Sidebar)
type SidebarView() as this =
    inherit FrameView()

    let filterLabel = new Label()
    let listView = new ListView()

    do
        this.Title <- " Containers "
        filterLabel.X <- Pos.Absolute(0)
        filterLabel.Y <- Pos.Absolute(0)
        filterLabel.Width <- Dim.Fill()
        filterLabel.Height <- Dim.Absolute(1)
        filterLabel.Visible <- false

        listView.X <- Pos.Absolute(0)
        listView.Y <- Pos.Absolute(0)
        listView.Width <- Dim.Fill()
        listView.Height <- Dim.Fill()

        this.Add(filterLabel) |> ignore
        this.Add(listView) |> ignore

    let truncate (text: string) (maxLen: int) =
        if String.IsNullOrEmpty(text) then ""
        elif text.Length <= maxLen then text
        elif maxLen <= 3 then text.Substring(0, maxLen)
        else sprintf "%s..." (text.Substring(0, maxLen - 3))

    let formatPorts (ports: PortMapping list) =
        match ports with
        | [] -> ""
        | [ p ] -> sprintf "%d->%d/%s" p.HostPort p.ContainerPort p.Protocol
        | p :: _ -> sprintf "%d->%d (+%d)" p.HostPort p.ContainerPort (ports.Length - 1)

    let getStatusDot (status: ContainerStatus) =
        match status with
        | ContainerStatus.Running -> "●"
        | ContainerStatus.Exited -> "○"
        | ContainerStatus.Paused -> "◌"
        | ContainerStatus.Restarting -> "↻"
        | ContainerStatus.Dead -> "✖"
        | ContainerStatus.Created -> "◇"

    let formatItem (layoutMode: LayoutBreakpoint) (c: ContainerSummary) =
        let dot = getStatusDot c.Status
        match layoutMode with
        | Compact ->
            sprintf "%s %s" dot (truncate c.PrimaryName 20)
        | Standard ->
            let portsStr = formatPorts c.Ports
            sprintf "%s %-12s %-15s %s" dot c.ShortId (truncate c.PrimaryName 15) portsStr
        | Expanded ->
            let portsStr = formatPorts c.Ports
            sprintf "%s %-12s %-18s %-22s %s" dot c.ShortId (truncate c.PrimaryName 18) (truncate c.Image 22) portsStr

    member this.ListView = listView

    /// Cập nhật hiển thị Sidebar theo mô hình trạng thái MVU
    member this.Update(model: DashboardModel, layoutMode: LayoutBreakpoint) =
        let isFocused = (model.CurrentFocus = ActiveFocus.Sidebar)

        match model.ActiveCategory with
        | NavigationCategory.Containers ->
            let titleText = sprintf "Containers (%d)" model.FilteredContainers.Length
            this.Title <- if isFocused then sprintf " ▶ %s ◀ " titleText else sprintf " %s " titleText

            // Cập nhật thanh hiển thị từ khóa lọc
            if model.IsFilterActive || not (String.IsNullOrEmpty model.FilterQuery) then
                filterLabel.Visible <- true
                filterLabel.Text <- sprintf "Filter: %s" model.FilterQuery
                listView.Y <- Pos.Absolute(1)
            else
                filterLabel.Visible <- false
                listView.Y <- Pos.Absolute(0)

            // Cập nhật danh sách các container
            let items =
                if model.FilteredContainers.IsEmpty then
                    [ "  (No containers found)" ]
                else
                    model.FilteredContainers
                    |> List.map (formatItem layoutMode)

            let collection = ObservableCollection<string>(items)
            listView.SetSource(collection)

            if not model.FilteredContainers.IsEmpty && model.SelectedIndex >= 0 && model.SelectedIndex < items.Length then
                listView.SelectedItem <- model.SelectedIndex

        | otherCat ->
            let catName = sprintf "%A" otherCat
            this.Title <- if isFocused then sprintf " ▶ %s (0) ◀ " catName else sprintf " %s (0) " catName
            filterLabel.Visible <- false
            listView.Y <- Pos.Absolute(0)

            let items =
                [ sprintf "  (No %s available)" catName
                  ""
                  sprintf "  %s management feature" catName
                  "  is scheduled for Milestone 4."
                  ""
                  "  ► Press [2] to return to Containers" ]

            let collection = ObservableCollection<string>(items)
            listView.SetSource(collection)
            listView.SelectedItem <- 0
