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

### 2.3. Khởi tạo Dự án Mã nguồn F# (Next Steps)
- [ ] Khởi tạo F# Solution (`podman-FUI.sln`) sử dụng .NET SDK trên hệ thống.
- [ ] Tạo dự án Console F# chính (`src/PodmanFUI/PodmanFUI.fsproj`).
- [ ] Thêm các gói NuGet cần thiết:
  - `Terminal.Gui` (phiên bản v2).
  - `Spectre.Console`.
- [ ] Cấu hình trình biên dịch và định dạng mã nguồn.

### 2.4. Xây dựng PoC Kết nối Unix Domain Socket
- [ ] Thiết lập module kết nối socket sử dụng `SocketsHttpHandler` của .NET với `UnixDomainSocketEndPoint`.
- [ ] Tự động phát hiện đường dẫn socket Rootless (`$XDG_RUNTIME_DIR/podman/podman.sock`).
- [ ] Gửi yêu cầu kiểm tra tới endpoint `GET /v4.0.0/libpod/info`.
- [ ] Đọc và giải mã dữ liệu JSON trả về, in ra màn hình terminal các thông tin: Phiên bản Podman, Cgroup Version, Storage Driver, OS/Arch.

---

## 3. TIÊU CHÍ HOÀN THÀNH (DEFINITION OF DONE - DOD)

1. Toàn bộ tài liệu trong `docs/1.Investigation/` được hoàn thiện, tuân thủ nguyên tắc Zero Code Sample.
2. Chương trình PoC viết bằng F# chạy được trực tiếp trên terminal với lệnh `dotnet run`, kết nối thành công tới `podman.socket` đang hoạt động của người dùng mà không cần quyền root (`sudo`).
3. Dữ liệu JSON từ `/libpod/info` được deserialize thành công thành cấu trúc dữ liệu F# mà không có lỗi runtime.
