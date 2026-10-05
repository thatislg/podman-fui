# BẢN THIẾT KẾ CHI TIẾT (DETAIL DESIGN): MILESTONE 2
## KIẾN TRÚC MVU, ĐIỀU PHỐI GIAO DIỆN & QUẢN LÝ CONTAINER (CORE MVP)

- **Mã tài liệu:** DD-M2-CORE-MVP-DASHBOARD
- **Vị trí lưu trữ:** `docs/2.Design/Design_M2_Core_MVP_Dashboard.md`
- **Phiên bản:** 2.0.0 (Nâng cấp toàn diện từ Basic Design lên Detail Design)
- **Ngày phê duyệt:** 2026-10-05
- **Tài liệu căn cứ:** 
  - [`SRS_podman-FUI.md`](../1.Investigation/SRS_podman-FUI.md)
  - [`02_UI_Framework_and_Rendering_Investigation.md`](../1.Investigation/02_UI_Framework_and_Rendering_Investigation.md)
  - [`03_FSharp_Architecture_and_State_Management_Investigation.md`](../1.Investigation/03_FSharp_Architecture_and_State_Management_Investigation.md)
  - [`07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md`](../1.Investigation/07_DeepDive_Terminal_PTY_RawMode_and_Sanitization.md)
- **Tài liệu tiến độ gắn kèm:** [`Milestone_2_Core_MVP_Dashboard.md`](../3.Progress/Milestone_2_Core_MVP_Dashboard.md)

---

## 1. TỔNG QUAN & PHẠM VI THIẾT KẾ CHI TIẾT

Tài liệu này nâng cấp toàn bộ thiết kế Milestone 2 lên mức **Thiết kế Chi tiết (Detail Design)**:
- Đặc tả toàn bộ cây mô-đun và tệp mã nguồn mới bổ sung vào Solution F#.
- Đặc tả chi tiết các Hợp đồng Dữ liệu (Data Contracts) cho thực thể Container, thông điệp MVU, và trạng thái điều hướng.
- Mô tả chi tiết thuật toán từng bước cho chu trình MVU một chiều, cơ chế co giãn giao diện thích ứng đa độ phân giải, và kỹ thuật bọc tiến trình Interactive Shell PTY an toàn.
- Thiết lập bảng ma trận mã lỗi, bảng phím tắt 1 ký tự, và ma trận ca kiểm thử nghiệm thu.
- **Quy tắc bắt buộc:** Toàn văn được trình bày bằng ngôn ngữ tự nhiên, bảng biểu, quy trình tuần tự; tuyệt đối không chèn đoạn mã lập trình cụ thể (Zero Code Sample).

---

## 2. PHÂN RÃ DANH MỤC TỆP & MÔ-ĐUN MÃ NGUỒN (MODULE INVENTORY)

Milestone 2 bổ sung các tệp chức năng vào cấu trúc 4 dự án của Solution:

### 2.1. Dự án `PodmanFUI.Domain`
- **Tệp: `ContainerModels.fs` (Thực thể miền Container):**
  - Chứa kiểu dữ liệu trạng thái container: `ContainerStatus` (`Running`, `Exited`, `Paused`, `Created`, `Restarting`, `Dead`).
  - Chứa kiểu cổng ánh xạ: `PortMapping`.
  - Chứa kiểu tóm tắt container: `ContainerSummary`.
  - Chứa kiểu chi tiết chuyên sâu: `ContainerDetail`.
  - Chứa định nghĩa hành động: `ContainerAction` (`Start`, `Stop`, `Restart`, `Pause`, `Unpause`, `Delete`, `ExecShell`).
- **Tệp: `NavigationModels.fs` (Điều hướng & Phân vùng):**
  - Chứa kiểu danh mục: `NavigationCategory` (`Pods`, `Containers`, `Images`, `Volumes`, `Networks`).
  - Chứa kiểu tab chi tiết: `DetailTab` (`Logs`, `Inspect`, `Top`, `Env`).
  - Chứa kiểu tiêu điểm khung nhìn: `ActiveFocus` (`Sidebar`, `DetailPane`, `ModalDialog`).
  - Chứa kiểu trạng thái hộp thoại modal: `ModalState` (`Closed`, `ConfirmActionDialog`, `FilterInputDialog`, `ErrorAlertDialog`).
