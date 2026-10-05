# LỘ TRÌNH CHI TIẾT: MILESTONE 2 - XÂY DỰNG KIẾN TRÚC MVU & QUẢN LÝ CONTAINER (CORE MVP)
## DỰ ÁN: PODMAN-FUI (F# TERMINAL USER INTERFACE FOR PODMAN)

- **Mã cột mốc:** M2-CORE-MVP
- **Vị trí lưu trữ:** `docs/3.Progress/Milestone_2_Core_MVP_Dashboard.md`
- **Trạng thái:** **Hoàn thành (Completed)**
- **Bản thiết kế kỹ thuật thực thi (Design Specification):**
  - [**`Design_M2_Core_MVP_Dashboard.md`**](../2.Design/Design_M2_Core_MVP_Dashboard.md) *(Bản thiết kế chi tiết DD-M2-CORE-MVP-DASHBOARD đã phê duyệt)*
- **Tài liệu điều tra liên kết (Investigation References):**
  - [**`02_UI_Framework_and_Rendering_Investigation.md`**](../1.Investigation/02_UI_Framework_and_Rendering_Investigation.md)
  - [**`03_FSharp_Architecture_and_State_Management_Investigation.md`**](../1.Investigation/03_FSharp_Architecture_and_State_Management_Investigation.md)
  - [**`07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md`**](../1.Investigation/07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md)

---

## 1. MỤC TIÊU CỘT MỐC 2
Hiện thực hóa bộ khung kiến trúc luồng dữ liệu một chiều MVU (Model-View-Update) bằng F#, dựng bố cục giao diện Dashboard đa bảng co giãn động (Responsive Auto-scaling), và hoàn thành các chức năng quản lý cốt lõi cho Container (liệt kê danh sách, thao tác nhanh 1 chạm Start/Stop/Restart/Pause/Delete, và mở Interactive Shell an toàn).

---

## 2. DANH MỤC CÔNG VIỆC CHI TIẾT (TASK CHECKLIST)

### 2.1. Thiết kế Kiến trúc Mã nguồn (Thư mục `docs/2.Design/`)
*[Căn cứ thiết kế: `Design_M2_Core_MVP_Dashboard.md` - Toàn văn bản vẽ DD-M2-CORE-MVP-DASHBOARD]*
- [x] Soạn thảo tài liệu thiết kế chi tiết phân rã module hệ thống (Domain, Infrastructure, Presentation, App).
- [x] Đặc tả chi tiết Hợp đồng dữ liệu nguyên tử, sơ đồ luồng MVU, thuật toán co giãn layout và bọc shell PTY.

### 2.2. Hiện thực hóa Mô hình Dữ liệu & Hợp đồng Tầng Domain
*[Căn cứ thiết kế: `Design_M2_Core_MVP_Dashboard.md` - Mục 2.1: Phân Rã Domain & Mục 3: Đặc Tả Hợp Đồng Dữ Liệu]*
- [x] **Subtask 2.2.1 - Thực thể miền Container (`ContainerModels.fs`):**
  - Định nghĩa DU/Enum `ContainerStatus` (`Running`, `Exited`, `Paused`, `Created`, `Restarting`, `Dead`).
  - Định nghĩa Record `PortMapping` (`HostIp`, `HostPort`, `ContainerPort`, `Protocol`).
  - Định nghĩa Record `ContainerSummary` (đầy đủ 10 trường: `Id`, `ShortId`, `Names`, `PrimaryName`, `Image`, `Status`, `StatusText`, `Created`, `Ports`, `PodName`) theo Mục 3.1.
  - Định nghĩa Record `ContainerDetail` phục vụ tab Inspect.
  - Định nghĩa DU `ContainerAction` (`Start`, `Stop`, `Restart`, `Pause`, `Unpause`, `Delete`, `ExecShell`).
- [x] **Subtask 2.2.2 - Thực thể Điều hướng & Khung nhìn (`NavigationModels.fs`):**
  - Định nghĩa DU `NavigationCategory` (`Pods`, `Containers`, `Images`, `Volumes`, `Networks`).
  - Định nghĩa DU `DetailTab` (`Logs`, `Inspect`, `Top`, `Env`).
  - Định nghĩa DU `ActiveFocus` (`Sidebar`, `DetailPane`, `ModalDialog`).
  - Định nghĩa DU `ModalState` (`Closed`, `ConfirmActionDialog`, `FilterInputDialog`, `ErrorAlertDialog`).
