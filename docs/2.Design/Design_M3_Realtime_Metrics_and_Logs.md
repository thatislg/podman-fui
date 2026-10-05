# BẢN THIẾT KẾ CHI TIẾT (DETAIL DESIGN): MILESTONE 3
## GIÁM SÁT TRỰC QUAN THỜI GIAN THỰC & STREAMING LOGS ĐA KÊNH

- **Mã tài liệu:** DD-M3-REALTIME-MONITORING
- **Vị trí lưu trữ:** `docs/2.Design/Design_M3_Realtime_Metrics_and_Logs.md`
- **Phiên bản:** 2.0.0 (Nâng cấp toàn diện từ Basic Design lên Detail Design)
- **Ngày phê duyệt:** 2026-10-05
- **Tài liệu căn cứ:** 
  - [`SRS_podman-FUI.md`](../1.Investigation/SRS_podman-FUI.md)
  - [`01_Podman_Socket_and_API_Investigation.md`](../1.Investigation/01_Podman_Socket_and_API_Investigation.md)
  - [`07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md`](../1.Investigation/07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md)
  - [`08_DeepDive_Event_Driven_Socket_and_Zero_Allocation.md`](../1.Investigation/08_DeepDive_Event_Driven_Socket_and_Zero_Allocation.md)
- **Tài liệu tiến độ gắn kèm:** [`Milestone_3_Realtime_Metrics_and_Logs.md`](../3.Progress/Milestone_3_Realtime_Metrics_and_Logs.md)

---

## 1. TỔNG QUAN & PHẠM VI THIẾT KẾ CHI TIẾT

Tài liệu này đặc tả chi tiết kiến trúc tầng thấp cho phân hệ streaming thời gian thực trong Milestone 3:
- Bộ phân tách khung truyền nhị phân đa kênh (Multiplexed Frame Stream Decoder) cho luồng Logs từ Podman Engine.
- Máy trạng thái hữu hạn 4 bước (4-State FSM) khử độc ký tự thoát ANSI độc hại, bảo vệ cấu trúc layout TUI.
- Thuật toán tính toán chỉ số tài nguyên (CPU, Memory, Network I/O, Block I/O) và ánh xạ đồ thị Unicode Sparklines 8 mức.
- Bộ đệm vòng tròn cố định (Ring Buffer) kết hợp kỹ thuật tái sử dụng bộ nhớ đệm (Zero-Allocation Buffer Pooling).
- Cơ chế cuộn thông minh (Smart Auto-Scroll) tự động bám đuôi dòng dữ liệu mới và ma trận kiểm thử nghiệm thu.
- **Tuân thủ tuyệt đối:** Trình bày hoàn toàn bằng lời văn, bảng biểu, quy trình thuật toán tuần tự, không sử dụng code sample (Zero Code Sample).

---

## 2. PHÂN RÃ DANH MỤC TỆP & MÔ-ĐUN MÃ NGUỒN (MODULE INVENTORY)

Milestone 3 mở rộng các dự án thành phần với các tệp mã nguồn chuyên biệt:

### 2.1. Dự án `PodmanFUI.Domain`
- **Tệp: `LogModels.fs` (Thực thể miền Nhật ký):**
  - Chứa kiểu phân loại kênh luồng: `StreamChannel` (`StdIn = 0`, `StdOut = 1`, `StdErr = 2`, `SystemAlert = 3`).
  - Chứa bản ghi dòng log: `LogEntry`.
  - Chứa kiểu phân đoạn màu sắc: `ColoredSpan`.
- **Tệp: `MetricModels.fs` (Thực thể miền Đo lường & Chỉ số):**
  - Chứa bản ghi mẫu đo lường: `MetricSample`.
  - Chứa bản ghi chuỗi lịch sử đo: `MetricTimeSeries`.
  - Chứa kiểu cấu hình đồ thị: `SparklineConfig`.
- **Tệp: `IRingBuffer.fs` (Giao diện cấu trúc dữ liệu vòng):**
  - Định nghĩa hợp đồng interface `IRingBuffer<'T>` với các thao tác `Push`, `GetSnapshot`, `Clear`, `Capacity`, `Count`.

