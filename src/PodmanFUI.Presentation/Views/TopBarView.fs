namespace PodmanFUI.Presentation.Views

open Terminal.Gui.Views
open Terminal.Gui.ViewBase
open Terminal.Gui.Input
open PodmanFUI.Domain.NavigationModels
open PodmanFUI.Domain.MvuTypes

/// Khung điều hướng danh mục tài nguyên phía trên cùng (TopBar)
type TopBarView() as this =
    inherit View()

    let label = new Label()
    let categoryChangedEvent = new Event<NavigationCategory>()

    do
        this.Height <- Dim.Absolute(1)
        this.Width <- Dim.Fill()
        label.X <- Pos.Absolute(1)
        label.Y <- Pos.Absolute(0)
        label.Width <- Dim.Fill()
        label.Height <- Dim.Absolute(1)
        this.Add(label) |> ignore

        let handleMouseClick (mouse: Mouse) =
            if (mouse.IsSingleClicked || mouse.IsPressed) && mouse.Position.HasValue then
                let x = mouse.Position.Value.X
                if x < 14 then categoryChangedEvent.Trigger NavigationCategory.Pods
                elif x < 33 then categoryChangedEvent.Trigger NavigationCategory.Containers
                elif x < 48 then categoryChangedEvent.Trigger NavigationCategory.Images
                elif x < 63 then categoryChangedEvent.Trigger NavigationCategory.Volumes
                elif x < 78 then categoryChangedEvent.Trigger NavigationCategory.Networks

        this.MouseEvent.Add(handleMouseClick)
        label.MouseEvent.Add(handleMouseClick)

    let formatCategory (cat: NavigationCategory) (active: NavigationCategory) (key: string) (name: string) =
        if cat = active then
            sprintf "[▶%s: %s◀]" key name
        else
            sprintf " %s: %s " key name

    [<CLIEvent>]
    member this.CategoryClicked = categoryChangedEvent.Publish

    /// Cập nhật hiển thị TopBar theo trạng thái mô hình MVU
    member this.Update(model: DashboardModel) =
        let categories =
            [ formatCategory NavigationCategory.Pods model.ActiveCategory "1" "Pods"
              formatCategory NavigationCategory.Containers model.ActiveCategory "2" "Containers"
              formatCategory NavigationCategory.Images model.ActiveCategory "3" "Images"
              formatCategory NavigationCategory.Volumes model.ActiveCategory "4" "Volumes"
              formatCategory NavigationCategory.Networks model.ActiveCategory "5" "Networks" ]
            |> String.concat " │ "

        let statusIndicator = "Podman (Rootless) ● Connected"
        let displayText = sprintf "%s    │    %s" categories statusIndicator
        label.Text <- displayText
