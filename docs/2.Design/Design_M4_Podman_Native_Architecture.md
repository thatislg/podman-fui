# BẢN THIẾT KẾ CHI TIẾT (DETAIL DESIGN): MILESTONE 4
## KIẾN TRÚC PHÂN HỆ PODMAN CHUYÊN SÂU (PODS, RESOURCES & QUADLET SYSTEMD)

- **Mã tài liệu:** DD-M4-PODMAN-NATIVE
- **Vị trí lưu trữ:** `docs/2.Design/Design_M4_Podman_Native_Architecture.md`
- **Phiên bản:** 2.0.0 (Nâng cấp toàn diện từ Basic Design lên Detail Design)
- **Ngày phê duyệt:** 2026-10-05
- **Tài liệu căn cứ:** 
  - [`SRS_podman-FUI.md`](../1.Investigation/SRS_podman-FUI.md)
  - [`01_Podman_Socket_and_API_Investigation.md`](../1.Investigation/01_Podman_Socket_and_API_Investigation.md)
  - [`04_Benchmarking_and_Feature_Mapping_Investigation.md`](../1.Investigation/04_Benchmarking_and_Feature_Mapping_Investigation.md)
  - [`09_DeepDive_Rootless_Cgroups_Networking_and_SELinux.md`](../1.Investigation/09_DeepDive_Rootless_Cgroups_Networking_and_SELinux.md)
  - [`10_DeepDive_Quadlet_and_Systemd_Integration.md`](../1.Investigation/10_DeepDive_Quadlet_and_Systemd_Integration.md)
- **Tài liệu tiến độ gắn kèm:** [`Milestone_4_Podman_Native_Features.md`](../3.Progress/Milestone_4_Podman_Native_Features.md)

---

## 1. TỔNG QUAN & PHẠM VI THIẾT KẾ CHI TIẾT

Tài liệu này đặc tả chi tiết kiến trúc cho các tính năng gốc độc quyền của hệ sinh thái Podman trong Milestone 4:
- Quản trị thực thể **Pods** (Đơn vị nhóm container theo kiến trúc Kubernetes) với phân cấp Container Hạ tầng (Infra Container) và Container Ứng dụng.
- Quản trị toàn diện vòng đời tài nguyên: **Images** (kèm lịch sử layer), **Volumes** (kèm cờ an ninh SELinux `:Z`), **Networks** (Bridge, Pasta), và quy trình dọn dẹp mồ côi (System Prune).
- Tích hợp động cơ **Quadlet** (Trình sinh file dịch vụ Systemd tự động) và cơ chế khởi động lại đồng bộ thông qua `systemctl --user`.
- **Tuân thủ tuyệt đối:** Trình bày hoàn toàn bằng lời văn, bảng biểu, quy trình thuật toán tuần tự, không sử dụng code sample (Zero Code Sample).

---

## 2. PHÂN RÃ DANH MỤC TỆP & MÔ-ĐUN MÃ NGUỒN (MODULE INVENTORY)

Milestone 4 mở rộng hệ thống với các mô-đun chức năng sau:

### 2.1. Dự án `PodmanFUI.Domain`
- **Tệp: `PodModels.fs` (Thực thể miền Pod):**
  - Chứa kiểu trạng thái Pod: `PodStatus` (`Running`, `Degraded`, `Stopped`, `Created`, `Exited`).
  - Chứa kiểu container thành viên trong Pod: `PodContainerSummary`.
  - Chứa kiểu tóm tắt Pod: `PodSummary`.
  - Chứa cấu hình tạo Pod mới: `PodCreationConfig`.
- **Tệp: `ImageModels.fs` (Thực thể miền Image):**
  - Chứa kiểu thông tin Image: `ImageSummary`.
  - Chứa kiểu thông tin từng layer lịch sử: `ImageLayerHistory`.
  - Chứa kiểu trạng thái tiến trình tải image: `PullProgressState`.