### 2.2. Dự án `PodmanFUI.Infrastructure`
- **Tệp: `MultiplexedFrameDecoder.fs` (Giải mã khung nhị phân Libpod):**
  - Thực thi đọc chính xác 8 bytes header: phân tách mã kênh (Channel Type) và kích thước khối dữ liệu (Big-Endian Length).
  - Đọc luồng dữ liệu liên tục không phong bế (Non-blocking async stream).
- **Tệp: `AnsiSanitizer.fs` (Bộ lọc mã thoát ANSI & Chuẩn hóa văn bản):**
  - Hiện thực máy trạng thái FSM 4 trạng thái để bóc tách mã màu SGR và triệt tiêu mã điều khiển con trỏ.
- **Tệp: `ArrayPoolMemoryManager.fs` (Quản lý bộ nhớ đệm mượn trả):**
  - Bọc cơ chế mượn và hoàn trả bộ nhớ đệm byte thông qua `System.Buffers.ArrayPool<byte>.Shared`.
- **Tệp: `LogsStreamClient.fs` (Bộ kết nối luồng Log bất đồng bộ):**
  - Quản lý vòng đời yêu cầu HTTP Stream tới `/v4.0.0/libpod/containers/{name}/logs?follow=true&stdout=true&stderr=true&tail=100`.
- **Tệp: `StatsStreamClient.fs` (Bộ kết nối luồng Chỉ số thời gian thực):**
  - Quản lý vòng đời yêu cầu HTTP Stream tới `/v4.0.0/libpod/containers/{name}/stats?stream=true`.

### 2.3. Dự án `PodmanFUI.Presentation`
- **Tệp: `Views/LiveLogView.fs` (Giao diện hiển thị Log trực tiếp):**
  - Điều phối hiển thị danh sách dòng log, tô màu theo kênh (Stdout màu trắng/xám, Stderr màu vàng/đỏ).
  - Quản lý cờ trạng thái cuộn bám đuôi (Auto-scroll Tail Mode).
- **Tệp: `Views/SparklineView.fs` (Bộ vẽ đồ thị Sparkline Unicode):**
  - Ánh xạ mảng 30 điểm đo thành chuỗi 30 ký tự khối Unicode kèm mã màu ANSI biến thiên gradient.
- **Tệp: `Views/GaugeView.fs` (Bộ vẽ thanh đo phần trăm dạng thanh ngang):**
  - Vẽ thanh đo dung lượng RAM và phần trăm CPU với tỷ lệ co giãn theo bề rộng cửa sổ.

---

## 3. ĐẶC TẢ CHI TIẾT HỢP ĐỒNG DỮ LIỆU (DATA CONTRACTS SPECIFICATION)

### 3.1. Hợp đồng `LogEntry` (Thực thể Dòng Nhật ký)

| Tên trường | Kiểu dữ liệu | Bắt buộc | Mô tả & Quy tắc kiểm tra |
| :--- | :--- | :---: | :--- |
| `SequenceId` | `int64` | Có | Số thứ tự tăng dần đơn điệu của dòng log trong phiên xem hiện tại. |
| `Channel` | `StreamChannel` | Có | Định danh kênh: `StdOut` (kênh 1), `StdErr` (kênh 2), hoặc `SystemAlert`. |
| `Timestamp` | `DateTimeOffset option` | Không | Thời điểm log phát sinh nếu bật cờ gắn mốc thời gian. |
| `RawText` | `string` | Có | Nội dung văn bản nguyên bản đã qua bóc tách xuống dòng. |
| `SanitizedSpans`| `ColoredSpan list` | Có | Danh sách các phân đoạn văn bản kèm màu sắc đã được lọc bỏ mã độc ANSI. |
| `IsOverwritten`| `bool` | Có | Cờ đánh dấu nếu dòng này bị ghi đè bởi ký tự về đầu dòng `\r` (như thanh tiến trình tải). |

### 3.2. Hợp đồng `MetricSample` (Thực thể Mẫu Đo Lường)

