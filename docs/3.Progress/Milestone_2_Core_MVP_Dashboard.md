# LỘ TRÌNH CHI TIẾT: MILESTONE 2 - XÂY DỰNG KIẾN TRÚC MVU & QUẢN LÝ CONTAINER (CORE MVP)
## DỰ ÁN: PODMAN-FUI (F# TERMINAL USER INTERFACE FOR PODMAN)

- **Mã cột mốc:** M2-CORE-MVP
- **Vị trí lưu trữ:** `docs/3.Progress/Milestone_2_Core_MVP_Dashboard.md`
- **Trạng thái:** **Chờ thực hiện (Next Up)**
- **Bản thiết kế kỹ thuật thực thi (Design Specification):**
  - [**`Design_M2_Core_MVP_Dashboard.md`**](../2.Design/Design_M2_Core_MVP_Dashboard.md) *(Dự kiến soạn thảo trước khi bắt đầu M2)*
- **Tài liệu điều tra liên kết (Investigation References):**
  - [**`02_UI_Framework_and_Rendering_Investigation.md`**](../1.Investigation/02_UI_Framework_and_Rendering_Investigation.md)
  - [**`03_FSharp_Architecture_and_State_Management_Investigation.md`**](../1.Investigation/03_FSharp_Architecture_and_State_Management_Investigation.md)
  - [**`07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md`**](../1.Investigation/07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md)

---

## 1. MỤC TIÊU CỘT MỐC 2
Hiện thực hóa bộ khung kiến trúc luồng dữ liệu một chiều MVU (Model-View-Update) bằng F#, dựng bố cục giao diện Dashboard đa bảng co giãn động (Responsive Auto-scaling), và hoàn thành các chức năng quản lý cốt lõi cho Container (liệt kê danh sách, thao tác Start/Stop/Restart/Pause/Delete, và mở Interactive Shell).

---

## 2. DANH MỤC CÔNG VIỆC CHI TIẾT (TASK CHECKLIST)

### 2.1. Thiết kế Kiến trúc Mã nguồn (Thư mục `docs/2.Design/`)
- [ ] Soạn thảo tài liệu thiết kế phân rã module hệ thống (Domain, Infrastructure, Presentation, State).
- [ ] Thiết kế chi tiết sơ đồ luồng dữ liệu MVU và các định nghĩa kiểu dữ liệu bất biến (Model Types & Discriminated Unions).

### 2.2. Xây dựng Khung Điều khiển MVU (Elmish Loop)
- [ ] Xây dựng vòng lặp MVU thuần F#:
  - Hàm `init`: Khởi tạo trạng thái ban đầu.
  - Hàm `update`: Xử lý thông điệp (`Msg`) và chuyển đổi trạng thái (`Model`).
  - Hàm `view`: Render trạng thái ra các component giao diện.
- [ ] Tích hợp cơ chế đồng bộ luồng giao diện thông qua `Application.Invoke`.

### 2.3. Dựng Bố cục Dashboard Co giãn Động (Responsive Layout)
- [ ] Tạo Top Bar: Hiển thị 5 danh mục chính ([1] Pods, [2] Containers, [3] Images, [4] Volumes, [5] Networks).
- [ ] Tạo Split-View 2 Panel theo tỷ lệ phần trăm:
  - Panel bên trái: Danh sách thực thể (chiếm 40% chiều rộng).
  - Panel bên phải: Khung chi tiết và giám sát (chiếm 60% chiều rộng).
- [ ] Tạo Footer Bar: Hiển thị hướng dẫn phím tắt ngữ cảnh (Help, Start/Stop, Delete, Restart, Filter, Menu, Quit).
- [ ] Bắt sự kiện resize `SIGWINCH`, tính toán lại tỷ lệ layout và chuyển đổi chế độ Stacked Mode khi màn hình hẹp (< 80 cột).

### 2.4. Tính năng Quản lý Container Cốt lõi
- [ ] Triển khai gọi Libpod API `GET /v4.0.0/libpod/containers/json?all=true`.
- [ ] Hiển thị danh sách container trên Panel trái với màu sắc trạng thái (Xanh lá: Running, Xám: Exited, Vàng: Paused).
- [ ] Thao tác tức thời:
  - Phím `Space` hoặc `s`: Start/Stop container.
  - Phím `r`: Khởi động lại container (Restart).
  - Phím `p`: Tạm dừng / Tiếp tục container (Pause / Unpause).
  - Phím `d`: Mở modal xác nhận an toàn trước khi xóa container.
- [ ] Tính năng Interactive Shell (`e`): Tạm dừng TUI an toàn, chuyển quyền terminal sang `/bin/sh` hoặc `/bin/bash` và khôi phục dashboard nguyên vẹn khi thoát (theo thiết kế `INV-07`).

---

## 3. TIÊU CHÍ HOÀN THÀNH (DEFINITION OF DONE - DOD)

1. Dashboard hiển thị mượt mà trên terminal, tự động co giãn tỷ lệ khi resize cửa sổ mà không bị tràn viền hay vỡ chữ.
2. Danh sách container được cập nhật chính xác từ Podman Socket.
3. Người dùng có thể Start, Stop, Restart, Xóa container bằng phím tắt một chạm.
4. Nhấn phím `e` mở được shell trực tiếp vào container, khi gõ `exit` quay trở lại dashboard bình thường mà không bị lỗi con trỏ chuột hay mất hiển thị terminal.
