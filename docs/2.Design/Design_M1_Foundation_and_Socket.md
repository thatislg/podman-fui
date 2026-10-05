# BẢN THIẾT KẾ CHI TIẾT (DETAIL DESIGN): MILESTONE 1
## NỀN TẢNG SOLUTION F# & GIAO TIẾP UNIX DOMAIN SOCKET

- **Mã tài liệu:** DD-M1-FOUNDATION-SOCKET
- **Vị trí lưu trữ:** `docs/2.Design/Design_M1_Foundation_and_Socket.md`
- **Phiên bản:** 2.0.0 (Nâng cấp từ Basic Design lên Detail Design)
- **Ngày phê duyệt:** 2026-10-05
- **Tài liệu căn cứ:** 
  - [`SRS_podman-FUI.md`](../1.Investigation/SRS_podman-FUI.md)
  - [`01_Podman_Socket_and_API_Investigation.md`](../1.Investigation/01_Podman_Socket_and_API_Investigation.md)
- **Tài liệu tiến độ gắn kèm:** [`Milestone_1_Foundation_and_Investigation.md`](../3.Progress/Milestone_1_Foundation_and_Investigation.md)

---

## 1. TỔNG QUAN & PHẠM VI THIẾT KẾ CHI TIẾT

Tài liệu này nâng cấp toàn bộ nội dung từ mức Thiết kế Cơ sở (Basic Design) lên **Thiết kế Chi tiết (Detail Design)** cho Milestone 1:
- Định rõ từng tệp mã nguồn trong từng dự án thành phần.
- Đặc tả chi tiết từng trường dữ liệu của Data Contracts (kiểu dữ liệu, ánh xạ JSON, quy tắc kiểm tra).
- Đặc tả chi tiết từng hàm nghiệp vụ: tham số đầu vào, kiểu dữ liệu trả về, thuật toán tuần tự từng bước (Procedural Steps).
- Bảng ma trận mã lỗi chi tiết và các ca kiểm thử nghiệm thu (Test Cases).
- **Tuân thủ tuyệt đối:** Toàn bộ bản thiết kế được diễn giải bằng lời văn, bảng biểu, lược đồ, không sử dụng đoạn mã lập trình cụ thể (Zero Code Sample).

---

## 2. PHÂN RÃ DANH MỤC TỆP & MÔ-ĐUN MÃ NGUỒN (MODULE INVENTORY)

Toàn bộ Solution `podman-FUI.sln` được phân rã thành 4 dự án với danh mục tệp chi tiết:

### 2.1. Dự án `PodmanFUI.Domain` (Target: .NET 10 / net10.0)
- **Tệp 1: `Errors.fs` (Mô-đun quản lý mã lỗi & ngoại lệ miền):**
  - Định nghĩa tập hợp các trường hợp lỗi kết nối (`ConnectionError`) và lỗi phân giải dữ liệu.
- **Tệp 2: `Models.fs` (Mô-đun định nghĩa cấu trúc dữ liệu miền):**
  - Chứa các kiểu bản ghi (Record Types): `CgroupInfo`, `HostInfo`, `VersionInfo`, `SystemInfo`.
  - Chứa kiểu phân loại chế độ socket: `SocketMode` (`Rootless`, `Rootful`, `Custom`).
  - Chứa kết quả dò tìm socket: `SocketDiscoveryResult` (`Discovered`, `NotFound`).
- **Tệp 3: `ISocketClient.fs` (Giao diện trừu tượng tầng Socket):**
  - Định nghĩa hợp đồng interface `IPodmanSocketClient` với phương thức bất đồng bộ `GetSystemInfoAsync`.

### 2.2. Dự án `PodmanFUI.Infrastructure` (Target: .NET 10 / net10.0)
- **Tệp 1: `SocketDiscovery.fs` (Mô-đun dò tìm socket tự động):**
  - Chứa hàm `discoverSocket: unit -> SocketDiscoveryResult`.
  - Chứa hàm phụ trợ chuẩn hóa đường dẫn URI `normalizePath: string -> string`.
- **Tệp 2: `LibpodJsonParser.fs` (Mô-đun bóc tách JSON phản hồi):**
  - Chứa hàm phân giải JSON chuyên biệt: `parseSystemInfo: string -> string -> SocketMode -> Result<SystemInfo, ConnectionError>`.
- **Tệp 3: `PodmanSocketClient.fs` (Bộ điều phối kết nối vật lý):**
  - Hiện thực hóa giao diện `IPodmanSocketClient`.
  - Quản lý vòng đời của `SocketsHttpHandler` và `HttpClient`.

