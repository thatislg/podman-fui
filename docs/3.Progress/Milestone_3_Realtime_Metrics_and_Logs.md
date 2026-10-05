# LỘ TRÌNH CHI TIẾT: MILESTONE 3 - GIÁM SÁT TRỰC QUAN HÓA REALTIME & STREAM LOGS
## DỰ ÁN: PODMAN-FUI (F# TERMINAL USER INTERFACE FOR PODMAN)

- **Mã cột mốc:** M3-REALTIME-MONITORING
- **Vị trí lưu trữ:** `docs/3.Progress/Milestone_3_Realtime_Metrics_and_Logs.md`
- **Trạng thái:** **Chờ thực hiện (Scheduled)**
- **Bản thiết kế kỹ thuật thực thi (Design Specification):**
  - [**`Design_M3_Realtime_Metrics_and_Logs.md`**](../2.Design/Design_M3_Realtime_Metrics_and_Logs.md) *(Bản thiết kế chi tiết DD-M3-REALTIME-MONITORING đã phê duyệt)*
- **Tài liệu điều tra liên kết (Investigation References):**
  - [**`01_Podman_Socket_and_API_Investigation.md`**](../1.Investigation/01_Podman_Socket_and_API_Investigation.md)
  - [**`02_UI_Framework_and_Rendering_Investigation.md`**](../1.Investigation/02_UI_Framework_and_Rendering_Investigation.md)
  - [**`07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md`**](../1.Investigation/07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md)
  - [**`08_DeepDive_Event_Driven_Socket_and_Zero_Allocation.md`**](../1.Investigation/08_DeepDive_Event_Driven_Socket_and_Zero_Allocation.md)

---

## 1. MỤC TIÊU CỘT MỐC 3
Hiện thực hóa trải nghiệm giám sát trực quan sống động của LazyDocker trên nền tảng Podman: Stream log thời gian thực có lọc mã thoát ANSI độc hại, vẽ đồ thị tài nguyên Sparklines Unicode 8 mức và thanh đo Gauge bằng Spectre.Console, đồng thời tối ưu hóa bộ nhớ đệm vòng tròn không cấp phát (Zero-Allocation Ring Buffer) để duy trì ứng dụng siêu nhẹ dưới 45MB RAM.

---

## 2. DANH MỤC CÔNG VIỆC CHI TIẾT (TASK CHECKLIST)

### 2.1. Thiết kế Chi tiết & Nghiên cứu Kỹ thuật
*[Căn cứ thiết kế: `Design_M3_Realtime_Metrics_and_Logs.md` - Toàn văn bản vẽ DD-M3-REALTIME-MONITORING]*
- [x] Soạn thảo tài liệu thiết kế chi tiết phân rã mô-đun streaming, khử độc ANSI và tối ưu bộ nhớ.
- [x] Đặc tả chi tiết Hợp đồng dữ liệu `LogEntry`, `MetricSample`, thuật toán giải mã khung 8 bytes và máy trạng thái FSM.

### 2.2. Hiện thực hóa Tầng Domain (Mô hình Dữ liệu Nhật ký & Chỉ số)
*[Căn cứ thiết kế: `Design_M3_Realtime_Metrics_and_Logs.md` - Mục 2.1: Phân Rã Domain & Mục 3: Đặc Tả Hợp Đồng Dữ Liệu]*
- [ ] **Subtask 2.2.1 - Thực thể miền Nhật ký (`LogModels.fs`):**
  - Định nghĩa Enum `StreamChannel` (`StdIn = 0`, `StdOut = 1`, `StdErr = 2`, `SystemAlert = 3`).
  - Định nghĩa Record `ColoredSpan` chứa văn bản kèm màu chữ và nền.
  - Định nghĩa Record `LogEntry` (đầy đủ 6 trường: `SequenceId`, `Channel`, `Timestamp`, `RawText`, `SanitizedSpans`, `IsOverwritten`) theo Mục 3.1.
