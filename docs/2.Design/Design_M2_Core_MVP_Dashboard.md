# BẢN THIẾT KẾ KỸ THUẬT BẰNG LỜI: MILESTONE 2
## THIẾT KẾ KIẾN TRÚC MVU & MODULE QUẢN LÝ CONTAINER (CORE MVP)

- **Mã tài liệu:** DES-M2-CORE-MVP-DASHBOARD
- **Vị trí lưu trữ:** `docs/2.Design/Design_M2_Core_MVP_Dashboard.md`
- **Phiên bản:** 1.0.0
- **Trạng thái:** Bản thiết kế đề xuất (Draft)
- **Tài liệu căn cứ:** 
  - [`SRS_podman-FUI.md`](../1.Investigation/SRS_podman-FUI.md)
  - [`02_UI_Framework_and_Rendering_Investigation.md`](../1.Investigation/02_UI_Framework_and_Rendering_Investigation.md)
  - [`03_FSharp_Architecture_and_State_Management_Investigation.md`](../1.Investigation/03_FSharp_Architecture_and_State_Management_Investigation.md)
  - [`07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md`](../1.Investigation/07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md)
- **Tài liệu tiến độ gắn kèm:** [`Milestone_2_Core_MVP_Dashboard.md`](../3.Progress/Milestone_2_Core_MVP_Dashboard.md)

---

## 1. MỤC TIÊU THIẾT KẾ
Tài liệu này đặc tả chi tiết bằng lời toàn bộ kiến trúc cho Milestone 2:
1. Thiết kế chu trình điều khiển luồng dữ liệu một chiều MVU (Model-View-Update).
2. Thiết kế thuật toán phân bổ tỷ lệ giao diện co giãn động (Responsive Auto-scaling Layout) và các ngưỡng breakpoint khi terminal thay đổi kích thước.
3. Thiết kế luồng nghiệp vụ quản lý Container (Start, Stop, Restart, Pause, Delete) và cơ chế bọc tiến trình Interactive Shell an toàn.

---

## 2. THIẾT KẾ CHU TRÌNH MVU (MODEL-VIEW-UPDATE)

### 2.1. Cấu trúc Mô hình Dữ liệu Trạng thái Toàn cục (The Model)
Mô hình trạng thái toàn cục được thiết kế dạng cây cấu trúc dữ liệu bất biến:
- **Trạng thái Điều hướng Chung:**
  - Danh mục đang chọn (Category): `Pods`, `Containers`, `Images`, `Volumes`, `Networks`. Mặc định khi khởi động là `Containers`.
  - Tab chi tiết đang chọn (DetailTab): `Logs`, `Stats`, `Inspect`, `Top`, `Env`. Mặc định là `Logs`.
  - Khung nhìn đang nhận tiêu điểm (FocusedPanel): `SidebarLeft` (Panel Danh sách) hoặc `MainRight` (Panel Chi tiết).
- **Trạng thái Dữ liệu Thực thể:**
  - Danh sách container hiện có trên máy (Container List).
  - Chỉ số container đang được con trỏ trỏ tới (SelectedIndex).
  - Tập hợp các container được đánh dấu chọn hàng loạt (SelectedIds Set).
- **Trạng thái Tìm kiếm & Lọc:**
  - Cờ bật/tắt chế độ gõ tìm kiếm (IsFiltering: true/false).
  - Chuỗi truy vấn lọc mờ (FilterQuery: string).
- **Trạng thái Hộp thoại Modal:**
  - Định nghĩa các cửa sổ popup: Không mở modal nào (`None`), Modal xác nhận xóa (`ConfirmDelete`), hoặc Modal thông báo lỗi (`ErrorMessage`).

### 2.2. Danh mục Thông điệp Sự kiện (Message Dictionary)
Toàn bộ các tác động lên hệ thống được phân thành 3 nhóm thông điệp có cấu trúc:
1. **Thông điệp Nhập liệu Bàn phím & Chuột:**
   - `KeyDown(KeyName)`: Bắt các phím Vim (`h`, `j`, `k`, `l`), phím số (`1..5`), phím Tab, Space, Delete.
   - `MouseClick(PanelTarget, RowIndex)`: Bắt sự kiện người dùng click chuột trái vào một dòng hoặc tab.
   - `MouseScroll(ScrollDirection)`: Bắt sự kiện cuộn con lăn chuột trên danh sách hoặc vùng log.
2. **Thông điệp Tác vụ Nghiệp vụ Container:**
   - `TriggerStart`: Yêu cầu khởi động container đang chọn.
   - `TriggerStop`: Yêu cầu dừng container đang chọn.
   - `TriggerRestart`: Yêu cầu khởi động lại container.
   - `TriggerDeleteConfirmed`: Người dùng nhấn đồng ý xóa trên hộp thoại modal.
   - `TriggerExecShell`: Yêu cầu mở phiên làm việc dòng lệnh vào container.
3. **Thông điệp Kết quả Bất đồng bộ từ Socket:**
   - `ContainersRefreshed(ContainerDataList)`: Nhận danh sách container mới nhất từ socket.
   - `OperationCompleted(ActionResult)`: Nhận kết quả thành công hoặc thất bại của một lệnh quản lý.