- [x] **Subtask 2.2.3 - Khung kiểu dữ liệu MVU (`MvuTypes.fs`):**
  - Định nghĩa Record `DashboardModel` (đầy đủ 14 trường dữ liệu theo bảng 3.2).
  - Định nghĩa DU `DashboardMsg` phân thành 3 nhóm: Bàn phím & Chuột, Tác vụ Container, Dữ liệu Nền theo bảng 3.3.
  - Định nghĩa DU `DashboardCmd` đại diện các chỉ thị bất đồng bộ cần gửi tới hạ tầng.
- [x] **Subtask 2.2.4 - Giao diện Dịch vụ trừu tượng (`IContainerService.fs`):**
  - Khai báo Interface `IContainerService` với các phương thức: `ListContainersAsync`, `GetContainerDetailAsync`, `PerformActionAsync`.

### 2.3. Hiện thực hóa Tầng Hạ tầng Infrastructure
*[Căn cứ thiết kế: `Design_M2_Core_MVP_Dashboard.md` - Mục 2.2 & Mục 4.3: Thuật Toán Bọc Tiến Trình Shell PTY]*
- [x] **Subtask 2.3.1 - Giao tiếp Container REST API (`ContainerApiClient.fs`):**
  - Hiện thực hóa gọi `GET /v4.0.0/libpod/containers/json?all=true` và phân giải danh sách `ContainerSummary list`.
  - Hiện thực hóa gọi `GET /v4.0.0/libpod/containers/{name}/json` nạp dữ liệu chi tiết.
  - Hiện thực hóa các API tác động:
    - `POST /containers/{name}/start`
    - `POST /containers/{name}/stop?t=10` (Graceful timeout 10 giây)
    - `POST /containers/{name}/restart?t=10`
    - `POST /containers/{name}/pause` & `POST /containers/{name}/unpause`
    - `DELETE /containers/{name}?force=false`
- [x] **Subtask 2.3.2 - Điều phối Interactive Shell PTY an toàn (`ProcessExecutionService.fs`):**
  - Hiện thực hóa thuật toán 3 bước theo Mục 4.3:
    1. Kiểm tra trạng thái container đang `Running`.
    2. Gọi `Application.Driver.Suspend()`, lưu cấu hình `termios` gốc của host.
    3. Khởi tạo tiến trình `podman exec -it <id> /bin/sh` (fallback `/bin/bash`), tắt chuyển hướng stream để kết nối trực tiếp bàn phím/màn hình host.
    4. Bọc trong khối `try ... finally` tối cao: luôn phục hồi `termios`, gọi `Application.Driver.Resume()` và làm mới bộ đệm màn hình Dashboard.

### 2.4. Hiện thực hóa Động cơ Co giãn Bố cục & Giao diện Presentation
*[Căn cứ thiết kế: `Design_M2_Core_MVP_Dashboard.md` - Mục 2.3, Mục 4.2 & Mục 6: Đặc Tả Phím Tắt]*
- [x] **Subtask 2.4.1 - Động cơ tính toán bố cục thích ứng (`ResponsiveLayoutManager.fs`):**
  - Hiện thực hóa thuật toán phân loại breakpoint theo Mục 4.2:
    - Chế độ `Compact` (< 80 cột hoặc < 24 dòng): Tự động chuyển sang chế độ màn hình đơn (Stacked Single View).
    - Chế độ `Standard` (80 - 120 cột): Bố cục song song 38% Sidebar - 62% Detail, ẩn bớt cột phụ.
    - Chế độ `Expanded` (> 120 cột): Hiển thị đầy đủ toàn bộ thông tin.
- [x] **Subtask 2.4.2 - Thành phần giao diện Danh mục trên cùng (`Views/TopBarView.fs`):**
  - Render 5 danh mục chính ([1] Pods, [2] Containers, [3] Images, [4] Volumes, [5] Networks).
  - Highlight danh mục đang nhận tiêu điểm; hiển thị trạng thái kết nối socket.
- [x] **Subtask 2.4.3 - Thành phần giao diện Danh sách bên trái (`Views/SidebarView.fs`):**
  - Sử dụng Terminal.Gui TableView hiển thị danh sách container.
  - Vẽ biểu tượng trạng thái trực quan: Chấm xanh (Running), Chấm xám (Exited), Chấm vàng (Paused), Chấm đỏ (Dead).
- [x] **Subtask 2.4.4 - Thành phần giao diện Chi tiết bên phải (`Views/ContainerDetailView.fs`):**
  - Quản lý 4 tab: `[Logs]`, `[Inspect]`, `[Top]`, `[Env]`.
  - Hỗ trợ đổi tab bằng chuột hoặc phím `[` và `]`.