### 2.3. Dự án `PodmanFUI.Presentation` (Target: .NET 10 / net10.0)
- **Tệp 1: `Theme.fs` (Mô-đun bảng màu & phong cách hiển thị):**
  - Định nghĩa các mã màu ANSI chuẩn: Màu chủ đạo (Cyan), Màu thành công (Green), Màu cảnh báo (Yellow), Màu lỗi (Red), Màu viền bảng (DarkCyan).
- **Tệp 2: `PocRenderer.fs` (Mô-đun xuất dữ liệu PoC):**
  - Chứa hàm render thông điệp chào mừng: `renderWelcomeBanner: unit -> unit`.
  - Chứa hàm vẽ bảng Spectre Table: `renderSystemInfoTable: SystemInfo -> unit`.
  - Chứa hàm hiển thị cảnh báo lỗi: `renderErrorDialog: ConnectionError -> unit`.

### 2.4. Dự án `PodmanFUI.App` (Target: .NET 10 / net10.0 - Console Executable)
- **Tệp 1: `Program.fs` (Điểm thực thi chính):**
  - Chứa hàm `main: string[] -> int` điều phối luồng chạy kiểm thử nghiệm thu PoC.

---

## 3. ĐẶC TẢ CHI TIẾT HỢP ĐỒNG DỮ LIỆU (DATA CONTRACTS SPECIFICATION)

### 3.1. Hợp đồng `CgroupInfo`
Mô tả hiện trạng phân hệ Cgroup của máy chủ Linux:

| Tên trường | Kiểu dữ liệu | Đường dẫn ánh xạ JSON từ API `/info` | Bắt buộc | Mô tả & Quy tắc kiểm tra |
| :--- | :--- | :--- | :---: | :--- |
| `Version` | `string` | `host.cgroupVersion` | Có | Phiên bản Cgroup. Chuỗi hợp lệ: `"v1"` hoặc `"v2"`. |
| `Manager` | `string` | `host.cgroupManager` | Có | Trình điều khiển Cgroup. Giá trị chuẩn: `"systemd"` hoặc `"cgroupfs"`. |
| `Controllers` | `string list` | `host.cgroupControllers` | Không | Mảng danh sách các bộ điều khiển được ủy quyền (ví dụ `["cpu", "memory", "pids"]`). Nếu thiếu, gán danh sách rỗng. |

### 3.2. Hợp đồng `HostInfo`
Mô tả chi tiết phần cứng và cấu hình động cơ Podman:

| Tên trường | Kiểu dữ liệu | Đường dẫn ánh xạ JSON từ API `/info` | Bắt buộc | Mô tả & Quy tắc kiểm tra |
| :--- | :--- | :--- | :---: | :--- |
| `Arch` | `string` | `host.arch` | Có | Kiến trúc phần cứng (ví dụ `"amd64"`, `"arm64"`). |
| `OS` | `string` | `host.os` | Có | Tên hệ điều hành nhân (ví dụ `"linux"`). |
| `Kernel` | `string` | `host.kernel` | Có | Phiên bản bản dựng Linux Kernel (ví dụ `"7.0.0-34-generic"`). |
| `Cgroup` | `CgroupInfo` | Đối tượng `host` | Có | Đối tượng lồng nhau mô tả chi tiết Cgroup. |
| `StorageDriver` | `string` | `store.graphDriverName` | Không | Tên driver lưu trữ (ví dụ `"overlay"`). Mặc định là `"unknown"`. |
| `GraphRoot` | `string` | `store.graphRoot` | Không | Đường dẫn thư mục dữ liệu (ví dụ `"/home/user/.local/share/containers/storage"`). |
| `Rootless` | `bool` | Suy luận từ `SocketMode` & `store.graphRoot` | Có | `true` nếu chạy ở quyền người dùng unprivileged, `false` nếu root. |
| `ConmonVersion`| `string` | `host.conmon.version` | Không | Phiên bản tiến trình giám sát container conmon. |

### 3.3. Hợp đồng `VersionInfo`
Mô tả phiên bản phần mềm Podman:

| Tên trường | Kiểu dữ liệu | Đường dẫn ánh xạ JSON từ API `/info` | Bắt buộc | Mô tả |
| :--- | :--- | :--- | :---: | :--- |
| `Version` | `string` | `version.Version` | Có | Số phiên bản Podman (ví dụ `"4.9.3"`). |
| `ApiVersion` | `string` | `version.APIVersion` | Có | Phiên bản API Libpod tương ứng (ví dụ `"4.9.3"`). |
| `GoVersion` | `string` | `version.GoVersion` | Không | Phiên bản trình biên dịch Go đã build Podman. |
| `GitCommit` | `string` | `version.GitCommit` | Không | Mã băm Git commit của bản build. |
| `BuiltTime` | `string` | `version.BuildTime` | Không | Thời điểm build phần mềm. |

