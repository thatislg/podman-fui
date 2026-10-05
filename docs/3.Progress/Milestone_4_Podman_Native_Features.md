# LỘ TRÌNH CHI TIẾT: MILESTONE 4 - TRIỂN KHAI TÍNH NĂNG PODMAN CHUYÊN SÂU (PODMAN-NATIVE)
## DỰ ÁN: PODMAN-FUI (F# TERMINAL USER INTERFACE FOR PODMAN)

- **Mã cột mốc:** M4-PODMAN-NATIVE
- **Vị trí lưu trữ:** `docs/3.Progress/Milestone_4_Podman_Native_Features.md`
- **Trạng thái:** **Chờ thực hiện (Scheduled)**
- **Bản thiết kế kỹ thuật thực thi (Design Specification):**
  - [**`Design_M4_Podman_Native_Architecture.md`**](../2.Design/Design_M4_Podman_Native_Architecture.md) *(Bản thiết kế chi tiết DD-M4-PODMAN-NATIVE đã phê duyệt)*
- **Tài liệu điều tra liên kết (Investigation References):**
  - [**`01_Podman_Socket_and_API_Investigation.md`**](../1.Investigation/01_Podman_Socket_and_API_Investigation.md)
  - [**`04_Benchmarking_and_Feature_Mapping_Investigation.md`**](../1.Investigation/04_Benchmarking_and_Feature_Mapping_Investigation.md)
  - [**`09_DeepDive_Rootless_Cgroups_Networking_and_SELinux.md`**](../1.Investigation/09_DeepDive_Rootless_Cgroups_Networking_and_SELinux.md)
  - [**`10_DeepDive_Quadlet_and_Systemd_Integration.md`**](../1.Investigation/10_DeepDive_Quadlet_and_Systemd_Integration.md)

---

## 1. MỤC TIÊU CỘT MỐC 4
Đạt được đầy đủ sức mạnh quản lý hệ sinh thái Podman vượt trội so với Docker: Quản trị các nhóm Pods chuẩn Kubernetes với phân cấp Infra Container, quản lý toàn diện Images (kèm lịch sử layer), Volumes (tự động gợi ý cờ SELinux `:Z`), Networks, dọn dẹp hệ thống (System Prune), và tích hợp sâu với động cơ Quadlet (Systemd Service Generator).

---

## 2. DANH MỤC CÔNG VIỆC CHI TIẾT (TASK CHECKLIST)

### 2.1. Thiết kế Chi tiết & Nghiên cứu Kỹ thuật
*[Căn cứ thiết kế: `Design_M4_Podman_Native_Architecture.md` - Toàn văn bản vẽ DD-M4-PODMAN-NATIVE]*
- [x] Soạn thảo tài liệu thiết kế chi tiết phân rã mô-đun Pods, Image Layers, Volumes, SELinux và Quadlet Systemd.
- [x] Đặc tả chi tiết Hợp đồng dữ liệu `PodSummary`, `ImageSummary`, thuật toán dựng cây phân cấp Pods và giải thuật stream kéo Image.

### 2.2. Hiện thực hóa Tầng Domain (Thực thể Podman Chuyên sâu)
*[Căn cứ thiết kế: `Design_M4_Podman_Native_Architecture.md` - Mục 2.1: Phân Rã Domain & Mục 3: Đặc Tả Hợp Đồng Dữ Liệu]*
- [ ] **Subtask 2.2.1 - Thực thể miền Pods (`PodModels.fs`):**
  - Định nghĩa DU `PodStatus` (`Running`, `Degraded`, `Stopped`, `Created`, `Exited`).
  - Định nghĩa Record `PodContainerSummary` (Id, Names, Status, IsInfra) theo Mục 3.2.
  - Định nghĩa Record `PodSummary` (Id, Name, Status, InfraContainerId, Created, NumContainers, Containers, Cgroup) theo Mục 3.1.
  - Định nghĩa Record `PodCreationConfig` (Tên Pod, Port Mappings, Cấu hình Network Bridge/Pasta).
- [ ] **Subtask 2.2.2 - Thực thể miền Images (`ImageModels.fs`):**
  - Định nghĩa Record `ImageSummary` (Id, Repositories, SizeBytes, Created, Layers) theo Mục 3.3.
  - Định nghĩa Record `ImageLayerHistory` (Lệnh tạo layer, Dung lượng, Layer ID).
  - Định nghĩa Record `PullProgressState` (Map tiến độ từng layer, Tổng bytes tải, Trạng thái).