| Tên trường | Kiểu dữ liệu | Ánh xạ JSON Libpod Stats | Bắt buộc | Mô tả công thức tính |
| :--- | :--- | :--- | :---: | :--- |
| `Timestamp` | `DateTimeOffset` | Thời điểm nhận gói tin | Có | Mốc thời gian ghi nhận mẫu đo. |
| `CpuPercent` | `float` | Tính từ `cpu_stats` và `precpu_stats` | Có | Tỷ lệ phần trăm tải CPU (từ 0.0% đến N * 100.0% với N là số CPU core). |
| `MemoryUsageBytes` | `int64` | `memory_stats.usage` | Có | Số byte bộ nhớ RAM vật lý đang chiếm dụng. |
| `MemoryLimitBytes` | `int64` | `memory_stats.limit` | Có | Giới hạn bộ nhớ tối đa của container (hoặc tổng RAM của host). |
| `MemoryPercent` | `float` | Thương số giữa `usage` và `limit` * 100 | Có | Tỷ lệ phần trăm bộ nhớ đang dùng (từ 0.0% đến 100.0%). |
| `NetworkInputBytes`| `int64` | Tổng `rx_bytes` trên toàn bộ interfaces | Có | Tổng số byte mạng nhận vào từ lúc container khởi động. |
| `NetworkOutputBytes`| `int64` | Tổng `tx_bytes` trên toàn bộ interfaces | Có | Tổng số byte mạng gửi đi. |
| `BlockInputBytes` | `int64` | Tính từ `io_service_bytes_recursive` (Read) | Có | Tổng số byte đọc đĩa I/O. |
| `BlockOutputBytes` | `int64` | Tính từ `io_service_bytes_recursive` (Write)| Có | Tổng số byte ghi đĩa I/O. |
| `PidsCount` | `int` | `pids_stats.current` | Có | Số lượng luồng/tiến trình đang hoạt động trong container. |

### 3.3. Hợp đồng `MetricTimeSeries` (Chuỗi Thời Gian Lịch Sử Đo)

| Tên trường | Kiểu dữ liệu | Sức chứa tối đa | Mô tả |
| :--- | :--- | :---: | :--- |
| `ContainerId` | `string` | 64 ký tự | ID của container mục tiêu. |
| `CpuHistory` | `float list` | 30 phần tử | Danh sách 30 giá trị % CPU gần nhất phục vụ vẽ biểu đồ Sparkline. |
| `MemoryHistory` | `float list` | 30 phần tử | Danh sách 30 giá trị % RAM gần nhất. |
| `LatestSample` | `MetricSample option` | 1 phần tử | Dữ liệu mẫu đo lường mới nhất vừa nhận được. |

---

## 4. ĐẶC TẢ CHI TIẾT THUẬT TOÁN & TỪNG HÀM NGHIỆP VỤ

### 4.1. Thuật toán Phân giải Khung Nhị phân Đa kênh (Multiplexed Frame Stream)
- **Đầu vào:** `stream: System.IO.Stream`, `cancellationToken: CancellationToken`.
- **Đầu ra:** Luồng các gói dữ liệu phân kênh `AsyncSeq<StreamChannel * byte[]>`.
- **Quy trình tuần tự từng bước:**
  1. Mượn một mảng byte tạm thời gồm đúng 8 bytes từ `ArrayPool<byte>.Shared` để làm bộ đệm đọc tiêu đề (`headerBuffer`).
  2. **Vòng lặp đọc Khung (Frame Reading Loop):**
     - Đọc tuần tự đủ 8 bytes từ luồng vào `headerBuffer`. Nếu luồng trả về 0 byte (kết thúc luồng), thoát vòng lặp.
     - **Giải mã Byte 0:** Xác định kiểu kênh:
       - Nếu giá trị là `1`: Gán kênh là `StreamChannel.StdOut`.
       - Nếu giá trị là `2`: Gán kênh là `StreamChannel.StdErr`.
       - Nếu giá trị khác: Gán kênh là `StreamChannel.SystemAlert`.
     - **Bỏ qua Byte 1, 2, 3:** Đây là các byte dự phòng hệ thống.
     - **Giải mã Byte 4 đến 7 (Payload Length):**
       - Đọc 4 byte theo thứ tự Big-Endian (Byte có trọng số lớn nhất đứng trước).
       - Tính toán kích thước khối: `payloadSize = (byte4 << 24) | (byte5 << 16) | (byte6 << 8) | byte7`.
     - **Kiểm tra an toàn giới hạn kích thước:** Nếu `payloadSize` âm hoặc lớn hơn 10MB (dấu hiệu khung dữ liệu bị sai lệch), lập tức ngắt kết nối và báo lỗi hỏng khung `ERR_FRAME_CORRUPT`.
  3. **Đọc Thân Khung (Payload):**
     - Mượn mảng byte từ `ArrayPool` có kích thước tối thiểu bằng `payloadSize` (`payloadBuffer`).
     - Đọc chính xác `payloadSize` bytes từ luồng vào `payloadBuffer`.
     - Trích xuất dữ liệu sang đối tượng kết quả, sau đó hoàn trả `payloadBuffer` về cho `ArrayPool`.
     - Phát sinh sự kiện đẩy dữ liệu lên cho máy trạng thái lọc ANSI.
  4. Lặp lại bước 2 cho đến khi có tín hiệu hủy (`cancellationToken.IsCancellationRequested`) hoặc container dừng.
  5. Hoàn trả `headerBuffer` về cho `ArrayPool` trong khối dọn dẹp cuối cùng.