### 3.4. Hợp đồng `SystemInfo` (Tổng hợp)
- Bao gồm: `Host: HostInfo`, `Version: VersionInfo`, `SocketPath: string`, `SocketMode: SocketMode`.

---

## 4. ĐẶC TẢ CHI TIẾT THUẬT TOÁN & TỪNG HÀM NGHIỆP VỤ

### 4.1. Hàm `SocketDiscovery.discoverSocket()`
- **Đầu vào:** Không có (`unit`).
- **Đầu ra:** `SocketDiscoveryResult` (`Discovered` chứa đường dẫn và chế độ, hoặc `NotFound` chứa danh sách đường dẫn đã thử và thông điệp hướng dẫn).
- **Thuật toán thực thi từng bước (Procedural Steps):**
  1. Khởi tạo một danh sách lưu vết các đường dẫn đã kiểm tra (`attemptedPaths`).
  2. Đọc biến môi trường `CONTAINER_HOST`. Nếu rỗng, đọc tiếp biến `PODMAN_SOCKET`.
  3. **Kiểm tra Điều kiện 1 (Biến môi trường tùy chỉnh):**
     - Nếu có giá trị: Gọi hàm `normalizePath` để cắt bỏ tiền tố `unix://` nếu có.
     - Thêm đường dẫn vào `attemptedPaths`.
     - Kiểm tra tệp vật lý bằng `File.Exists(path)`.
     - Nếu tồn tại: Trả về kết quả `Discovered(path, Custom)`.
     - Nếu không tồn tại: Trả về kết quả `NotFound(attemptedPaths, "Đường dẫn socket từ biến môi trường không tồn tại")`.
  4. **Kiểm tra Điều kiện 2 (Rootless Socket người dùng):**
     - Đọc biến môi trường `$XDG_RUNTIME_DIR`.
     - Nếu biến có giá trị: Ghép chuỗi đường dẫn `Path.Combine(xdgRuntimeDir, "podman", "podman.sock")`.
     - Nếu biến không có giá trị: Lấy định danh user hiện tại và tạo chuỗi `sprintf "/run/user/%s/podman/podman.sock" userName`.
     - Thêm đường dẫn vào `attemptedPaths`.
     - Kiểm tra tệp vật lý bằng `File.Exists`. Nếu tồn tại: Trả về kết quả `Discovered(rootlessPath, Rootless)`.
  5. **Kiểm tra Điều kiện 3 (Rootful Socket hệ thống):**
     - Đặt đường dẫn mục tiêu là `"/run/podman/podman.sock"`.
     - Thêm đường dẫn vào `attemptedPaths`.
     - Kiểm tra tệp vật lý. Nếu tồn tại: Trả về `Discovered(rootfulPath, Rootful)`.
  6. **Kết luận thất bại:** Trả về `NotFound(attemptedPaths, "Dịch vụ socket chưa chạy. Kích hoạt bằng lệnh: systemctl --user enable --now podman.socket")`.

---

### 4.2. Cấu hình Tầng Hạ tầng `PodmanSocketClient`
- **Thông số kỹ thuật của Handler:**
  - Khởi tạo `SocketsHttpHandler` tùy biến.
  - Thiết lập thuộc tính `ConnectCallback`:
    - Nhận ngữ cảnh kết nối và `CancellationToken`.
    - Tạo một Socket mới với các thông số:
      - Họ địa chỉ: `AddressFamily.Unix`.
      - Kiểu Socket: `SocketType.Stream`.
      - Giao thức: `ProtocolType.Unspecified`.
    - Tạo đối tượng `UnixDomainSocketEndPoint` trỏ tới file socket đã dò tìm.
    - Gọi phương thức `ConnectAsync` truyền kèm `CancellationToken`.
    - Bọc đối tượng Socket đã kết nối vào một `NetworkStream` với cờ sở hữu tài nguyên `ownsSocket = true`.
    - Trả về đối tượng `ValueTask<Stream>`.
- **Thông số kỹ thuật của HttpClient:**
  - Gắn Handler vừa tạo vào `HttpClient`.
  - BaseAddress cố định: `http://d/v4.0.0/libpod/`.
  - Cấu hình Timeout mặc định: 10 giây (ngăn chặn tình trạng ứng dụng bị treo vô hạn nếu socket bị tắc nghẽn).

