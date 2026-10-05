namespace PodmanFUI.Domain

module Models =

    /// Chế độ hoạt động của Podman Socket
    type SocketMode =
        | Rootless
        | Rootful
        | Custom

    /// Kết quả của thuật toán dò tìm đường dẫn Unix Domain Socket
    type SocketDiscoveryResult =
        | Discovered of path: string * mode: SocketMode
        | NotFound of attemptedPaths: string list * hint: string

    /// Thông tin phân hệ Cgroup máy chủ
    type CgroupInfo =
        { Version: string
          Manager: string
          Controllers: string list }

    /// Thông tin phần cứng và cấu hình động cơ Podman
    type HostInfo =
        { Arch: string
          OS: string
          Kernel: string
          Cgroup: CgroupInfo
          StorageDriver: string
          GraphRoot: string
          Rootless: bool
          ConmonVersion: string }

    /// Thông tin phiên bản phần mềm Podman
    type VersionInfo =
        { Version: string
          ApiVersion: string
          GoVersion: string
          GitCommit: string
          BuiltTime: string }

    /// Dữ liệu tổng hợp môi trường động cơ Podman
    type SystemInfo =
        { Host: HostInfo
          Version: VersionInfo
          SocketPath: string
          SocketMode: SocketMode }