- [ ] **Subtask 2.2.2 - Thực thể miền Đo lường Tài nguyên (`MetricModels.fs`):**
  - Định nghĩa Record `MetricSample` (10 trường số liệu: `Timestamp`, `CpuPercent`, `MemoryUsageBytes`, `MemoryLimitBytes`, `MemoryPercent`, `NetworkInputBytes`, `NetworkOutputBytes`, `BlockInputBytes`, `BlockOutputBytes`, `PidsCount`) theo Mục 3.2.
  - Định nghĩa Record `MetricTimeSeries` lưu trữ lịch sử 30 mẫu đo gần nhất cho CPU và RAM theo Mục 3.3.
  - Định nghĩa Record `SparklineConfig` thiết lập dải màu và độ dài chuỗi ký tự.
- [ ] **Subtask 2.2.3 - Giao diện Bộ đệm Vòng tròn trừu tượng (`IRingBuffer.fs`):**
  - Định nghĩa interface `IRingBuffer<'T>` với các phương thức `Push: 'T -> unit`, `GetSnapshot: unit -> 'T list`, `Clear: unit -> unit`, `Capacity: int`, `Count: int`.

### 2.3. Hiện thực hóa Tầng Hạ tầng Infrastructure (Streaming & Parsing)
*[Căn cứ thiết kế: `Design_M3_Realtime_Metrics_and_Logs.md` - Mục 2.2, Mục 4.1, 4.2, 4.3, 4.5]*
- [ ] **Subtask 2.3.1 - Quản lý Bộ nhớ Đệm Tái sử dụng (`ArrayPoolMemoryManager.fs`):**
  - Hiện thực hàm mượn và hoàn trả mảng byte qua `System.Buffers.ArrayPool<byte>.Shared` theo Mục 4.5, loại bỏ cấp phát trên Heap.
- [ ] **Subtask 2.3.2 - Bộ phân tách Khung truyền Đa kênh (`MultiplexedFrameDecoder.fs`):**
  - Hiện thực thuật toán đọc khung nhị phân tuần tự theo Mục 4.1: Đọc chính xác 8 bytes header (Byte 0: Channel, Bytes 4..7: Big-Endian Length), kiểm tra giới hạn an toàn payload (< 10MB), đọc thân payload và chuyển đổi UTF-8.
- [ ] **Subtask 2.3.3 - Máy trạng thái Khử độc Ký tự Thoát ANSI (`AnsiSanitizer.fs`):**
  - Hiện thực Máy trạng thái hữu hạn 4 bước theo Mục 4.2 (`Normal` -> `EscapePending` -> `CsiParam` -> Phân loại SGR color `m` vs Cursor control `H/f/J/K`).
  - Xử lý ký tự về đầu dòng `\r` thành thao tác cập nhật đè dòng hiện tại (phục vụ thanh download của tiến trình container).
- [ ] **Subtask 2.3.4 - Bộ kết nối Luồng Nhật ký Container (`LogsStreamClient.fs`):**
  - Gửi yêu cầu HTTP Stream tới `/v4.0.0/libpod/containers/{name}/logs?follow=true&stdout=true&stderr=true&tail=100`.
  - Quản lý hủy luồng an toàn qua `CancellationToken` khi người dùng chuyển sang container khác.
- [ ] **Subtask 2.3.5 - Bộ kết nối Luồng Chỉ số Tài nguyên (`StatsStreamClient.fs`):**
  - Gửi yêu cầu HTTP Stream tới `/v4.0.0/libpod/containers/{name}/stats?stream=true`.
  - Hiện thực thuật toán tính biến thiên CPU (`deltaCpu / deltaSystem * onlineCpus * 100`) và phần trăm RAM thực tế (trừ cache inactive file) theo Mục 4.3.

### 2.4. Hiện thực hóa Tầng Presentation (Trực quan hóa Dữ liệu)
*[Căn cứ thiết kế: `Design_M3_Realtime_Metrics_and_Logs.md` - Mục 2.3, Mục 4.4, 4.6 & Mục 6: Đặc Tả Bố Cục]*
- [ ] **Subtask 2.4.1 - Khung nhìn Nhật ký Trực tiếp (`Views/LiveLogView.fs`):**
  - Render danh sách dòng log trong Tab `[Logs]`; phân biệt màu Stdout (Trắng/Xám) và Stderr (Đỏ/Vàng có tiền tố `[ERR]`).
  - Hiện thực thuật toán Cuộn thông minh (Smart Auto-Scroll) theo Mục 4.6: Tự động cuộn bám đuôi (`FollowTail = true`); tự động dừng cuộn khi người dùng lăn chuột ngược hoặc bấm phím lên (`k`/`Up`); tự động khôi phục bám đuôi khi cuộn chạm đáy.
  - Tích hợp thanh tìm kiếm và lọc dòng log trực tiếp theo từ khóa (`/`).
