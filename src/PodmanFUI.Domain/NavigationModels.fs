namespace PodmanFUI.Domain

open PodmanFUI.Domain.ContainerModels

module NavigationModels =

    /// Danh mục tài nguyên chính trên Top Bar
    type NavigationCategory =
        | Pods
        | Containers
        | Images
        | Volumes
        | Networks

    /// Các tab hiển thị thông tin chi tiết trên Panel bên phải
    type DetailTab =
        | Logs
        | Inspect
        | Top
        | Env

    /// Khung giao diện đang nhận tiêu điểm bàn phím và chuột
    type ActiveFocus =
        | Sidebar
        | DetailPane
        | ModalDialog

    /// Trạng thái của các hộp thoại Modal popup
    type ModalState =
        | Closed
        | ConfirmActionDialog of action: ContainerAction * containerId: string * containerName: string
        | FilterInputDialog
        | ErrorAlertDialog of title: string * message: string