- [ ] **Subtask 2.2.3 - Thực thể miền Volumes & Networks (`VolumeModels.fs`, `NetworkModels.fs`):**
  - Định nghĩa Record `VolumeSummary` (Name, Driver, Scope, Mountpoint, SizeBytes, UsageCount).
  - Định nghĩa DU `SelinuxVolumeFlag` (`None`, `SharedPrivateZ`, `SharedPublicz`).
  - Định nghĩa Record `NetworkSummary` (Name, Id, Driver, Subnets, Ipv6Enabled, Internal, DnsEnabled).
- [ ] **Subtask 2.2.4 - Thực thể miền Quadlet Systemd (`QuadletModels.fs`):**
  - Định nghĩa Record `QuadletUnitInfo` (ServiceName, UnitFilePath, IsManagedBySystemd, FileContent) theo Mục 3.4.
  - Định nghĩa DU `RestartTarget` (`DirectEngine`, `SystemdUnit`).
- [ ] **Subtask 2.2.5 - Hợp đồng Giao diện Dịch vụ trừu tượng (`IPodService.fs`, `IResourceService.fs`):**
  - Khai báo các interface điều phối Pods, Images, Volumes, Networks và Quadlet.

### 2.3. Hiện thực hóa Tầng Hạ tầng Infrastructure
*[Căn cứ thiết kế: `Design_M4_Podman_Native_Architecture.md` - Mục 2.2, Mục 4.2, 4.3, 4.4]*
- [ ] **Subtask 2.3.1 - Giao tiếp Pod REST API (`PodApiClient.fs`):**
  - Hiện thực `GET /v4.0.0/libpod/pods/json` lấy danh sách Pods kèm các container thành viên.
  - Hiện thực các thao tác Pod đồng bộ: `POST /pods/{name}/start`, `POST /pods/{name}/stop`, `DELETE /pods/{name}?force=true`, `POST /pods/create`.
- [ ] **Subtask 2.3.2 - Giao tiếp Image API & Kéo Image đa Layer (`ImageApiClient.fs`):**
  - Hiện thực `GET /images/json` và `GET /images/{name}/history`.
  - Hiện thực thuật toán Kéo Image stream theo Mục 4.2: Đọc luồng JSON từ `/images/pull`, bóc tách `id`, `status`, `progressDetail`, cập nhật bản đồ tiến độ từng layer và thông báo về presentation.
- [ ] **Subtask 2.3.3 - Quản lý Volumes & Networks (`VolumeApiClient.fs`, `NetworkApiClient.fs`):**
  - Hiện thực liệt kê Volume và thao tác dọn dẹp mồ côi qua `POST /v4.0.0/libpod/volumes/prune`.
  - Hiện thực liệt kê Network (`/networks/json`).
- [ ] **Subtask 2.3.4 - Tự động phát hiện và xử lý chính sách SELinux (`SelinuxHelper.fs`):**
  - Hiện thực thuật toán kiểm tra `/sys/fs/selinux/enforce` theo Mục 4.3: Nếu ở chế độ `Enforcing`, kiểm tra nhãn xattr của thư mục host và tự động gợi ý cờ `:Z` khi mount volume.
- [ ] **Subtask 2.3.5 - Dò tìm & Tích hợp Quadlet Systemd (`QuadletDiscovery.fs`):**
  - Hiện thực thuật toán quét file `.container` tại `~/.config/containers/systemd/` và `/etc/containers/systemd/` theo Mục 4.4, ánh xạ nhãn container với file service của systemd.

### 2.4. Hiện thực hóa Tầng Presentation (Pods & Resources UI)
*[Căn cứ thiết kế: `Design_M4_Podman_Native_Architecture.md` - Mục 2.3, Mục 4.1 & Mục 6: Đặc Tả Giao Diện]*
- [ ] **Subtask 2.4.1 - Khung nhìn Cây phân cấp Pods (`Views/PodTreeView.fs`):**
  - Hiện thực thuật toán dựng cây phân cấp theo Mục 4.1: Node gốc mang tên Pod kèm màu trạng thái (Xanh: Running, Vàng: Degraded, Xám: Stopped).
  - Node con đầu tiên gắn nhãn màu tím nhạt `[INFRA]`; các node con tiếp theo hiển thị container ứng dụng theo Mục 6.1.
  - Xử lý phím tắt chuyên biệt: `Space` (Start/Stop toàn bộ Pod), `d` (Xóa Pod), `c` (Mở modal tạo Pod mới).
