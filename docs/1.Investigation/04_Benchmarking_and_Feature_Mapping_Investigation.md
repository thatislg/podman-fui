# ĐIỀU TRA KỸ THUẬT 04: ĐỐI CHIẾU & ÁNH XẠ TÍNH NĂNG (PODMAN-TUI VS LAZYDOCKER)
## DỰ ÁN: PODMAN-FUI

- **Mã tài liệu:** INV-04-BENCHMARK-MAPPING
- **Trạng thái:** Hoàn thành điều tra
- **Mục tiêu:** Mổ xẻ chi tiết kiến trúc, ưu nhược điểm của hai dự án tham khảo `podman-tui` và `lazydocker`, từ đó xây dựng ma trận ánh xạ tính năng chi tiết cho `podman-FUI`.

---

## 1. PHÂN TÍCH CHI TIẾT DỰ ÁN THAM KHẢO 1: PODMAN-TUI

`podman-tui` là công cụ TUI chính thức của cộng đồng Podman, viết bằng Go, sử dụng thư viện `rivo/tview` và `gdamore/tcell`.

### 1.1. Điểm mạnh vượt trội
- **Hỗ trợ toàn diện hệ sinh thái Podman:** Quản lý trọn vẹn khái niệm **Pods** (chuẩn container group tương thích Kubernetes). Đây là điểm mà các công cụ Docker (như lazydocker) hoàn toàn không có.
- **Tính năng chuyên sâu:**
  - Hỗ trợ đầy đủ các thực thể: Pods, Containers, Images, Volumes, Networks, Secrets, và System Info.
  - Quản lý Secret (tạo secret, mã hóa, gán vào container).
  - Khả năng tạo Pod với đầy đủ các thiết lập mạng (bridge, slirp4netns, pasta), port forwarding và chia sẻ namespace.
- **Tương thích Rootless hoàn hảo:** Tự động phát hiện và làm việc trơn tru với môi trường rootless user mà không yêu cầu quyền `sudo`.

### 1.2. Nhược điểm về Trải nghiệm Người dùng (UX Limitations)
- **Mô hình Màn hình Đơn (Screen-based Navigation):** Chuyển đổi qua lại giữa các màn hình toàn diện (Full-screen switch bằng các phím `F1` đến `F8`). Mỗi khi chuyển màn hình, ngữ cảnh của màn hình trước bị che khuất hoàn toàn.
- **Phụ thuộc quá nhiều vào Modal Dialog:** Hầu hết các thao tác (kể cả xem log, xem tiến trình, xem chi tiết inspect) đều mở một popup dialog đè lên màn hình chính, khiến người dùng phải liên tục bấm phím `Esc` để đóng.
- **Thiếu trực quan hóa dữ liệu thời gian thực:**
  - Không có đồ thị tài nguyên (CPU/RAM) dạng biểu đồ đường hay Sparkline.
  - Số liệu hiển thị dưới dạng bảng số thô, khó nắm bắt xu hướng tải tức thời.
- **Thao tác nhiều bước:** Để thực hiện một lệnh đơn giản thường phải nhấn phím mở menu (`m`), dùng mũi tên tìm lệnh, rồi nhấn `Enter`.

---

## 2. PHÂN TÍCH CHI TIẾT DỰ ÁN THAM KHẢO 2: LAZYDOCKER

`lazydocker` là công cụ TUI nổi tiếng dành cho Docker và Docker-Compose, viết bằng Go, sử dụng thư viện `jroimartin/gocui`.

### 2.1. Điểm mạnh vượt trội về Trải nghiệm Người dùng
- **Triết lý Bảng điều khiển Đa năng (Multi-panel Dashboard):** Toàn bộ thông tin quan trọng được hiển thị cùng lúc trên một màn hình: Danh sách thực thể ở bên trái, thông tin chi tiết và stream log trực tiếp ở bên phải.
- **Triết lý "1-Keypress Away" (Thao tác một chạm):**
  - Mọi thao tác thông dụng đều được gắn với một phím tắt đơn lẻ: `Space` (Start/Stop), `r` (Restart), `d` (Delete), `e` (Exec shell), `c` (Custom command), `b` (Bulk menu).
  - Không bắt buộc người dùng phải đi qua menu đa tầng.
- **Đồ họa Trực quan Thời gian thực (Ascii & Sparkline Graphs):** Tích hợp biểu đồ biến thiên CPU và Memory trực tiếp vào panel chi tiết, giúp lập trình viên phát hiện ngay tình trạng memory leak hay spike CPU.
- **Tương tác Chuột Tuyệt vời:** Click chọn nhanh panel, click chọn container, lăn chuột xem log, kéo thả chia màn hình rất tự nhiên.

### 2.2. Nhược điểm về Phạm vi Nền tảng
- **Thuần Docker, Không hiểu Podman:** Sử dụng Docker Engine API client, hoàn toàn không hỗ trợ các khái niệm nguyên bản của Podman (không có Pods, không hỗ trợ Libpod API, không quản lý được cấu hình cgroup rootless nâng cao của Podman).
- **Hỗ trợ Compose-centric:** Thiết kế tập trung nhiều vào các service của Docker-Compose, trong khi thế giới Podman vận hành theo tư duy Kubernetes Pods và Quadlet (systemd generator).

---

## 3. MA TRẬN ÁNH XẠ TÍNH NĂNG TỔNG THỂ CHO PODMAN-FUI

Bảng dưới đây xác lập chi tiết cách `podman-FUI` chắt lọc ưu điểm của cả hai công cụ và nâng cấp thành các tính năng thế hệ mới:

