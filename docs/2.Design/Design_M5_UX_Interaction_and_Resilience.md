# BẢN THIẾT KẾ CHI TIẾT (DETAIL DESIGN): MILESTONE 5
## TRẢI NGHIỆM NGƯỜI DÙNG CHUYÊN SÂU, TỰ PHỤC HỒI LỖI & BẢN ĐỊA HÓA (I18N)

- **Mã tài liệu:** DD-M5-UX-RESILIENCE
- **Vị trí lưu trữ:** `docs/2.Design/Design_M5_UX_Interaction_and_Resilience.md`
- **Phiên bản:** 2.0.0 (Nâng cấp toàn diện từ Basic Design lên Detail Design)
- **Ngày phê duyệt:** 2026-10-05
- **Tài liệu căn cứ:** 
  - [`SRS_podman-FUI.md`](../1.Investigation/SRS_podman-FUI.md)
  - [`02_UI_Framework_and_Rendering_Investigation.md`](../1.Investigation/02_UI_Framework_and_Rendering_Investigation.md)
  - [`05_Packaging_and_Cross_Distro_Investigation.md`](../1.Investigation/05_Packaging_and_Cross_Distro_Investigation.md)
  - [`06_Internationalization_i18n_Investigation.md`](../1.Investigation/06_Internationalization_i18n_Investigation.md)
- **Tài liệu tiến độ gắn kèm:** [`Milestone_5_UX_Polish_and_Cross_Distro.md`](../3.Progress/Milestone_5_UX_Polish_and_Cross_Distro.md)

---

## 1. TỔNG QUAN & PHẠM VI THIẾT KẾ CHI TIẾT

Tài liệu này đặc tả chi tiết kiến trúc tầng hiển thị, tương tác và cơ chế chịu lỗi tự động cho Milestone 5:
- Hệ thống điều phối sự kiện bàn phím nâng cao (chuẩn Vim-keys), chuột đa điểm (Click, Scroll, Kéo splitter chia màn hình), và bảng tìm kiếm lệnh nhanh (Command Palette).
- Máy trạng thái tự phục hồi kết nối Unix Socket ngầm (Exponential Backoff Watchdog) đảm bảo ứng dụng không bao giờ bị sập khi dịch vụ hệ điều hành khởi động lại.
- Hệ thống bản địa hóa đa ngôn ngữ (i18n) với cấu trúc phân cấp, cơ chế dự phòng an toàn (Fallback), và thuật toán xử lý độ rộng ký tự đôi CJK (East Asian Width) cho tiếng Nhật (`ja-JP`).
- **Tuân thủ tuyệt đối:** Trình bày hoàn toàn bằng lời văn, bảng biểu, quy trình thuật toán tuần tự, không sử dụng code sample (Zero Code Sample).

---

## 2. PHÂN RÃ DANH MỤC TỆP & MÔ-ĐUN MÃ NGUỒN (MODULE INVENTORY)

Milestone 5 bổ sung các tệp chức năng vào cấu trúc 4 dự án của Solution:

### 2.1. Dự án `PodmanFUI.Domain`
- **Tệp: `I18nModels.fs` (Thực thể miền Bản địa hóa):**
  - Chứa kiểu mã ngôn ngữ: `LanguageCode` (`EnUS`, `ViVN`, `JaJP`).
  - Chứa kiểu danh mục tài nguyên ngôn ngữ: `LocalizationCatalog`.
  - Chứa khóa tra cứu tài nguyên chuỗi: `I18nKey`.
- **Tệp: `KeymapModels.fs` (Thực thể miền Bàn phím & Lệnh):**
  - Chứa kiểu phân loại ngữ cảnh phím: `KeyContext` (`Global`, `ListNavigation`, `DetailPanel`, `ModalInput`).
  - Chứa bản ghi mô tả hành động lệnh: `CommandActionItem`.
- **Tệp: `ResilienceModels.fs` (Thực thể miền Tự phục hồi):**
  - Chứa kiểu trạng thái giám sát kết nối: `ConnectionState` (`Connected`, `Reconnecting`, `PermanentlyFailed`).
  - Chứa cấu hình độ trễ thử lại: `BackoffConfig`.
- **Tệp: `II18nService.fs` & `IWatchdogService.fs`:**
  - Định nghĩa hợp đồng interface quản lý ngôn ngữ và giám sát kết nối nền.