### 2.3. Quy tắc Chuyển đổi Trạng thái (The Update Rules)
- Hàm `Update` là một hàm toán học thuần khiết: Khi nhận một `Msg` và `Model`, nó trả về bản sao `Model` mới kèm theo danh sách lệnh bất đồng bộ (`Cmd`) cần gửi tới tầng Socket.
- Khi người dùng nhấn phím `s` hoặc `Space` trên một container đang `Running`:
  - Trạng thái container tạm thời chuyển sang cờ `Stopping` (giao diện hiển thị biểu tượng đồng hồ cát màu vàng).
  - Phát sinh lệnh bất đồng bộ gửi yêu cầu `POST /containers/{name}/stop` xuống Unix Socket.
  - Khi Socket phản hồi thành công, phát sinh thông điệp `ContainersRefreshed` để cập nhật lại giao diện về trạng thái `Exited`.

---

## 3. THIẾT KẾ BỐ CỤC GIAO DIỆN CO GIÃN ĐỘNG (RESPONSIVE LAYOUT)

### 3.1. Thuật toán Phân bổ Khung nhìn (Layout Computation Engine)
Giao diện không sử dụng bất kỳ hằng số kích thước cố định nào mà áp dụng công thức tính toán tương đối:
- **Thanh Top Bar:**
  - Vị trí: Tọa độ gốc `X = 0`, `Y = 0`.
  - Kích thước: Chiều rộng chiếm `100%` độ rộng terminal (`Dim.Fill()`), chiều cao cố định đúng `1 dòng`.
- **Panel Trái (Danh sách Thực thể):**
  - Vị trí: `X = 0`, `Y = 1`.
  - Kích thước: Chiều rộng tính bằng `40%` tổng số cột màn hình (`Dim.Percent(40)`). Chiều cao chiếm toàn bộ không gian còn lại trừ thanh Footer (`Dim.Fill() - 1`).
- **Panel Phải (Khung Chi tiết & Giám sát):**
  - Vị trí: Bắt đầu từ mép phải của Panel Trái (`Pos.Right(PanelTrai)`), `Y = 1`.
  - Kích thước: Chiều rộng chiếm toàn bộ phần còn lại (`Dim.Fill()`), chiều cao bằng chiều cao Panel Trái.
- **Thanh Footer Bar:**
  - Vị trí: Dòng cuối cùng của cửa sổ (`Pos.AnchorEnd(1)`), `X = 0`.
  - Kích thước: Chiều rộng `100%`, chiều cao `1 dòng`.

### 3.2. Quy tắc Thích ứng theo Kích thước Cửa sổ (Breakpoints)
- **Chế độ Màn hình Rộng (Bề ngang >= 100 cột):**
  - Áp dụng bố cục song song tiêu chuẩn Master-Detail (40% - 60%).
  - Bảng danh sách bên trái hiển thị đầy đủ: Trạng thái, Tên container, Cổng kết nối (Ports), Tên Image.
- **Chế độ Màn hình Trung bình (80 đến 99 cột):**
  - Tự động ẩn cột Image và rút gọn cột Ports để nhường chỗ cho Tên container.
- **Chế độ Màn hình Thu nhỏ (Dưới 80 cột hoặc chiều cao dưới 24 dòng):**
  - Tự động chuyển đổi sang **Chế độ Xếp chồng (Stacked / Single View Mode)**: Chỉ hiển thị duy nhất 1 Panel toàn màn hình tại một thời điểm. Người dùng nhấn phím `Tab` để luân chuyển qua lại giữa màn hình Danh sách và màn hình Chi tiết.

---

## 4. THIẾT KẾ CƠ CHẾ BỌC TIẾN TRÌNH INTERACTIVE SHELL (EXEC SHELL WRAPPER)

Khi người dùng nhấn phím `e` để mở Shell vào container:
1. **Bước 1 - Tạm dừng TUI (Suspend Mode):**
   - Ứng dụng gửi lệnh tới `Application.Driver.Suspend()` của `Terminal.Gui`.
   - Lưu trữ trạng thái cấu hình terminal (`termios`) gốc của máy host.
   - Đưa con trỏ chuột và chế độ console về Normal Mode (cho phép echo ký tự và enter xuống dòng bình thường).
2. **Bước 2 - Khởi tạo Tiến trình Con (Spawn Process):**
   - Khởi tạo tiến trình `podman exec -it <container_name> /bin/sh`.
   - Nếu `/bin/sh` không tồn tại, thử lại với `/bin/bash`.
   - Gắn kết trực tiếp luồng `stdin`, `stdout`, `stderr` của tiến trình con với terminal hiện tại của máy host.
3. **Bước 3 - Chờ Đợi & Khôi phục An toàn (Finally & Resume):**
   - Đặt quy trình trong khối bảo vệ an toàn tối cao (`try ... finally`).
   - Khi tiến trình con kết thúc (dù kết thúc bình thường hay do lỗi):
     - Khôi phục lại trạng thái `termios` gốc.
     - Khởi động lại vòng lặp `Application.Driver.Resume()`.
     - Kích hoạt vẽ lại toàn bộ màn hình Dashboard (Redraw Screen Buffer).

---

## 5. KẾT LUẬN & ĐIỀU KIỆN NGHIỆM THU MILESTONE 2
Bản thiết kế này là cơ sở duy nhất để triển khai mã nguồn Milestone 2. Khi bước vào pha code, toàn bộ logic điều phối và giao diện phải tuân thủ nghiêm ngặt các quy tắc phân bổ tỷ lệ và quản lý trạng thái đã nêu.
