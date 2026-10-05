# LỘ TRÌNH CHI TIẾT: MILESTONE 5 - HOÀN THIỆN TRẢI NGHIỆM NGƯỜI DÙNG & TỐI ƯU TOÀN DIỆN
## DỰ ÁN: PODMAN-FUI (F# TERMINAL USER INTERFACE FOR PODMAN)

- **Mã cột mốc:** M5-UX-POLISH
- **Vị trí lưu trữ:** `docs/3.Progress/Milestone_5_UX_Polish_and_Cross_Distro.md`
- **Trạng thái:** **Chờ thực hiện (Scheduled)**
- **Bản thiết kế kỹ thuật thực thi (Design Specification):**
  - [**`Design_M5_UX_Interaction_and_Resilience.md`**](../2.Design/Design_M5_UX_Interaction_and_Resilience.md) *(Bản thiết kế chi tiết DD-M5-UX-RESILIENCE đã phê duyệt)*
- **Tài liệu điều tra liên kết (Investigation References):**
  - [**`02_UI_Framework_and_Rendering_Investigation.md`**](../1.Investigation/02_UI_Framework_and_Rendering_Investigation.md)
  - [**`05_Packaging_and_Cross_Distro_Investigation.md`**](../1.Investigation/05_Packaging_and_Cross_Distro_Investigation.md)
  - [**`06_Internationalization_i18n_Investigation.md`**](../1.Investigation/06_Internationalization_i18n_Investigation.md)
  - [**`09_DeepDive_Rootless_Cgroups_Networking_and_SELinux.md`**](../1.Investigation/09_DeepDive_Rootless_Cgroups_Networking_and_SELinux.md)

---

## 1. MỤC TIÊU CỘT MỐC 5
Đánh bóng toàn diện trải nghiệm người dùng: Hoàn thiện hệ thống phím tắt Vim-keys và thao tác chuột mượt mà (kéo splitter, cuộn mượt), xây dựng máy trạng thái tự phục hồi kết nối socket với Exponential Backoff, xử lý hiển thị chuẩn xác ký tự đôi CJK (tiếng Nhật), và tích hợp hệ thống đa ngôn ngữ (i18n) với cơ chế dự phòng an toàn.

---

## 2. DANH MỤC CÔNG VIỆC CHI TIẾT (TASK CHECKLIST)

### 2.1. Thiết kế Chi tiết & Nghiên cứu Kỹ thuật
*[Căn cứ thiết kế: `Design_M5_UX_Interaction_and_Resilience.md` - Toàn văn bản vẽ DD-M5-UX-RESILIENCE]*
- [x] Soạn thảo tài liệu thiết kế chi tiết phân rã mô-đun bàn phím Vim, chuột, máy trạng thái tự phục hồi và i18n CJK.
- [x] Đặc tả chi tiết Hợp đồng dữ liệu `LocalizationCatalog`, `WatchdogState`, thuật toán Exponential Backoff và East Asian Width.

### 2.2. Hiện thực hóa Tầng Domain (Bản địa hóa & Tự phục hồi)
*[Căn cứ thiết kế: `Design_M5_UX_Interaction_and_Resilience.md` - Mục 2.1: Phân Rã Domain & Mục 3: Đặc Tả Hợp Đồng Dữ Liệu]*
- [ ] **Subtask 2.2.1 - Thực thể miền Bản địa hóa (`I18nModels.fs`):**
  - Định nghĩa DU `LanguageCode` (`EnUS`, `ViVN`, `JaJP`).
  - Định nghĩa Record `LocalizationCatalog` (`CurrentLanguage`, `StringMap`, `FallbackMap`) theo Mục 3.1.
  - Định nghĩa kiểu khóa chuỗi tài nguyên phân cấp `I18nKey`.
- [ ] **Subtask 2.2.2 - Thực thể miền Bàn phím & Lệnh (`KeymapModels.fs`):**
  - Định nghĩa DU `KeyContext` (`Global`, `ListNavigation`, `DetailPanel`, `ModalInput`).
  - Định nghĩa Record `CommandActionItem` (Id, Title, Category, ShortcutKey, TargetMsg) theo Mục 3.2.