### 2.2. Dự án `PodmanFUI.Infrastructure`
- **Tệp: `JsonI18nProvider.fs` (Nạp và phân giải tệp ngôn ngữ JSON):**
  - Nạp các file tài nguyên từ thư mục `locales/en.json`, `locales/vi.json`, `locales/ja.json`.
  - Cơ chế dự phòng fallback: Nếu thiếu khóa ở ngôn ngữ đang chọn, tự động nạp chuỗi từ `en.json`.
- **Tệp: `UnicodeWidthCalculator.fs` (Bộ tính toán độ rộng ký tự chuẩn CJK):**
  - Áp dụng chuẩn Unicode Standard Annex #11 (East Asian Width). Phân biệt ký tự độ rộng đơn (1 cột) và độ rộng đôi (2 cột).
- **Tệp: `SocketWatchdogService.fs` (Tiến trình giám sát nhịp tim Socket):**
  - Chạy tiến trình ngầm định kỳ 2 giây kiểm tra trạng thái hoạt động của socket bằng lệnh Ping siêu nhẹ.
  - Quản lý thuật toán Exponential Backoff tự động kết nối lại.

### 2.3. Dự án `PodmanFUI.Presentation`
- **Tệp: `KeymapDispatcher.fs` (Bộ phân phối phím tắt thông minh):**
  - Tiếp nhận phím bấm từ Terminal.Gui, so khớp với ngữ cảnh đang kích hoạt để thực thi hành động tương ứng.
- **Tệp: `MouseGestureHandler.fs` (Bộ xử lý thao tác chuột):**
  - Bắt các sự kiện click danh mục, click chọn dòng, con lăn chuột cuộn log, và thao tác kéo thả thanh chia tỷ lệ (Splitter).
- **Tệp: `Views/CommandPaletteModal.fs` (Hộp thoại tìm kiếm lệnh nhanh):**
  - Mở khi người dùng nhấn `m` hoặc `F10`; cung cấp thanh tìm kiếm mờ toàn bộ các tính năng của phần mềm.
- **Tệp: `Views/ConnectionLossOverlay.fs` (Màn hình cảnh báo mất kết nối):**
  - Hiển thị lớp phủ màu đỏ nhạt khi socket bị đứt, kèm bộ đếm lùi thời gian thử kết nối lại.

---

## 3. ĐẶC TẢ CHI TIẾT HỢP ĐỒNG DỮ LIỆU (DATA CONTRACTS SPECIFICATION)

### 3.1. Hợp đồng `LocalizationCatalog` (Từ điển Bản địa hóa)

| Tên trường | Kiểu dữ liệu | Bắt buộc | Mô tả & Quy tắc kiểm tra |
| :--- | :--- | :---: | :--- |
| `CurrentLanguage` | `LanguageCode` | Có | Ngôn ngữ đang được áp dụng trên toàn giao diện. |
| `StringMap` | `Map<string, string>` | Có | Bảng băm chứa các cặp `Khóa Tài Nguyên -> Chuỗi Bản Dịch`. |
| `FallbackMap` | `Map<string, string>` | Có | Bảng băm chuỗi gốc tiếng Anh dùng khi khóa dịch bị thiếu. |

### 3.2. Hợp đồng `CommandActionItem` (Thực thể Mục Lệnh trong Palette)

| Tên trường | Kiểu dữ liệu | Bắt buộc | Mô tả |
| :--- | :--- | :---: | :--- |
| `Id` | `string` | Có | Định danh duy nhất của lệnh (ví dụ: `"cmd.container.start"`). |
| `Title` | `string` | Có | Tên lệnh hiển thị cho người dùng (ví dụ: `"Start Container"`). |
| `Category` | `string` | Có | Nhóm chức năng (ví dụ: `"Container Operations"`, `"Navigation"`). |
| `ShortcutKey` | `string option` | Không | Chuỗi ký tự phím tắt tương ứng nếu có (ví dụ: `"s"`, `"Ctrl+R"`). |
| `TargetMsg` | `DashboardMsg` | Có | Thông điệp sự kiện MVU sẽ được phát sinh khi chọn lệnh này. |

### 3.3. Hợp đồng `WatchdogState` (Trạng thái Giám sát Nhịp tim Socket)

