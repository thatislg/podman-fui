# BẢN THIẾT KẾ KỸ THUẬT BẰNG LỜI: MILESTONE 5
## THIẾT KẾ TRẢI NGHIỆM NGƯỜI DÙNG, TỰ PHỤC HỒI LỖI & BẢN ĐỊA HÓA
- **Mã tài liệu:** DES-M5-UX-RESILIENCE
- **Vị trí lưu trữ:** `docs/2.Design/Design_M5_UX_Interaction_and_Resilience.md`
- **Phiên bản:** 1.0.0
- **Trạng thái:** Bản thiết kế đề xuất (Draft)
- **Tài liệu căn cứ:** 
  - [`SRS_podman-FUI.md`](../1.Investigation/SRS_podman-FUI.md)
  - [`02_UI_Framework_and_Rendering_Investigation.md`](../1.Investigation/02_UI_Framework_and_Rendering_Investigation.md)
  - [`05_Packaging_and_Cross_Distro_Investigation.md`](../1.Investigation/05_Packaging_and_Cross_Distro_Investigation.md)
  - [`06_Internationalization_i18n_Investigation.md`](../1.Investigation/06_Internationalization_i18n_Investigation.md)
- **Tài liệu tiến độ gắn kèm:** [`Milestone_5_UX_Polish_and_Cross_Distro.md`](../3.Progress/Milestone_5_UX_Polish_and_Cross_Distro.md)

---

## 1. MỤC TIÊU THIẾT KẾ
Tài liệu này đặc tả chi tiết bằng lời các giải pháp hoàn thiện chất lượng sản phẩm (Polish & Hardening):
1. Thiết kế bản đồ điều khiển bàn phím toàn diện (hỗ trợ Vim-keys) và ma trận tương tác chuột mượt mà.
2. Thiết kế máy trạng thái tự động phục hồi kết nối Socket (Resilience & Auto-reconnect State Machine).
3. Thiết kế kiến trúc trừu tượng hóa đa ngôn ngữ (i18n) và giải pháp xử lý ký tự CJK (Ký tự đôi tiếng Nhật) trên lưới terminal.

---

## 2. THIẾT KẾ BẢN ĐỒ ĐIỀU HƯỚNG BÀN PHÍM VÀ CHUỘT

### 2.1. Bản đồ Phím tắt Toàn cục (Global Keymap Specification)
- **Cụm Phím Điều hướng Vim (Vim-Style Navigation):**
  - Phím `j`: Di chuyển con trỏ xuống một dòng trong danh sách.
  - Phím `k`: Di chuyển con trỏ lên một dòng trong danh sách.
  - Phím `h`: Di chuyển tiêu điểm sang Panel bên trái.
  - Phím `l`: Di chuyển tiêu điểm sang Panel bên phải.
  - Phím `g`: Nhảy về dòng đầu tiên của danh sách.
  - Phím `G`: Nhảy xuống dòng cuối cùng của danh sách.
- **Cụm Phím Nhảy Danh mục Nhanh:**
  - Phím số `1`: Nhảy tới bảng Pods.
  - Phím số `2`: Nhảy tới bảng Containers.
  - Phím số `3`: Nhảy tới bảng Images.
  - Phím số `4`: Nhảy tới bảng Volumes.
  - Phím số `5`: Nhảy tới bảng Networks.
- **Cụm Phím Chuyển Tab Chi tiết:**
  - Phím `[`: Chuyển sang tab con liền trước bên trái.
  - Phím `]`: Chuyển sang tab con liền sau bên phải.
- **Bảng Chọn Lệnh Mở rộng (Command Palette):**
  - Nhấn `m` hoặc `F10`: Hiển thị modal popup liệt kê toàn bộ các thao tác có thể thực hiện kèm phím tắt tương ứng, hỗ trợ tìm kiếm lệnh bằng cách gõ chữ.
- **Tính năng Lọc Mờ Nhanh (Fuzzy Filtering):**
  - Nhấn phím `/`: Tự động kích hoạt thanh nhập liệu tìm kiếm ở góc dưới. Khi người dùng gõ từ khóa, danh sách tức thời thu hẹp chỉ hiển thị các dòng khớp với từ khóa tìm kiếm.

### 2.2. Ma trận Xử lý Sự kiện Chuột (Mouse Interaction Matrix)
- **Click Chuột Trái (Left Click):**
  - Click vào bất kỳ thẻ danh mục nào trên Top Bar: Lập tức chuyển sang danh mục đó.
  - Click vào bất kỳ dòng nào trong danh sách thực thể: Đặt tiêu điểm vào dòng đó và tự động nạp dữ liệu chi tiết tương ứng ở panel bên phải.
  - Click vào các nút chuyển tab `[Logs]`, `[Stats]`, `[Inspect]`: Đổi tab con ngay lập tức.