- [ ] **Subtask 2.2.3 - Thực thể miền Giám sát Nhịp tim Socket (`ResilienceModels.fs`):**
  - Định nghĩa DU `ConnectionState` (`Connected`, `Reconnecting`, `PermanentlyFailed`).
  - Định nghĩa Record `WatchdogState` (Status, ConsecutiveFailures, CurrentBackoffDelay, NextAttemptAt) theo Mục 3.3.
- [ ] **Subtask 2.2.4 - Giao diện Dịch vụ trừu tượng (`II18nService.fs`, `IWatchdogService.fs`):**
  - Khai báo các interface phục vụ dịch thuật và giám sát kết nối ngầm.

### 2.3. Hiện thực hóa Tầng Hạ tầng Infrastructure
*[Căn cứ thiết kế: `Design_M5_UX_Interaction_and_Resilience.md` - Mục 2.2, Mục 4.1, 4.2, 4.3]*
- [ ] **Subtask 2.3.1 - Nạp và Quản lý Tệp Bản địa hóa JSON (`JsonI18nProvider.fs`):**
  - Khởi tạo cấu trúc các tệp: `locales/en.json` (ngôn ngữ gốc), `locales/vi.json`, `locales/ja.json`.
  - Hiện thực thuật toán tra cứu i18n với Fallback theo Mục 4.3: Tìm trong ngôn ngữ hiện tại -> Nếu thiếu lấy từ `en.json` -> Nếu thiếu in chuỗi placeholder tránh crash.
- [ ] **Subtask 2.3.2 - Động cơ Tính toán Độ rộng Ký tự Unicode CJK (`UnicodeWidthCalculator.fs`):**
  - Hiện thực thuật toán chuẩn Unicode Standard Annex #11 (East Asian Width) theo Mục 4.2:
    - Ký tự Latinh/Tiếng Việt = 1 cột.
    - Ký tự Kanji, Hiragana, Katakana, Hangul, Fullwidth, Emoji = 2 cột.
    - Ký tự Combining Marks = 0 cột.
    - Cung cấp hàm đo độ rộng cột thực tế `measureDisplayWidth: string -> int` cho toàn bộ các component giao diện.
- [ ] **Subtask 2.3.3 - Tiến trình Giám sát Nhịp tim & Tự phục hồi Kết nối (`SocketWatchdogService.fs`):**
  - Hiện thực luồng kiểm tra nhịp tim định kỳ 2 giây gửi yêu cầu siêu nhẹ `GET /v4.0.0/libpod/_ping` theo Mục 4.1.
  - Hiện thực thuật toán Exponential Backoff: Khi mất kết nối, thử lại sau 1s, 2s, 4s, tối đa 8s; tự động khôi phục luồng dữ liệu khi socket mở lại.

### 2.4. Hiện thực hóa Tầng Presentation (Tương tác & Giao diện Nâng cao)
*[Căn cứ thiết kế: `Design_M5_UX_Interaction_and_Resilience.md` - Mục 2.3, Mục 4.4, Mục 6: Đặc Tả Command Palette]*
- [ ] **Subtask 2.4.1 - Bộ phân phối Phím tắt Toàn cục & Vim-keys (`KeymapDispatcher.fs`):**
  - Tích hợp cụm điều hướng Vim: `j` (Xuống), `k` (Lên), `h` (Sang trái), `l` (Sang phải), `g` (Đầu danh sách), `G` (Cuối danh sách).
  - Tích hợp cụm phím số `1..5` chuyển danh mục tức thời, `[` và `]` chuyển tab chi tiết.