| Tên trường | Kiểu dữ liệu | Giá trị mặc định | Mô tả |
| :--- | :--- | :---: | :--- |
| `Status` | `ConnectionState` | `Connected` | Tình trạng liên lạc với Unix Domain Socket. |
| `ConsecutiveFailures`| `int` | `0` | Số lần thất bại liên tiếp khi gửi tín hiệu ping. |
| `CurrentBackoffDelay`| `TimeSpan` | `1 giây` | Khoảng thời gian chờ trước lần thử kết nối tiếp theo. |
| `NextAttemptAt` | `DateTimeOffset option` | `None` | Mốc thời gian dự kiến cho lần thử kết nối lại kế tiếp. |

---

## 4. ĐẶC TẢ CHI TIẾT THUẬT TOÁN & TỪNG HÀM NGHIỆP VỤ

### 4.1. Thuật toán Tự Phục hồi Kết nối với Exponential Backoff (Watchdog Loop)
- **Đầu vào:** `socketPath: string`, `cancellationToken: CancellationToken`.
- **Đầu ra:** Luồng các thông điệp trạng thái kết nối `AsyncSeq<ConnectionState>`.
- **Quy trình tuần tự từng bước:**
  1. Khởi tạo `delay = 1.0 giây`, `maxDelay = 8.0 giây`, `failureCount = 0`.
  2. **Vòng lặp giám sát (Heartbeat Loop):**
     - Thực hiện gửi yêu cầu siêu nhẹ `GET /v4.0.0/libpod/_ping` với thời gian timeout ngắn (1.5 giây).
     - **Nếu phản hồi thành công (HTTP 200 OK):**
       - Nếu trạng thái trước đó là `Reconnecting`: Phát sinh thông điệp `ConnectionState.Connected` để giao diện khôi phục hoạt động, kích hoạt làm mới toàn bộ danh mục tài nguyên.
       - Đặt lại `failureCount = 0`, `delay = 1.0 giây`.
       - Chờ 2.0 giây trước lượt ping định kỳ tiếp theo.
     - **Nếu phản hồi thất bại (Socket đóng, Timeout, hoặc File bị xóa):**
       - Tăng `failureCount = failureCount + 1`.
       - Phát sinh thông điệp `ConnectionState.Reconnecting(failureCount, delay)`.
       - Chờ đợi một khoảng thời gian bằng `delay` hiện tại.
       - Tăng gấp đôi độ trễ cho lần sau: `delay = min (delay * 2.0) maxDelay`.
  3. Lặp lại bước 2 cho đến khi ứng dụng kết thúc.

---

### 4.2. Thuật toán Phân giải Ký tự Đôi CJK (East Asian Width Engine)
- **Đầu vào:** Chuỗi văn bản Unicode `text: string`.
- **Đầu ra:** Tổng số cột hiển thị thực tế trên màn hình `displayWidth: int`.
- **Quy trình tính toán từng bước:**
  1. Phân tách chuỗi `text` thành danh sách các Rune Unicode độc lập (`System.Text.Rune`).
  2. Khởi tạo `totalColumns = 0`.
  3. Với mỗi `rune` trong chuỗi:
     - Lấy điểm mã Unicode (`codePoint = rune.Value`).
     - **Nhóm Ký tự Rộng Đôi (Độ rộng = 2 cột):**
       - Khối chữ tượng hình CJK Unified Ideographs (`0x4E00` đến `0x9FFF`).
       - Bảng chữ cái tiếng Nhật Hiragana (`0x3040` đến `0x309F`) và Katakana (`0x30A0` đến `0x30FF`).
       - Bảng mã chữ Hangul tiếng Hàn (`0xAC00` đến `0xD7AF`).
       - Các ký tự khối Fullwidth ASCII (`0xFF01` đến `0xFF60`).
       - Ký tự biểu cảm Emoji nhiều màu sắc.
       - Nếu thuộc các nhóm này: Tăng `totalColumns = totalColumns + 2`.
     - **Nhóm Ký tự Không Chiếm Cột (Độ rộng = 0 cột):**
       - Dấu kết hợp (Combining Diacritical Marks: `0x0300` đến `0x036F`).
       - Ký tự điều khiển vô hình (Zero-Width Joiner/Non-Joiner: `0x200B` đến `0x200D`).
       - Không tăng `totalColumns`.
     - **Nhóm Ký tự Đơn Tiêu chuẩn (Độ rộng = 1 cột):**
       - Chữ cái Latinh, chữ số, dấu câu thông thường, và ký tự tiếng Việt có dấu.
       - Tăng `totalColumns = totalColumns + 1`.
  4. Trả về `totalColumns`. Mọi phép tính toán độ rộng khung viền của Terminal.Gui đều sử dụng kết quả từ hàm này để đảm bảo lưới hiển thị không bị xô lệch trên tiếng Nhật.