- [x] **Subtask 2.4.5 - Thành phần giao diện Chân trang & Hộp thoại (`Views/FooterView.fs`, `Views/ModalsView.fs`):**
  - Render thanh phím tắt ngữ cảnh ở đáy màn hình.
  - Render Modal xác nhận xóa container (`ConfirmActionDialog`), Modal nhập từ khóa tìm kiếm (`FilterInputDialog`), Modal báo lỗi hệ thống (`ErrorAlertDialog`).
- [x] **Subtask 2.4.6 - Điều hướng Bàn phím Toàn cục & Hỗ trợ Chuột Toàn diện:**
  - Chuyển `top.KeyDown` sang `app.Keyboard.KeyDown` bắt phím ở tầng Application cao nhất, giải quyết triệt để lỗi `ListView` nuốt phím số `1..5`, `Tab`, `[`, `]`.
  - Bổ sung `CategoryClicked` trên `TopBarView` và `TabClicked` trên `ContainerDetailView` hỗ trợ click chuột trực tiếp đổi danh mục và tab.
  - Bổ sung phím mũi tên `←` / `→` và phím chức năng `F1` - `F4` chuyển tab chi tiết vòng tròn.
  - Bổ sung màn hình tổng quan và hướng dẫn quay lại khi chọn các danh mục chưa mở (Pods, Images, Volumes, Networks).
- [x] **Subtask 2.4.7 - Teardown Terminal & Vô hiệu hóa Mouse Tracking ANSI SGR:**
  - Bọc vòng lặp trong `try ... finally` ở cả `MvuLoop.fs` và `Program.fs`.
  - Gọi `(app :> IDisposable).Dispose()` và `top.Dispose()` dừng các thread chạy nền của Terminal.Gui.
  - Gửi chuỗi ANSI reset tiêu chuẩn (`\u001B[?1006l\u001B[?1015l\u001B[?1003l\u001B[?1002l\u001B[?1001l\u001B[?1000l\u001B[?2004l\u001B[?1049l\u001B[?25h\u001B[0m`), tắt triệt để chế độ chuột SGR và Any-motion, giải quyết dứt điểm hiện tượng in rác tọa độ chuột `21M14;81` vào terminal prompt khi thoát.
- [x] **Subtask 2.4.8 - Triệt tiêu Cảnh báo Obsolete (0 Warnings Build):**
  - Thêm chỉ thị `#nowarn "0044"` tại `ContainerDetailView.fs` triệt tiêu cảnh báo obsolete `FS0044` của `TextView`, đưa giải pháp về trạng thái build hoàn toàn sạch sẽ: **0 Warning(s), 0 Error(s)**.
- [x] **Subtask 2.4.9 - Bảng màu Cyberpunk / Modern Cyan Dark Theme & Viền Bo Tròn:**
  - Mở rộng `Theme.fs` với bộ phối màu chuẩn: viền bo tròn mềm mại (`LineStyle.Rounded`), tone nền Dark Navy (`#0F141E`), viền và tiêu đề Cyan1 rực rỡ khi được focus (`▶ Containers ◀`), highlight hàng container được chọn với DeepSkyBlue (`#005096`), modal cảnh báo xóa container viền đỏ nổi bật.

### 2.5. Hiện thực hóa Động cơ Vòng lặp MVU & App Host
*[Căn cứ thiết kế: `Design_M2_Core_MVP_Dashboard.md` - Mục 4.1: Thuật Toán Vòng Lặp MVU]*
- [x] **Subtask 2.5.1 - Hiện thực hóa Vòng lặp Elmish (`MvuLoop.fs`):**
  - Hiện thực hàm khởi tạo thuần khiết `init: unit -> DashboardModel * DashboardCmd`.
  - Hiện thực hàm cập nhật thuần khiết `update: DashboardMsg -> DashboardModel -> DashboardModel * DashboardCmd` xử lý toàn bộ các kịch bản bàn phím, chuột, và thông điệp socket theo Mục 4.1.
  - Đồng bộ luồng giao diện thông qua `Application.Invoke` của Terminal.Gui v2.
- [x] **Subtask 2.5.2 - Điểm thực thi Dashboard App (`src/PodmanFUI.App/Program.fs`):**
  - Khởi tạo `Application.Init()`, gắn kết `MvuLoop`, bắt sự kiện tắt ứng dụng an toàn khi người dùng nhấn `q`, `Q`, `Ctrl+Q` hoặc `Ctrl+C`.