- [ ] **Subtask 2.4.2 - Khung nhìn Danh sách & Lịch sử Layer Images (`Views/ImageListView.fs`, `Views/ImageHistoryView.fs`):**
  - Render bảng danh sách Image (Tag, Size, Created).
  - Render tab con hiển thị bảng layer lịch sử Dockerfile (Lệnh build, kích thước layer).
  - Render hộp thoại modal kéo Image với thanh tiến trình trực quan theo từng layer (`Views/PullImageProgressDialog.fs`).
- [ ] **Subtask 2.4.3 - Khung nhìn Volumes & Networks (`Views/VolumeListView.fs`, `Views/NetworkListView.fs`):**
  - Render danh sách Volume, hiển thị cảnh báo dung lượng đĩa và nút bấm phím `p` kích hoạt dọn dẹp mồ côi (Volume Prune).
  - Render danh sách Network (Subnet, Gateway, Trạng thái hoạt động).
- [ ] **Subtask 2.4.4 - Hộp thoại Điều phối Kép Quadlet (`Views/QuadletActionDialog.fs`):**
  - Khi nhấn phím `r` trên container có nhãn `[Quadlet]`: Mở hộp thoại thông minh cho phép chọn khởi động lại qua Systemd (`systemctl --user restart`) hoặc trực tiếp qua Engine theo Mục 4.4.

### 2.5. Kiểm thử Nghiệm thu Kỹ thuật (Technical Acceptance Testing)
*[Căn cứ thiết kế: `Design_M4_Podman_Native_Architecture.md` - Mục 7: Ma Trận Ca Kiểm Thử Nghiệm Thu]*
- [ ] **Subtask 2.5.1 - Thực thi kiểm thử ca TC-M4-01 (Cây phân cấp Pods):**
  - Mở tab Pods (phím `1`), xác nhận hiển thị chuẩn xác Node cha và các Node con; container hạ tầng có nhãn `[INFRA]`.
- [ ] **Subtask 2.5.2 - Thực thi kiểm thử ca TC-M4-02 (Dừng/Chạy đồng bộ toàn Pod):**
  - Nhấn `Space` trên Pod; xác nhận toàn bộ container con dừng; nhấn `Space` chạy lại; xác nhận Infra container chạy trước, sau đó là app.
- [ ] **Subtask 2.5.3 - Thực thi kiểm thử ca TC-M4-03 (Kéo Image đa Layer):**
  - Nhập tên Image mới; modal hiển thị thanh tiến trình phần trăm riêng cho từng layer; sau khi hoàn tất, Image mới xuất hiện trên bảng.
- [ ] **Subtask 2.5.4 - Thực thi kiểm thử ca TC-M4-04 (Dọn dẹp Volume mồ côi Prune):**
  - Nhấn `p` trên tab Volumes; xác nhận popup thông báo dung lượng đĩa thu hồi; nhấn `y` để dọn dẹp an toàn.
- [ ] **Subtask 2.5.5 - Thực thi kiểm thử ca TC-M4-05 (Điều phối khởi động lại Quadlet):**
  - Chọn container Quadlet, nhấn `r`; chọn Systemd restart; xác nhận lệnh `systemctl --user restart` thực thi thành công.

---

## 3. TIÊU CHÍ HOÀN THÀNH (DEFINITION OF DONE - DOD)

1. Cây phân cấp Pods hiển thị rõ ràng quan hệ Infra Container và Application Containers, hỗ trợ thao tác đồng bộ 1-chạm.
2. Quản lý đầy đủ vòng đời của Images, Volumes, Networks, dọn dẹp mồ côi thu hồi tài nguyên đĩa cứng.
3. Kéo Image bất đồng bộ hiển thị chi tiết tiến độ tải và giải nén theo từng layer.
4. Tự động kiểm tra chính sách an ninh SELinux và gợi ý cờ mount volume `:Z` an toàn trên hệ Fedora/RHEL.
5. Nhận diện chuẩn xác container do Quadlet quản lý và hỗ trợ điều phối dịch vụ qua Systemd.
6. Vượt qua toàn bộ các ca kiểm thử từ TC-M4-01 đến TC-M4-05.
