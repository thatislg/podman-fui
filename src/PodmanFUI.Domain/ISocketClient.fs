namespace PodmanFUI.Domain

open PodmanFUI.Domain.Errors
open PodmanFUI.Domain.Models

/// Giao diện trừu tượng giao tiếp với Podman Unix Domain Socket
type IPodmanSocketClient =
    /// Lấy thông tin tổng hợp của Podman Engine từ endpoint GET /v4.0.0/libpod/info
    abstract member GetSystemInfoAsync: unit -> Async<Result<SystemInfo, ConnectionError>>