---

### 4.3. Hàm `LibpodJsonParser.parseSystemInfo()`
- **Đầu vào:** `rawJson: string`, `socketPath: string`, `mode: SocketMode`.
- **Đầu ra:** `Result<SystemInfo, ConnectionError>`.
- **Thuật toán phân giải từng bước:**
  1. Sử dụng `JsonDocument.Parse` để nạp chuỗi JSON vào bộ nhớ phân tích dạng DOM.
  2. Bóc tách nhánh `host`: Trích xuất các thuộc tính `arch`, `os`, `kernel`, `cgroupVersion`, `cgroupManager`.
  3. Lặp mảng `cgroupControllers`: Đọc từng chuỗi tên controller và gom vào danh sách F# list.
  4. Bóc tách nhánh `store`: Trích xuất `graphDriverName` và `graphRoot`.
  5. Bóc tách nhánh `version`: Trích xuất `Version`, `APIVersion`, `GoVersion`, `GitCommit`, `BuildTime`.
  6. Khởi tạo đối tượng `HostInfo` và `VersionInfo`, đóng gói thành `SystemInfo`.
  7. Bọc kết quả vào trường hợp `Ok(systemInfo)`. Nếu xảy ra lỗi phân tích cú pháp JSON, bắt ngoại lệ và trả về `Error(DeserializationError message)`.

---

## 5. MA TRẬN MÃ LỖI & KỊCH BẢN XỬ LÝ NGOẠI LỆ (ERROR MATRIX)

Bảng phân loại chi tiết các mã lỗi và cơ chế xử lý:

| Mã Lỗi | Tên Ngoại Lệ Miền | Nguyên Nhân Gốc | Cơ Chế Phát Hiện | Thông Báo Hiển Thị Người Dùng |
| :---: | :--- | :--- | :--- | :--- |
| **`ERR_SOCK_404`** | `SocketNotFound` | File `.sock` không tồn tại do chưa bật service systemd | `File.Exists = false` trên toàn bộ đường dẫn | "Không tìm thấy Podman Socket. Hướng dẫn: Chạy lệnh 'systemctl --user enable --now podman.socket'" |
| **`ERR_SOCK_403`** | `AccessDenied` | Quyền truy cập Linux không cho phép đọc/ghi vào socket | Bắt `SocketException` với mã lỗi `SocketError.AccessDenied` (EACCES) | "Bị từ chối quyền truy cập socket tại [đường dẫn]. Vui lòng kiểm tra quyền hạn user." |
| **`ERR_CONN_TIMEOUT`**| `HttpFailure` | Tiến trình socket bị treo hoặc cgroup bị khóa | Bắt `TaskCanceledException` khi vượt quá 10 giây | "Kết nối tới Podman socket bị quá thời gian (Timeout). Động cơ Podman có thể đang bị quá tải." |
| **`ERR_HTTP_500`** | `HttpFailure` | Podman Engine gặp sự cố nội bộ | Phản hồi HTTP có mã trạng thái >= 400 | "Động cơ Podman trả về lỗi: [Mã trạng thái] - [Lý do]." |
| **`ERR_JSON_PARSE`** | `DeserializationError`| Phản hồi JSON không đúng định dạng Libpod v4 | Bắt `JsonException` khi parse | "Không thể phân giải dữ liệu phản hồi từ Podman API. Phiên bản API có thể không tương thích." |

---

## 6. ĐẶC TẢ CHI TIẾT GIAO DIỆN HIỂN THỊ NGHIỆM THU POC (SPECTRE THEME & LAYOUT)

Chương trình PoC trong Milestone 1 sử dụng `Spectre.Console` để xuất màn hình nghiệm thu với các quy chuẩn thị giác sau:

### 6.1. Bảng Màu Hệ Thống (Color Palette)
- Màu tiêu đề ứng dụng (Title): `Color.Cyan1` (In đậm - Bold).
- Màu trạng thái thành công (Success): `Color.Green` (Ký hiệu `[bold green]✓[/]`).
- Màu trạng thái cảnh báo (Warning): `Color.Yellow` (Ký hiệu `[bold yellow]![/]`).
- Màu trạng thái thất bại (Failure): `Color.Red` (Ký hiệu `[bold red]✗[/]`).
- Màu đường viền khung bảng (Border): `Color.DeepSkyBlue1`.
- Màu nhãn dữ liệu (Labels): `Color.Grey` (Chữ nghiêng - Italic).
- Màu giá trị dữ liệu (Values): `Color.White` (In đậm - Bold).