- **Tệp: `MvuTypes.fs` (Hợp đồng chu trình MVU):**
  - Chứa cấu trúc trạng thái toàn cục `DashboardModel`.
  - Chứa tập hợp thông điệp sự kiện `DashboardMsg`.
  - Chứa kiểu chỉ thị tác vụ bất đồng bộ `DashboardCmd`.
- **Tệp: `IContainerService.fs` (Giao diện dịch vụ trừu tượng):**
  - Định nghĩa interface `IContainerService` với các phương thức bất đồng bộ: `ListContainersAsync`, `GetContainerDetailAsync`, `PerformActionAsync`.

### 2.2. Dự án `PodmanFUI.Infrastructure`
- **Tệp: `ContainerApiClient.fs` (Giao tiếp Libpod Container API):**
  - Thực thi các endpoint REST qua Unix Domain Socket:
    - `GET /v4.0.0/libpod/containers/json?all=true` (Liệt kê container).
    - `GET /v4.0.0/libpod/containers/{name}/json` (Inspect chi tiết).
    - `POST /v4.0.0/libpod/containers/{name}/start` (Khởi động).
    - `POST /v4.0.0/libpod/containers/{name}/stop?t=10` (Dừng với thời gian chờ graceful 10s).
    - `POST /v4.0.0/libpod/containers/{name}/restart?t=10` (Khởi động lại).
    - `POST /v4.0.0/libpod/containers/{name}/pause` (Tạm dừng).
    - `POST /v4.0.0/libpod/containers/{name}/unpause` (Tiếp tục).
    - `DELETE /v4.0.0/libpod/containers/{name}?force=false` (Xóa container).
- **Tệp: `ProcessExecutionService.fs` (Điều phối tiến trình con & PTY):**
  - Quản lý việc thực thi lệnh ngoài hệ thống: `podman exec -it <name> /bin/sh`.
  - Đảm bảo an toàn luồng PTY và khôi phục chế độ console terminal.

### 2.3. Dự án `PodmanFUI.Presentation`
- **Tệp: `ResponsiveLayoutManager.fs` (Tính toán kích thước thích ứng):**
  - Tính toán phân bổ tọa độ và tỷ lệ hiển thị theo kích thước terminal thực tế.
  - Phân loại ngưỡng hiển thị (Breakpoints): `Compact` (< 80 cột), `Standard` (80 - 120 cột), `Expanded` (> 120 cột).
- **Tệp: `Views/TopBarView.fs` (Khung điều hướng trên cùng):**
  - Hiển thị danh mục tài nguyên (1: Pods, 2: Containers, 3: Images, 4: Volumes, 5: Networks).
  - Hiển thị chế độ kết nối và thông tin phiên bản Podman.
- **Tệp: `Views/SidebarView.fs` (Khung danh sách bên trái):**
  - Hiển thị danh sách container dưới dạng bảng Terminal.Gui TableView/ListView.
  - Vẽ biểu tượng trạng thái trực quan màu sắc ANSI.
- **Tệp: `Views/ContainerDetailView.fs` (Khung thông tin chi tiết bên phải):**
  - Quản lý cụm tab: Logs tĩnh ban đầu, Inspect JSON rút gọn, Danh sách tiến trình Top, Biến môi trường Env.
- **Tệp: `Views/FooterView.fs` (Thanh trạng thái & Trợ giúp phím tắt):**
  - Hiển thị danh sách các phím thao tác khả dụng tương ứng với trạng thái thực thể hiện tại.
- **Tệp: `Views/ModalsView.fs` (Hộp thoại tương tác nổi):**
  - Hiển thị modal xác nhận xóa, modal tìm kiếm/lọc mờ, modal báo lỗi.
