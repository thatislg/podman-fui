# LỘ TRÌNH CHI TIẾT: MILESTONE 4 - TRIỂN KHAI TÍNH NĂNG PODMAN CHUYÊN SÂU (PODMAN-NATIVE)
## DỰ ÁN: PODMAN-FUI (F# TERMINAL USER INTERFACE FOR PODMAN)

- **Mã cột mốc:** M4-PODMAN-NATIVE
- **Vị trí lưu trữ:** `docs/3.Progress/Milestone_4_Podman_Native_Features.md`
- **Trạng thái:** **Chờ thực hiện (Scheduled)**
- **Bản thiết kế kỹ thuật thực thi (Design Specification):**
  - [**`Design_M4_Podman_Native_Architecture.md`**](../2.Design/Design_M4_Podman_Native_Architecture.md) *(Bản thiết kế chi tiết DD-M4-PODMAN-NATIVE đã hoàn thiện)*
- **Tài liệu điều tra liên kết (Investigation References):**
  - [**`01_Podman_Socket_and_API_Investigation.md`**](../1.Investigation/01_Podman_Socket_and_API_Investigation.md)
  - [**`04_Benchmarking_and_Feature_Mapping_Investigation.md`**](../1.Investigation/04_Benchmarking_and_Feature_Mapping_Investigation.md)
  - [**`09_DeepDive_Rootless_Cgroups_Networking_and_SELinux.md`**](../1.Investigation/09_DeepDive_Rootless_Cgroups_Networking_and_SELinux.md)
  - [**`10_DeepDive_Quadlet_and_Systemd_Integration.md`**](../1.Investigation/10_DeepDive_Quadlet_and_Systemd_Integration.md)

---

## 1. MỤC TIÊU CỘT MỐC 4
Đạt được đầy đủ sức mạnh quản lý hệ sinh thái Podman tương đương `podman-tui`: Quản lý Pods chuẩn Kubernetes, Images, Volumes, Networks, dọn dẹp hệ thống (System Prune/df), và hỗ trợ tính năng hiện đại Quadlet (Systemd Service Generator).

---

## 2. DANH MỤC CÔNG VIỆC CHI TIẾT (TASK CHECKLIST)

### 2.1. Quản lý Pods (Kubernetes-like Pod Management)
- [ ] Triển khai gọi Libpod API `GET /v4.0.0/libpod/pods/json`.
- [ ] Hiển thị danh sách Pods trên danh mục `[1] Pods` (Tên, ID, Trạng thái tổng thể, Số lượng container).
- [ ] Sử dụng Spectre.Console Tree để vẽ sơ đồ cây phân cấp: Pod cha -> Infra Container -> Các Application Containers con.
- [ ] Thao tác nhanh trên toàn Pod: Start Pod, Stop Pod, Restart Pod, Xóa Pod (`force`).
- [ ] Form modal tạo Pod mới: Cấu hình tên, chia sẻ mạng, port mapping.

### 2.2. Quản lý Images (Image Management & Layers)
- [ ] Liệt kê danh sách image cục bộ (`GET /v4.0.0/libpod/images/json`).
- [ ] Xem chi tiết các tầng lịch sử của image (Ancestor Layers) qua endpoint `/images/{name}/history`.
- [ ] Chức năng Kéo Image mới (Pull Image): Nhập tên image trên registry, hiển thị thanh tiến trình download từng layer thời gian thực.
- [ ] Xóa image và tính năng dọn dẹp rác (Image Prune).

### 2.3. Quản lý Volumes & Networks
- [ ] Liệt kê danh sách Volumes (`/libpod/volumes/json`) và Networks (`/libpod/networks/json`).
- [ ] Xem thông tin cấu hình chi tiết (Mountpoint, Driver, Subnet, Gateway, DNS).
- [ ] Xóa volume hoặc network đơn lẻ; dọn dẹp các volume/network mồ côi (không còn gắn với container nào).
- [ ] Tự động đề xuất nhãn mount `:Z` / `:z` nếu phát hiện hệ thống chạy Fedora có bật SELinux (theo `INV-09`).

### 2.4. Quản trị Dung lượng Ổ đĩa & Tích hợp Quadlet
- [ ] Thống kê dung lượng ổ đĩa (`GET /v4.0.0/libpod/system/df`): Dung lượng chiếm dụng của Containers, Images, Volumes, Build Cache.
- [ ] Tính năng `System Prune` tổng thể có hộp thoại cảnh báo 2 bước để bảo vệ dữ liệu.
- [ ] Tích hợp Quadlet (theo `INV-10`): Nhận diện các container do Systemd quản lý, hiển thị huy hiệu `[Quadlet]` và hỗ trợ cơ chế Smart Restart qua Systemd.

---

## 3. TIÊU CHÍ HOÀN THÀNH (DEFINITION OF DONE - DOD)

1. Người dùng có thể quản lý trọn vẹn cả 5 danh mục thực thể: Pods, Containers, Images, Volumes, Networks trên cùng một giao diện.
2. Thao tác Start/Stop một Pod sẽ đồng bộ trạng thái của toàn bộ các container con bên trong Pod đó.
3. Kéo image từ Docker Hub hoặc Quay.io hiển thị thanh tiến trình trực quan, tải xong tự động cập nhật danh sách.
4. Container do Quadlet quản lý được nhận diện chuẩn xác và có thể restart an toàn.