- [ ] **Subtask 2.4.2 - Bộ điều phối Cử chỉ Chuột Đa điểm (`MouseGestureHandler.fs`):**
  - Bắt sự kiện click chuột trái chọn danh mục, chọn dòng, chuyển tab.
  - Bắt sự kiện con lăn chuột cuộn log hoặc danh sách thực thể.
  - Hiện thực thuật toán kéo thả thanh phân cách (Splitter Drag) theo Mục 4.4: Thay đổi tỷ lệ chiều rộng Panel Trái/Phải mượt mà theo chuyển động chuột (giới hạn an toàn từ 25 cột đến Max - 40 cột).
- [ ] **Subtask 2.4.3 - Bảng Chọn Lệnh Nhanh (`Views/CommandPaletteModal.fs`):**
  - Kích hoạt khi nhấn `m` hoặc `F10`: Cung cấp thanh tìm kiếm mờ (Fuzzy Search) toàn bộ tính năng và phím tắt của ứng dụng theo Mục 6.
- [ ] **Subtask 2.4.4 - Lớp phủ Cảnh báo Mất Kết nối (`Views/ConnectionLossOverlay.fs`):**
  - Khi socket bị ngắt: Hiển thị thanh cảnh báo màu đỏ nhạt, vòng xoay chờ thử lại và bộ đếm lùi thời gian. Tự động đóng lớp phủ khi kết nối thành công trở lại.

### 2.5. Kiểm thử Nghiệm thu Kỹ thuật (Technical Acceptance Testing)
*[Căn cứ thiết kế: `Design_M5_UX_Interaction_and_Resilience.md` - Mục 7: Ma Trận Ca Kiểm Thử Nghiệm Thu]*
- [ ] **Subtask 2.5.1 - Thực thi kiểm thử ca TC-M5-01 (Điều hướng Vim-keys mượt mà):**
  - Sử dụng toàn bộ cụm phím `j`, `k`, `h`, `l`, `g`, `G`; xác nhận con trỏ và khung tiêu điểm chuyển động chuẩn xác 100%.
- [ ] **Subtask 2.5.2 - Thực thi kiểm thử ca TC-M5-02 (Tự phục hồi kết nối Socket):**
  - Dừng socket `systemctl --user stop podman.socket`; ứng dụng chuyển sang overlay cảnh báo; bật lại socket; ứng dụng tự khôi phục dữ liệu không cần khởi động lại.
- [ ] **Subtask 2.5.3 - Thực thi kiểm thử ca TC-M5-03 (Hiển thị CJK không lệch khung):**
  - Thử nghiệm hiển thị tên container bằng tiếng Nhật Kanji/Hiragana; xác nhận toàn bộ đường viền khung bảng giữ thẳng hàng, không bị méo lệch.
- [ ] **Subtask 2.5.4 - Thực thi kiểm thử ca TC-M5-04 (Command Palette):**
  - Nhấn `m`, gõ từ khóa tìm lệnh; nhấn `Enter` thực thi ngay lập tức thao tác mong muốn.
- [ ] **Subtask 2.5.5 - Thực thi kiểm thử ca TC-M5-05 (Kéo thả chuột Splitter):**
  - Nhấn giữ chuột trái trên đường phân cách dọc giữa 2 panel và kéo thả; xác nhận giao diện co giãn mượt mà theo vị trí chuột.

---

## 3. TIÊU CHÍ HOÀN THÀNH (DEFINITION OF DONE - DOD)

1. Hỗ trợ đầy đủ bộ phím tắt Vim-keys và thao tác chuột hoàn chỉnh (Click, Scroll, Kéo Splitter).
2. Khi socket bị ngắt kết nối đột ngột, ứng dụng tự động kích hoạt cơ chế Exponential Backoff và phục hồi mượt mà ngay khi socket hoạt động trở lại mà không gây crash.
3. Độ rộng ký tự CJK tiếng Nhật được tính toán chuẩn xác theo tiêu chuẩn Unicode Annex #11, viền bảng không bao giờ bị xô lệch.
4. Bảng Command Palette (`m` / `F10`) cho phép tìm kiếm và thực thi mọi tác vụ trong 1 nốt nhạc.
5. Vượt qua toàn bộ các ca kiểm thử từ TC-M5-01 đến TC-M5-05.
