# ĐIỀU TRA KỸ THUẬT 07: QUẢN LÝ TERMINAL RAW MODE, PTY VÀ LỌC MÃ THOÁT ANSI
## DỰ ÁN: PODMAN-FUI

- **Mã tài liệu:** INV-07-TERMINAL-PTY-RAWMODE
- **Trạng thái:** Kế hoạch dự phòng / Chờ thực hiện (Scheduled)
- **Thời điểm tiến hành:** **Milestone 2** (khi triển khai tính năng Exec Shell) và **Milestone 3** (khi triển khai Realtime Log Streaming)

---

## 1. MỤC TIÊU ĐIỀU TRA
Tài liệu này xác định phương án kỹ thuật chuyên sâu nhằm giải quyết hai vấn đề nhạy cảm nhất về tương tác dòng lệnh ở tầng thấp (low-level terminal handling):
1. Đảm bảo trạng thái Terminal của máy host không bao giờ bị "treo/hỏng" (Corrupted Terminal State) khi người dùng mở phiên làm việc tương tác Shell (`podman exec -it`).
2. Khử toàn bộ các mã điều khiển con trỏ và xóa màn hình của luồng log container (VT100 escape sequences) trước khi nạp vào giao diện TUI, chống triệt để hiện tượng vỡ layout.

---

## 2. BỐI CẢNH & CÁC NGUY CƠ KỸ THUẬT CỐT LÕI

### 2.1. Nguy cơ Mất Trạng thái Terminal (Terminal Corruption)
- Khi ứng dụng TUI chạy, terminal được đưa về chế độ Raw Mode để bắt phím tức thì mà không cần nhấn Enter.
- Khi người dùng muốn nhảy vào container bằng phím `e` (`podman exec -it`):
  - Quyền điều khiển terminal được bàn giao cho tiến trình con.
  - Nếu tiến trình con bị ngắt đột ngột (do container bị crash, bị kill từ bên ngoài, hoặc do người dùng ngắt kết nối SSH): cấu hình cờ terminal (`termios`) có thể bị kẹt ở trạng thái dở dang (mất echo ký tự, enter không tạo dòng mới `\r\n`).

### 2.2. Nguy cơ Xé Rách Giao diện do Ký tự Điều khiển Log
- Các ứng dụng trong container thường xuất ra các ký tự điều khiển con trỏ:
  - Mã xóa toàn bộ màn hình: `\x1b[2J`
  - Mã đưa con trỏ về góc trái trên: `\x1b[H`
  - Mã nhảy con trỏ tới tọa độ tùy ý: `\x1b[{line};{col}H`
  - Ký tự lùi về đầu dòng để vẽ tiến trình: `\r`
- Nếu các chuỗi này lọt vào khung hiển thị Log của `Terminal.Gui`, chúng sẽ ra lệnh cho terminal host thực thi trực tiếp, đè nát toàn bộ các đường viền khung của dashboard.

---

## 3. DANH MỤC HẠNG MỤC CẦN ĐIỀU TRA CHI TIẾT

- [ ] **Khảo sát cơ chế bắt và khôi phục `termios` qua Linux Libc:**
  - Nghiên cứu cấu trúc dữ liệu `termios` của Linux (`tcgetattr`, `tcsetattr`, hằng số cờ `ECHO`, `ICANON`, `ISIG`).
  - Xây dựng mô hình khôi phục an toàn (Safe Restoration Pattern): Sao lưu `termios` gốc trước khi spawn tiến trình shell, và sử dụng khối bảo vệ an toàn để khôi phục cấu hình gốc ngay khi tiến trình con kết thúc.
- [ ] **Điều tra cơ chế xử lý tín hiệu hệ thống (Signal Handling):**
  - Chuyển tiếp tín hiệu `SIGWINCH` (thay đổi kích thước) cho phiên shell trong container.
  - Chặn hoặc phân bổ lại tín hiệu `SIGINT` (Ctrl+C) và `SIGTSTP` (Ctrl+Z) để không làm văng ứng dụng mẹ `podman-FUI`.
- [ ] **Đặc tả bộ lọc chuỗi thoát ANSI (Log Stream Sanitizer State Machine):**
  - Xây dựng thuật toán phân tích luồng ký tự (Stream Tokenizer) dựa trên máy trạng thái hữu hạn (Finite State Machine).
  - Phân loại mã thoát:
    - **Nhóm được giữ lại:** Mã định dạng màu sắc (SGR - Select Graphic Rendition: mã màu 30-37, 40-47, mã màu 256, mã màu TrueColor RGB).
    - **Nhóm bắt buộc phải triệt tiêu (Strip):** Toàn bộ mã điều khiển con trỏ, mã xóa màn hình, mã xóa dòng.
    - **Nhóm chuyển đổi:** Chuỗi `\r\n` và `\r` được chuẩn hóa thành ký tự xuống dòng an toàn cho bộ đệm văn bản.

---

## 4. TIÊU CHÍ NGHIỆM THU (ACCEPTANCE CRITERIA)

1. Khi người dùng gõ `exit` trong phiên shell, giao diện dashboard phải được vẽ lại nguyên vẹn trong vòng dưới 100ms.
2. Dù tiến trình `podman exec` có bị `kill -9` từ terminal khác, terminal của ứng dụng vẫn tự động phục hồi về trạng thái bình thường mà không cần gõ `stty sane`.
3. Khung Log hiển thị đầy đủ màu sắc của ứng dụng container nhưng không bị giật, không làm lệch viền khung của các panel xung quanh.
