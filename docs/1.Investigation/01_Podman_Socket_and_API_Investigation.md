# ĐIỀU TRA KỸ THUẬT 01: GIAO TIẾP PODMAN SOCKET & LIBPOD REST API
## DỰ ÁN: PODMAN-FUI

- **Mã tài liệu:** INV-01-PODMAN-API
- **Trạng thái:** Hoàn thành điều tra
- **Mục tiêu:** Làm rõ toàn bộ cơ chế kết nối Unix Domain Socket, giao thức REST API của Podman (Libpod), cơ chế stream Logs/Stats, và phương án tự động phát hiện socket trên Linux.

---

## 1. TỔNG QUAN KIẾN TRÚC PODMAN ENGINE & SOCKET

### 1.1. Bản chất Daemonless và Socket Activation của Podman
Khác với Docker (luôn có một daemon `dockerd` chạy nền liên tục với quyền `root`), Podman hoạt động theo triết lý **Daemonless**:
- Khi người dùng gõ lệnh `podman run`, tiến trình Podman trực tiếp giao tiếp với runtime OCI (`crun` hoặc `runc`) và trình giám sát container (`conmon`) để tạo container mà không cần một daemon trung gian.
- Để hỗ trợ các công cụ quản lý từ xa hoặc giao diện người dùng (như `podman-remote`, Docker Compose, và `podman-FUI`), Podman cung cấp dịch vụ **`podman.socket`**.
- Dịch vụ này sử dụng cơ chế **Systemd Socket Activation**: Khi chưa có kết nối, tiến trình API service không chiếm dụng CPU hay RAM. Khi có ứng dụng (như `podman-FUI`) mở kết nối vào file socket, Systemd tự động kích hoạt tiến trình `podman system service` để phục vụ các yêu cầu HTTP REST.

### 1.2. Phân biệt Rootless Socket và Rootful Socket

| Đặc tính | Rootless Mode (Người dùng thường) | Rootful Mode (Người dùng root / system) |
| :--- | :--- | :--- |
| **Đường dẫn socket mặc định** | `/run/user/<UID>/podman/podman.sock` (thông qua biến `$XDG_RUNTIME_DIR`) | `/run/podman/podman.sock` |
| **Quyền hạn truy cập** | Thuộc sở hữu của chính User (`srw-rw---- <user> <group>`) | Yêu cầu quyền `root` hoặc nhóm quản trị |
| **User Namespace** | Chạy trong User Namespace riêng (UID ánh xạ qua `/etc/subuid`) | Chạy trực tiếp trên Host Namespace |
| **Cgroup Controller** | Cgroup v2 delegation (`/user.slice/user-<UID>.slice`) | Quản lý toàn bộ hệ thống root cgroup |
| **Lệnh kích hoạt Systemd** | `systemctl --user enable --now podman.socket` | `sudo systemctl enable --now podman.socket` |

### 1.3. Cơ chế Tự động Phát hiện Socket (Socket Auto-Discovery)
Hệ thống `podman-FUI` sẽ kiểm tra đường dẫn socket theo thứ tự ưu tiên sau:
1. **Biến môi trường tùy chỉnh:** Kiểm tra `$CONTAINER_HOST` hoặc `$PODMAN_SOCKET` nếu người dùng cấu hình thủ công.
2. **Rootless Socket của phiên làm việc:** Kiểm tra sự tồn tại của file socket tại `$XDG_RUNTIME_DIR/podman/podman.sock` (với `$XDG_RUNTIME_DIR` thường là `/run/user/<UID>`).
3. **Rootful Socket hệ thống:** Kiểm tra tại `/run/podman/podman.sock` (nếu ứng dụng được khởi chạy với quyền root hoặc người dùng thuộc nhóm cho phép).
4. **Trường hợp không tìm thấy socket:** Ứng dụng không được crash mà kích hoạt trạng thái cảnh báo, hiển thị giao diện hướng dẫn người dùng lệnh kích hoạt socket bằng `systemctl --user enable --now podman.socket`.

---

## 2. ĐẶC TẢ GIAO THỨC LIBPOD REST API

Podman cung cấp 2 bộ API trên cùng một socket:
- **Docker-compatible API:** Phục vụ các công cụ tương thích Docker (`/v1.40/...`).
- **Libpod API:** Bộ API mở rộng nguyên bản của Podman (`/v4.0.0/libpod/...` hoặc `/v5.0.0/libpod/...`), hỗ trợ đầy đủ các tính năng độc quyền như Pods, Secrets, Quadlet, Rootless stats. `podman-FUI` tập trung sử dụng bộ **Libpod API** này.

### 2.1. Nhóm API Quản lý Pod (Pods Endpoints)
- **Liệt kê Pod:** `GET /v4.0.0/libpod/pods/json`
  - Tham số lọc: `filters={"status": ["running", "paused"]}`
  - Dữ liệu trả về: Danh sách các đối tượng Pod bao gồm `Id`, `Name`, `Status`, `Created`, `Cgroup`, mảng các container nằm trong Pod (`Containers`), các cổng mạng được chuyển tiếp (`PortMappings`).
