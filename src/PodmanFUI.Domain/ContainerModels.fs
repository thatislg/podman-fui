namespace PodmanFUI.Domain

open System

module ContainerModels =

    /// Trạng thái hoạt động của Container
    type ContainerStatus =
        | Running
        | Exited
        | Paused
        | Created
        | Restarting
        | Dead

    /// Cấu hình cổng ánh xạ giữa máy Host và Container
    type PortMapping =
        { HostIp: string
          HostPort: int
          ContainerPort: int
          Protocol: string }

    /// Thực thể tóm tắt Container hiển thị trên danh sách Sidebar
    type ContainerSummary =
        { Id: string
          ShortId: string
          Names: string list
          PrimaryName: string
          Image: string
          Status: ContainerStatus
          StatusText: string
          Created: DateTimeOffset
          Ports: PortMapping list
          PodName: string option
          IsQuadletManaged: bool }

    /// Thực thể chi tiết chuyên sâu của Container phục vụ tab Inspect
    type ContainerDetail =
        { Id: string
          Name: string
          Image: string
          Created: DateTimeOffset
          State: string
          Pid: int
          StartedAt: DateTimeOffset option
          FinishedAt: DateTimeOffset option
          ExitCode: int
          Ports: PortMapping list
          Mounts: (string * string) list
          Env: string list
          Labels: Map<string, string> }

    /// Các hành động nghiệp vụ có thể tác động lên Container
    type ContainerAction =
        | Start
        | Stop
        | Restart
        | Pause
        | Unpause
        | Delete
        | ExecShell
