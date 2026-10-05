# ĐIỀU TRA KỸ THUẬT 06: KIẾN TRÚC ĐA NGÔN NGỮ & BẢN ĐỊA HÓA (I18N & L10N)
## DỰ ÁN: PODMAN-FUI

- **Mã tài liệu:** INV-06-I18N-LOCALIZATION
- **Trạng thái:** Hoàn thành điều tra
- **Mục tiêu:** Phân tích kiến trúc hỗ trợ đa ngôn ngữ (EN, VI, JA), cơ chế quản lý tệp tài nguyên chuỗi ký tự, các thách thức đặc thù về độ rộng ký tự CJK (Ký tự đôi Nhật Bản) trên lưới terminal, và quy hoạch lộ trình triển khai.

---

## 1. QUY HOẠCH NGÔN NGỮ & PHẠM VI TRIỂN KHAI

### 1.1. Ma trận Ngôn ngữ Mục tiêu

| Ngôn ngữ | Mã chuẩn (ISO) | Vai trò trong Dự án | Trạng thái Thực thi |
| :--- | :--- | :--- | :--- |
| **Tiếng Anh (English)** | `en-US` | Ngôn ngữ thực thi mặc định (Default Execution Language) | **Kích hoạt 100% ở Giai đoạn 1 (Phase 1 / MVP)** |
| **Tiếng Việt (Vietnamese)** | `vi-VN` | Ngôn ngữ bản địa hóa & Ngôn ngữ tài liệu dự án | **Sẵn sàng cấu trúc tệp tài nguyên, kích hoạt ở Phase 2** |
| **Tiếng Nhật (Japanese)** | `ja-JP` | Ngôn ngữ bản địa hóa khu vực châu Á | **Sẵn sàng cấu trúc tệp tài nguyên, kích hoạt ở Phase 2** |

### 1.2. Nguyên tắc Tách biệt Phạm vi
- **Phạm vi Giao diện Người dùng (UI Runtime):**
  - Trong giai đoạn hiện tại (Phase 1), toàn bộ các dòng chữ hiển thị trên giao diện (tiêu đề panel, nhãn trạng thái, thông báo lỗi, menu ngữ cảnh, nội dung hộp thoại modal) **chỉ chạy bằng Tiếng Anh (EN)**.
  - Tuyệt đối không hardcode chuỗi ký tự tiếng Anh trực tiếp trong code UI, mà phải đi qua hệ thống khóa tài nguyên (Resource Keys).
- **Phạm vi Tài liệu Kỹ thuật (Project Documentation):**
  - Toàn bộ tài liệu đặc tả yêu cầu (SRS), tài liệu điều tra kỹ thuật (`1.Investigation`), tài liệu thiết kế kiến trúc (`2.Design`), và nhật ký tiến độ (`3.Progress`) **tiếp tục sử dụng hoàn toàn bằng Tiếng Việt**.

---

## 2. KIẾN TRÚC QUẢN LÝ TÀI NGUYÊN BẢN ĐỊA HÓA (I18N ARCHITECTURE)

### 2.1. Cấu trúc Từ điển Khóa - Giá trị (Key-Value Localization Dictionary)
Mỗi chuỗi văn bản trên giao diện được định danh bằng một chuỗi khóa phân cấp logic:

```
[Danh mục].[Màn hình / Khung nhìn].[Tên nhãn]
```

- **Ví dụ phân cấp khóa chuẩn:**
  - `topbar.pods` -> EN: `"Pods"` | VI: `"Pods"` | JA: `"ポッド"`
  - `topbar.containers` -> EN: `"Containers"` | VI: `"Container"` | JA: `"コンテナ"`
  - `status.running` -> EN: `"Running"` | VI: `"Đang chạy"` | JA: `"実行中"`
  - `status.exited` -> EN: `"Exited"` | VI: `"Đã thoát"` | JA: `"停止"`
  - `action.restart` -> EN: `"Restart"` | VI: `"Khởi động lại"` | JA: `"再起動"`
  - `dialog.delete.title` -> EN: `"Delete Confirmation"` | VI: `"Xác nhận xóa"` | JA: `"削除の確認"`
  - `dialog.delete.body` -> EN: `"Are you sure you want to remove this entity?"` | VI: `"Bạn có chắc chắn muốn xóa thực thể này?"` | JA: `"このエンティティを削除してもよろしいですか？"`

### 2.2. Lựa chọn Định dạng Tệp Bản địa hóa
So sánh các định dạng tệp lưu trữ chuỗi dịch:

| Định dạng | Ưu điểm | Nhược điểm | Đánh giá lựa chọn |
| :--- | :--- | :--- | :--- |
| **.resx (.NET standard)** | Chuẩn mực của .NET, hỗ trợ sẵn công cụ Visual Studio | Phải biên dịch thành Satellite Assembly DLL, khó để cộng đồng đóng góp dịch trực tiếp | Không tối ưu cho TUI mã nguồn mở |
| **GNU .po / .mo (Gettext)** | Chuẩn mực trên hệ thống Linux | Cần thêm thư viện phân giải bên ngoài | Khá cồng kềnh |
| **JSON phẳng / Phân cấp (`en.json`, `vi.json`, `ja.json`)** | Dễ đọc, dễ chỉnh sửa bằng bất kỳ editor nào, hỗ trợ tải động (Hot-reload), nhẹ | Cần bộ nạp JSON lúc khởi động | **TỐI ƯU NHẤT (Khuyến nghị lựa chọn)** |

