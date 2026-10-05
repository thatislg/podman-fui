# BẢN THIẾT KẾ KỸ THUẬT BẰNG LỜI: MILESTONE 4
## THIẾT KẾ CÁC TÍNH NĂNG PODMAN CHUYÊN SÂU (PODMAN-NATIVE ARCHITECTURE)

- **Mã tài liệu:** DES-M4-PODMAN-NATIVE
- **Vị trí lưu trữ:** `docs/2.Design/Design_M4_Podman_Native_Architecture.md`
- **Phiên bản:** 1.0.0
- **Trạng thái:** Bản thiết kế đề xuất (Draft)
- **Tài liệu căn cứ:** 
  - [`SRS_podman-FUI.md`](../1.Investigation/SRS_podman-FUI.md)
  - [`01_Podman_Socket_and_API_Investigation.md`](../1.Investigation/01_Podman_Socket_and_API_Investigation.md)
  - [`04_Benchmarking_and_Feature_Mapping_Investigation.md`](../1.Investigation/04_Benchmarking_and_Feature_Mapping_Investigation.md)
  - [`09_DeepDive_Rootless_Cgroups_Networking_and_SELinux.md`](../1.Investigation/09_DeepDive_Rootless_Cgroups_Networking_and_SELinux.md)
  - [`10_DeepDive_Quadlet_and_Systemd_Integration.md`](../1.Investigation/10_DeepDive_Quadlet_and_Systemd_Integration.md)
- **Tài liệu tiến độ gắn kèm:** [`Milestone_4_Podman_Native_Features.md`](../3.Progress/Milestone_4_Podman_Native_Features.md)

---

## 1. MỤC TIÊU THIẾT KẾ
Tài liệu này đặc tả chi tiết bằng lời các tính năng độc quyền của hệ sinh thái Podman, giúp `podman-FUI` vượt qua các giới hạn của các công cụ Docker truyền thống:
1. Thiết kế kiến trúc quản lý **Pods** (cấu trúc nhóm container theo chuẩn Kubernetes).
2. Thiết kế module quản lý vòng đời **Images, Volumes, Networks** và quy trình dọn dẹp hệ thống (System Prune).
3. Thiết kế cơ chế tích hợp **Quadlet (Systemd Service Generator)** và tự động xử lý cờ bảo mật **SELinux**.

---

## 2. THIẾT KẾ PHÂN HỆ QUẢN LÝ PODS (KUBERNETES-LIKE PODS)

Khác với Docker (chỉ quản lý container đơn lẻ), Podman coi Pod là một đơn vị quản trị hạng nhất.

### 2.1. Cấu trúc Mô hình Pod & Cây Quan hệ
- Một Pod bao gồm:
  - Một **Infra Container** (Container hạ tầng làm nhiệm vụ giữ các namespace mạng, IPC, UTS chung).
  - Một hoặc nhiều **Application Containers** cùng chia sẻ chung địa chỉ IP (`localhost`) và cổng mạng.
- **Quy cách biểu diễn cây thực thể (Spectre Tree View):**
  - Node Gốc: Tên Pod kèm trạng thái tổng quát (`Running`, `Degraded`, hoặc `Stopped`).
  - Node Con cấp 1: Danh sách các container thành viên bên trong Pod đó.
  - Mỗi node con hiển thị rõ: Tên container, loại vai trò (Infra hay Application), ID ngắn, trạng thái hiện tại, và lượng tài nguyên chiếm dụng.

