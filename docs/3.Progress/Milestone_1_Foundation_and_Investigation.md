# LỘ TRÌNH CHI TIẾT: MILESTONE 1 - NỀN TẢNG & KHẢO SÁT KỸ THUẬT
## DỰ ÁN: PODMAN-FUI (F# TERMINAL USER INTERFACE FOR PODMAN)

- **Mã cột mốc:** M1-FOUNDATION
- **Vị trí lưu trữ:** `docs/3.Progress/Milestone_1_Foundation_and_Investigation.md`
- **Trạng thái:** **Đang thực hiện (In Progress - Ước tính đạt ~75%)**
- **Bản thiết kế kỹ thuật thực thi (Design Specification):**
  - [**`Design_M1_Foundation_and_Socket.md`**](../2.Design/Design_M1_Foundation_and_Socket.md) *(Bản thiết kế chi tiết DD-M1-FOUNDATION-SOCKET đã phê duyệt)*
- **Tài liệu điều tra liên kết (Investigation References):**
  - [**`SRS_podman-FUI.md`**](../1.Investigation/SRS_podman-FUI.md)
  - [**`01_Podman_Socket_and_API_Investigation.md`**](../1.Investigation/01_Podman_Socket_and_API_Investigation.md)
  - [**`02_UI_Framework_and_Rendering_Investigation.md`**](../1.Investigation/02_UI_Framework_and_Rendering_Investigation.md)
  - [**`03_FSharp_Architecture_and_State_Management_Investigation.md`**](../1.Investigation/03_FSharp_Architecture_and_State_Management_Investigation.md)
  - [**`04_Benchmarking_and_Feature_Mapping_Investigation.md`**](../1.Investigation/04_Benchmarking_and_Feature_Mapping_Investigation.md)
  - [**`05_Packaging_and_Cross_Distro_Investigation.md`**](../1.Investigation/05_Packaging_and_Cross_Distro_Investigation.md)
  - [**`06_Internationalization_i18n_Investigation.md`**](../1.Investigation/06_Internationalization_i18n_Investigation.md)

---

## 1. MỤC TIÊU CỘT MỐC 1
Xây dựng toàn bộ cơ sở lý thuyết, tài liệu kiến trúc, đặc tả yêu cầu, và hoàn thành ứng dụng thử nghiệm (PoC) kết nối thành công từ ngôn ngữ F# (.NET 10) tới Unix Domain Socket của Podman Engine trên môi trường Linux mà không gặp bất kỳ rào cản phân quyền nào.

---

## 2. DANH MỤC CÔNG VIỆC CHI TIẾT (TASK CHECKLIST)

### 2.1. Thiết lập Môi trường & Quản trị Mã nguồn
- [x] Khởi tạo Git repository cục bộ, thiết lập remote GitHub (`https://github.com/thatislg/podman-fui.git`).
- [x] Cấu hình file `.gitignore` loại bỏ các thư mục tham khảo (`lazydocker/`, `podman-tui/`) và các tệp build tạm thời.
- [x] Đẩy commit ban đầu lên nhánh `main` trên GitHub.
- [x] Dọn dẹp triệt để thư mục làm việc, đảm bảo đường dẫn chuẩn không dính ký tự ẩn (`\r`).

### 2.2. Khảo sát & Soạn thảo Tài liệu Kỹ thuật
- [x] Soạn thảo toàn văn Tài liệu Đặc tả Yêu cầu Phần mềm ([`SRS_podman-FUI.md`](../1.Investigation/SRS_podman-FUI.md)).
- [x] Hoàn thành 6 tài liệu điều tra kỹ thuật cơ sở (`INV-01` đến `INV-06`).
- [x] Khởi tạo 4 tài liệu điều tra chuyên sâu dự phòng (`INV-07` đến `INV-10`).
- [x] Chuẩn hóa toàn bộ cú pháp Mermaid trong các tài liệu, đảm bảo không có lỗi phân tích cú pháp trên Zed và GitHub preview.

### 2.3. Khởi tạo Cấu trúc Solution & Phân tầng Dự án F#
*[Căn cứ thiết kế: `Design_M1_Foundation_and_Socket.md` - Mục 2: Phân Rã Danh Mục Tệp & Mô-đun Mã Nguồn]*
- [ ] **Subtask 2.3.1 - Khởi tạo Solution File:**
  - Chạy `dotnet new sln -n podman-FUI` tại thư mục gốc repository.