---

## 3. THÁCH THỨC KỸ THUẬT ĐẶC THÙ: ĐỘ RỘNG KÝ TỰ CJK TRÊN TERMINAL

Khác với giao diện đồ họa GUI (nơi văn bản tính theo pixel), giao diện TUI hiển thị trên một lưới ô ký tự (Character Grid). Đây là thách thức kỹ thuật lớn nhất khi hỗ trợ tiếng Nhật (`ja-JP`):

### 3.1. Khái niệm Ký tự Đơn (Single-width) vs Ký tự Kép (Double-width)
- **Ký tự Latinh / Tiếng Anh / Tiếng Việt:** Mỗi ký tự (kể cả có dấu thanh tiếng Việt như `á`, `ớ`, `ệ`) chỉ chiếm đúng **1 ô lưới (1 Monospace Cell - Width = 1)** trên terminal.
- **Ký tự CJK (Tiếng Nhật Kanji, Hiragana, Katakana):** Được định nghĩa theo chuẩn Unicode East Asian Width là dạng `Wide` hoặc `Fullwidth`. Mỗi ký tự chiếm đúng **2 ô lưới (2 Monospace Cells - Width = 2)** trên màn hình terminal.

```
Lưới Terminal:  [ 1 ][ 2 ][ 3 ][ 4 ][ 5 ][ 6 ][ 7 ][ 8 ]
Tiếng Anh:      | P  | o  | d  | s  |    |    |    |    |  -> 4 ký tự = 4 ô
Tiếng Nhật:     |   ポ   |   ッ   |   ド   |    |    |  -> 3 ký tự = 6 ô
```

### 3.2. Nguy cơ Vỡ Khung & Lệch Cột (Layout Disruption)
- Nếu hệ thống tính toán độ dài chuỗi bằng hàm đếm độ dài thông thường (`String.Length`), từ `"ポッド"` sẽ được tính là 3 ký tự.
- Nhưng khi in ra màn hình terminal, nó thực tế chiếm 6 cột. Hậu quả là:
  - Cột bảng bên cạnh bị đẩy lệch sang phải 3 ô.
  - Đường viền khung (`│`) bị vỡ hàng, gây méo mó toàn bộ layout.
- **Giải pháp kỹ thuật yêu cầu:**
  - Module hiển thị phải sử dụng bộ đếm độ rộng hiển thị (Display Column Width / `Rune.ColumnWidth` trong .NET), tính toán chính xác số ô thực tế mà ký tự chiếm dụng thay vì đếm số byte hoặc số ký tự Unicode.
  - Các ô nhập liệu hoặc nút bấm phải tự động tính toán đệm khoảng trắng (Padding) dựa trên Column Width.

### 3.3. Xử lý Dấu Thanh Tiếng Việt (Diacritics & Combining Characters)
- Tiếng Việt sử dụng các ký tự Latinh mở rộng có dấu thanh (`ề`, `ở`, `ậ`, `ứ`).
- Cần đảm bảo hệ thống chuẩn hóa chuỗi về dạng **NFC (Normalization Form C - Canonical Composition)** trước khi render, tránh tình trạng dấu thanh bị tách thành ký tự tổ hợp riêng biệt làm sai lệch độ rộng ô hiển thị.

---

## 4. CHIẾN LƯỢC CO GIÃN ĐỘ DÀI NHÃN (DYNAMIC LABEL PADDING)

Do độ dài của cùng một từ ngữ sẽ khác nhau giữa các ngôn ngữ:
- Ví dụ:
  - Nút Start: Tiếng Anh `"Start"` (5 ô) | Tiếng Việt `"Bắt đầu"` (7 ô) | Tiếng Nhật `"開始"` (4 ô).
  - Nút Delete: Tiếng Anh `"Delete"` (6 ô) | Tiếng Việt `"Xóa"` (3 ô) | Tiếng Nhật `"削除"` (4 ô).

**Nguyên tắc thiết kế giao diện:**
1. Tuyệt đối không cố định kích thước của các nút bấm hoặc nhãn trạng thái (ví dụ không fix cứng `Width = 6`).
2. Kích thước của các nút bấm phải được tính toán tự động:
   $$\text{Button Width} = \text{Text Display Width} + 2 \times \text{Horizontal Padding}$$
3. Đối với các cột trong danh sách (Columns in Tables): Độ rộng của cột phải được tự động co giãn theo giá trị dài nhất của phần tử hoặc tiêu đề cột đó.

---

## 5. KẾT LUẬN & ĐỀ XUẤT CHO BƯỚC THIẾT KẾ (DESIGN)

1. **Khởi tạo Tệp Ngôn ngữ Mẫu:** Xây dựng tệp `en.json` hoàn chỉnh cho toàn bộ các nhãn giao diện của Phase 1.
2. **Cấu trúc Thư mục Tài nguyên:** Tổ chức thư mục `locales/` chứa:
   - `en.json` (Đang kích hoạt).
   - `vi.json` (Bộ khung sẵn sàng cho Phase 2).
   - `ja.json` (Bộ khung sẵn sàng cho Phase 2).
3. **Module Quản trị Bản địa hóa (I18n Service):** Cung cấp hàm tra cứu `translate(key: string): string` có cơ chế fallback về `en.json` nếu một khóa nào đó chưa được dịch ở các ngôn ngữ khác, đảm bảo ứng dụng không bao giờ bị hiển thị chuỗi rỗng.
