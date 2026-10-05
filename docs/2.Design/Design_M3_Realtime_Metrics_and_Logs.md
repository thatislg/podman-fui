# BẢN THIẾT KẾ KỸ THUẬT BẰNG LỜI: MILESTONE 3
## THIẾT KẾ GIÁM SÁT TRỰC QUAN HÓA THỜI GIAN THỰC & STREAM LOGS
- **Mã tài liệu:** DES-M3-REALTIME-MONITORING
- **Vị trí lưu trữ:** `docs/2.Design/Design_M3_Realtime_Metrics_and_Logs.md`
- **Phiên bản:** 1.0.0
- **Trạng thái:** Bản thiết kế đề xuất (Draft)
- **Tài liệu căn cứ:** 
  - [`SRS_podman-FUI.md`](../1.Investigation/SRS_podman-FUI.md)
  - [`01_Podman_Socket_and_API_Investigation.md`](../1.Investigation/01_Podman_Socket_and_API_Investigation.md)
  - [`07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md`](../1.Investigation/07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md)
  - [`08_DeepDive_Event_Driven_Socket_and_Zero_Allocation.md`](../1.Investigation/08_DeepDive_Event_Driven_Socket_and_Zero_Allocation.md)
- **Tài liệu tiến độ gắn kèm:** [`Milestone_3_Realtime_Metrics_and_Logs.md`](../3.Progress/Milestone_3_Realtime_Metrics_and_Logs.md)

---

## 1. MỤC TIÊU THIẾT KẾ
Tài liệu này đặc tả chi tiết bằng lời các giải pháp kỹ thuật phục vụ việc hiển thị dữ liệu sống động thời gian thực:
1. Thiết kế bộ phân giải luồng phân kênh (Multiplexed Stream) và máy trạng thái lọc chuỗi thoát ANSI cho Log container.
2. Thiết kế thuật toán tính toán và ánh xạ chỉ số tài nguyên thành biểu đồ Sparklines Unicode và thanh đo Gauge.
3. Thiết kế cấu trúc dữ liệu bộ đệm vòng tròn (Ring Buffer) và kỹ thuật quản lý bộ nhớ tái sử dụng (Zero-Allocation).

---

## 2. THIẾT KẾ KIẾN TRÚC STREAM LOG CONTAINER

### 2.1. Bộ Phân giải Luồng Phân kênh (Multiplexed Stream Frame Parser)
Khi gọi API stream log không có TTY (`/libpod/containers/{name}/logs?follow=true`), Podman gửi dữ liệu dưới dạng luồng phân kênh (Multiplexed Stream):
- **Cấu trúc khung dữ liệu (Frame):**
  - **8 Bytes Phần Đầu (Header):**
    - Byte 0: Chỉ định kênh luồng (`1` là luồng xuất chuẩn `stdout`, `2` là luồng lỗi `stderr`, `0` là luồng nhập `stdin`).
    - Bytes 1 đến 3: Dành riêng cho hệ thống (Reserved).
    - Bytes 4 đến 7: Kích thước dữ liệu nhị phân của khối payload tiếp theo, mã hóa dưới dạng số nguyên 32-bit (Big-Endian).
  - **Khối Dữ liệu (Payload):** Chuỗi byte văn bản UTF-8 có độ dài chính xác bằng kích thước đã đọc từ header.
- **Quy trình xử lý tuần tự bằng lời:**
  - Bộ đọc socket thực hiện đọc liên tục đúng 8 bytes đầu tiên.
  - Chuyển đổi 4 byte kích thước thành một số nguyên chỉ số lượng byte dữ liệu.
  - Cấp phát vùng nhớ đọc tiếp đúng số byte đó.
  - Bóc tách chuỗi byte thành các dòng văn bản dựa trên ký tự phân tách xuống dòng.
  - Gắn nhãn màu sắc cho dòng log: Luồng `stdout` gán mã màu trắng/xám tiêu chuẩn, luồng `stderr` gán mã màu vàng/đỏ cảnh báo.

### 2.2. Máy Trạng thái Lọc Chuỗi Thoát ANSI (Log Sanitizer State Machine)
Để tránh tình trạng các mã điều khiển con trỏ từ container làm vỡ khung viền của Dashboard, luồng ký tự được đưa qua một Máy trạng thái hữu hạn (FSM) 4 trạng thái:
1. **Trạng thái Bình thường (Normal State):** Nhận ký tự thông thường và ghi thẳng vào dòng log hiển thị. Khi gặp ký tự thoát `Escape` (`\x1b`), chuyển sang Trạng thái Chờ Lệnh Thoát.
2. **Trạng thái Chờ Lệnh Thoát (Escape State):** Khi gặp ký tự mở ngoặc vuông `[`, chuyển sang Trạng thái Phân tích Tham số CSI (Control Sequence Introducer). Nếu gặp ký tự khác, bỏ qua và quay về Trạng thái Bình thường.
3. **Trạng thái Phân tích Tham số CSI:** Đọc liên tục các chữ số và dấu chấm phẩy đại diện cho tham số mã màu cho đến khi gặp ký tự lệnh kết thúc:
   - Nếu ký tự kết thúc là `m` (Lệnh định dạng màu sắc SGR): Giữ nguyên chuỗi mã màu này để chuyển cho bộ render màu sắc.
   - Nếu ký tự kết thúc là `H`, `f`, `J`, `K` (Các lệnh điều khiển con trỏ, xóa màn hình, xóa dòng): Lập tức hủy bỏ toàn bộ chuỗi này, không cho phép lọt vào bộ đệm hiển thị.
