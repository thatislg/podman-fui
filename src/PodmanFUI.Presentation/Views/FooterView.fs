namespace PodmanFUI.Presentation.Views

open Terminal.Gui.Views
open Terminal.Gui.ViewBase
open Terminal.Gui.Drawing
open PodmanFUI.Presentation
open PodmanFUI.Domain.ContainerModels
open PodmanFUI.Domain.NavigationModels
open PodmanFUI.Domain.MvuTypes

/// Khung chân trang hiển thị hướng dẫn phím tắt ngữ cảnh (FooterBar)
type FooterView() as this =
    inherit View()

    let label = new Label()

    do
        this.Height <- Dim.Absolute(1)
        this.Width <- Dim.Fill()
        this.SetScheme(Theme.footerScheme) |> ignore
        label.SetScheme(Theme.footerScheme) |> ignore
        label.X <- Pos.Absolute(1)
        label.Y <- Pos.Absolute(0)
        label.Width <- Dim.Fill()
        label.Height <- Dim.Absolute(1)
        this.Add(label) |> ignore

    /// Cập nhật nhãn phím tắt theo ngữ cảnh thực thể Container đang chọn
    member this.Update(model: DashboardModel) =
        match model.ActiveCategory with
        | NavigationCategory.Containers ->
            let selectedContainer =
                if model.SelectedIndex >= 0 && model.SelectedIndex < model.FilteredContainers.Length then
                    Some model.FilteredContainers.[model.SelectedIndex]
                else
                    None

            let actionHint =
                match selectedContainer with
                | Some c ->
                    match c.Status with
                    | ContainerStatus.Running ->
                        "[s] Stop  [r] Restart  [p] Pause  [d] Delete  [e] Shell"
                    | ContainerStatus.Paused ->
                        "[p] Unpause  [d] Delete"
                    | ContainerStatus.Exited
                    | ContainerStatus.Created
                    | ContainerStatus.Restarting
                    | ContainerStatus.Dead ->
                        "[s] Start  [r] Restart  [d] Delete"
                | None ->
                    "[s] Action  [d] Delete"

            let guideText =
                sprintf "[1-5] Category  [Tab] Focus  [←/→] Tabs  [↑/↓] Nav  %s  [/] Filter  [q] Quit" actionHint

            label.Text <- guideText

        | otherCat ->
            let guideText =
                sprintf "[1-5] Switch Category  [2] Back to Containers  [Tab] Toggle Pane  [q] Quit"
            label.Text <- guideText