### 2.2. Luồng Nghiệp vụ Điều khiển Đồng bộ trên Pod
- **Thao tác Khởi động / Dừng Pod (Start / Stop):** Khi người dùng nhấn `Space` trên một Pod, hệ thống gửi lệnh tới endpoint `/libpod/pods/{name}/start` hoặc `/stop`. Podman sẽ tự động khởi động Infra Container trước, sau đó khởi động tuần tự các container ứng dụng.
- **Thao tác Xóa Pod (Delete):** Luôn mở hộp thoại cảnh báo hai lớp, cung cấp tùy chọn xóa ép buộc (`force=true`) để tự động dừng và dọn dẹp toàn bộ các container con bên trong Pod trong một thao tác duy nhất.
- **Form Modal Tạo Pod Mới:**
  - Nhập Tên Pod (Pod Name).
  - Cấu hình Port Mapping (Ánh xạ cổng giữa máy Host và Pod).
  - Cấu hình chia sẻ Network (chọn mạng Bridge hoặc Pasta).

---

## 3. THIẾT KẾ PHÂN HỆ IMAGES, VOLUMES & NETWORKS

### 3.1. Quản lý Hình ảnh (Images & Ancestor Layers)
- **Hiển thị Danh sách:** Bảng dữ liệu gồm Tên Repository, Tag, Image ID rút gọn, Kích thước thực tế, Thời điểm tạo.
- **Cây Lịch sử Tầng (Ancestor Layer History):** Khi người dùng xem chi tiết một Image, hệ thống gọi endpoint `/images/{name}/history` và vẽ bảng phân tích từng layer: Lệnh Dockerfile sinh ra layer, kích thước tăng thêm của layer, và ID của layer tương ứng.
- **Quy trình Kéo Image Bất đồng bộ (Pull Image Workflow):**
  - Người dùng nhập chuỗi định danh image (ví dụ `docker.io/library/redis:alpine`).
  - Hệ thống gửi yêu cầu kéo image qua socket.
  - Phân tích luồng stream trả về để cập nhật thanh tiến trình (ProgressBar) hiển thị chi tiết tiến độ tải xuống (Downloading) và giải nén (Extracting) của từng layer.

### 3.2. Quản lý Ổ đĩa Lưu trữ (Volumes) & Mạng (Networks)
- **Dọn dẹp Mồ côi (Dangling Prune):** Tự động phát hiện các Volume không còn gắn với bất kỳ container nào và cho phép người dùng dọn dẹp để thu hồi dung lượng đĩa cứng.
- **Tự động Gán cờ SELinux (`:Z` / `:z`):**
  - Hệ thống kiểm tra tệp `/sys/fs/selinux/enforce`.
  - Nếu SELinux đang ở trạng thái `Enforcing` (thường thấy trên Fedora/RHEL), khi người dùng cấu hình mount volume, hệ thống tự động cảnh báo và đề xuất gắn thêm cờ `:Z` (gán nhãn ngữ cảnh riêng cho container) để tránh bị chặn truy cập.

---

## 4. THIẾT KẾ TÍCH HỢP HỆ THỐNG QUADLET VÀ SYSTEMD

### 4.1. Nhận diện Container do Quadlet Quản lý
- Khi nạp danh sách container từ socket, hệ thống kiểm tra các nhãn cấu hình nội bộ (`Labels`).
- Nếu container chứa nhãn liên kết tới Systemd service hoặc nguồn file `.container` trong thư mục `~/.config/containers/systemd/`:
  - Giao diện gắn thêm một huy hiệu màu tím đặc biệt: `[Quadlet]`.
  - Panel chi tiết bổ sung một tab con mới: `Quadlet` để hiển thị nội dung file khai báo cấu hình gốc.

### 4.2. Cơ chế Khởi động lại Thông minh (Smart Restart)
- Khi người dùng nhấn phím `r` trên một container thông thường: Gọi API restart trực tiếp của Podman.
- Khi người dùng nhấn phím `r` trên một container do Quadlet quản lý:
  - Hiển thị hộp thoại hỏi người dùng: Bạn muốn khởi động lại container qua Podman Engine hay khởi động lại toàn bộ dịch vụ thông qua Systemd (`systemctl --user restart <service>`)?
  - Việc điều phối qua Systemd giúp chu kỳ sống của container luôn đồng bộ hoàn hảo với hệ điều hành Linux.