- **Tệp: `VolumeModels.fs` & `NetworkModels.fs` (Thực thể Ổ đĩa & Mạng):**
  - Chứa kiểu dữ liệu `VolumeSummary`, `NetworkSummary`, và cờ SELinux `SelinuxVolumeFlag` (`None`, `SharedPrivateZ`, `SharedPublicz`).
- **Tệp: `QuadletModels.fs` (Thực thể tích hợp Systemd Quadlet):**
  - Chứa kiểu mô tả dịch vụ Quadlet: `QuadletUnitInfo`.
  - Chứa kiểu chỉ thị điều phối: `RestartTarget` (`DirectEngine`, `SystemdUnit`).
- **Tệp: `IPodService.fs` & `IResourceService.fs` (Hợp đồng dịch vụ trừu tượng):**
  - Định nghĩa các interface quản trị Pod, Image, Volume, Network và Quadlet.

### 2.2. Dự án `PodmanFUI.Infrastructure`
- **Tệp: `PodApiClient.fs` (Giao tiếp Libpod Pod API):**
  - Các endpoint REST: `GET /v4.0.0/libpod/pods/json`, `POST /pods/create`, `POST /pods/{name}/start`, `POST /pods/{name}/stop`, `DELETE /pods/{name}`.
- **Tệp: `ImageApiClient.fs` (Giao tiếp Libpod Image API):**
  - Các endpoint: `GET /images/json`, `GET /images/{name}/history`, `POST /images/pull`, `DELETE /images/{name}`.
- **Tệp: `VolumeApiClient.fs` & `NetworkApiClient.fs`:**
  - Thực thi quản trị Volume (`/volumes/json`, `/volumes/prune`) và Network (`/networks/json`).
- **Tệp: `QuadletDiscovery.fs` (Dò tìm file định nghĩa Quadlet):**
  - Quét hệ thống tệp tại `~/.config/containers/systemd/` và `/etc/containers/systemd/` để ánh xạ nhãn container với file unit.
- **Tệp: `SelinuxHelper.fs` (Trợ lý kiểm tra chính sách an ninh SELinux):**
  - Đọc trạng thái SELinux trên máy host (`/sys/fs/selinux/enforce`) và tự động khuyến nghị cờ mount volume an toàn.

### 2.3. Dự án `PodmanFUI.Presentation`
- **Tệp: `Views/PodTreeView.fs` (Hiển thị danh sách Pod dạng Cây phân cấp):**
  - Khung nhìn dạng TreeView thể hiện quan hệ cha-con: Pod -> Infra Container -> Application Containers.
- **Tệp: `Views/ImageListView.fs` & `Views/ImageHistoryView.fs`:**
  - Danh sách Image và bảng trực quan hóa cây layer của Image đang chọn.
- **Tệp: `Views/VolumeListView.fs` & `Views/NetworkListView.fs`:**
  - Danh sách Volume và Mạng, kèm nút dọn dẹp mồ côi (Prune).
- **Tệp: `Views/QuadletActionDialog.fs`:**
  - Hộp thoại tương tác thông minh cho phép người dùng lựa chọn khởi động lại qua Podman Engine hoặc qua Systemd service.

---

## 3. ĐẶC TẢ CHI TIẾT HỢP ĐỒNG DỮ LIỆU (DATA CONTRACTS SPECIFICATION)

### 3.1. Hợp đồng `PodSummary` (Thực thể Tóm tắt Pod)