- **Tệp: `MvuLoop.fs` (Động cơ điều phối chu trình MVU):**
  - Hàm khởi tạo: `init: unit -> DashboardModel * DashboardCmd`.
  - Hàm cập nhật trạng thái: `update: DashboardMsg -> DashboardModel -> DashboardModel * DashboardCmd`.
  - Hàm kết nối vòng lặp Terminal.Gui Application Run.

### 2.4. Dự án `PodmanFUI.App`
- **Tệp: `Program.fs`:**
  - Khởi động ứng dụng Terminal.Gui v2, kích hoạt chu trình MVU và xử lý tắt ứng dụng an toàn.

---

## 3. ĐẶC TẢ CHI TIẾT HỢP ĐỒNG DỮ LIỆU (DATA CONTRACTS SPECIFICATION)

### 3.1. Hợp đồng `ContainerSummary` (Thực thể tóm tắt)

| Tên trường | Kiểu dữ liệu | Đường dẫn ánh xạ JSON Libpod | Bắt buộc | Mô tả & Quy tắc kiểm tra |
| :--- | :--- | :--- | :---: | :--- |
| `Id` | `string` | `Id` | Có | Chuỗi băm nhận dạng SHA256 đầy đủ của container. |
| `ShortId` | `string` | Suy luận từ 12 ký tự đầu của `Id` | Có | Mã ngắn gọn 12 ký tự phục vụ hiển thị trên giao diện. |
| `Names` | `string list` | `Names` | Có | Danh sách tên container. Tên chính thức là phần tử đầu tiên, loại bỏ tiền tố gạch chéo `/` nếu có. |
| `PrimaryName` | `string` | Suy luận từ `Names` | Có | Tên đại diện duy nhất hiển thị trên danh sách. |
| `Image` | `string` | `Image` | Có | Tên và thẻ của Container Image (ví dụ: `docker.io/library/nginx:alpine`). |
| `Status` | `ContainerStatus` | Ánh xạ từ `State` | Có | Enum trạng thái: `Running`, `Exited`, `Paused`, `Created`, `Restarting`, `Dead`. |
| `StatusText` | `string` | `Status` | Có | Chuỗi mô tả trạng thái từ Podman (ví dụ: `"Up 2 hours"`, `"Exited (0) 5 mins ago"`). |
| `Created` | `DateTimeOffset` | `Created` (UNIX Timestamp) | Có | Thời điểm khởi tạo container. |
| `Ports` | `PortMapping list` | `Ports` | Không | Danh sách cổng mở ra máy host và mạng container. |
| `PodName` | `string option` | `PodName` | Không | Tên Pod sở hữu (nếu container thuộc về một Pod cụ thể). |

### 3.2. Cấu trúc Mô hình Toàn cục `DashboardModel`

| Phân vùng dữ liệu | Tên trường | Kiểu dữ liệu | Giá trị khởi tạo mặc định | Mục đích sử dụng |
| :--- | :--- | :--- | :--- | :--- |
| **Điều hướng** | `ActiveCategory` | `NavigationCategory` | `Containers` | Xác định nhóm tài nguyên đang được quản lý. |
| | `ActiveTab` | `DetailTab` | `Logs` | Xác định tab nội dung đang hiển thị tại khung bên phải. |
| | `CurrentFocus` | `ActiveFocus` | `Sidebar` | Xác định khung cửa sổ đang nhận sự kiện bàn phím. |
| **Dữ liệu Container** | `Containers` | `ContainerSummary list` | Danh sách rỗng `[]` | Toàn bộ danh sách container lấy về từ socket. |
| | `FilteredContainers` | `ContainerSummary list` | Danh sách rỗng `[]` | Danh sách sau khi áp dụng bộ lọc tìm kiếm. |
| | `SelectedIndex` | `int` | `0` | Chỉ số hàng đang được con trỏ trỏ tới. |
| | `SelectedContainerId`| `string option` | `None` | ID của container đang được chọn để xem chi tiết. |
| | `DetailData` | `ContainerDetail option` | `None` | Dữ liệu Inspect chi tiết của container đang chọn. |
| **Bộ lọc tìm kiếm** | `IsFilterActive` | `bool` | `false` | Cờ trạng thái thanh nhập từ khóa lọc có đang mở hay không. |
| | `FilterQuery` | `string` | Chuỗi rỗng `""` | Chuỗi từ khóa lọc do người dùng gõ vào. |
| **Hộp thoại Modal** | `ActiveModal` | `ModalState` | `Closed` | Trạng thái hiển thị cửa sổ hộp thoại nổi. |
| **Hệ thống & Tải** | `IsLoading` | `bool` | `true` | Cờ hiển thị biểu tượng tiến trình tải dữ liệu nền. |
| | `ErrorMessage` | `string option` | `None` | Chuỗi thông báo lỗi hệ thống cần thông báo. |
| | `TerminalWidth` | `int` | `100` | Số cột bề ngang terminal hiện tại. |
| | `TerminalHeight` | `int` | `30` | Số dòng chiều cao terminal hiện tại. |