- [ ] **Subtask 2.4.2 - Bộ vẽ Biểu đồ Sparklines Unicode (`Views/SparklineView.fs`):**
  - Hiện thực thuật toán ánh xạ 8 mức ký tự khối Unicode (` ▂▃▄▅▆▇█`) theo Mục 4.4.
  - Chuẩn hóa 30 giá trị đo gần nhất về dải 0..7; tô màu biến thiên gradient: Xanh lá (<60%), Vàng cam (60-85%), Đỏ rực (>85%).
- [ ] **Subtask 2.4.3 - Bộ vẽ Thanh đo Gauge & Bảng Thống kê I/O (`Views/GaugeView.fs`, `Views/StatsChartView.fs`):**
  - Render thanh đo phần trăm RAM và CPU dạng thanh ngang co giãn theo độ rộng cửa sổ.
  - Render bảng số liệu Network I/O (RX/TX), Disk Block I/O (Read/Write) và số luồng PIDs theo Mục 6.1.
- [ ] **Subtask 2.4.4 - Khung nhìn Cấu hình Inspect (`Views/ContainerInspectView.fs`):**
  - Gọi API `GET /v4.0.0/libpod/containers/{name}/json`, hiển thị JSON có thụt lề và phân tách màu cú pháp.

### 2.5. Kiểm thử Nghiệm thu Kỹ thuật (Technical Acceptance Testing)
*[Căn cứ thiết kế: `Design_M3_Realtime_Metrics_and_Logs.md` - Mục 7: Ma Trận Ca Kiểm Thử Nghiệm Thu]*
- [ ] **Subtask 2.5.1 - Thực thi kiểm thử ca TC-M3-01 (Bóc tách Stdout và Stderr):**
  - Mở tab Logs của container phát cả 2 kênh; xác nhận Stdout có màu trắng/xám và Stderr có nhãn `[ERR]` màu đỏ.
- [ ] **Subtask 2.5.2 - Thực thi kiểm thử ca TC-M3-02 (Khử độc chuỗi ANSI):**
  - Container xuất mã xóa màn hình `\x1b[2J`; xác nhận giao diện TUI không bị chớp giật hay xóa trắng.
- [ ] **Subtask 2.5.3 - Thực thi kiểm thử ca TC-M3-03 (Smart Auto-Scroll):**
  - Khi log đang in liên tục: cuộn ngược lên 5 dòng -> màn hình đứng yên; cuộn xuống đáy -> màn hình tự bám đuôi dòng mới nhất.
- [ ] **Subtask 2.5.4 - Thực thi kiểm thử ca TC-M3-04 (Trực quan hóa Sparklines):**
  - Container tăng dần tải CPU; xác nhận đồ thị Sparklines tăng dần độ cao từ ` ` đến `█` và chuyển màu chuẩn xác.
- [ ] **Subtask 2.5.5 - Thực thi kiểm thử ca TC-M3-05 (Zero-Allocation & Giới hạn RAM):**
  - Container xuất hơn 100.000 dòng log; kiểm tra RAM của tiến trình `podman-fui` duy trì ổn định dưới 45MB, không bị rò rỉ bộ nhớ.

---

## 3. TIÊU CHÍ HOÀN THÀNH (DEFINITION OF DONE - DOD)

1. Khi chọn một container bất kỳ, các dòng log mới nhất tự động xuất hiện tức thì trên Panel bên phải với định dạng màu sắc chuẩn xác mà không làm vỡ khung viền TUI.
2. Đồ thị Sparklines của CPU và RAM cập nhật mượt mà theo chu kỳ thời gian thực, phản ánh đúng tải thực tế của container.
3. Khi hệ thống ở trạng thái nhàn rỗi (idle), mức chiếm dụng CPU của `podman-fui` duy trì ổn định dưới **0.5%**.
4. RAM duy trì ổn định dưới **45MB** trong điều kiện sử dụng thông thường nhờ bộ đệm vòng tròn không cấp phát.
5. Vượt qua toàn bộ các ca kiểm thử từ TC-M3-01 đến TC-M3-05.