---

### 4.3. Thuật toán Tra cứu Tài nguyên Ngôn ngữ với Cơ chế Dự phòng (i18n Lookup)
- **Đầu vào:** `key: string`, `catalog: LocalizationCatalog`.
- **Đầu ra:** Chuỗi bản dịch hoàn chỉnh `resultText: string`.
- **Quy trình tra cứu từng bước:**
  1. Tìm kiếm `key` trong từ điển của ngôn ngữ hiện tại `catalog.StringMap`.
  2. Nếu tìm thấy: Trả về chuỗi bản dịch tương ứng.
  3. Nếu không tìm thấy:
     - Ghi cảnh báo nội bộ `Warning: Missing localization key [key] for language [CurrentLanguage]`.
     - Tìm kiếm `key` trong từ điển dự phòng tiếng Anh `catalog.FallbackMap`.
     - Nếu tìm thấy trong từ điển dự phòng: Trả về chuỗi tiếng Anh kèm định dạng cảnh báo nhẹ nếu ở chế độ gỡ lỗi.
  4. Nếu cả trong từ điển tiếng Anh cũng không có: Trả về chính chuỗi `key` (ví dụ: `"[missing: btn.container.start]"`) để tránh làm sập ứng dụng.

---

### 4.4. Thuật toán Kéo Thả Thanh Phân Cách (Splitter Drag Logic)
- **Đầu vào:** Sự kiện di chuyển chuột `MouseEvent` có nút chuột trái đang giữ.
- **Đầu ra:** Cập nhật tỷ lệ chiều rộng `SidebarWidth`.
- **Quy trình xử lý từng bước:**
  1. Kiểm tra tọa độ X của con trỏ chuột `mouse.X`.
  2. Đảm bảo độ rộng khung trái nằm trong ngưỡng an toàn:
     - Giới hạn tối thiểu: Không được nhỏ hơn `25 cột` (đủ để xem tên container).
     - Giới hạn tối đa: Không được lớn hơn `Tổng chiều rộng terminal - 40 cột` (để dành không gian cho panel chi tiết).
  3. Gán `SidebarWidth = mouse.X`.
  4. Phát sinh lệnh vẽ lại giao diện bố cục tức thời để phản hồi theo chuyển động chuột của người dùng một cách mượt mà (60 FPS).

---

## 5. MA TRẬN MÃ LỖI & KỊCH BẢN XỬ LÝ NGOẠI LỆ (ERROR MATRIX)

| Mã Lỗi | Tên Định Danh | Nguyên Nhân Gốc | Phản Ứng Hệ Thống | Thông Báo Hiển Thị |
| :---: | :--- | :--- | :--- | :--- |
| **`ERR_I18N_MISSING`** | `TranslationKeyMissing` | File ngôn ngữ JSON thiếu một khóa mới bổ sung | Tự động lấy chuỗi từ bản gốc tiếng Anh, không gây crash giao diện | "[Warning] Tự động hiển thị fallback chuỗi tiếng Anh" |
| **`ERR_WATCHDOG_DEAD`** | `WatchdogExhausted` | Socket bị dừng liên tục quá 10 phút không thể phục hồi | Hiển thị hộp thoại lỗi nghiêm trọng, cho phép thoát an toàn | "[bold red]Không thể tái kết nối Podman Socket sau nhiều lần thử. Nhấn 'q' để thoát.[/]" |
| **`ERR_CJK_OVERFLOW`** | `TextWidthOverflow` | Tên container chứa nhiều chữ Kanji làm tràn độ rộng cột | Cắt ngắn chuỗi dựa trên số cột thực tế thay vì số ký tự, thêm dấu `..` | Tên được rút gọn tự nhiên, viền khung được giữ thẳng hàng |
| **`ERR_PALETTE_NO_MATCH`**| `CommandPaletteEmpty` | Từ khóa tìm kiếm lệnh không khớp với bất kỳ thao tác nào | Hiển thị thông báo rỗng trong danh sách, không gây lỗi con trỏ | "[bold grey]Không tìm thấy lệnh nào phù hợp với từ khóa[/]" |

