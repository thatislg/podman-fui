# ĐIỀU TRA KỸ THUẬT 09: HẠ TẦNG ROOTLESS, CGROUP V2, MẠNG VÀ BẢO MẬT SELINUX / APPARMOR
## DỰ ÁN: PODMAN-FUI

- **Mã tài liệu:** INV-09-ROOTLESS-CGROUP-SECURITY
- **Trạng thái:** Kế hoạch dự phòng / Chờ thực hiện (Scheduled)
- **Thời điểm tiến hành:** **Milestone 4** (khi phát triển các tính năng Podman chuyên sâu: Volumes, Networks, Pods) và **Milestone 5** (khi kiểm thử chéo tương thích đa bản phân phối Linux)

---

## 1. MỤC TIÊU ĐIỀU TRA
Tài liệu này xác định phương án kỹ thuật chuyên sâu nhằm giải quyết các khác biệt hạ tầng nhân Linux (Kernel-level Differences) giữa hai họ hệ điều hành Debian và Fedora:
1. Đảm bảo thu thập số liệu thống kê tài nguyên (Metrics) chính xác trên cơ chế phân quyền Cgroups v2 Rootless.
2. Xử lý tương thích đa backend mạng của Podman (Netavark, Pasta, Slirp4netns) khi trích xuất thông tin IP và Port mapping.
3. Ngăn ngừa lỗi phân quyền âm thầm do hệ thống bảo mật SELinux (trên Fedora/RHEL) và AppArmor (trên Debian/Ubuntu).

---

## 2. BỐI CẢNH & CÁC NGUY CƠ KỸ THUẬT CỐT LÕI

### 2.1. Nguy cơ Mất Số liệu Metric do Cgroup Delegation
- Trong chế độ Rootless, người dùng không có quyền ghi trực tiếp vào cây thư mục `/sys/fs/cgroup`.
- Việc lấy thông số CPU và RAM của container phụ thuộc hoàn toàn vào việc Systemd có ủy quyền (Delegate) các bộ điều khiển `cpu` và `memory` cho user slice (`/user.slice/user-<UID>.slice`) hay không.
- Trên Fedora/RHEL, tính năng này được cấu hình sẵn. Nhưng trên một số bản cài Debian/Ubuntu tối giản, nếu chưa bật delegation, API `/stats` của Podman sẽ trả về giá trị CPU/RAM bằng `0`, khiến đồ thị Sparkline bị phẳng hoàn toàn.

### 2.2. Nguy cơ Lỗi Quyền Hạn do SELinux Enforcing
- Fedora và RHEL kích hoạt SELinux ở chế độ `Enforcing` theo mặc định.
- Khi container muốn đọc/ghi dữ liệu vào một thư mục được mount từ máy host, thư mục đó bắt buộc phải được gán nhãn ngữ cảnh phù hợp (thường là `system_u:object_r:container_file_t:s0`).
- Nếu ứng dụng `podman-FUI` hỗ trợ tính năng tạo container hoặc mount volume mà không hiểu cờ nhãn ngữ cảnh (`:z` chia sẻ hoặc `:Z` độc quyền), container sẽ lập tức bị hệ điều hành chặn truy cập và báo lỗi `Permission Denied` mà không rõ nguyên nhân.

### 2.3. Sự Chuyển dịch Backend Mạng giữa các Phiên bản Podman
- Podman 4.0 - 4.3: Sử dụng `slirp4netns` kết hợp kiến trúc CNI (Container Network Interface).
- Podman 4.4+ và 5.x: Chuyển dịch toàn diện sang `pasta` (hiệu năng cao hơn, không cần tiến trình daemon riêng) kết hợp `Netavark` và `Aardvark-dns`.
- Cấu trúc JSON trả về của mạng và địa chỉ IP có sự thay đổi giữa các phiên bản này, đòi hỏi tầng phân tích dữ liệu phải có khả năng tương thích ngược.

---

## 3. DANH MỤC HẠNG MỤC CẦN ĐIỀU TRA CHI TIẾT

- [ ] **Kiểm tra trạng thái Ủy quyền Cgroup v2 (Cgroup Delegation Check):**
  - Đọc thông tin từ endpoint `GET /v4.0.0/libpod/info`.
  - Phân tích trường `host.cgroupVersion` (phải là `v2`) và mảng `host.cgroupControllers` (phải chứa `cpu`, `memory`, `pids`).
  - Xây dựng cơ chế phát hiện sớm: Nếu thiếu bộ điều khiển, ứng dụng sẽ hiển thị biểu tượng cảnh báo nhỏ cạnh đồ thị Sparkline kèm hướng dẫn bật cgroup delegation trong file cấu hình Systemd của người dùng.
- [ ] **Điều tra tương thích mạng Rootless (Pasta vs Slirp4netns):**
  - Khảo sát cách bóc tách địa chỉ IP nội bộ, IP cổng kết nối và bản đồ ánh xạ cổng (Port Mapping) từ cấu trúc dữ liệu `NetworkSettings`.
  - Đảm bảo bảng danh sách Container hiển thị chính xác cổng dịch vụ bất kể hệ thống đang sử dụng Netavark hay CNI cũ.
- [ ] **Xây dựng quy tắc gán nhãn SELinux an toàn:**
  - Khảo sát biến môi trường và tệp `/sys/fs/selinux/enforce` để tự động nhận biết máy host có đang bật SELinux hay không.
  - Khi thiết kế form tạo Volume hoặc Container mount: Tự động đề xuất gắn thêm cờ `:Z` hoặc `:z` an toàn vào chuỗi cấu hình đường dẫn mount nếu phát hiện SELinux đang ở trạng thái `Enforcing`.
- [ ] **Kiểm tra quyền hạn socket bằng System Call `SO_PEERCRED`:**
  - Nghiên cứu cơ chế xác thực danh tính tiến trình kết nối vào file Unix Domain Socket để đảm bảo an toàn, tránh trường hợp ứng dụng vô tình kết nối nhầm vào socket của user khác trên hệ thống đa người dùng.

---

## 4. TIÊU CHÍ NGHIỆM THU (ACCEPTANCE CRITERIA)

1. Đồ thị Sparklines và thanh đo Gauge hiển thị đúng thông số CPU và RAM thực tế trên cả hai môi trường thử nghiệm: Fedora (SELinux Enforcing) và Ubuntu/Debian (Rootless).
2. Thông tin IP và Port Forwarding của các container và Pods hiển thị đồng nhất và chuẩn xác trên cả hai thế hệ mạng: Pasta (mới) và Slirp4netns (cũ).
3. Không phát sinh bất kỳ lỗi `AVC Denial` nào trong `audit.log` của SELinux khi `podman-FUI` thực hiện các tác vụ quản lý trên Fedora.