| Tên trường | Kiểu dữ liệu | Đường dẫn ánh xạ JSON Libpod | Bắt buộc | Mô tả & Quy tắc kiểm tra |
| :--- | :--- | :--- | :---: | :--- |
| `Id` | `string` | `Id` | Có | Chuỗi băm SHA256 nhận dạng Pod. |
| `Name` | `string` | `Name` | Có | Tên duy nhất của Pod trên hệ thống. |
| `Status` | `PodStatus` | `Status` | Có | Trạng thái tổng quát: `Running`, `Degraded` (chỉ một số container chạy), `Stopped`. |
| `InfraContainerId`| `string` | `InfraContainerID` | Có | ID của container hạ tầng giữ các namespace chung. |
| `Created` | `DateTimeOffset` | `Created` (Timestamp) | Có | Thời điểm khởi tạo Pod. |
| `NumContainers` | `int` | `NumContainers` | Có | Tổng số container con thuộc Pod. |
| `Containers` | `PodContainerSummary list` | `Containers` | Có | Danh sách chi tiết các container bên trong Pod. |
| `Cgroup` | `string` | `Cgroup` | Không | Đường dẫn cgroup quản lý tài nguyên của Pod. |

### 3.2. Hợp đồng `PodContainerSummary` (Container Con trong Pod)

| Tên trường | Kiểu dữ liệu | Bắt buộc | Mô tả |
| :--- | :--- | :---: | :--- |
| `Id` | `string` | Có | Chuỗi băm định danh container con. |
| `Names` | `string` | Có | Tên container con. |
| `Status` | `string` | Có | Trạng thái chạy (`Running`, `Exited`). |
| `IsInfra` | `bool` | Có | Cờ phân biệt: `true` nếu là container hạ tầng nội bộ, `false` nếu là ứng dụng. |

### 3.3. Hợp đồng `ImageSummary` & `ImageLayerHistory`

| Tên trường | Kiểu dữ liệu | Đường dẫn JSON | Bắt buộc | Mô tả |
| :--- | :--- | :--- | :---: | :--- |
| `Id` | `string` | `Id` | Có | Mã định danh SHA256 của Image. |
| `Repositories` | `string list` | `RepoTags` | Có | Danh sách tag của Image (ví dụ: `quay.io/podman/hello:latest`). |
| `SizeBytes` | `int64` | `Size` | Có | Dung lượng chiếm dụng trên ổ đĩa vật lý. |
| `Created` | `DateTimeOffset` | `Created` | Có | Ngày giờ biên dịch Image. |
| `Layers` | `ImageLayerHistory list`| Gọi từ `/history` | Không | Danh sách các layer tạo nên Image (Lệnh build, kích thước từng layer). |

### 3.4. Hợp đồng `QuadletUnitInfo` (Thông tin Dịch vụ Quadlet)

| Tên trường | Kiểu dữ liệu | Nguồn xác định | Bắt buộc | Mô tả |
| :--- | :--- | :--- | :---: | :--- |
| `ServiceName` | `string` | Đọc từ nhãn container hoặc file unit | Có | Tên unit systemd (ví dụ: `my-web-app.service`). |
| `UnitFilePath` | `string` | Thư mục `systemd/` | Có | Đường dẫn tệp `.container` khai báo cấu hình. |
| `IsManagedBySystemd`| `bool` | Kiểm tra qua systemctl | Có | `true` nếu container được quản lý thông qua Quadlet service. |
| `FileContent` | `string` | Đọc nội dung tệp vật lý | Không | Nội dung text của file khai báo cấu hình Quadlet. |

---

## 4. ĐẶC TẢ CHI TIẾT THUẬT TOÁN & TỪNG HÀM NGHIỆP VỤ

### 4.1. Thuật toán Xây dựng Cây Phân cấp Pods (Pod Tree Hierarchy Builder)
- **Đầu vào:** Danh sách `pods: PodSummary list`.
- **Đầu ra:** Cấu trúc cây dữ liệu phân cấp `TreeNode list` để render lên giao diện.
- **Quy trình tuần tự từng bước:**
  1. Với mỗi phần tử `pod` trong danh sách:
     - Tạo Node Cha (Root Node) mang tên Pod.
     - Đánh giá màu sắc trạng thái Node Cha:
       - Nếu toàn bộ container con đều `Running`: Gán màu Xanh lá (`Color.Green`).
       - Nếu có một số container chạy và một số dừng: Gán màu Vàng cam (`Color.Yellow`) kèm nhãn `[Degraded]`.
       - Nếu toàn bộ dừng: Gán màu Xám (`Color.Grey`).
     - Duyệt danh sách `pod.Containers`:
       - Tìm container có `Id == pod.InfraContainerId`: Tạo Node Con đầu tiên mang nhãn đặc biệt `[INFRA]` màu tím nhạt.
       - Với các container ứng dụng còn lại: Tạo các Node Con kế tiếp hiển thị Tên, Trạng thái và Cổng mạng.
  2. Gom toàn bộ cây vào danh sách và trả về cho bộ điều phối khung nhìn `PodTreeView`.