---

### 4.2. Thuật toán Máy Trạng thái 4 Bước Khử Độc ANSI (ANSI Sanitizer FSM)
- **Đầu vào:** Mảng byte UTF-8 `payloadBytes: byte[]`.
- **Đầu ra:** `LogEntry` chứa chuỗi văn bản an toàn và danh sách các phân đoạn màu hợp lệ `ColoredSpan list`.
- **Bảng chuyển đổi máy trạng thái (FSM State Transitions):**

| Trạng thái hiện tại | Ký tự đầu vào | Trạng thái tiếp theo | Hành động xử lý dữ liệu |
| :--- | :--- | :--- | :--- |
| **Normal** (Bình thường) | Ký tự văn bản thông thường | `Normal` | Ghi ký tự vào bộ đệm dòng hiện tại. |
| **Normal** | Ký tự Escape (`\x1b`) | `EscapePending` | Tạm ngừng ghi, lưu vị trí ký tự thoát. |
| **Normal** | Ký tự về đầu dòng (`\r`) | `Normal` | Đặt cờ `IsOverwritten = true`, xóa nội dung dòng hiện tại để đón ký tự mới. |
| **Normal** | Ký tự xuống dòng (`\n`) | `Normal` | Đóng gói dòng hiện tại thành `LogEntry` và đẩy vào Ring Buffer. |
| **EscapePending** | Dấu ngoặc vuông `[` | `CsiParam` | Nhận diện bắt đầu chuỗi CSI (Control Sequence Introducer). |
| **EscapePending** | Ký tự khác | `Normal` | Chuỗi thoát không hợp lệ, hủy bỏ và quay lại bình thường. |
| **CsiParam** | Ký tự số (`0`..`9`) hoặc `;` | `CsiParam` | Tích lũy chuỗi tham số màu vào bộ đệm số. |
| **CsiParam** | Ký tự `m` | `Normal` | **Hợp lệ:** Chuyển đổi mã số ANSI vừa gom thành cấu hình màu chữ/nền (`ColoredSpan`). |
| **CsiParam** | Ký tự điều khiển con trỏ (`H`, `f`, `J`, `K`) | `Normal` | **Khử độc:** Tiêu hủy hoàn toàn chuỗi tham số này, không áp dụng lên terminal. |

---