- **Cuộn Chuột (Mouse Wheel):**
  - Khi con trỏ chuột nằm trên danh sách thực thể: Cuộn danh sách lên hoặc xuống 3 dòng mỗi nấc cuộn.
  - Khi con trỏ chuột nằm trên khung nhìn Log: Cuộn lịch sử log ngược về trước (đồng thời tạm dừng chế độ tự cuộn theo log mới).
- **Kéo Thả Thanh Chia (Splitter Drag):**
  - Cho phép người dùng nhấn giữ chuột trái trên đường viền dọc phân cách giữa panel trái và panel phải, kéo sang trái hoặc phải để điều chỉnh tỷ lệ hiển thị theo ý muốn.

---

## 3. THIẾT KẾ MÁY TRẠNG THÁI TỰ ĐỘNG PHỤC HỒI KẾT NỐI (AUTO-RECONNECT)

Để ứng dụng không bao giờ bị sập (crash) khi dịch vụ socket của hệ điều hành bị gián đoạn:

### 3.1. Kịch bản Khởi động khi Socket Chưa Bật
- Khi ứng dụng bật lên và phát hiện tệp socket không tồn tại:
  - Ứng dụng không thoát đột ngột mà hiển thị màn hình hướng dẫn thân thiện.
  - Nội dung mô tả rõ đường dẫn socket dự kiến không tìm thấy.
  - Cung cấp câu lệnh shell để người dùng bật dịch vụ: `systemctl --user enable --now podman.socket`.
  - Cung cấp 2 phím bấm: Nhấn `r` để thử kết nối lại ngay lập tức, hoặc nhấn `q` để thoát ứng dụng.

### 3.2. Máy Trạng thái Tự Phục hồi khi Đang Chạy (Reconnection Loop)
- Khi đang hoạt động mà kết nối socket bị đứt (ví dụ người dùng khởi động lại Podman service):
  1. **Trạng thái Mất Kết nối (Disconnected State):** Giao diện chuyển thanh trạng thái phía dưới sang màu đỏ cảnh báo `[!] Podman Socket Disconnected. Reconnecting...`. Tạm dừng việc đọc log và stats.
  2. **Chiến lược Thử lại với Độ trễ Tăng dần (Exponential Backoff):**
     - Lần 1: Thử lại sau 1 giây.
     - Lần 2: Thử lại sau 2 giây.
     - Lần 3: Thử lại sau 4 giây.
     - Tối đa: Cố định thử lại mỗi 8 giây một lần.
  3. **Trạng thái Tái Kết nối Thành công (Reconnected State):** Ngay khi socket mở lại, ứng dụng tự động khôi phục luồng dữ liệu, cập nhật danh sách thực thể và tiếp tục stream log mà không yêu cầu người dùng phải khởi động lại phần mềm.

---

## 4. THIẾT KẾ HỆ THỐNG ĐA NGÔN NGỮ (I18N) VÀ XỬ LÝ KÝ TỰ CJK

### 4.1. Cấu trúc Quản lý Chuỗi Tài nguyên
- Sử dụng các tệp JSON bản địa hóa lưu trong thư mục `locales/`:
  - `en.json`: Chứa toàn bộ chuỗi ký tự tiếng Anh (kích hoạt mặc định ở Phase 1).
  - `vi.json` và `ja.json`: Cấu trúc sẵn sàng cho giai đoạn mở rộng tiếp theo.
- Toàn bộ các nhãn giao diện đều được truy vấn thông qua Khóa Tài nguyên (Resource Key) phân cấp logic: `danh_muc.man_hinh.ten_nhan`.

### 4.2. Giải pháp Độ rộng Ký tự Đôi CJK (Ký tự Tiếng Nhật)
- Để tiếng Nhật (`ja-JP`) không làm xô lệch các cột bảng và làm vỡ viền khung TUI:
  - Hệ thống áp dụng thuật toán tính toán độ rộng cột hiển thị dựa trên tiêu chuẩn Unicode East Asian Width (sử dụng độ rộng thực tế `wcwidth` / `Rune.ColumnWidth`).
  - Mỗi ký tự Latinh hoặc tiếng Việt được tính là 1 cột hiển thị. Mỗi ký tự Kanji/Hiragana/Katakana được tính chính xác là 2 cột hiển thị.
  - Kích thước của các nút bấm và khung viền được tính toán động dựa trên tổng số cột hiển thị thực tế cộng thêm khoảng đệm an toàn, đảm bảo giao diện luôn vuông vắn trên mọi ngôn ngữ.
