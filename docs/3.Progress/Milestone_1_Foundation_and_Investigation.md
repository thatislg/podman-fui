# LỘ TRÌNH CHI TIẾT: MILESTONE 1 - NỀN TẢNG & KHẢO SÁT KỸ THUẬT
## DỰ ÁN: PODMAN-FUI (F# TERMINAL USER INTERFACE FOR PODMAN)

- **Mã cột mốc:** M1-FOUNDATION
- **Vị trí lưu trữ:** `docs/3.Progress/Milestone_1_Foundation_and_Investigation.md`
- **Trạng thái:** **Đang thực hiện (In Progress - Ước tính đạt ~75%)**
- **Bản thiết kế kỹ thuật thực thi (Design Specification):**
  - [**`Design_M1_Foundation_and_Socket.md`**](../2.Design/Design_M1_Foundation_and_Socket.md) *(Bản thiết kế bằng lời đã phê duyệt)*
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
Xây dựng toàn bộ cơ sở lý thuyết, tài liệu kiến trúc, đặc tả yêu cầu, và hoàn thành ứng dụng thử nghiệm (PoC) kết nối thành công từ ngôn ngữ F# tới Unix Domain Socket của Podman Engine trên môi trường Linux mà không gặp bất kỳ rào cản phân quyền nào.

---

## 2. DANH MỤC CÔNG VIỆC CHI TIẾT (TASK CHECKLIST)

### 2.1. Thiết lập Môi trường & Quản trị Mã nguồn
- [x] Khởi tạo Git repository cục bộ, thiết lập remote GitHub (`https://github.com/thatislg/podman-fui.git`).
- [x] Cấu hình file `.gitignore` loại bỏ các thư mục tham khảo (`lazydocker/`, `podman-tui/`) và các tệp build tạm thời.
- [x] Đẩy commit ban đầu lên nhánh `main` trên GitHub.
- [x] Dọn dẹp triệt để thư mục làm việc, đảm bảo đường dẫn chuẩn không dính ký tự ẩn.

### 2.2. Khảo sát & Soạn thảo Tài liệu Kỹ thuật
- [x] Soạn thảo toàn văn Tài liệu Đặc tả Yêu cầu Phần mềm ([`SRS_podman-FUI.md`](../1.Investigation/SRS_podman-FUI.md)).
- [x] Hoàn thành 6 tài liệu điều tra kỹ thuật cơ sở (`INV-01` đến `INV-06`).
- [x] Khởi tạo 4 tài liệu điều tra chuyên sâu dự phòng (`INV-07` đến `INV-10`).
- [x] Chuẩn hóa toàn bộ cú pháp Mermaid trong các tài liệu, đảm bảo không có lỗi phân tích cú pháp.

### 2.3. Khởi tạo Cấu trúc Solution & Phân tầng Dự án F#
- [ ] Khởi tạo F# Solution (`podman-FUI.sln`) sử dụng .NET 10 SDK (`net10.0`).
- [ ] Khởi tạo 4 dự án thành phần theo kiến trúc phân tầng Clean Architecture:
  - `src/PodmanFUI.Domain` (Class Library - Chứa Models, Errors, Interfaces).
  - `src/PodmanFUI.Infrastructure` (Class Library - Chứa Socket Handler, JSON Parser, Discovery).
  - `src/PodmanFUI.Presentation` (Class Library - Chứa Theme, Renderer Terminal.Gui/Spectre).
  - `src/PodmanFUI.App` (Console Application - Điểm thực thi chính).
- [ ] Thiết lập liên kết phụ thuộc một chiều (Project References) giữa các tầng.
- [ ] Tích hợp các gói thư viện NuGet cần thiết:
  - `Terminal.Gui` (phiên bản v2).
  - `Spectre.Console` (phiên bản ổn định mới nhất).

### 2.4. Xây dựng Mã nguồn Thực thi theo Bản Thiết kế Chi tiết
- [ ] Hiện thực hóa tầng Domain: `Errors.fs`, `Models.fs`, `ISocketClient.fs`.
- [ ] Hiện thực hóa tầng Infrastructure:
  - `SocketDiscovery.fs`: Thuật toán tự động phát hiện đường dẫn socket (Rootless/Rootful/Custom).
  - `LibpodJsonParser.fs`: Thuật toán bóc tách dữ liệu JSON từ endpoint `/v4.0.0/libpod/info`.
  - `PodmanSocketClient.fs`: Cơ chế kết nối `SocketsHttpHandler` với `UnixDomainSocketEndPoint`.
- [ ] Hiện thực hóa tầng Presentation:
  - `Theme.fs`: Bảng mã màu chuẩn ANSI và phong cách đồ họa.
  - `PocRenderer.fs`: Hàm hiển thị bảng thông tin nghiệm thu bằng Spectre Table.
- [ ] Hiện thực hóa tầng App:
  - `Program.fs`: Điều phối luồng khởi động PoC và bắt ngoại lệ an toàn.

### 2.5. Kiểm thử Nghiệm thu Kỹ thuật (Technical Acceptance Testing)
- [ ] Thực thi kiểm thử ca **TC-01**: Kết nối Rootless tiêu chuẩn (Happy Path) thành công, in bảng thông tin máy chủ.
- [ ] Thực thi kiểm thử ca **TC-02**: Kiểm tra cơ chế xử lý lỗi khi socket không tồn tại hoặc chưa bật service.
- [ ] Thực thi kiểm thử ca **TC-03**: Kiểm tra nhận diện socket qua biến môi trường tùy chỉnh (`CONTAINER_HOST`).
- [ ] Thực thi kiểm thử ca **TC-04**: Kiểm tra tính đầy đủ của trường Cgroup Controllers (`cpu`, `memory`).

---

## 3. TIÊU CHÍ HOÀN THÀNH (DEFINITION OF DONE - DOD)

1. **Tài liệu thiết kế chi tiết:** Hoàn tất bản Thiết kế Chi tiết [**`Design_M1_Foundation_and_Socket.md`**](../2.Design/Design_M1_Foundation_and_Socket.md) với đầy đủ danh mục tệp, bảng dữ liệu, thuật toán từng bước, ma trận lỗi và kịch bản test (Zero Code Sample).
2. **Chương trình PoC F#:** Biên dịch thành công và chạy mượt mà bằng lệnh `dotnet run --project src/PodmanFUI.App`.
3. **Kết nối Socket thực tế:** Kết nối thành công tới socket `/run/user/1000/podman/podman.sock` mà không yêu cầu quyền root (`sudo`).
4. **Trực quan hóa dữ liệu:** Hiển thị chuẩn xác bảng thông tin Podman Engine (Phiên bản, Cgroup v2, Controllers, Storage Driver, Kernel) với viền bo tròn bo góc theo chuẩn Spectre.Console.
5. **Kiểm thử nghiệm thu:** Vượt qua toàn bộ các ca kiểm thử từ TC-01 đến TC-04.