4. **Xử lý Ký tự Về Đầu Dòng (`\r`):** Thay vì cho phép con trỏ nhảy về đầu dòng đè lên chữ cũ, máy trạng thái chuyển đổi ký tự `\r` thành thao tác cập nhật đè nội dung dòng log hiện tại, giữ cho màn hình luôn gọn gàng.

### 2.3. Cơ chế Cuộn Log Thông minh (Smart Auto-Scroll Logic)
- **Chế độ Bám Đuôi (Follow Tail Mode - Mặc định):** Mỗi khi có dòng log mới được đẩy vào, thanh cuộn tự động gán vị trí hiển thị ở dòng dưới cùng.
- **Chế độ Đọc Lịch sử (History Mode):** Khi người dùng lăn con trỏ chuột ngược lên trên hoặc nhấn phím mũi tên lên, hệ thống tự động tắt cờ bám đuôi, giữ cố định khung nhìn tại vị trí người dùng đang đọc.
- **Tái kích hoạt:** Khi người dùng cuộn trở lại dòng cuối cùng, hệ thống tự động bật lại cờ bám đuôi.

---

## 3. THIẾT KẾ ĐỒ THỊ TÀI NGUYÊN THỜI GIAN THỰC (SPARKLINES & METRICS)

### 3.1. Thuật toán Tính toán Chỉ số CPU & Memory
Dữ liệu từ endpoint `/stats?stream=true` được xử lý định kỳ theo công thức:
- **Tỷ lệ phần trăm CPU:**
  - Lấy hiệu số thời gian CPU của container giữa mẫu hiện tại và mẫu liền kề trước.
  - Lấy hiệu số thời gian CPU của toàn bộ hệ thống giữa hai thời điểm đo.
  - Tỷ lệ CPU được tính bằng thương số của hai hiệu số trên, nhân với số nhân CPU của máy và nhân với 100.
- **Tỷ lệ phần trăm Memory:**
  - Lấy dung lượng bộ nhớ đang sử dụng chia cho giới hạn bộ nhớ tối đa của container và nhân với 100.

### 3.2. Thuật toán Ánh xạ Ký tự Khối Sparklines
Hệ thống sử dụng bộ 8 ký tự khối Unicode tăng dần độ cao: ` ` (Mức 1), `▂` (Mức 2), `▃` (Mức 3), `▄` (Mức 4), `▅` (Mức 5), `▆` (Mức 6), `▇` (Mức 7), `█` (Mức 8).
- **Quy tắc ánh xạ:**
  - Lấy giá trị nhỏ nhất (Min) và lớn nhất (Max) trong chuỗi 30 mẫu đo gần nhất của container.
  - Chia dải giá trị thành 8 khoảng đều nhau.
  - Mỗi mẫu đo được ánh xạ trực tiếp thành ký tự khối tương ứng với khoảng giá trị của nó.
  - Chuỗi ký tự được tô màu gradient: Xanh lá cây khi tải dưới 60%, Vàng cam khi tải từ 60% đến 85%, và Đỏ rực khi tải trên 85%.

---

## 4. THIẾT KẾ QUẢN LÝ BỘ NHỚ ZERO-ALLOCATION

Để loại bỏ hoàn toàn hiện tượng khựng giật giao diện do Garbage Collector thu gom rác:
1. **Bộ đệm Vòng tròn Cố định (Fixed-capacity Ring Buffer):**
   - Bộ đệm lưu trữ log có kích thước tối đa đúng 2000 dòng.
   - Khi đầy, con trỏ ghi tự động quay về vị trí đầu mảng và ghi đè phần tử cũ nhất mà không cần thực hiện thao tác xóa mảng hay dịch chuyển vị trí các phần tử.
2. **Tái sử dụng Mảng Byte Đệm (Memory Pooling):**
   - Mọi thao tác đọc khung dữ liệu từ Unix Socket đều mượn các khối mảng byte có sẵn từ bể nhớ dùng chung (`ArrayPool`), sử dụng xong hoàn trả lại ngay cho bể nhớ, không cấp phát bộ nhớ mới trên Heap.
