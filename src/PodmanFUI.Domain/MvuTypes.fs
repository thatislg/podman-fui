namespace PodmanFUI.Domain

open PodmanFUI.Domain.Errors
open PodmanFUI.Domain.ContainerModels
open PodmanFUI.Domain.NavigationModels

module MvuTypes =

    /// Mô hình trạng thái toàn cục của Dashboard theo chu trình một chiều MVU
    type DashboardModel =
        { ActiveCategory: NavigationCategory
          ActiveTab: DetailTab
          CurrentFocus: ActiveFocus
          Containers: ContainerSummary list
          FilteredContainers: ContainerSummary list
          SelectedIndex: int
          SelectedContainerId: string option
          DetailData: ContainerDetail option
          IsFilterActive: bool
          FilterQuery: string
          ActiveModal: ModalState
          IsLoading: bool
          ErrorMessage: string option
          TerminalWidth: int
          TerminalHeight: int }

    /// Danh mục các thông điệp sự kiện MVU
    type DashboardMsg =
        // 1. Nhập liệu Bàn phím & Chuột
        | KeyPressed of key: string
        | RowSelected of index: int
        | CategoryChanged of category: NavigationCategory
        | TabChanged of tab: DetailTab
        | ToggleFocus
        // 2. Tác vụ nghiệp vụ Container
        | RequestAction of action: ContainerAction * containerId: string
        | ConfirmModal
        | DismissModal
        | UpdateFilter of query: string
        | ClearFilter
        // 3. Kết quả bất đồng bộ từ tầng Socket / Hệ điều hành
        | ContainersLoaded of Result<ContainerSummary list, ConnectionError>
        | DetailLoaded of Result<ContainerDetail, ConnectionError>
        | ActionExecuted of Result<ContainerAction * string, ConnectionError>
        | TerminalResized of width: int * height: int

    /// Chỉ thị tác vụ bất đồng bộ sinh ra từ hàm Update
    type DashboardCmd =
        | FetchContainers
        | FetchDetail of containerId: string
        | PerformContainerAction of action: ContainerAction * containerId: string
        | ExecuteShell of containerId: string
        | Batch of DashboardCmd list
        | NoCmd