### 3.3. Danh mục Thông điệp `DashboardMsg`

| Nhóm thông điệp | Tên thông điệp | Tham số truyền kèm (Payload) | Nguồn phát sinh sự kiện |
| :--- | :--- | :--- | :--- |
| **Bàn phím & Chuột** | `KeyPressed` | `KeyStroke` | Người dùng gõ phím từ bàn phím. |
| | `RowSelected` | `int` (Index) | Người dùng dùng phím mũi tên hoặc click chuột vào dòng. |
| | `CategoryChanged`| `NavigationCategory` | Nhấn phím số 1..5 hoặc click chuột vào danh mục TopBar. |
| | `TabChanged` | `DetailTab` | Nhấn phím `]` hoặc `[` hoặc click vào tab bên phải. |
| | `ToggleFocus` | Không có | Nhấn phím `Tab` để hoán chuyển tiêu điểm Trái/Phải. |
| **Tác vụ Container** | `RequestAction` | `ContainerAction * string` | Nhấn phím tắt hành động (`s`, `d`, `r`, `p`, `e`). |
| | `ConfirmModal` | Không có | Nhấn `Enter` hoặc `y` xác nhận hành động nguy hiểm (xóa). |
| | `DismissModal` | Không có | Nhấn `Esc` hoặc `n` đóng hộp thoại modal. |
| | `UpdateFilter` | `string` | Người dùng gõ ký tự trong ô tìm kiếm. |
| | `ClearFilter` | Không có | Nhấn phím `Esc` khi đang mở chế độ tìm kiếm. |
| **Dữ liệu Nền** | `ContainersLoaded` | `Result<ContainerSummary list, DomainError>` | Luồng tải danh sách container từ Socket hoàn tất. |
| | `DetailLoaded` | `Result<ContainerDetail, DomainError>` | Luồng tải thông tin chi tiết Inspect hoàn tất. |
| | `ActionExecuted` | `Result<ContainerAction * string, DomainError>` | Tác vụ tác động container gửi tới API hoàn tất. |
| | `TerminalResized` | `int * int` (Width, Height) | Hệ điều hành gửi tín hiệu kích thước màn hình thay đổi. |

---

## 4. ĐẶC TẢ CHI TIẾT THUẬT TOÁN & TỪNG HÀM NGHIỆP VỤ