### 4.3. Thuật toán Tính toán Chỉ số CPU & RAM từ Libpod Stats
- **Đầu vào:** Mẫu đo hiện tại `curr: JsonDocument`, Mẫu đo trước đó `prev: JsonDocument option`.
- **Đầu ra:** `MetricSample`.
- **Công thức tính toán từng bước:**
  1. **Tính Tỷ lệ CPU (`CpuPercent`):**
     - Đọc `currCpuTotal = curr.cpu_stats.cpu_usage.total_usage`.
     - Đọc `currSystemTotal = curr.cpu_stats.system_cpu_usage`.
     - Nếu không có mẫu đo trước (`prev = None`): Gán `CpuPercent = 0.0`.
     - Nếu có mẫu trước:
       - Tính biến thiên CPU của container: `deltaCpu = currCpuTotal - prevCpuTotal`.
       - Tính biến thiên CPU của toàn host: `deltaSystem = currSystemTotal - prevSystemTotal`.
       - Đọc số lõi CPU trực tuyến: `onlineCpus = curr.cpu_stats.online_cpus` (nếu không có, mặc định là độ dài mảng `percpu_usage`).
       - Nếu `deltaSystem > 0` và `deltaCpu >= 0`:
         `CpuPercent = (deltaCpu / deltaSystem) * onlineCpus * 100.0`.
       - Ngược lại: Gán `CpuPercent = 0.0`.
  2. **Tính Tỷ lệ Bộ nhớ (`MemoryPercent`):**
     - Đọc `usedMemory = curr.memory_stats.usage`.
     - Đọc bộ đệm ẩn nếu có: `cacheMemory = curr.memory_stats.stats.inactive_file`.
     - Tính dung lượng bộ nhớ thực tế: `actualMemory = usedMemory - cacheMemory`.
     - Đọc giới hạn: `limitMemory = curr.memory_stats.limit`.
     - `MemoryPercent = (actualMemory / limitMemory) * 100.0`.

---

### 4.4. Thuật toán Ánh xạ Đồ thị Unicode Sparklines 8 Mức
- **Đầu vào:** Danh sách 30 giá trị `values: float list`, Chiều rộng hiển thị `width: int`.
- **Đầu ra:** Chuỗi ký tự hiển thị kèm màu `(string * AnsiColor)`.
- **Quy trình tuần tự từng bước:**
  1. Bảng ký tự chuẩn 8 mức:
     - Mức 1: ` ` (U+2581)
     - Mức 2: `▂` (U+2582)
     - Mức 3: `▃` (U+2583)
     - Mức 4: `▄` (U+2584)
     - Mức 5: `▅` (U+2585)
     - Mức 6: `▆` (U+2586)
     - Mức 7: `▇` (U+2587)
     - Mức 8: `█` (U+2588)
  2. Tìm giá trị nhỏ nhất `minValue` và lớn nhất `maxValue` trong danh sách.
  3. Nếu `maxValue == minValue`: Gán toàn bộ bằng ký tự Mức 1.
  4. Với mỗi giá trị `val` trong chuỗi:
     - Chuẩn hóa về tỷ lệ từ 0.0 đến 1.0: `normalized = (val - minValue) / (maxValue - minValue)`.
     - Tính chỉ số mức (từ 0 đến 7): `index = int (normalized * 7.0)`. Giới hạn chỉ số trong đoạn `[0, 7]`.
     - Lấy ký tự tương ứng tại `index`.
  5. **Quy tắc gán màu ANSI cho toàn chuỗi:**
     - Nếu giá trị mới nhất `< 60.0%`: Gán màu Xanh lá (`Color.Green`).
     - Nếu giá trị mới nhất từ `60.0%` đến `85.0%`: Gán màu Vàng cam (`Color.Yellow`).
     - Nếu giá trị mới nhất `> 85.0%`: Gán màu Đỏ rực (`Color.Red`).

---

### 4.5. Thuật toán Bộ đệm Vòng tròn Cố định (Fixed Ring Buffer)
- **Đầu vào:** Bản ghi mới `entry: LogEntry`, Bộ đệm `buffer: RingBuffer<LogEntry>` sức chứa 2000 phần tử.
- **Đầu ra:** Trạng thái bộ đệm sau khi ghi.
- **Quy trình thao tác con trỏ không cấp phát bộ nhớ (Zero-Allocation):**
  1. Bộ đệm duy trì một mảng cố định `items` gồm đúng 2000 phần tử, biến `head = 0`, biến `tail = 0`, và biến đếm `count = 0`.
  2. Khi ghi một phần tử mới `entry`:
     - Ghi dữ liệu vào vị trí con trỏ `items[head] = entry`.
     - Tăng con trỏ `head = (head + 1) % 2000`.
     - Nếu `count < 2000`: Tăng `count = count + 1`.
     - Nếu `count == 2000` (đã đầy): Dịch con trỏ đuôi `tail = (tail + 1) % 2000` để bỏ qua phần tử cũ nhất.
  3. Khi đọc dữ liệu ra màn hình (Snapshot):
     - Lấy lần lượt các phần tử từ chỉ số `tail` đến `head` theo công thức modulo mà không cần sao chép mảng gốc, tiết kiệm 100% chi phí Garbage Collection.

