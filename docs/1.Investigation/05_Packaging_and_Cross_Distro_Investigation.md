# ĐIỀU TRA KỸ THUẬT 05: KHẢ NĂNG TƯƠNG THÍCH ĐA HỆ ĐIỀU HÀNH & ĐÓNG GÓI PHÂN PHỐI
## DỰ ÁN: PODMAN-FUI

- **Mã tài liệu:** INV-05-PACKAGING-DISTRO
- **Trạng thái:** Hoàn thành điều tra
- **Mục tiêu:** Phân tích khả năng tương thích trên các hệ điều hành mục tiêu (Debian & Fedora), phương án đóng gói Self-contained Single-file (không phụ thuộc .NET Runtime), cấu trúc gói cài đặt chuẩn `.deb` và `.rpm`, cùng với điều tra các trình giả lập terminal.

---

## 1. PHÂN TÍCH HỆ ĐIỀU HÀNH MỤC TIÊU & MÔI TRƯỜNG THỰC THI

`podman-FUI` hướng tới hai họ phân phối Linux chủ đạo trong môi trường máy chủ và máy trạm:

### 1.1. Họ Phân Phối Debian / Ubuntu
- **Các bản phân phối đại diện:** Debian 11 (Bullseye), Debian 12 (Bookworm), Ubuntu 22.04 LTS (Jammy), Ubuntu 24.04 LTS (Noble), Linux Mint 21/22.
- **Thư viện hệ thống (C Runtime):** `glibc` phiên bản từ 2.31 đến 2.39.
- **Trình quản lý gói chuẩn:** `dpkg` / `apt` với định dạng gói **`.deb`**.
- **Cấu hình Podman mặc định:** Cài đặt qua `apt install podman`, cấu hình rootless socket nằm tại `/run/user/<UID>/podman/podman.sock`.

### 1.2. Họ Phân Phối Fedora / Red Hat Enterprise Linux (RHEL)
- **Các bản phân phối đại diện:** Fedora 39, 40, 41; RHEL 8, 9; CentOS Stream 9; Rocky Linux 9; AlmaLinux 9.
- **Thư viện hệ thống (C Runtime):** `glibc` phiên bản từ 2.34 đến 2.40.
- **Trình quản lý gói chuẩn:** `rpm` / `dnf` với định dạng gói **`.rpm`**.
- **Cấu hình Podman mặc định:** Podman là công cụ container mặc định số một của hệ thống. Socket activation được tích hợp sẵn sâu vào Systemd của Red Hat.

---

## 2. PHƯƠNG ÁN XUẤT BẢN THỰC THI: SELF-CONTAINED SINGLE-FILE

Để người dùng cuối có thể tải về và sử dụng ngay lập tức mà **không cần cài đặt .NET SDK hoặc .NET Runtime** trên máy:

### 2.1. So sánh Phương án Triển khai

| Tiêu chí | Cần .NET Runtime (Framework-dependent) | Tự Chứa Single-File (Self-Contained) | Biên dịch Tĩnh Native AOT |
| :--- | :--- | :--- | :--- |
| **Yêu cầu máy người dùng** | Phải cài trước `dotnet-runtime-8.0` hoặc `10.0` | **Không yêu cầu bất cứ thành phần .NET nào** | Không yêu cầu bất cứ thành phần .NET nào |
| **Kích thước file thực thi** | Rất nhỏ (~2 - 5 MB) | Trung bình (~40 - 55 MB, chứa đầy đủ Runtime) | Nhỏ (~15 - 25 MB) |
| **Thời gian khởi động** | ~200 - 300 ms | **Cực nhanh (~100 - 180 ms)** | Nhanh nhất (~50 - 100 ms) |
| **Khả năng tương thích Thư viện** | 100% | **100% (Hoàn toàn tương thích Terminal.Gui v2)** | Hạn chế (Dễ lỗi Reflection/Trimming) |
| **Mức độ khả thi cho dự án** | Kém (Gây phiền hà cho người dùng Linux) | **TỐI ƯU NHẤT (Khuyến nghị lựa chọn)** | Cần khảo sát thêm ở Phase 2 |

### 2.2. Chiến lược Tối ưu hóa Kích thước Single-File
- Sử dụng cờ nén tích hợp của trình xuất bản .NET để gộp toàn bộ file thư viện liên kết (`.so`, `.dll`), metadata và file cấu hình thành một file nhị phân duy nhất (`podman-fui`).
- Bật tính năng loại bỏ mã thừa (Assembly Trimming) an toàn cho các module không dùng tới để giảm kích thước tệp tải về xuống mức tối ưu nhất.

---

## 3. THIẾT KẾ CẤU TRÚC GÓI CÀI ĐẶT CHUẨN (.DEB VÀ .RPM)

### 3.1. Cấu trúc Gói `.deb` (Debian & Ubuntu)
Gói `.deb` được cấu trúc theo tiêu chuẩn của hệ thống tệp Linux (Filesystem Hierarchy Standard - FHS):

```
podman-fui_<version>_<arch>/
├── DEBIAN/
│   ├── control              # Metadata: Tên gói, phiên bản, tác giả, mô tả, phụ thuộc
│   ├── postinst             # Script thực thi sau khi cài đặt (phân quyền /usr/bin/podman-fui)
│   └── prerm                # Script dọn dẹp trước khi gỡ cài đặt
└── usr/
    ├── bin/
    │   └── podman-fui       # Binary thực thi độc lập (đặt quyền 0755)
    └── share/
        ├── doc/
        │   └── podman-fui/
        │       └── copyright # Thông tin bản quyền phần mềm
        └── man/
            └── man1/
                └── podman-fui.1.gz # Hướng dẫn sử dụng manpage
```