### 4.1. Thuật toán Vòng lặp MVU: Hàm `update(msg, model)`
- **Đầu vào:** Thông điệp `msg: DashboardMsg`, trạng thái hiện tại `model: DashboardModel`.
- **Đầu ra:** Cặp giá trị gồm trạng thái mới và chỉ thị bất đồng bộ `(DashboardModel * DashboardCmd)`.
- **Quy tắc chuyển trạng thái từng bước:**
  1. **Khi nhận `KeyPressed(key)`:**
     - Nếu `ActiveModal` đang mở: Chuyển tiếp phím tới bộ điều phối Modal (ví dụ: `y` là đồng ý, `n` hoặc `Esc` là hủy).
     - Nếu `IsFilterActive` là đúng: Ghép ký tự vào `FilterQuery`, cập nhật `FilteredContainers`, điều chỉnh `SelectedIndex = 0`.
     - Nếu ở chế độ duyệt thông thường:
       - Phím `j` hoặc `Down`: Tăng `SelectedIndex` lên 1 (không vượt quá tổng số hàng trừ 1). Kích hoạt lệnh tải Inspect chi tiết cho container mới chọn.
       - Phím `k` hoặc `Up`: Giảm `SelectedIndex` đi 1 (không nhỏ hơn 0). Kích hoạt tải Inspect chi tiết.
       - Phím `s`: Xác định container tại vị trí hiện tại. Nếu đang `Running`, phát sinh `RequestAction(Stop, id)`. Nếu đang `Exited`, phát sinh `RequestAction(Start, id)`.
       - Phím `d`: Mở hộp thoại `ModalState.ConfirmActionDialog` yêu cầu người dùng xác nhận xóa.
       - Phím `r`: Phát sinh `RequestAction(Restart, id)`.
       - Phím `p`: Nếu đang `Running`, phát sinh `Pause`. Nếu đang `Paused`, phát sinh `Unpause`.
       - Phím `e`: Phát sinh lệnh `RequestAction(ExecShell, id)`.
       - Phím `/`: Đặt `IsFilterActive = true` và chuyển tiêu điểm vào ô nhập từ khóa.
       - Phím `q`: Gửi lệnh thoát ứng dụng.
  2. **Khi nhận `RequestAction(action, id)`:**
     - Kiểm tra nếu `action` là `ExecShell`: Trả về `model` kèm chỉ thị đặc biệt `Cmd.ExecuteShell(id)`.
     - Với các action khác: Đánh dấu trạng thái tạm thời của container đó trong bộ nhớ để hiển thị biểu tượng tiến trình, đồng thời phát sinh chỉ thị gọi API Socket `Cmd.PerformContainerAction(action, id)`.
  3. **Khi nhận `ActionExecuted(result)`:**
     - Nếu thành công: Phát sinh tiếp lệnh tải lại toàn bộ danh sách container `Cmd.FetchContainers` để đồng bộ thực tế.
     - Nếu thất bại: Mở hộp thoại `ModalState.ErrorAlertDialog` chứa nội dung lỗi chi tiết.

---

### 4.2. Thuật toán Phân bổ Giao diện Thích ứng (Responsive Engine)
- **Đầu vào:** Chiều rộng màn hình `width: int`, chiều cao màn hình `height: int`.
- **Đầu ra:** Cấu trúc kế hoạch bố cục `LayoutPlan` (vị trí X, Y, Width, Height của từng khung nhìn).
- **Quy trình tính toán từng bước:**
  1. **Xác định Ngưỡng Breakpoint:**
     - Nếu `width < 80` hoặc `height < 24`: Gán chế độ `LayoutMode.Compact`.
     - Nếu `80 <= width <= 120`: Gán chế độ `LayoutMode.Standard`.
     - Nếu `width > 120`: Gán chế độ `LayoutMode.Expanded`.
  2. **Phân bổ Tọa độ Khung nhìn:**
     - **TopBar:** `X = 0`, `Y = 0`, `Width = width`, `Height = 1`.
     - **FooterBar:** `X = 0`, `Y = height - 1`, `Width = width`, `Height = 1`.
     - Không gian làm việc khả dụng: `AvailableHeight = height - 2`.
  3. **Phân bổ Khung làm việc theo Chế độ:**
     - **Nếu ở chế độ `Compact`:**
       - Áp dụng kỹ thuật màn hình xếp chồng (Single Active View).
       - Khung nào đang giữ tiêu điểm (`CurrentFocus`) sẽ chiếm toàn bộ không gian làm việc: `X = 0`, `Y = 1`, `Width = width`, `Height = AvailableHeight`.
       - Khung còn lại được ẩn hoàn toàn khỏi chu trình vẽ để tránh tràn bộ đệm màn hình.
     - **Nếu ở chế độ `Standard` hoặc `Expanded`:**
       - Áp dụng bố cục song song Master-Detail.
       - Tính chiều rộng khung trái: `SidebarWidth = (width * 38) / 100`. Đảm bảo giá trị nằm trong khoảng từ tối thiểu 28 cột đến tối đa 50 cột.
       - Khung trái (Sidebar): `X = 0`, `Y = 1`, `Width = SidebarWidth`, `Height = AvailableHeight`.
       - Khung phải (Detail): `X = SidebarWidth`, `Y = 1`, `Width = width - SidebarWidth`, `Height = AvailableHeight`.
  4. **Cắt tỉa Cột Dữ liệu trên Danh sách (Column Truncation):**
     - Ở chế độ `Standard`: Ẩn cột ngày tạo (`Created`) và cột Image ID, chỉ hiển thị Cột Trạng thái, Tên container và Cổng mạng.
     - Ở chế độ `Compact`: Chỉ hiển thị Cột Trạng thái và Tên container; rút gọn tên container bằng dấu ba chấm nếu dài quá chiều rộng khả dụng.