| Nhóm Tính Năng | Hiện Trạng ở `podman-tui` | Hiện Trạng ở `lazydocker` | Thiết Kế Nâng Cấp ở `podman-FUI` |
| :--- | :--- | :--- | :--- |
| **Bố cục Màn hình** | Đơn màn hình chuyển trang (`F1`..`F8`), nhiều modal che khuất | Đa bảng Master-Detail cố định | **Đa bảng Master-Detail Co giãn Động (Responsive Auto-scaling), tự thích ứng khi resize** |
| **Quản lý Pods** | Rất chi tiết, nhiều form cấu hình | Không hỗ trợ | **Hạng nhất (First-class citizen): Xem cấu trúc cây Pod-Container, Start/Stop/Delete toàn Pod** |
| **Quản lý Container** | Danh sách dạng Table toàn màn hình | Danh sách panel trái, chi tiết panel phải | **Panel danh sách trái kèm huy hiệu màu (Badge), phím tắt nhanh 1-chạm** |
| **Biểu đồ Tài nguyên (Metrics)** | Bảng số thô, không có đồ thị | Đồ thị ASCII Sparkline cơ bản | **Spectre.Console Sparklines Unicode (` ▂▃▄▅▆▇█`) + Thanh đo Gauge màu sắc sống động** |
| **Xem & Stream Log** | Bật modal xem log riêng biệt | Tích hợp vào panel tab bên phải | **Tab Log trực tiếp tại Dashboard chính, tự cuộn, tô màu theo stdout/stderr, có thanh tìm kiếm** |
| **Thao tác Bàn phím** | Menu `m` đa tầng, điều hướng form | Phím tắt nhanh 1-chạm (`Space`, `d`, `r`, `b`) | **Kế thừa phím tắt 1-chạm của lazydocker + Hỗ trợ toàn diện phím Vim (`h, j, k, l`)** |
| **Hỗ trợ Chuột** | Tương tác cơ bản | Tốt (Click chọn, scroll log) | **Hoàn chỉnh: Click chọn danh mục, click chọn thực thể, scroll log mượt mà, kéo splitter** |
| **Mở Shell Container** | Mở phiên exec trong modal | Tạm dừng TUI, mở shell trực tiếp trên terminal | **Tạm dừng TUI an toàn, mở `/bin/sh` hoặc `/bin/bash` trực tiếp, tự khôi phục dashboard khi exit** |
| **Quản lý Images & Layers** | Liệt kê, pull, inspect, xóa | Liệt kê, xem ancestor layers | **Xem danh sách, tìm kiếm Registry, xem cây lịch sử Layer, thanh tiến trình kéo image (Pull Progress)** |
| **Dọn dẹp Hệ thống (Prune)** | Có tính năng prune trong từng mục | Có prune containers/images/volumes | **Menu dọn dẹp hàng loạt (Bulk menu `b`) + System Prune tổng thể có hộp thoại xác nhận an toàn 2 bước** |
| **Hỗ trợ Đa ngôn ngữ** | Chỉ có tiếng Anh | Chỉ có tiếng Anh | **Kiến trúc tách chuỗi i18n: Triển khai EN hoàn chỉnh ở Phase 1, sẵn sàng mở rộng VI và JA** |

---

## 4. CHI TIẾT THIẾT KẾ ĐỐI CHIẾU CÁC FLOW TƯƠNG TÁC CHÍNH

### 4.1. Quy trình Giám sát Container (Monitoring Workflow)
- **Cách tiếp cận cũ (`podman-tui`):** 
  Nhấn `F4` (vào Containers) -> Tìm container bằng phím mũi tên -> Nhấn `m` -> Chọn `top` hoặc `logs` -> Màn hình popup hiện ra -> Muốn xem thông số khác phải nhấn `Esc` rồi chọn lại menu.
- **Cách tiếp cận mới (`podman-FUI`):**
  Nhấn `2` (nhảy sang Containers) -> Dùng `j`/`k` chọn container -> Panel bên phải tức thời hiển thị song song cả biểu đồ tải CPU/RAM sống động và các dòng log mới nhất. Chuyển tab con bằng `[` và `]` để xem `Inspect` hoặc `Processes` ngay tại chỗ mà không làm mất dấu container đang chọn.

### 4.2. Quy trình Quản lý Pod & Nhóm Container (Pod Workflow)
- **Cách tiếp cận mới (`podman-FUI`):**
  - Nhấn `1` (vào Pods).
  - Panel bên trái hiển thị danh sách Pod kèm trạng thái tổng thể (`Degraded`, `Running`, `Stopped`) và số container bên trong.
  - Khi chọn một Pod, panel bên phải vẽ sơ đồ cây (Spectre Tree) hiển thị rõ container nào là `Infra Container` (pause container) và các container ứng dụng con.
  - Nhấn `Space` để khởi động/dừng đồng loạt toàn bộ các container trong Pod đó chỉ bằng một nút bấm.

---

## 5. KẾT LUẬN & ĐỀ XUẤT CHO BƯỚC THIẾT KẾ (DESIGN)

1. **Giữ vững sự đơn giản của LazyDocker:** Mọi chức năng phức tạp của Podman đều phải được đưa về các phím tắt trực tiếp hoặc bảng điều hướng rõ ràng, không giấu tính năng sâu trong các lớp menu con.
2. **Kế thừa sự chuẩn mực của Podman-TUI:** Tuân thủ đúng ngữ nghĩa và cơ chế hoạt động của Podman (như phân biệt giữa Pod và Container đơn lẻ, cơ chế cgroup rootless, cổng chuyển tiếp của Pod).
3. **Độc lập nền tảng UI:** Toàn bộ dữ liệu nghiệp vụ của Pod và Container được chuẩn hóa ở tầng Domain Model, độc lập hoàn toàn với việc hiển thị, giúp mã nguồn sạch và dễ bảo trì.
