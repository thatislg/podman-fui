# LỘ TRÌNH CHI TIẾT: MILESTONE 5 - HOÀN THIỆN TRẢI NGHIỆM NGƯỜI DÙNG & TỐI ƯU TOÀN DIỆN
## DỰ ÁN: PODMAN-FUI (F# TERMINAL USER INTERFACE FOR PODMAN)

- **Mã cột mốc:** M5-UX-POLISH
- **Vị trí lưu trữ:** `docs/3.Progress/Milestone_5_UX_Polish_and_Cross_Distro.md`
- **Trạng thái:** **Chờ thực hiện (Scheduled)**
- **Bản thiết kế kỹ thuật thực thi (Design Specification):**
  - [**`Design_M5_UX_Interaction_and_Resilience.md`**](../2.Design/Design_M5_UX_Interaction_and_Resilience.md) *(Bản thiết kế chi tiết DD-M5-UX-RESILIENCE đã hoàn thiện)*
- **Tài liệu điều tra liên kết (Investigation References):**
  - [**`02_UI_Framework_and_Rendering_Investigation.md`**](../1.Investigation/02_UI_Framework_and_Rendering_Investigation.md)
  - [**`05_Packaging_and_Cross_Distro_Investigation.md`**](../1.Investigation/05_Packaging_and_Cross_Distro_Investigation.md)
  - [**`06_Internationalization_i18n_Investigation.md`**](../1.Investigation/06_Internationalization_i18n_Investigation.md)
  - [**`09_DeepDive_Rootless_Cgroups_Networking_and_SELinux.md`**](../1.Investigation/09_DeepDive_Rootless_Cgroups_Networking_and_SELinux.md)

---

## 1. MỤC TIÊU CỘT MỐC 5
Đánh bóng toàn diện trải nghiệm người dùng: Hoàn thiện hệ thống phím tắt Vim-keys và thao tác chuột mượt mà, xử lý các kịch bản lỗi mạng/socket thân thiện, kiểm thử tương thích chéo trên cả hai họ hệ điều hành Debian và Fedora, đồng thời chuẩn bị sẵn sàng tệp bản địa hóa đa ngôn ngữ.

---

## 2. DANH MỤC CÔNG VIỆC CHI TIẾT (TASK CHECKLIST)

### 2.1. Hoàn thiện Hệ thống Phím tắt & Điều hướng
- [ ] Tích hợp trọn vẹn bộ phím điều hướng Vim: `h` (Trái), `j` (Xuống), `k` (Lên), `l` (Phải).
- [ ] Phím số chuyển danh mục tức thì: `1` (Pods), `2` (Containers), `3` (Images), `4` (Volumes), `5` (Networks).
- [ ] Phím chuyển tab chi tiết: `[` và `]` để chuyển qua lại giữa `Logs`, `Stats`, `Inspect`, `Top`, `Env`.
- [ ] Bảng chọn lệnh toàn cục (Command Palette): Nhấn `m` hoặc `F10` để mở danh sách toàn bộ các lệnh có thể thực thi kèm mô tả phím tắt tương ứng.
- [ ] Tính năng Lọc mờ (Fuzzy Filtering): Nhấn `/` để tìm kiếm tức thì trên danh sách.

### 2.2. Hoàn thiện Tương tác Chuột Toàn diện (Mouse Support)
- [ ] Click chuột chọn danh mục trên Top bar.
- [ ] Click chuột chọn dòng thực thể trong danh sách.
- [ ] Cuộn bánh xe chuột (Mouse Wheel Up / Down) mượt mà trên bảng danh sách và khung đọc Log.
- [ ] Kéo thả thanh chia (Splitter Drag) giữa panel trái và panel phải để tùy biến độ rộng hiển thị.

### 2.3. Khả năng Chịu lỗi & Tự Phục hồi (Fault Tolerance & Resilience)
- [ ] Màn hình hướng dẫn khi khởi động: Nếu `podman.socket` chưa bật, hiển thị giao diện hướng dẫn thân thiện kèm lệnh `systemctl --user enable --now podman.socket`, nhấn `r` để thử lại, `q` để thoát.
- [ ] Cơ chế tự động thử kết nối lại (Auto-reconnect): Khi socket bị ngắt quãng ngắn hạn (ví dụ khởi động lại service), ứng dụng tự động kết nối lại mà không bị sập.

### 2.4. Kiểm thử Tương thích Chéo Đa Nền tảng (Cross-Distro Validation)
- [ ] Kiểm thử trên các bản phân phối Debian / Ubuntu: Debian 11/12, Ubuntu 22.04/24.04 LTS, Linux Mint.
- [ ] Kiểm thử trên các bản phân phối Fedora / RHEL: Fedora 39/40/41, RHEL/Rocky Linux 9 (đặc biệt xác nhận hoạt động ổn định dưới chế độ SELinux Enforcing).
- [ ] Kiểm thử trên các trình giả lập terminal: GNOME Terminal, Konsole, Alacritty, Kitty, WezTerm, tmux.
- [ ] Đóng gói cấu trúc tệp bản địa hóa chuẩn bị cho Phase 2 (`locales/en.json`, `locales/vi.json`, `locales/ja.json` theo `INV-06`).

---

## 3. TIÊU CHÍ HOÀN THÀNH (DEFINITION OF DONE - DOD)

1. Mọi thao tác phím tắt và chuột phản hồi mượt mà, độ trễ thao tác dưới **16ms** (đạt chuẩn 60 FPS).
2. Khi tắt dịch vụ `podman.socket` rồi bật lại, ứng dụng tự khôi phục kết nối bình thường mà không cần restart lại ứng dụng.
3. Chạy ổn định, không lỗi font, không vỡ layout trên cả hệ Debian và Fedora.