---

### 4.2. Thuật toán Kéo Image Bất đồng bộ với Thanh Tiến trình Đa Layer (Image Pull Stream)
- **Đầu vào:** `imageName: string`, `cancellationToken: CancellationToken`.
- **Đầu ra:** `Async<Result<unit, DomainError>>`.
- **Quy trình tuần tự từng bước:**
  1. Gửi yêu cầu HTTP POST tới `/v4.0.0/libpod/images/pull?reference={encodedName}`.
  2. Mở luồng stream đọc liên tục từng dòng phản hồi JSON từ socket.
  3. Với mỗi dòng JSON nhận được:
     - Đọc trường `status` (ví dụ: `"Downloading"`, `"Extracting"`, `"Pulling fs layer"`).
     - Đọc trường `id` (Mã định danh của từng Layer).
     - Đọc trường `progressDetail` chứa `current` và `total` bytes.
     - Cập nhật bảng băm quản lý tiến độ từng layer trong bộ nhớ giao diện:
       `layerProgressMap[layerId] = (currentBytes, totalBytes, status)`.
     - Kích hoạt vẽ lại hộp thoại modal `PullImageProgressDialog`, hiển thị thanh tiến trình tổng hợp và tiến độ riêng của từng layer.
  4. Khi dòng JSON cuối cùng trả về thông điệp hoàn tất (`"Download complete"` hoặc Image ID hoàn chỉnh): Đóng hộp thoại modal và thông báo thành công.

---

### 4.3. Thuật toán Tự động Gợi ý Cờ An ninh SELinux cho Volume Mount
- **Đầu vào:** `hostDirectory: string`.
- **Đầu ra:** `SelinuxVolumeFlag` (`SharedPrivateZ` hoặc `None`).
- **Quy trình kiểm tra từng bước:**
  1. Đọc tệp `/sys/fs/selinux/enforce`.
  2. Nếu tệp không tồn tại: Kết luận hệ điều hành không bật SELinux (ví dụ: Debian, Ubuntu mặc định). Trả về `None`.
  3. Nếu tệp tồn tại và giá trị đọc được là `1` (Chế độ `Enforcing` trên Fedora/RHEL/CentOS):
     - Kiểm tra nhãn bảo mật của thư mục host bằng hàm đọc ngữ cảnh xattr (`security.selinux`).
     - Nếu thư mục host chưa được gắn nhãn kiểu `container_file_t`:
       - Giao diện tự động bật hộp thoại cảnh báo: Thông báo cho người dùng rằng SELinux có thể chặn container đọc ghi thư mục này.
       - Tự động bổ sung hậu tố cờ an toàn `:Z` (cho phép container gán nhãn riêng biệt) vào cấu hình mount volume.

---