---

### 4.6. Thuật toán Cuộn Thông minh (Smart Auto-Scroll Logic)
- **Trạng thái:** Biến cờ `FollowTail: bool` (mặc định là `true`).
- **Xử lý sự kiện bàn phím/chuột:**
  1. Khi người dùng nhấn phím cuộn lên (`Up`, `PageUp`, `k`) hoặc lăn chuột ngược:
     - Đặt cờ `FollowTail = false`.
     - Vị trí hiển thị giữ nguyên tại dòng người dùng vừa cuộn tới, không bị dòng log mới đẩy trôi.
  2. Khi người dùng cuộn xuống dưới cùng (`Down`, `PageDown`, `j`) và vị trí con trỏ chạm tới dòng log mới nhất:
     - Tự động bật lại cờ `FollowTail = true`.
  3. Khi có dòng log mới tới từ Socket:
     - Nếu `FollowTail == true`: Tự động gán vị trí cuộn màn hình tới dòng cuối cùng.
     - Nếu `FollowTail == false`: Chỉ cập nhật số đếm chỉ số dòng trong bộ nhớ ngầm.

---

## 5. MA TRẬN MÃ LỖI & KỊCH BẢN XỬ LÝ NGOẠI LỆ (ERROR MATRIX)

| Mã Lỗi | Tên Lỗi Kỹ Thuật | Nguyên Nhân Gốc | Phản Ứng Hệ Thống | Thông Báo Hiển Thị |
| :---: | :--- | :--- | :--- | :--- |
| **`ERR_LOG_STREAM_BREAK`** | `LogStreamDisconnected` | Container bị dừng đột ngột hoặc socket bị đóng khi đang stream | Dừng luồng đọc an toàn, hiển thị thông báo kết thúc luồng | "[bold yellow]Luồng nhật ký đã kết thúc (Container đã dừng hoặc đóng kết nối)[/]" |
| **`ERR_FRAME_CORRUPT`** | `MultiplexFrameCorrupted` | Khung truyền trả về kích thước payload bất thường (> 10MB) | Hủy luồng hiện tại để bảo toàn bộ nhớ, tự động thử mở lại luồng sau 2s | "[bold red]Lỗi cấu trúc khung truyền dữ liệu từ Podman Socket. Đang tái kết nối...[/]" |
| **`ERR_LOG_FLOOD`** | `LogBackpressureDetected` | Container xuất log với tần suất cực lớn (> 20.000 dòng/giây) | Kích hoạt cơ chế nuốt bớt sự kiện giao diện (Throttle 50ms), chỉ render theo từng mảng khối | "[bold grey]Phát hiện tần suất log cao: Đang áp dụng cơ chế điều tiết khung nhìn[/]" |
| **`ERR_STATS_DISABLED`** | `CgroupStatsUnavailable` | Cgroup controller không được ủy quyền cho rootless user | Trả về giá trị 0% cho CPU/RAM thay vì làm vỡ đồ thị Sparkline | "[bold yellow]Không đọc được thông số chi tiết Cgroup. Vui lòng kiểm tra cgroup v2 delegation[/]" |

---

## 6. ĐẶC TẢ BỐ CỤC KHUNG NHÌN GIÁM SÁT (SPECTRE & TERMINAL.GUI LAYOUT)

Khi người dùng chọn một container và xem chi tiết tại Panel bên phải:

### 6.1. Bố cục Phân vùng Tab Chi tiết
- **Tab 1: Logs (`[Logs]`)**:
  - Chiếm toàn bộ chiều cao của Panel Chi tiết.
  - Phía trên cùng có thanh trạng thái thu nhỏ: Hiển thị trạng thái kết nối `[LIVE STREAMING]` (màu xanh lá nhấp nháy) hoặc `[PAUSED SCROLL]` (màu vàng), kèm theo số dòng hiện có `(2000/2000 lines)`.
  - Khung nội dung log: Hiển thị văn bản kèm màu gốc của container, dòng stderr có tiền tố màu đỏ nhạt `[ERR]`.