---

### 4.3. Thuật toán Cơ chế Bọc Tiến trình Interactive Shell (Exec Shell PTY)
- **Đầu vào:** `containerId: string`.
- **Đầu ra:** `Result<int, DomainError>` (Mã thoát của tiến trình shell).
- **Thuật toán thực thi an toàn từng bước:**
  1. Kiểm tra trạng thái hiện tại của container: Nếu container không ở trạng thái `Running`, hủy thao tác và trả về lỗi `ERR_CNT_NOT_RUNNING`.
  2. Lưu trữ toàn bộ trạng thái bộ điều khiển terminal hiện tại (`termios` gốc của hệ thống máy host).
  3. Gọi lệnh tạm dừng trình điều khiển giao diện `Application.Driver.Suspend()`.
  4. Xóa trắng màn hình terminal máy host và đưa con trỏ chuột về trạng thái hiển thị bình thường.
  5. Cấu hình đối tượng khởi tạo tiến trình hệ điều hành:
     - Tệp thực thi: `podman`.
     - Danh sách đối số: `["exec", "-it", containerId, "/bin/sh"]`.
     - Chuyển hướng dòng dữ liệu: Tắt toàn bộ chuyển hướng dòng dữ liệu để kết nối trực tiếp `stdin`, `stdout`, `stderr` của tiến trình con với terminal vật lý của người dùng.
  6. Khởi động tiến trình con và theo dõi trong khối xử lý ngoại lệ tối cao (`try ... finally`):
     - Chờ tiến trình kết thúc hoàn toàn bằng lệnh đồng bộ `process.WaitForExit()`.
     - Lưu lại mã thoát `exitCode = process.ExitCode`.
     - Nếu xảy ra lỗi không tìm thấy shell `/bin/sh`, tự động kích hoạt tiến trình thay thế với đối số `["exec", "-it", containerId, "/bin/bash"]`.
  7. **Khối bắt buộc dọn dẹp và hoàn nguyên (`Finally`):**
     - Hoàn nguyên nguyên trạng cấu hình `termios` gốc cho terminal máy host.
     - Gọi lệnh khôi phục trình điều khiển giao diện `Application.Driver.Resume()`.
     - Gửi thông điệp yêu cầu Terminal.Gui vẽ lại toàn bộ bộ đệm khung hình màn hình (`Application.Refresh()`).
     - Trả về mã kết quả `Ok(exitCode)`.

---

## 5. MA TRẬN MÃ LỖI & KỊCH BẢN XỬ LÝ NGOẠI LỆ (ERROR MATRIX)