### 4.4. Thuật toán Nhận diện & Điều phối Khởi động lại Quadlet Systemd
- **Đầu vào:** `container: ContainerSummary`.
- **Đầu ra:** Chỉ thị thực thi `Cmd.ExecuteRestart`.
- **Quy trình tuần tự từng bước:**
  1. Kiểm tra nhãn của container: Tìm kiếm nhãn `io.podman.annotations.autoupdate` hoặc tiền tố tên bắt đầu bằng cấu hình Quadlet.
  2. Quét thư mục người dùng `~/.config/containers/systemd/` tìm kiếm tệp `.container` có cấu hình khớp với tên container.
  3. Nếu tìm thấy tệp:
     - Gán cờ `container.IsQuadletManaged = true`.
     - Đọc tên service tương ứng: `<tên-file>.service`.
  4. Khi người dùng nhấn phím `r` trên container này:
     - Mở hộp thoại `QuadletActionDialog` với 2 lựa chọn:
       - **Lựa chọn A (Khuyến nghị):** Khởi động lại thông qua Systemd (`systemctl --user restart <service>`). Giúp nạp lại cấu hình mới trong file `.container` nếu có chỉnh sửa.
       - **Lựa chọn B:** Khởi động lại nhanh trực tiếp qua Podman Engine (giữ nguyên tiến trình dịch vụ systemd).
  5. Tiếp nhận lựa chọn của người dùng và phát sinh chỉ thị điều phối tương ứng.

---

## 5. MA TRẬN MÃ LỖI & KỊCH BẢN XỬ LÝ NGOẠI LỆ (ERROR MATRIX)

| Mã Lỗi | Tên Định Danh | Nguyên Nhân Gốc | Phản Ứng Hệ Thống | Thông Báo Hiển Thị Người Dùng |
| :---: | :--- | :--- | :--- | :--- |
| **`ERR_POD_INFRA_FAIL`** | `InfraContainerFailed` | Container hạ tầng của Pod bị dừng đột ngột hoặc hỏng mạng | Đánh dấu toàn bộ Pod sang trạng thái `Degraded`, khóa các thao tác ghi | "[bold red]Lỗi nghiêm trọng: Container hạ tầng của Pod bị ngắt. Cần khởi động lại toàn bộ Pod.[/]" |
| **`ERR_IMG_AUTH_FAIL`** | `ImagePullUnauthorized` | Tải Image từ Private Registry nhưng chưa đăng nhập thông tin xác thực | Mở hộp thoại thông báo lỗi xác thực, hướng dẫn lệnh `podman login` | "[bold yellow]Không có quyền tải Image: Vui lòng đăng nhập registry bằng lệnh 'podman login [domain]'[/]" |
| **`ERR_VOL_IN_USE`** | `VolumeDeletionBlocked` | Người dùng yêu cầu xóa Volume nhưng đang có Container khác gắn vào | Từ chối xóa, hiển thị danh sách các container đang chiếm dụng | "[bold red]Không thể xóa ổ đĩa: Ổ đĩa đang được sử dụng bởi các container: [Danh sách tên container][/]" |
| **`ERR_SELINUX_AVOID`** | `SelinuxAccessDenied` | Quyền truy cập bị chặn bởi chính sách bảo mật SELinux | Hiển thị cờ cảnh báo an ninh, gợi ý gắn thêm đuôi `:Z` | "[bold yellow]Cảnh báo SELinux: Truy cập thư mục bị từ chối. Hãy gắn cờ :Z khi mount volume.[/]" |
| **`ERR_QUADLET_NOT_FOUND`**| `QuadletUnitNotFound` | File `.container` bị di chuyển hoặc đổi tên bên ngoài hệ thống | Thu hồi nhãn Quadlet trên container, chuyển về chế độ container độc lập | "[bold grey]Không còn tìm thấy file unit Quadlet tương ứng. Đã hoàn nguyên về chế độ quản lý tiêu chuẩn.[/]" |

---

## 6. ĐẶC TẢ GIAO DIỆN & TƯƠNG TÁC NGƯỜI DÙNG (PODS & RESOURCES UI)

### 6.1. Khung nhìn Cây Danh mục Pods (`[Pods] - Phím 1`)
- Phía trên danh sách hiển thị số lượng thống kê: `Total Pods: X | Running: Y | Degraded: Z`.
- Danh sách hiển thị dạng Cây thụt lề:
  - Dòng cấp 1: Biểu tượng mũi tên mở rộng `▼`, theo sau là tên Pod `[bold cyan]frontend-pod[/]`, nhãn trạng thái `[bold green][RUNNING][/]`, số container `(3 containers)`.
  - Dòng cấp 2 (Thụt vào 2 ký tự):
    - `├─ [bold magenta][INFRA][/] 12a4f5c09e8b (pause)`
    - `├─ [bold white]nginx-proxy[/] (running) - Port: 0.0.0.0:80->80/tcp`
    - `└─ [bold white]web-static[/] (running)`