- **Tab 2: Stats & Metrics (`[Stats]`):**
  - **Khu vực Trên (Sparklines):**
    - Biểu đồ CPU: Tiêu đề `CPU Usage [%]`, theo sau là đồ thị Sparkline 30 ký tự, bên cạnh hiển thị giá trị số thực tế (ví dụ: `[bold green] 12.4%[/]`).
    - Biểu đồ Memory: Tiêu đề `Memory Usage [%]`, đồ thị Sparkline 30 ký tự kèm dung lượng thực tế (ví dụ: `[bold cyan] 142.5 MB / 512 MB (27.8%)[/]`).
  - **Khu vực Dưới (Bảng thống kê I/O & Tiến trình):**
    - Bảng thông tin mạng: `Network RX / TX` (ví dụ: `1.2 MB / 850 KB`).
    - Bảng I/O đĩa: `Block Input / Output` (ví dụ: `4.5 MB / 120 KB`).
    - Số tiến trình con: `PIDs: 8`.

---

## 7. MA TRẬN KỊCH BẢN KIỂM THỬ NGHIỆM THU (TEST CASES MATRIX)

| Mã Ca Kiểm Thử | Tên Kịch Bản Kiểm Thử | Điều Kiện Tiền Đề | Các Bước Thực Hiện | Tiêu Chí Đạt Nghiệm Thu (Pass Criteria) |
| :---: | :--- | :--- | :--- | :--- |
| **TC-M3-01** | Bóc tách chính xác khung đa kênh Stdout và Stderr | Chạy một container xuất đồng thời cả `stdout` và `stderr` | Mở tab Logs của container đó | Dòng stdout hiển thị màu trắng/xám bình thường; dòng stderr được gắn nhãn `[ERR]` và hiển thị màu đỏ/vàng rõ ràng. |
| **TC-M3-02** | Khử độc chuỗi điều khiển ANSI độc hại | Container xuất chuỗi escape `\x1b[2J` (lệnh xóa trắng màn hình) | Quan sát giao diện TUI | Màn hình TUI không bị xóa trắng hay giật lag; chuỗi mã độc bị triệt tiêu hoàn toàn. |
| **TC-M3-03** | Cơ chế cuộn thông minh (Smart Auto-Scroll) | Container đang liên tục in log mới mỗi giây | Nhấn phím `k` để cuộn lên 5 dòng; đợi 5 giây; sau đó nhấn `j` xuống đáy | Khi cuộn lên, màn hình giữ cố định; khi cuộn chạm đáy, màn hình tự động bám theo dòng log mới nhất. |
| **TC-M3-04** | Trực quan hóa Sparklines động | Container đang thực hiện tác vụ tăng dần tải CPU | Mở tab Stats của container | Đồ thị Sparklines vẽ các ký tự khối tăng dần từ ` ` tới `█`; màu sắc chuyển linh hoạt từ Xanh lá sang Vàng và Đỏ. |
| **TC-M3-05** | Giới hạn dung lượng bộ nhớ cố định (Zero-Allocation Test) | Container sinh ra hơn 100.000 dòng log liên tục | Theo dõi mức chiếm dụng RAM của tiến trình `podman-fui` | Mức RAM của ứng dụng duy trì ổn định không tăng phi mã, bộ đệm Ring Buffer giữ đúng 2000 dòng mới nhất. |

---

## 8. KẾT LUẬN & ĐIỀU KIỆN CHUYỂN BƯỚC THỰC THI

Tài liệu Thiết kế Chi tiết này hoàn thiện toàn bộ đặc tả toán học, thuật toán phân giải nhị phân tầng thấp, máy trạng thái khử độc, và cấu trúc dữ liệu không cấp phát bộ nhớ cho Milestone 3. 

Toàn bộ tài liệu tuân thủ tuyệt đối quy định không chèn mã nguồn lập trình, tạo điều kiện thuận lợi và chuẩn mực cho việc nghiệm thu kỹ thuật.