| Mã Lỗi | Tên Định Danh | Tình Huống Kích Hoạt | Phản Ứng Hệ Thống | Thông Báo Hiển Thị Người Dùng |
| :---: | :--- | :--- | :--- | :--- |
| **`ERR_CNT_404`** | `ContainerNotFound` | Container đã bị xóa bởi tiến trình khác bên ngoài giao diện | Xóa container khỏi danh sách bộ nhớ cục bộ, chuyển con trỏ về hàng trước đó | "Container không còn tồn tại trên hệ thống. Danh sách đã được tự động làm mới." |
| **`ERR_CNT_CONFLICT`** | `ContainerActionConflict` | Ra lệnh Start một container đã Running, hoặc Stop một container đã dừng | Giữ nguyên trạng thái, hủy bỏ hiệu ứng tải | "Thao tác không hợp lệ với trạng thái hiện tại của container." |
| **`ERR_CNT_TIMEOUT`** | `StopOperationTimeout` | Container không thể dừng graceful sau 10 giây chờ | Mở modal hỏi người dùng có muốn cưỡng chế dừng (SIGKILL) không | "Container không phản hồi lệnh dừng an toàn sau 10 giây. Bạn có muốn cưỡng chế dừng (Force Kill) không?" |
| **`ERR_SHELL_FAIL`** | `ShellSpawnFailed` | Container không có sẵn `/bin/sh` hoặc `/bin/bash` | Khôi phục TUI ngay lập tức, không gây treo ứng dụng | "Không thể khởi tạo phiên làm việc dòng lệnh: Container không có sẵn shell tương thích." |
| **`ERR_SOCKET_DISC`** | `SocketDisconnected` | Podman socket bị ngắt kết nối đột ngột trong khi đang duyệt | Chuyển sang màn hình cảnh báo mất kết nối, tự động kích hoạt bộ đếm thử lại sau 3s | "Mất kết nối với Podman Engine Socket. Đang tự động kết nối lại..." |

---

## 6. ĐẶC TẢ BẢNG PHÍM TẮT & TƯƠNG TÁC NGƯỜI DÙNG (KEYBINDINGS SPECIFICATION)

Toàn bộ thao tác cốt lõi hỗ trợ cơ chế 1 phím nhấn (1-Keypress Quick Actions) mô phỏng trải nghiệm người dùng hiện đại:

| Phím Nhấn | Ngữ Cảnh Kích Hoạt | Hành Động Thực Hiện | Phản Hồi Trực Quan |
| :---: | :--- | :--- | :--- |
| `1` .. `5` | Toàn cục | Chuyển danh mục: 1: Pods, 2: Containers, 3: Images, 4: Volumes, 5: Networks | Đổi thẻ được bôi sáng trên thanh TopBar. |
| `j` / `Down` | Danh sách Container | Di chuyển con trỏ xuống hàng tiếp theo | Highlight dòng mới, cập nhật nội dung Inspect bên phải. |
| `k` / `Up` | Danh sách Container | Di chuyển con trỏ lên hàng phía trên | Highlight dòng mới, cập nhật nội dung Inspect bên phải. |
| `s` | Trên Container đang chọn | Đảo trạng thái: Start (nếu đang dừng) hoặc Stop (nếu đang chạy) | Hiển thị biểu tượng đồng hồ cát chờ xử lý. |
| `d` | Trên Container đang chọn | Xóa container | Mở Popup Modal màu đỏ yêu cầu nhấn `y` để xác nhận xóa. |
| `r` | Trên Container đang chọn | Khởi động lại container (Restart) | Biểu tượng xoay vòng hiển thị trên trạng thái. |
| `p` | Trên Container đang chọn | Tạm dừng (Pause) hoặc Tiếp tục (Unpause) | Đổi nhãn trạng thái sang Paused/Running tương ứng. |
| `e` | Trên Container đang chạy | Mở phiên Interactive Shell PTY | Tạm dừng TUI, chuyển toàn quyền cho Shell bên trong container. |
| `Tab` | Khung giao diện chính | Luân chuyển tiêu điểm giữa Danh sách bên trái và Chi tiết bên phải | Đổi màu viền khung nhận tiêu điểm sang màu Cyan sáng. |
| `]` / `[` | Khung Chi tiết | Chuyển đổi qua lại giữa các tab: Logs -> Inspect -> Top -> Env | Đổi tab hiển thị dữ liệu tương ứng. |
| `/` | Danh sách Container | Mở thanh nhập liệu từ khóa tìm kiếm | Con trỏ nhảy vào ô Search ở chân danh sách. |
| `Esc` | Hộp thoại Modal / Search | Hủy bỏ thao tác, đóng modal hoặc thoát chế độ lọc từ khóa | Trả giao diện về trạng thái bình thường. |
| `q` | Toàn cục | Thoát ứng dụng hoàn toàn | Dọn dẹp tài nguyên và trả lại terminal sạch cho host. |