- **Tạo Pod:** `POST /v4.0.0/libpod/pods/create`
  - Body: Đối tượng cấu hình Pod (Tên pod, port mapping, nhãn, cấu hình mạng).
- **Thao tác vòng đời Pod:**
  - Start Pod: `POST /v4.0.0/libpod/pods/{name}/start`
  - Stop Pod: `POST /v4.0.0/libpod/pods/{name}/stop?t={timeout}`
  - Pause / Unpause Pod: `POST /v4.0.0/libpod/pods/{name}/pause` và `unpause`
  - Restart Pod: `POST /v4.0.0/libpod/pods/{name}/restart`
  - Xóa Pod: `DELETE /v4.0.0/libpod/pods/{name}?force=true`
- **Thống kê Pod:** `GET /v4.0.0/libpod/pods/stats`
  - Lấy thông số tài nguyên tổng hợp của toàn bộ container bên trong Pod.

### 2.2. Nhóm API Quản lý Container (Containers Endpoints)
- **Liệt kê Container:** `GET /v4.0.0/libpod/containers/json?all=true`
  - Dữ liệu trả về: Mảng container gồm `Id`, `Names`, `Image`, `State`, `Status`, `Created`, `Ports`, `Pod` (ID của Pod cha nếu có).
- **Chi tiết Container (Inspect):** `GET /v4.0.0/libpod/containers/{name}/json`
  - Dữ liệu trả về: Toàn bộ JSON mô tả cấu hình chi tiết (Mounts, Environment variables, NetworkSettings, State, RestartPolicy).
- **Thao tác trạng thái:**
  - Start: `POST /v4.0.0/libpod/containers/{name}/start`
  - Stop: `POST /v4.0.0/libpod/containers/{name}/stop?t=10`
  - Restart: `POST /v4.0.0/libpod/containers/{name}/restart`
  - Pause / Unpause: `POST /v4.0.0/libpod/containers/{name}/pause` và `unpause`
  - Kill: `POST /v4.0.0/libpod/containers/{name}/kill?signal=SIGKILL`
  - Xóa: `DELETE /v4.0.0/libpod/containers/{name}?force=true&v=true`
- **Danh sách tiến trình (Top):** `GET /v4.0.0/libpod/containers/{name}/top?ps_args=aux`
  - Trả về danh sách PID, %CPU, %MEM, Command của các tiến trình đang thực thi bên trong container.

### 2.3. Nhóm API Quản lý Images, Volumes, Networks & System
- **Images:**
  - Liệt kê: `GET /v4.0.0/libpod/images/json`
  - Kéo Image: `POST /v4.0.0/libpod/images/pull?reference={image_name}` (phản hồi stream trạng thái kéo từng layer)
  - Xóa Image: `DELETE /v4.0.0/libpod/images/{name}?force=false`
  - Lịch sử Layers: `GET /v4.0.0/libpod/images/{name}/history`
  - Dọn dẹp rác: `POST /v4.0.0/libpod/images/prune`
- **Volumes & Networks:**
  - `GET /v4.0.0/libpod/volumes/json` và `DELETE /v4.0.0/libpod/volumes/{name}`
  - `GET /v4.0.0/libpod/networks/json` và `DELETE /v4.0.0/libpod/networks/{name}`
- **System Information & Disk Usage:**
  - Thông tin host: `GET /v4.0.0/libpod/info` (OS, Kernel, Cgroup version, Arch, Conmon version).
  - Dung lượng chiếm dụng: `GET /v4.0.0/libpod/system/df` (Kích thước thực tế của Images, Containers, Volumes).
  - Dọn dẹp toàn hệ thống: `POST /v4.0.0/libpod/system/prune`.

---

## 3. ĐIỀU TRA CƠ CHẾ STREAMING TRỰC TIẾP (LOGS & STATS)

Một trong những tính năng cốt lõi tạo nên trải nghiệm "như LazyDocker" cho `podman-FUI` là khả năng stream liên tục log và biểu đồ tài nguyên mà không làm đơ giao diện.

### 3.1. Giao thức Stream Log (Log Streaming Protocol)
Endpoint: `GET /v4.0.0/libpod/containers/{name}/logs?follow=true&stdout=true&stderr=true&tail=100&timestamps=true`