- **Tệp `control` quy định:**
  - `Package`: `podman-fui`
  - `Architecture`: `amd64` (hoặc `arm64`)
  - `Depends`: `libc6 (>= 2.31), podman (>= 4.0.0)`
  - `Recommends`: Khởi động dịch vụ socket của người dùng.

### 3.2. Cấu trúc Gói `.rpm` (Fedora, RHEL & CentOS)
Gói `.rpm` được định nghĩa thông qua tệp đặc tả kỹ thuật (RPM Spec File):
- **Phần Header:** Khai báo `Name`, `Version`, `Release`, `Summary`, `License`, `URL`.
- **Phần Build & Install:** Đặt file thực thi `podman-fui` vào thư mục đích `%{buildroot}%{_bindir}/podman-fui`.
- **Phần Files:** Xác định quyền sở hữu đối với `%{_bindir}/podman-fui` (thuộc sở hữu của `root:root`, quyền `0755`).
- **Phụ thuộc thực thi:** Khai báo yêu cầu hệ thống đã có sẵn `podman` và thư viện C chuẩn.

### 3.3. Gói Nén Độc lập (Tarball `.tar.gz`)
Dành cho người dùng Arch Linux, openSUSE hoặc các môi trường không muốn cài đặt qua trình quản lý gói:
- Đóng gói file thực thi `podman-fui` kèm một file script cài đặt nhanh `install.sh` để tự động đưa binary vào `$HOME/.local/bin` hoặc `/usr/local/bin`.

---

## 4. KHẢO SÁT KHẢ NĂNG TƯƠNG THÍCH TRÌNH GIẢ LẬP TERMINAL

Giao diện TUI hiện đại phụ thuộc nhiều vào khả năng hiển thị mã màu và bộ ký tự Unicode của terminal.

### 4.1. Ma trận Tương thích Terminal Phổ biến

| Trình giả lập Terminal | Hệ điều hành phổ biến | Hỗ trợ TrueColor (24-bit) | Hỗ trợ Chuột (Mouse Events) | Hiển thị Ký tự Khối Sparklines (` ▂▃▄...`) |
| :--- | :--- | :--- | :--- | :--- |
| **GNOME Terminal** | Ubuntu, Debian, Fedora | Hoàn hảo | Rất tốt | Hoàn hảo |
| **Konsole** | KDE Plasma, Fedora, Mint | Hoàn hảo | Rất tốt | Hoàn hảo |
| **Alacritty** | Đa nền tảng (GPU accelerated) | Hoàn hảo | Rất tốt | Hoàn hảo |
| **Kitty** | Đa nền tảng | Hoàn hảo | Rất tốt | Hoàn hảo |
| **WezTerm** | Đa nền tảng (Lua config) | Hoàn hảo | Rất tốt | Hoàn hảo |
| **xterm** | Mọi hệ thống Linux | Cần cấu hình (256 colors) | Cơ bản | Tùy font cài đặt |
| **tmux (Multiplexer)** | Mọi hệ thống Linux | Hoàn hảo (nếu cấu hình `set -g default-terminal`) | Hoàn hảo (khi bật `mouse on`) | Hoàn hảo |
| **Windows Terminal (SSH)** | Kết nối từ Windows vào Linux | Hoàn hảo | Rất tốt | Hoàn hảo |

### 4.2. Cơ chế Thích ứng Màu sắc (Color Adaptation)
- **TrueColor (24-bit Color):** Tự động kích hoạt khi phát hiện biến môi trường `$COLORTERM` có giá trị `truecolor` hoặc `24bit`. Cho phép hiển thị gradient màu mượt mà trên biểu đồ và bảng dữ liệu.
- **Fallback 256 Colors:** Khi biến `$COLORTERM` không tồn tại nhưng `$TERM` hỗ trợ `xterm-256color`, hệ thống tự động ánh xạ bảng màu RGB về 256 mã màu ANSI tương ứng gần nhất.
- **Fallback 16 Colors:** Khi chạy trên terminal tty thuần (Linux console ngoài chế độ đồ họa), hệ thống tự chuyển đổi về 16 màu ANSI cơ bản để tránh hiển thị ký tự rác.

### 4.3. Yêu cầu Bộ Ký tự & Locale
- Để hiển thị đúng các ký tự vẽ khung (Box-drawing characters: `┌`, `─`, `│`, `┐`) và các ký tự Sparkline (` ▂▃▄▅▆▇█`):
  - Hệ thống yêu cầu môi trường hỗ trợ mã hóa **UTF-8** (ví dụ biến môi trường `LANG=en_US.UTF-8` hoặc `vi_VN.UTF-8` hoặc `C.UTF-8`).
  - Terminal sử dụng font chữ monospace có hỗ trợ Unicode chuẩn (như JetBrains Mono, Fira Code, Ubuntu Mono, Noto Sans Mono).

---

## 5. KẾT LUẬN & ĐỀ XUẤT CHO BƯỚC THIẾT KẾ (DESIGN)

1. **Ưu tiên Quy trình Đóng gói Tự động:** Xây dựng quy trình CI/CD sử dụng công cụ chuẩn của Linux để đóng gói song song cả hai định dạng `.deb` và `.rpm` ngay khi phát hành phiên bản mới (Release tag).
2. **Kiểm tra Môi trường khi Khởi động:** Ứng dụng phải có bước tự kiểm tra (Sanity Check) khi bật lên: Kiểm tra mã hóa terminal có phải UTF-8 hay không, kiểm tra độ phân giải hiện tại có đạt tối thiểu 80x24 hay không để đưa ra cảnh báo kịp thời.