---

## 6. ĐẶC TẢ BẢNG CHỌN LỆNH NHANH (COMMAND PALETTE UI SPECIFICATION)

Khi người dùng nhấn phím `m` hoặc `F10`:
- **Vị trí hiển thị:** Cửa sổ Modal nổi chính giữa màn hình, chiều rộng chiếm 60% terminal, chiều cao tối đa 16 dòng.
- **Thành phần giao diện:**
  - Ô nhập liệu tìm kiếm ở trên cùng: Tiêu đề `Type a command or shortcut...` với biểu tượng kính lúp `🔍`.
  - Danh sách lệnh bên dưới: Hiển thị 2 cột:
    - Cột 1: Tên hành động (Ví dụ: `Start Selected Container`, `View Container Logs`, `Prune Unused Volumes`).
    - Cột 2: Phím tắt tương ứng được đóng khung (Ví dụ: `[ s ]`, `[ ] ]`, `[ p ]`).
- **Cơ chế điều khiển:** Phím mũi tên `Up`/`Down` hoặc `j`/`k` để chọn lệnh, phím `Enter` để thực thi ngay lập tức, phím `Esc` để đóng modal.

---

## 7. MA TRẬN KỊCH BẢN KIỂM THỬ NGHIỆM THU (TEST CASES MATRIX)

| Mã Ca Kiểm Thử | Tên Kịch Bản Kiểm Thử | Điều Kiện Tiền Đề | Các Bước Thực Hiện | Tiêu Chí Đạt Nghiệm Thu (Pass Criteria) |
| :---: | :--- | :--- | :--- | :--- |
| **TC-M5-01** | Điều hướng mượt mà hoàn toàn bằng Vim-keys | Đang ở màn hình Dashboard chính | Sử dụng các phím `j`, `k` để di chuyển, `h`, `l` để đổi panel, `g`, `G` để về đầu/cuối | Con trỏ di chuyển chính xác 100%, không bị trễ hoặc nhảy cóc dòng; panel tiêu điểm đổi màu viền ngay lập tức. |
| **TC-M5-02** | Tự động phục hồi kết nối khi Podman Socket bị ngắt | Ứng dụng đang chạy bình thường | Mở terminal khác gõ `systemctl --user stop podman.socket`; đợi 3s rồi gõ `start` lại | Ứng dụng lập tức chuyển sang overlay cảnh báo màu đỏ; sau khi bật lại socket, ứng dụng tự động kết nối lại thành công không cần bật lại app. |
| **TC-M5-03** | Hiển thị chuẩn xác tiếng Nhật không bị xô lệch viền | Đổi ngôn ngữ sang tiếng Nhật (`ja-JP`) hoặc đặt tên container bằng chữ Kanji | Quan sát các đường viền khung bảng và độ thẳng hàng của các cột | Các cột bảng thẳng hàng tuyệt đối, viền góc không bị xô lệch hay thụt thò; ký tự CJK chiếm chuẩn xác đúng 2 ô hiển thị. |
| **TC-M5-04** | Tìm kiếm và thực thi từ Command Palette | Đang ở bất kỳ màn hình nào | Nhấn `m`, gõ từ khóa `"restart"`, nhấn `Enter` | Danh sách lọc tức thời ra lệnh `Restart Container [ r ]`; sau khi nhấn Enter, thao tác restart được thực thi ngay trên thực thể đang chọn. |
| **TC-M5-05** | Kéo thả chuột thay đổi kích thước Splitter | Sử dụng terminal có hỗ trợ chuột | Nhấn giữ chuột trái tại thanh phân cách giữa 2 panel và kéo sang phải | Chiều rộng panel trái mở rộng mượt mà theo vị trí con trỏ chuột, tỷ lệ co giãn cập nhật liên tục mà không làm vỡ văn bản. |

---

## 8. KẾT LUẬN & ĐIỀU KIỆN CHUYỂN BƯỚC THỰC THI

Bản Thiết kế Chi tiết này hoàn thiện toàn bộ các yêu cầu tinh chỉnh trải nghiệm người dùng, khả năng tự hồi phục lỗi kết nối, tính năng i18n và tương thích đa phân giải cho Milestone 5.

Toàn bộ tài liệu đảm bảo không sử dụng mã nguồn lập trình, đáp ứng trọn vẹn tiêu chuẩn nghiệm thu của dự án.
