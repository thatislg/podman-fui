# ĐIỀU TRA KỸ THUẬT 10: TÍCH HỢP HỆ THỐNG QUADLET VÀ SYSTEMD SERVICE GENERATOR
## DỰ ÁN: PODMAN-FUI

- **Mã tài liệu:** INV-10-QUADLET-SYSTEMD
- **Trạng thái:** Kế hoạch dự phòng / Chờ thực hiện (Scheduled)
- **Thời điểm tiến hành:** **Milestone 4** (khi triển khai các tính năng chuyên sâu và nâng cao của hệ sinh thái Podman)

---

## 1. MỤC TIÊU ĐIỀU TRA
Tài liệu này xác định phương án kỹ thuật chuyên sâu nhằm khai thác **Quadlet** — tính năng quản trị container khai báo (Declarative Container Management) hiện đại nhất của Podman (từ phiên bản 4.4+):
1. Phân biệt chính xác giữa các container được tạo thủ công (`podman run`) và các container được kiểm soát bởi Systemd thông qua Quadlet.
2. Hiển thị thông tin dịch vụ Systemd gắn liền với container (Systemd Service Unit Status) và cho phép người dùng kích hoạt các lệnh quản lý vòng đời tương thích với Systemd.

---

## 2. BỐI CẢNH & TẦM QUAN TRỌNG CỦA QUADLET TRONG PODMAN

### 2.1. Quadlet là gì và tại sao lại thay thế Docker Compose?
- Trong môi trường máy chủ Red Hat/Fedora và hệ thống Linux hiện đại, việc dùng một daemon hay một công cụ độc lập như `docker-compose` để quản lý việc tự khởi động container khi bật máy thường gặp các vấn đề về phân quyền và đồng bộ thứ tự khởi động với hệ điều hành.
- **Quadlet** giải quyết triệt để vấn đề này bằng cách biến các file cấu hình đơn giản (`.container`, `.pod`, `.volume`, `.network`) đặt trong thư mục:
  - Chế độ Rootless: `~/.config/containers/systemd/`
  - Chế độ Hệ thống: `/etc/containers/systemd/`
- Trình tạo dịch vụ `systemd-generator` của Podman tự động chuyển đổi các file này thành các đơn vị dịch vụ Systemd tiêu chuẩn (`.service`).
- Nhờ đó, container được quản lý vòng đời, theo dõi sức khỏe, tự động khởi động lại (Restart Policy), và quản lý nhật ký log trực tiếp thông qua **Systemd Journal**.

### 2.2. Điểm Khuyết của các Công cụ TUI Hiện Tại
- Cả `lazydocker` lẫn `podman-tui` hiện tại đều coi mọi container là như nhau:
  - Nếu người dùng bấm phím Restart (`r`) trên một container do Quadlet quản lý, lệnh `podman restart` có thể bị xung đột với tiến trình giám sát của Systemd (Systemd có thể hiểu nhầm container bị crash và cố restart lần thứ hai, gây ra xung đột trạng thái).
  - Không công cụ nào hiển thị cho người dùng biết container này sinh ra từ file Quadlet nào trên ổ cứng.

---

## 3. DANH MỤC HẠNG MỤC CẦN ĐIỀU TRA CHI TIẾT

- [ ] **Khảo sát cơ chế phát hiện Container do Quadlet quản lý:**
  - Điều tra các nhãn nội bộ (Labels) được Podman tự động gắn vào container khi sinh ra từ Quadlet:
    - Nhãn chỉ định tên dịch vụ Systemd: Thường có dạng tiền tố `systemd...`.
    - Nhãn liên kết file nguồn: Đường dẫn tới file `.container` hoặc `.pod` gốc.
  - Phân loại trực quan trên UI: Hiển thị thêm huy hiệu (Badge) `[Quadlet]` hoặc `[Systemd]` cạnh tên container trên bảng danh sách.
- [ ] **Điều tra giao tiếp D-Bus Systemd để lấy trạng thái Service Unit:**
  - Nghiên cứu cách truy vấn trạng thái thực tế của Systemd Service Unit (`Active`, `Inactive`, `Failed`, `Reloading`) thông qua giao thức D-Bus của Linux hoặc qua CLI `systemctl --user is-active`.
  - Hiển thị song song hai tầng trạng thái: Trạng thái của Container ở tầng Podman và Trạng thái của Service Unit ở tầng Systemd.
- [ ] **Điều tra cơ chế Restart thông minh (Smart Restart):**
  - Xây dựng quy tắc hành động:
    - Nếu container là loại thủ công: Thực hiện lệnh API `POST /containers/{name}/restart`.
    - Nếu container thuộc quyền quản lý của Quadlet: Cung cấp tùy chọn điều phối lệnh restart thông qua Systemd (`systemctl --user restart <service_name>`) để đảm bảo tính nhất quán của hệ thống.
- [ ] **Khảo sát tính năng Xem nhanh Nội dung File Khai báo Quadlet:**
  - Thêm một tab con `Quadlet` trong khung chi tiết bên phải (song song với `Inspect`, `Logs`, `Env`) để đọc và hiển thị nội dung file khai báo `.container` hoặc `.pod` gốc với định dạng trực quan.

---

## 4. TIÊU CHÍ NGHIỆM THU (ACCEPTANCE CRITERIA)

1. Giao diện nhận diện chính xác 100% các container sinh ra từ Quadlet và hiển thị nhãn nhận diện rõ ràng.
2. Việc thực hiện thao tác Restart trên container Quadlet không gây ra lỗi xung đột trạng thái giữa Podman và Systemd.
3. Người dùng có thể xem được nội dung tệp cấu hình Quadlet gốc ngay trên dashboard mà không cần mở terminal khác để tìm file.
