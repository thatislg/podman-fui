namespace PodmanFUI.Presentation.Views

open System
open Terminal.Gui.Views
open Terminal.Gui.ViewBase
open Terminal.Gui.Drawing
open PodmanFUI.Presentation
open PodmanFUI.Domain.NavigationModels
open PodmanFUI.Domain.MvuTypes

/// Khung hiển thị Hộp thoại Modal nổi (Overlay Modal)
type ModalsView() as this =
    inherit FrameView()

    let messageLabel = new Label()
    let inputField = new TextField()
    let actionGuideLabel = new Label()

    do
        this.X <- Pos.Center()
        this.Y <- Pos.Center()
        this.Width <- Dim.Absolute(58)
        this.Height <- Dim.Absolute(10)
        this.Border.LineStyle <- Nullable LineStyle.Rounded
        this.Visible <- false

        messageLabel.X <- Pos.Absolute(2)
        messageLabel.Y <- Pos.Absolute(1)
        messageLabel.Width <- Dim.Fill(Dim.Absolute(2))
        messageLabel.Height <- Dim.Absolute(2)

        inputField.X <- Pos.Absolute(2)
        inputField.Y <- Pos.Absolute(3)
        inputField.Width <- Dim.Fill(Dim.Absolute(2))
        inputField.Height <- Dim.Absolute(1)
        inputField.Visible <- false

        actionGuideLabel.X <- Pos.Absolute(2)
        actionGuideLabel.Y <- Pos.Absolute(5)
        actionGuideLabel.Width <- Dim.Fill(Dim.Absolute(2))
        actionGuideLabel.Height <- Dim.Absolute(1)

        this.Add(messageLabel) |> ignore
        this.Add(inputField) |> ignore
        this.Add(actionGuideLabel) |> ignore

    member this.InputField = inputField

    /// Cập nhật hiển thị hộp thoại modal theo trạng thái mô hình MVU
    member this.Update(model: DashboardModel) =
        match model.ActiveModal with
        | Closed ->
            this.Visible <- false
            inputField.Visible <- false

        | ConfirmActionDialog (action, containerId, containerName) ->
            this.Visible <- true
            this.SetScheme(Theme.modalConfirmScheme) |> ignore
            inputField.Visible <- false
            let shortId = if containerId.Length > 12 then containerId.Substring(0, 12) else containerId
            this.Title <- sprintf " Confirm %A " action
            messageLabel.Text <- sprintf "Are you sure you want to %A container:\n'%s' (%s)?" action containerName shortId
            actionGuideLabel.Text <- "  Press [y] to Confirm   │   Press [n / Esc] to Cancel"

        | FilterInputDialog ->
            this.Visible <- true
            this.SetScheme(Theme.modalFilterScheme) |> ignore
            inputField.Visible <- true
            inputField.SetScheme(Theme.listViewScheme) |> ignore
            inputField.Text <- model.FilterQuery
            this.Title <- " Filter Containers "
            messageLabel.Text <- "Type keyword to filter containers by name or ID:"
            actionGuideLabel.Text <- "  Press [Enter] to Apply   │   Press [Esc] to Cancel"

        | ErrorAlertDialog (title, message) ->
            this.Visible <- true
            this.SetScheme(Theme.modalConfirmScheme) |> ignore
            inputField.Visible <- false
            this.Title <- sprintf " %s " title
            messageLabel.Text <- message
            actionGuideLabel.Text <- "  Press [Enter / Esc] to Close"