- **Đặc điểm luồng dữ liệu (Multiplexed Stream):**
  - Khi container không bật TTY (chế độ tiêu chuẩn), luồng dữ liệu trả về từ socket không phải là văn bản thuần mà là **luồng phân kênh (Multiplexed stream)**.
  - Mỗi khung dữ liệu (frame) gồm phần đầu (Header) 8 bytes:
    - **Byte 0:** Kênh dữ liệu (`1` = stdout, `2` = stderr, `0` = stdin).
    - **Bytes 1-3:** Dành riêng (Reserved, giá trị `00 00 00`).
    - **Bytes 4-7:** Độ dài của khối payload dữ liệu tiếp theo (Big-Endian 32-bit integer).
    - **Tiếp theo:** Đúng số bytes của payload được chỉ định ở header.
  - **Yêu cầu đối với bộ phân giải (Parser):**
    - Bộ đọc stream phải đọc chính xác 8 bytes header trước, lấy kích thước payload, sau đó đọc đúng số byte payload rồi giải mã UTF-8 thành dòng log.
    - Phân tách màu sắc: Dòng log từ `stdout` hiển thị màu tiêu chuẩn, dòng log từ `stderr` hiển thị màu cảnh báo (đỏ/vàng) trên khung nhìn.

### 3.2. Giao thức Stream Metrics (Resource Stats Protocol)
Endpoint: `GET /v4.0.0/libpod/containers/{name}/stats?stream=true&interval=2`

- **Đặc điểm luồng dữ liệu:**
  - Socket sẽ gửi liên tục các đối tượng JSON cách nhau bởi ký tự xuống dòng (`\n`).
  - Mỗi khối JSON chứa dữ liệu thống kê tích lũy của cgroup tại thời điểm đo:
    - `Cpu`: Thời gian sử dụng CPU của container (`cpu_usage`), thời gian CPU hệ thống (`system_cpu_usage`).
    - `Memory`: Mức sử dụng bộ nhớ hiện tại (`usage`), giới hạn bộ nhớ tối đa (`limit`).
    - `Network`: Số byte nhận (`rx_bytes`), số byte truyền (`tx_bytes`).
    - `BlockIO`: Số byte đọc/ghi từ đĩa cứng.

- **Công thức tính toán thời gian thực:**
  - **Tỷ lệ CPU (%)**:
    $$\text{CPU \%} = \frac{\Delta \text{Container CPU Usage}}{\Delta \text{System CPU Usage}} \times \text{Online CPUs} \times 100$$
    *(Trong đó $\Delta$ là hiệu số giữa mẫu đo hiện tại và mẫu đo liền trước).*
  - **Tỷ lệ Bộ nhớ (%)**:
    $$\text{Memory \%} = \frac{\text{Memory Usage}}{\text{Memory Limit}} \times 100$$
  - Các giá trị này sau khi tính toán sẽ được đẩy vào bộ đệm vòng tròn (Ring Buffer có độ dài cố định 30 mẫu) để chuyển cho module đồ họa vẽ biểu đồ Sparklines.

---

## 4. TÍNH NĂNG INTERACTIVE SHELL (EXEC SHELL)

Khác với các API truyền thống, khi người dùng nhấn phím `e` để mở Shell vào container:
- Do đặc thù TUI chạy trên Terminal console (`/dev/tty`), nếu dùng API REST qua socket để thực hiện tương tác TTY (bi-directional raw terminal input/output) sẽ rất phức tạp và dễ xung đột với vòng lặp vẽ màn hình của thư viện TUI.
- **Giải pháp tối ưu đã được kiểm chứng bởi `lazydocker` và `podman-tui`:**
  1. Tạm dừng (Suspend) vòng lặp render của thư viện TUI và trả lại quyền điều khiển terminal tiêu chuẩn (`Console.ResetColor()`, khôi phục raw mode về normal mode).
  2. Khởi tạo một tiến trình con trực tiếp: `podman exec -it <container_name> /bin/sh` (hoặc `/bin/bash`).
  3. Chờ tiến trình con kết thúc khi người dùng gõ `exit` hoặc `Ctrl+D`.
  4. Khởi động lại (Resume) vòng lặp TUI, vẽ lại màn hình dashboard nguyên vẹn mà không gây lỗi con trỏ hay vỡ ký tự.

---

## 5. KẾT LUẬN & ĐỀ XUẤT CHO BƯỚC THIẾT KẾ (DESIGN)

1. **Cấu trúc Socket Client:** Xây dựng module Socket Client độc lập, đóng gói hoàn toàn tầng HTTP qua Unix Domain Socket, cung cấp các hàm nghiệp vụ trả về kiểu dữ liệu thuần khiết của F# (Result/Async).
2. **Cơ chế chịu lỗi (Fault Tolerance):** Khi kết nối socket bị đứt (ví dụ service restart), tầng client phải có cơ chế Exponential Backoff để thử kết nối lại tự động mà không làm sập ứng dụng.
3. **Phân tách Stream Background:** Các tác vụ đọc log và stats phải được thực thi trên các background thread riêng biệt, đẩy dữ liệu về UI thông qua hàng đợi thông điệp bất biến để đảm bảo giao diện luôn đạt độ phản hồi 60 FPS.