### 2.6. Kiểm thử Nghiệm thu Kỹ thuật (Technical Acceptance Testing)
*[Căn cứ thiết kế: `Design_M2_Core_MVP_Dashboard.md` - Mục 7: Ma Trận Ca Kiểm Thử Nghiệm Thu]*
- [x] **Subtask 2.6.1 - Thực thi kiểm thử ca TC-M2-01 (Khởi động Dashboard):**
  - Khởi động ứng dụng, xác nhận nạp đúng danh sách container từ socket, hiển thị đủ 4 phân vùng giao diện.
- [x] **Subtask 2.6.2 - Thực thi kiểm thử ca TC-M2-02 (Thao tác 1 phím Start/Stop):**
  - Chọn container đang dừng, nhấn `s` -> chuyển sang Running dưới 2s không cần xác nhận.
- [x] **Subtask 2.6.3 - Thực thi kiểm thử ca TC-M2-03 (Interactive Shell & Khôi phục):**
  - Nhấn `e` trên container đang chạy, tương tác với shell bên trong, gõ `exit` -> giao diện TUI khôi phục 100% không lệch ký tự.
- [x] **Subtask 2.6.4 - Thực thi kiểm thử ca TC-M2-04 (Co giãn thích ứng đa độ phân giải):**
  - Kéo thu nhỏ cửa sổ terminal xuống < 80 cột -> chuyển chế độ Stacked Mode không bị vỡ giao diện.
- [x] **Subtask 2.6.5 - Thực thi kiểm thử ca TC-M2-05 (Tìm kiếm & Lọc mờ container):**
  - Nhấn `/`, gõ từ khóa tên container -> danh sách lọc tức thời theo thời gian thực.
- [x] **Subtask 2.6.6 - Thực thi kiểm thử ca TC-M2-06 (Hộp thoại xác nhận xóa an toàn):**
  - Nhấn `d` trên container dừng -> mở modal xác nhận; nhấn `Esc` để hủy, nhấn `y` để xóa.
- [x] **Subtask 2.6.7 - Thực thi kiểm thử ca TC-M2-07 (Điều hướng danh mục & Tab đa phương thức):**
  - Xác nhận phím số `1`..`5`, click chuột TopBar, phím mũi tên `←` / `→`, `[` / `]`, `F1` - `F4`, và phím `Tab` luân chuyển focus hoạt động mượt mà.
- [x] **Subtask 2.6.8 - Thực thi kiểm thử ca TC-M2-08 (Dọn dẹp Terminal khi thoát trong PTY):**
  - Kiểm thử thoát ứng dụng trong môi trường PTY, xác nhận toàn bộ mã ANSI tắt chuột (`?1006l`, `?1003l`), thoát buffer (`?1049l`) và hiện con trỏ (`?25h`) được gửi sạch sẽ.
- [x] **Subtask 2.6.9 - Thực thi kiểm thử ca TC-M2-09 (Chủ đề màu sắc & Viền bo tròn):**
  - Xác thực giao diện Modern Cyan Dark Theme và viền bo tròn `LineStyle.Rounded` hoạt động chuẩn xác, không chói mắt.

---

## 3. TIÊU CHÍ HOÀN THÀNH (DEFINITION OF DONE - DOD)

1. Dashboard hiển thị mượt mà trên terminal, tự động co giãn tỷ lệ khi resize cửa sổ mà không bị tràn viền hay vỡ chữ.
2. Danh sách container được cập nhật chính xác từ Podman Socket.
3. Người dùng có thể Start, Stop, Restart, Xóa container bằng phím tắt một chạm (1-keypress).
4. Nhấn phím `e` mở được shell trực tiếp vào container, khi gõ `exit` quay trở lại dashboard bình thường mà không bị lỗi con trỏ chuột hay mất hiển thị terminal.
5. Hỗ trợ điều hướng toàn diện cả bàn phím (`1`..`5`, `Tab`, `←`/`→`, `[`/`]`, `F1`-`F4`) và chuột (click danh mục TopBar và click tab chi tiết).
6. Khi thoát ứng dụng, terminal được dọn dẹp sạch sẽ 100%, không bị tràn ký tự rác tọa độ chuột `21M14;81`.
7. Mã nguồn biên dịch đạt trạng thái hoàn hảo **0 Warning(s), 0 Error(s)**.
8. Giao diện sở hữu phối màu Modern Cyan Dark Theme dịu mắt với đường viền bo góc tròn mềm mại.
9. Vượt qua toàn bộ các ca kiểm thử từ TC-M2-01 đến TC-M2-09.