- [ ] **Subtask 2.3.2 - Khởi tạo 4 dự án thành phần theo Clean Architecture:**
  - Tạo dự án `src/PodmanFUI.Domain` (Class Library F#, target `net10.0`).
  - Tạo dự án `src/PodmanFUI.Infrastructure` (Class Library F#, target `net10.0`).
  - Tạo dự án `src/PodmanFUI.Presentation` (Class Library F#, target `net10.0`).
  - Tạo dự án `src/PodmanFUI.App` (Console Application F#, target `net10.0`).
- [ ] **Subtask 2.3.3 - Đăng ký liên kết phụ thuộc một chiều (Project References):**
  - Gắn `PodmanFUI.Domain` vào `PodmanFUI.Infrastructure`.
  - Gắn `PodmanFUI.Domain` vào `PodmanFUI.Presentation`.
  - Gắn `PodmanFUI.Infrastructure` và `PodmanFUI.Presentation` vào `PodmanFUI.App`.
  - Thêm cả 4 dự án vào `podman-FUI.sln`.
- [ ] **Subtask 2.3.4 - Tích hợp các gói NuGet phụ thuộc:**
  - Thêm gói `Terminal.Gui` (v2.0.0-alpha hoặc mới nhất) vào `PodmanFUI.Presentation`.
  - Thêm gói `Spectre.Console` vào `PodmanFUI.Presentation`.
  - Kiểm tra lệnh `dotnet restore` và `dotnet build` biên dịch thành công.

### 2.4. Hiện thực hóa Mã nguồn Tầng Domain & Infrastructure
*[Căn cứ thiết kế: `Design_M1_Foundation_and_Socket.md` - Mục 3: Đặc Tả Hợp Đồng Dữ Liệu & Mục 4: Thuật Toán Nghiệp Vụ]*
- [ ] **Subtask 2.4.1 - Hiện thực hóa Tầng Domain:**
  - `src/PodmanFUI.Domain/Errors.fs`: Định nghĩa DU `ConnectionError` (`SocketNotFound`, `AccessDenied`, `HttpFailure`, `DeserializationError`) theo bảng mã lỗi Mục 5.
  - `src/PodmanFUI.Domain/Models.fs`: Định nghĩa các kiểu bản ghi F# (`CgroupInfo`, `HostInfo`, `VersionInfo`, `SystemInfo`) và kiểu phân loại (`SocketMode`, `SocketDiscoveryResult`) theo Mục 3.1..3.4.
  - `src/PodmanFUI.Domain/ISocketClient.fs`: Khai báo interface `IPodmanSocketClient` với phương thức `GetSystemInfoAsync: unit -> Async<Result<SystemInfo, ConnectionError>>`.
- [ ] **Subtask 2.4.2 - Hiện thực hóa Tầng Infrastructure:**
  - `src/PodmanFUI.Infrastructure/SocketDiscovery.fs`: Triển khai thuật toán dò tìm socket tuần tự theo Mục 4.1 (`CONTAINER_HOST` -> `PODMAN_SOCKET` -> `$XDG_RUNTIME_DIR/podman/podman.sock` -> `/run/podman/podman.sock`).
  - `src/PodmanFUI.Infrastructure/LibpodJsonParser.fs`: Triển khai thuật toán bóc tách dữ liệu JSON từ `/v4.0.0/libpod/info` theo Mục 4.3 bằng `JsonDocument`.
  - `src/PodmanFUI.Infrastructure/PodmanSocketClient.fs`: Khởi tạo `SocketsHttpHandler` tùy biến với `UnixDomainSocketEndPoint` và `HttpClient` kết nối qua Unix Domain Socket theo Mục 4.2.

### 2.5. Hiện thực hóa Tầng Presentation & App Host
*[Căn cứ thiết kế: `Design_M1_Foundation_and_Socket.md` - Mục 6: Đặc Tả Giao Diện Nghiệm Thu PoC]*
- [ ] **Subtask 2.5.1 - Bảng màu & Giao diện Presentation:**
  - `src/PodmanFUI.Presentation/Theme.fs`: Định nghĩa bảng màu ANSI (Title Cyan1, Success Green, Warning Yellow, Failure Red, Border DeepSkyBlue1) theo Mục 6.1.
  - `src/PodmanFUI.Presentation/PocRenderer.fs`: Hiện thực hàm `renderWelcomeBanner`, `renderSystemInfoTable` (sử dụng `TableBorder.Rounded`, in đầy đủ 8 hàng thuộc tính hệ thống), và `renderErrorDialog` theo Mục 6.2.
- [ ] **Subtask 2.5.2 - Điểm thực thi chính (App Entrypoint):**
  - `src/PodmanFUI.App/Program.fs`: Điều phối luồng khởi động PoC: Gọi `SocketDiscovery.discoverSocket()` -> Kết nối Socket -> Lấy JSON `/info` -> Parse dữ liệu -> Render bảng kết quả bằng Spectre.Console. Bắt ngoại lệ an toàn và trả mã thoát `0` (thành công) hoặc `1` (thất bại).

### 2.6. Kiểm thử Nghiệm thu Kỹ thuật (Technical Acceptance Testing)
*[Căn cứ thiết kế: `Design_M1_Foundation_and_Socket.md` - Mục 7: Ma Trận Ca Kiểm Thử Nghiệm Thu]*
- [ ] **Subtask 2.6.1 - Thực thi kiểm thử ca TC-01 (Happy Path Rootless):**
  - Điều kiện: Socket `/run/user/1000/podman/podman.sock` đang hoạt động.
  - Lệnh: `dotnet run --project src/PodmanFUI.App`.
  - Tiêu chí đạt: Bảng thông tin hệ thống hiển thị bo góc tròn, in đúng phiên bản Podman 4.9.3, Cgroup v2 (systemd), Storage Driver overlay, mã thoát `0`.
- [ ] **Subtask 2.6.2 - Thực thi kiểm thử ca TC-02 (Socket vắng mặt):**
  - Điều kiện: Tạm dừng socket bằng `systemctl --user stop podman.socket`.
  - Lệnh: `dotnet run --project src/PodmanFUI.App`.
  - Tiêu chí đạt: Báo lỗi `ERR_SOCK_404`, in hướng dẫn bật service bằng `systemctl --user enable --now podman.socket`, mã thoát `1`, không bị crash tiến trình. Bật lại socket sau khi test.
- [ ] **Subtask 2.6.3 - Thực thi kiểm thử ca TC-03 (Biến môi trường Custom):**
  - Lệnh: `CONTAINER_HOST=unix:///run/user/1000/podman/podman.sock dotnet run --project src/PodmanFUI.App`.
  - Tiêu chí đạt: Ứng dụng nhận diện chế độ `Custom`, kết nối thành công.
- [ ] **Subtask 2.6.4 - Thực thi kiểm thử ca TC-04 (Kiểm tra Cgroup Controllers):**
  - Tiêu chí đạt: Hàng Cgroup Controllers hiển thị đầy đủ danh sách gồm `cpu`, `memory`, `pids`.

---

## 3. TIÊU CHÍ HOÀN THÀNH (DEFINITION OF DONE - DOD)

1. **Tài liệu thiết kế chi tiết:** Hoàn tất bản Thiết kế Chi tiết [**`Design_M1_Foundation_and_Socket.md`**](../2.Design/Design_M1_Foundation_and_Socket.md) với đầy đủ danh mục tệp, bảng dữ liệu, thuật toán từng bước, ma trận lỗi và kịch bản test (Zero Code Sample).
2. **Chương trình PoC F#:** Biên dịch thành công và chạy mượt mà bằng lệnh `dotnet run --project src/PodmanFUI.App`.
3. **Kết nối Socket thực tế:** Kết nối thành công tới socket `/run/user/1000/podman/podman.sock` mà không yêu cầu quyền root (`sudo`).
4. **Trực quan hóa dữ liệu:** Hiển thị chuẩn xác bảng thông tin Podman Engine (Phiên bản, Cgroup v2, Controllers, Storage Driver, Kernel) với viền bo tròn bo góc theo chuẩn Spectre.Console.
5. **Kiểm thử nghiệm thu:** Vượt qua toàn bộ các ca kiểm thử từ TC-01 đến TC-04.
