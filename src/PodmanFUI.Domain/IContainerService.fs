namespace PodmanFUI.Domain

open PodmanFUI.Domain.Errors
open PodmanFUI.Domain.ContainerModels

/// Giao diện dịch vụ trừu tượng quản lý Container
type IContainerService =
    /// Lấy danh sách toàn bộ Container (tương đương podman ps -a)
    abstract member ListContainersAsync: unit -> Async<Result<ContainerSummary list, ConnectionError>>

    /// Lấy dữ liệu cấu hình chi tiết của một Container (tương đương podman inspect)
    abstract member GetContainerDetailAsync: containerId: string -> Async<Result<ContainerDetail, ConnectionError>>

    /// Thực thi một hành động vòng đời lên Container (Start, Stop, Restart, Pause, Unpause, Delete)
    abstract member PerformActionAsync: action: ContainerAction -> containerId: string -> Async<Result<unit, ConnectionError>>
