# BẢN THIẾT KẾ KỸ THUẬT BẰNG LỜI: MILESTONE 6
## THIẾT KẾ ĐÓNG GÓI PHÂN PHỐI & QUY TRÌNH CI/CD TỰ ĐỘNG
- **Mã tài liệu:** DES-M6-PACKAGING-CICD
- **Vị trí lưu trữ:** `docs/2.Design/Design_M6_Packaging_and_Distribution.md`
- **Phiên bản:** 1.0.0
- **Trạng thái:** Bản thiết kế đề xuất (Draft)
- **Tài liệu căn cứ:** 
  - [`SRS_podman-FUI.md`](../1.Investigation/SRS_podman-FUI.md)
  - [`05_Packaging_and_Cross_Distro_Investigation.md`](../1.Investigation/05_Packaging_and_Cross_Distro_Investigation.md)
- **Tài liệu tiến độ gắn kèm:** [`Milestone_6_Packaging_and_CICD.md`](../3.Progress/Milestone_6_Packaging_and_CICD.md)

---

## 1. MỤC TIÊU THIẾT KẾ
Tài liệu này đặc tả chi tiết bằng lời phương án phát hành sản phẩm phần mềm tới tay người dùng cuối:
1. Thiết kế thông số xuất bản tệp thực thi nhị phân độc lập dạng **Single-File Self-Contained** (người dùng không cần cài đặt .NET).
2. Thiết kế quy chuẩn cấu trúc đóng gói gói cài đặt **`.deb`** (cho hệ Debian/Ubuntu/Mint) và **`.rpm`** (cho hệ Fedora/RHEL/CentOS).
3. Thiết kế luồng tự động hóa quy trình kiểm thử, đóng gói và phát hành (CI/CD Pipeline) trên GitHub Actions.

---

## 2. THIẾT KẾ XUẤT BẢN THỰC THI SINGLE-FILE SELF-CONTAINED

### 2.1. Cấu hình Xuất bản .NET Publish
Để tạo ra một file nhị phân độc lập duy nhất có tên `podman-fui` chạy trực tiếp trên Linux:
- **Chế độ Tự chứa (Self-Contained):** Đóng gói kèm toàn bộ Runtime .NET và các thư viện cần thiết vào bên trong file nhị phân. Máy người dùng hoàn toàn không cần cài bất kỳ phiên bản .NET SDK hay Runtime nào.
- **Chế độ Tệp Đơn nhất (Single-File):** Gộp toàn bộ các thư viện liên kết động (`.so`), metadata và code thực thi vào một tệp nhị phân duy nhất.
- **Tính năng Nén Tích hợp (Embedded Compression):** Nén toàn bộ các thành phần nhị phân bên trong để giảm dung lượng file tải về xuống mức tối ưu.
- **Hỗ trợ Đa Kiến trúc CPU (Target Runtimes):**
  - `linux-x64`: Dành cho các dòng chip Intel và AMD 64-bit tiêu chuẩn.
  - `linux-arm64`: Dành cho các dòng chip ARM 64-bit (máy chủ ARM, Raspberry Pi 4/5).

---

## 3. THIẾT KẾ CẤU TRÚC GÓI CÀI ĐẶT CHUẨN LINUX

### 3.1. Thiết kế Gói `.deb` (Debian, Ubuntu, Linux Mint)
Tuân thủ tiêu chuẩn phân cấp hệ thống tệp Linux (FHS):
- **Tệp siêu dữ liệu `DEBIAN/control`:**
  - Tên gói: `podman-fui`
  - Phiên bản: Đồng bộ theo số phiên bản của Git Release (ví dụ `1.0.0`).
  - Kiến trúc: `amd64` hoặc `arm64`.
  - Phụ thuộc hệ thống: Khai báo thư viện C chuẩn `libc6 (>= 2.31)` và công cụ `podman (>= 4.0.0)`.
  - Mô tả: "Modern terminal user interface for Podman container engine written in F#".
- **Vị trí tệp cài đặt:**
  - File thực thi nhị phân được đặt tại: `/usr/bin/podman-fui` với quyền thực thi `0755` (sở hữu bởi `root:root`).
  - Hướng dẫn tra cứu dòng lệnh được đặt tại: `/usr/share/man/man1/podman-fui.1.gz`.
- **Kịch bản thực thi sau khi cài đặt (`DEBIAN/postinst`):** Đảm bảo phân quyền chính xác và kích hoạt cập nhật lại cơ sở dữ liệu manpage của hệ thống.

### 3.2. Thiết kế Gói `.rpm` (Fedora, RHEL, CentOS Stream, Rocky Linux)
Xây dựng thông qua tệp đặc tả RPM Spec (`podman-fui.spec`):
- **Phần Khai báo:** Tên, phiên bản, giấy phép bản quyền (Apache 2.0 hoặc MIT), tóm tắt tính năng, và địa chỉ kho mã nguồn GitHub.
- **Phần Build & Install:** Sao chép file thực thi nhị phân vào thư mục định danh chuẩn `%{buildroot}%{_bindir}/podman-fui`.
- **Phần Files:** Định nghĩa quyền sở hữu của `root:root` trên tệp `/usr/bin/podman-fui`.
- **Phần Yêu cầu (Requires):** Khai báo phụ thuộc vào `podman` và nhân Linux có hỗ trợ cgroup v2.

### 3.3. Bản Nén Độc lập (Portable Tarball `.tar.gz`)
Dành cho người dùng các bản phân phối Linux khác (Arch Linux, openSUSE, Alpine):
- Tệp nén chứa binary `podman-fui` và một script shell `install.sh`.
- Người dùng chỉ cần giải nén và chạy `./install.sh`, script sẽ tự động sao chép file thực thi vào thư mục `$HOME/.local/bin/` của người dùng mà không cần quyền `sudo`.

---

## 4. THIẾT KẾ QUY TRÌNH CI/CD TỰ ĐỘNG TRÊN GITHUB ACTIONS

Quy trình tự động hóa phát hành phần mềm được thiết kế theo luồng liên tục:

```
[Lập trình viên gắn Tag mới: v*.*.*]
                 │
                 ▼
[GitHub Actions Workflow được kích hoạt]
                 │
                 ▼
[Job 1: Build & Unit Test trên môi trường Ubuntu Runner]
                 │
                 ▼
[Job 2: Xuất bản Single-File Binary cho linux-x64 và linux-arm64]
                 │
                 ▼
[Job 3: Đóng gói đồng thời: .deb, .rpm, và .tar.gz]
                 │
                 ▼
[Job 4: Tạo mã băm SHA256 Checksum cho toàn bộ các file artifact]
                 │
                 ▼
[Job 5: Tự động khởi tạo GitHub Release và đính kèm toàn bộ các gói cài đặt]
```

### Tiêu chuẩn Nghiệm thu Quy trình Phát hành:
- Mỗi khi có bản phát hành mới, người dùng chỉ cần vào trang GitHub Releases là có sẵn các lựa chọn:
  - Tải file `.deb` để cài đặt trên Ubuntu/Debian/Mint bằng lệnh `sudo dpkg -i`.
  - Tải file `.rpm` để cài đặt trên Fedora/RHEL bằng lệnh `sudo dnf install`.
  - Tải file `.tar.gz` để sử dụng portable tức thời.
- Toàn bộ các gói phát hành đều có kèm mã băm SHA256 tương ứng để người dùng kiểm tra tính toàn vẹn dữ liệu.
