namespace PodmanFUI.Domain

module Errors =

    /// Phân loại chi tiết các lỗi kết nối và phân giải dữ liệu từ Podman Unix Domain Socket
    type ConnectionError =
        /// ERR_SOCK_404: File .sock không tồn tại
        | SocketNotFound of path: string * attemptedPaths: string list * hint: string
        /// ERR_SOCK_403: Không có quyền đọc/ghi vào socket vật lý
        | AccessDenied of path: string * message: string
        /// ERR_HTTP_500 / HTTP Errors: Lỗi phản hồi HTTP từ Libpod API
        | HttpFailure of statusCode: int * reason: string
        /// ERR_CONN_TIMEOUT: Quá thời gian chờ kết nối (Timeout 10s)
        | Timeout of message: string
        /// ERR_JSON_PARSE: Không thể phân giải cấu trúc JSON từ API /info
        | DeserializationError of message: string