---

## 7. MA TRẬN KỊCH BẢN KIỂM THỬ NGHIỆM THU (TEST CASES MATRIX)

| Mã Ca Kiểm Thử | Tên Kịch Bản Kiểm Thử | Điều Kiện Tiền Đề | Các Bước Thực Hiện | Tiêu Chí Đạt Nghiệm Thu (Pass Criteria) |
| :---: | :--- | :--- | :--- | :--- |
| **TC-M2-01** | Khởi động giao diện Dashboard hoàn chỉnh | Có ít nhất 1 container đang chạy và 1 container đã dừng | Chạy ứng dụng bằng lệnh thực thi | Màn hình hiển thị đầy đủ TopBar, Sidebar, DetailPane, FooterBar; thông tin container khớp với lệnh `podman ps -a`. |
| **TC-M2-02** | Thao tác 1 phím Start/Stop container | Chọn 1 container đang ở trạng thái `Exited` | Nhấn phím `s` | Trạng thái chuyển sang `Running` trong vòng dưới 2 giây mà không cần bấm thêm phím xác nhận; màu chuyển từ Xám sang Xanh lá. |
| **TC-M2-03** | Khởi tạo phiên Interactive Shell và khôi phục an toàn | Chọn container đang `Running` có sẵn shell | Nhấn phím `e`, thực hiện gõ lệnh `ls` trong container, sau đó gõ `exit` | Truy cập thành công vào shell container; sau khi `exit`, giao diện TUI tự động vẽ lại hoàn chỉnh 100%, không bị méo lệch ký tự. |
| **TC-M2-04** | Co giãn cửa sổ Terminal thích ứng (Responsive Test) | Đang mở Dashboard ở kích thước 120 cột | Kéo thu nhỏ cửa sổ terminal xuống dưới 80 cột | Giao diện tự động chuyển sang chế độ Single View không bị vỡ bố cục, không gây văng ứng dụng (Zero Crash). |
| **TC-M2-05** | Tìm kiếm & Lọc mờ container theo tên | Danh sách có 5 container với các tiền tố tên khác nhau | Nhấn phím `/`, gõ từ khóa một phần của tên container | Danh sách lọc tức thời chỉ hiển thị các container thỏa mãn điều kiện, `SelectedIndex` tự động reset về hàng đầu tiên. |
| **TC-M2-06** | Hộp thoại xác nhận xóa an toàn | Chọn container đang dừng | Nhấn phím `d`, xuất hiện modal; thử nhấn `Esc` rồi nhấn lại `d` và bấm `y` | Khi bấm `Esc`, modal đóng và không có hành động xóa; khi bấm `y`, container biến mất khỏi danh sách. |

---

## 8. KẾT LUẬN & ĐIỀU KIỆN CHUYỂN BƯỚC THỰC THI

Bản Thiết kế Chi tiết này hoàn thiện toàn bộ cơ sở lý luận, kiến trúc luồng dữ liệu MVU, danh mục tệp, bảng hợp đồng dữ liệu nguyên tử, thuật toán từng bước và ma trận kiểm thử cho Milestone 2. 

Toàn bộ quy chuẩn đảm bảo tính tương thích tuyệt đối với triết lý F# hàm thuần khiết và thiết kế co giãn không cố định độ phân giải theo yêu cầu cốt lõi của dự án.