### 6.2. Quy Cách Bảng Kết Quả Nghiệm Thu (Spectre Table Layout)
- **Tiêu đề bảng:** `Podman Engine Environment Validation (Milestone 1 PoC)`.
- **Kiểu đường viền:** `TableBorder.Rounded` (Đường viền bo tròn góc hiện đại).
- **Cấu trúc cột:**
  - Cột 1: `Property Name` (Canh lề trái, độ rộng tối thiểu 20 ký tự).
  - Cột 2: `Detected Value` (Canh lề trái, độ rộng linh hoạt theo dữ liệu).
- **Danh sách các hàng dữ liệu (Rows):**
  1. Hàng 1: `Podman Version` -> Hiển thị số phiên bản kèm API version.
  2. Hàng 2: `Target OS / Arch` -> Hiển thị hệ điều hành và kiến trúc chip.
  3. Hàng 3: `Linux Kernel` -> Hiển thị phiên bản kernel hiện tại.
  4. Hàng 4: `Cgroup Version` -> Hiển thị `v2` (hoặc `v1`) kèm tên Cgroup Manager (`systemd`).
  5. Hàng 5: `Cgroup Controllers` -> Liệt kê danh sách controller (ví dụ `cpu, memory, pids`).
  6. Hàng 6: `Storage Driver` -> Hiển thị driver và đường dẫn GraphRoot.
  7. Hàng 7: `Execution Mode` -> Hiển thị nhãn `[Rootless]` màu xanh hoặc `[Rootful]` màu vàng.
  8. Hàng 8: `Socket Endpoint` -> Hiển thị đường dẫn socket vật lý đã kết nối thành công.
- **Chân trang (Footer):** Dòng thông báo `[bold green]Milestone 1 Technical Validation Succeeded (DoD Achieved)[/]`.

---

## 7. MA TRẬN KỊCH BẢN KIỂM THỬ NGHIỆM THU (TEST CASES MATRIX)

| Mã Ca Kiểm Thử | Tên Kịch Bản Kiểm Thử | Điều Kiện Tiền Đề | Các Bước Thực Hiện | Kết Quả Kỳ Vọng (Pass Criteria) |
| :---: | :--- | :--- | :--- | :--- |
| **TC-01** | Kiểm tra kết nối Rootless tiêu chuẩn (Happy Path) | Dịch vụ `podman.socket` của user đang active | Chạy lệnh `dotnet run --project src/PodmanFUI.App` | Tìm thấy socket tại `/run/user/<UID>/...`, in ra đầy đủ bảng thông tin hệ thống, mã thoát là `0`. |
| **TC-02** | Xử lý khi socket chưa được kích hoạt | Dừng socket bằng `systemctl --user stop podman.socket` | Chạy lệnh `dotnet run --project src/PodmanFUI.App` | Không bị crash, in cảnh báo lỗi `ERR_SOCK_404` kèm lệnh hướng dẫn bật socket, mã thoát là `1`. |
| **TC-03** | Dò tìm qua biến môi trường tùy chỉnh | Đặt biến `export CONTAINER_HOST=unix:///run/user/1000/podman/podman.sock` | Chạy lệnh `dotnet run --project src/PodmanFUI.App` | Nhận diện chế độ `Custom`, kết nối thành công qua đường dẫn chỉ định từ biến môi trường. |
| **TC-04** | Kiểm tra tính đầy đủ của trường Cgroup | Chạy trên Linux Mint / Ubuntu hiện tại | Chạy lệnh kiểm thử | Trường `Controllers` phải chứa ít nhất 2 bộ điều khiển `cpu` và `memory`. |

---

## 8. KẾT LUẬN & ĐIỀU KIỆN CHUYỂN BƯỚC THỰC THI

Bản Thiết kế Chi tiết (Detail Design) này đã làm rõ đến mức nguyên tử từng tệp, từng kiểu dữ liệu, từng luồng thuật toán và từng kịch bản kiểm thử nghiệm thu. 

**Quy trình tiếp theo:**
1. Căn cứ vào các đặc tả trên, tiến hành khởi tạo cấu trúc Solution và các dự án trong `src/`.
2. Lập trình tầng Domain (`Errors.fs`, `Models.fs`).
3. Lập trình tầng Infrastructure (`SocketDiscovery.fs`, `LibpodJsonParser.fs`, `PodmanSocketClient.fs`).
4. Lập trình tầng Presentation & App (`Theme.fs`, `PocRenderer.fs`, `Program.fs`).
5. Thực thi kiểm thử chạy thực tế đạt kết quả như `TC-01` để đóng Milestone 1.