### 6.2. Phím Tắt Thao Tác Chuyên Biệt trên Pod
- Phím `Space`: Khởi động hoặc Dừng toàn bộ các container trong Pod đồng bộ.
- Phím `d`: Mở hộp thoại xác nhận xóa toàn bộ Pod và các container con bên trong.
- Phím `c`: Mở form pop-up tạo mới một Pod (Nhập tên Pod, Cổng mở, Driver mạng).

---

## 7. MA TRẬN KỊCH BẢN KIỂM THỬ NGHIỆM THU (TEST CASES MATRIX)

| Mã Ca Kiểm Thử | Tên Kịch Bản Kiểm Thử | Điều Kiện Tiền Đề | Các Bước Thực Hiện | Tiêu Chí Đạt Nghiệm Thu (Pass Criteria) |
| :---: | :--- | :--- | :--- | :--- |
| **TC-M4-01** | Hiển thị đúng cây phân cấp Pod và Infra Container | Có 1 Pod đang chạy gồm 1 Infra và 2 Container ứng dụng | Chuyển sang tab Pods (nhấn phím `1`) | Cây danh mục hiển thị chuẩn xác Node cha và 3 Node con; Node đầu tiên có nhãn `[INFRA]`; phím mũi tên di chuyển mượt mà qua các node. |
| **TC-M4-02** | Dừng và Khởi động đồng bộ toàn bộ Pod | Chọn Node gốc của một Pod đang `Running` | Nhấn phím `Space` để dừng, sau đó nhấn tiếp `Space` để chạy lại | Toàn bộ các container con bên trong chuyển sang `Exited` sau lệnh dừng; khi chạy lại, container Infra khởi động trước, sau đó là các ứng dụng. |
| **TC-M4-03** | Hiển thị tiến độ kéo Image trực quan theo từng layer | Chuẩn bị một tên Image chưa có trên máy (ví dụ `alpine:edge`) | Mở form kéo Image, nhập tên và xác nhận | Hộp thoại modal mở ra; các layer hiển thị thanh tiến trình phần trăm riêng biệt; sau khi tải xong, Image mới xuất hiện trên bảng Images. |
| **TC-M4-04** | Dọn dẹp Volume mồ côi (Volume Prune) | Tạo 1 volume thử nghiệm không gắn với container nào | Mở tab Volumes (phím `4`), nhấn phím `p` (Prune) | Hộp thoại xác nhận hiển thị dung lượng đĩa sẽ được thu hồi; sau khi nhấn `y`, volume mồ côi bị xóa và giải phóng tài nguyên. |
| **TC-M4-05** | Nhận diện container Quadlet và điều phối khởi động lại | Có container khởi tạo từ file `.container` của systemd | Chọn container đó, nhấn phím `r` | Xuất hiện huy hiệu `[Quadlet]`; hộp thoại hiển thị 2 lựa chọn (Systemd vs Podman); khi chọn Systemd, lệnh `systemctl --user restart` được kích hoạt thành công. |

---

## 8. KẾT LUẬN & ĐIỀU KIỆN CHUYỂN BƯỚC THỰC THI

Bản Thiết kế Chi tiết này hoàn thiện toàn bộ đặc tả cho các tính năng gốc của Podman (Pods, Volumes, Networks, Images, Quadlet), tạo nên sự vượt trội và khác biệt hoàn toàn của `podman-FUI` so với các công cụ Docker truyền thống.

Toàn bộ tài liệu đảm bảo không sử dụng mã nguồn lập trình, sẵn sàng cho việc nghiệm thu kỹ thuật.
