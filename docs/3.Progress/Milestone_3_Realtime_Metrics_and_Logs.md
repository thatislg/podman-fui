# LỘ TRÌNH CHI TIẾT: MILESTONE 3 - GIÁM SÁT TRỰC QUAN HÓA REALTIME & STREAM LOGS
## DỰ ÁN: PODMAN-FUI (F# TERMINAL USER INTERFACE FOR PODMAN)

- **Mã cột mốc:** M3-REALTIME-MONITORING
- **Vị trí lưu trữ:** `docs/3.Progress/Milestone_3_Realtime_Metrics_and_Logs.md`
- **Trạng thái:** **Chờ thực hiện (Scheduled)**
- **Tài liệu kỹ thuật liên kết:**
  - [**`01_Podman_Socket_and_API_Investigation.md`**](../1.Investigation/01_Podman_Socket_and_API_Investigation.md)
  - [**`02_UI_Framework_and_Rendering_Investigation.md`**](../1.Investigation/02_UI_Framework_and_Rendering_Investigation.md)
  - [**`07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md`**](../1.Investigation/07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md)
  - [**`08_DeepDive_Event_Driven_Socket_and_Zero_Allocation.md`**](../1.Investigation/08_DeepDive_Event_Driven_Socket_and_Zero_Allocation.md)

---

## 1. MỤC TIÊU CỘT MỐC 3
Hiện thực hóa trải nghiệm giám sát trực quan sống động của LazyDocker trên nền tảng Podman: Stream log thời gian thực có lọc mã thoát ANSI, vẽ đồ thị tài nguyên Sparklines Unicode và thanh đo Gauge bằng Spectre.Console, đồng thời tối ưu hóa bộ nhớ và kiến trúc hướng sự kiện để giảm tải CPU về mức tối thiểu.

---

## 2. DANH MỤC CÔNG VIỆC CHI TIẾT (TASK CHECKLIST)

### 2.1. Bộ lọc & Stream Log Thời gian thực (Realtime Log Viewer)
- [ ] Xây dựng tác vụ đọc Stream Log bất đồng bộ từ endpoint `GET /v4.0.0/libpod/containers/{name}/logs?follow=true`.
- [ ] Phân tích khung dữ liệu phân kênh 8-byte header: Phân biệt luồng `stdout` (hiển thị màu thường) và `stderr` (hiển thị màu cảnh báo).
- [ ] Triển khai bộ lọc chuỗi thoát ANSI (Log Sanitizer theo `INV-07`): Giữ lại mã màu sắc SGR, loại bỏ toàn bộ mã điều khiển con trỏ và xóa màn hình (`\x1b[2J`, `\x1b[H`).
- [ ] Tích hợp tính năng Tự động cuộn (Auto-scroll), cơ chế phát hiện người dùng cuộn ngược chuột để tạm dừng auto-scroll.
- [ ] Thanh tìm kiếm và lọc dòng log trực tiếp theo từ khóa (`/`).

### 2.2. Đồ thị Tài nguyên Sparklines & Gauge (Live Resource Stats)
- [ ] Kết nối stream số liệu cgroup từ endpoint `GET /v4.0.0/libpod/containers/{name}/stats?stream=true`.
- [ ] Tính toán phần trăm CPU và Memory theo công thức biến thiên thời gian thực (đặc tả tại `INV-01`).
- [ ] Lưu trữ chuỗi giá trị vào bộ đệm vòng tròn (Ring Buffer) 30 điểm đo.
- [ ] Sử dụng Spectre.Console để render biểu đồ Sparklines Unicode (` ▂▃▄▅▆▇█`) cho CPU và RAM.
- [ ] Render thanh đo Gauge màu sắc (Xanh: < 60%, Vàng: 60-85%, Đỏ: > 85%) và số liệu Network I/O, Block I/O.

### 2.3. Tối ưu hóa Hiệu năng & Bộ nhớ (Zero-Allocation & Event-Driven)
- [ ] Áp dụng `ArrayPool` và cấu trúc `Span`/`ReadOnlyMemory` để tái sử dụng mảng byte đệm, triệt tiêu áp lực Garbage Collection (GC) khi stream log lớn (theo `INV-08`).
- [ ] Tích hợp kết nối lắng nghe luồng sự kiện `GET /v4.0.0/libpod/events?stream=true` để tự động cập nhật danh sách container khi có biến động mà không cần polling liên tục.

### 2.4. Xem Thông tin Cấu hình Chi tiết (Inspect Viewer)
- [ ] Gọi API `GET /v4.0.0/libpod/containers/{name}/json`.
- [ ] Tích hợp tab `Inspect` với tính năng tô màu cú pháp JSON (Syntax Highlighting) và tìm kiếm chuỗi cấu hình.

---

## 3. TIÊU CHÍ HOÀN THÀNH (DEFINITION OF DONE - DOD)

1. Khi chọn một container bất kỳ, các dòng log mới nhất tự động xuất hiện tức thì trên Panel bên phải với định dạng màu sắc đẹp mắt mà không làm vỡ khung viền TUI.
2. Đồ thị Sparklines của CPU và RAM cập nhật mượt mà theo chu kỳ, phản ánh đúng tải thực tế của container.
3. Khi hệ thống ở trạng thái nhàn rỗi (idle), mức chiếm dụng CPU của `podman-fui` duy trì ổn định dưới **0.5%**.
4. RAM duy trì ổn định dưới **45MB** trong điều kiện sử dụng thông thường.
