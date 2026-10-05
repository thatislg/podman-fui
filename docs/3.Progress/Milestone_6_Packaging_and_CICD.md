# LỘ TRÌNH CHI TIẾT: MILESTONE 6 - ĐÓNG GÓI PHÂN PHỐI & CI/CD TỰ ĐỘNG
## DỰ ÁN: PODMAN-FUI (F# TERMINAL USER INTERFACE FOR PODMAN)

- **Mã cột mốc:** M6-PACKAGING-CICD
- **Vị trí lưu trữ:** `docs/3.Progress/Milestone_6_Packaging_and_CICD.md`
- **Trạng thái:** **Chờ thực hiện (Scheduled)**
- **Bản thiết kế kỹ thuật thực thi (Design Specification):**
  - [**`Design_M6_Packaging_and_Distribution.md`**](../2.Design/Design_M6_Packaging_and_Distribution.md) *(Bản thiết kế bằng lời đã lập)*
- **Tài liệu điều tra liên kết (Investigation References):**
  - [**`05_Packaging_and_Cross_Distro_Investigation.md`**](../1.Investigation/05_Packaging_and_Cross_Distro_Investigation.md)

---

## 1. MỤC TIÊU CỘT MỐC 6
Cung cấp các gói cài đặt hoàn chỉnh cho người dùng cuối trên hệ Debian và Fedora dưới dạng Single-File Self-Contained Binary (không bắt buộc cài đặt .NET SDK hoặc Runtime), đồng thời thiết lập quy trình tự động hóa kiểm thử và phát hành phiên bản mới qua GitHub Actions.

---

## 2. DANH MỤC CÔNG VIỆC CHI TIẾT (TASK CHECKLIST)

### 2.1. Cấu hình Xuất bản Nhị phân Độc lập (Single-File Self-Contained)
- [ ] Thiết lập profile xuất bản `.NET Publish`:
  - `PublishSingleFile=true`
  - `SelfContained=true`
  - `RuntimeIdentifier`: `linux-x64` và `linux-arm64`
  - Bật tính năng nén nhị phân tích hợp để tối ưu dung lượng tệp.
- [ ] Cấu hình loại bỏ mã dư thừa an toàn (Assembly Trimming) để giảm kích thước tệp tải về xuống mức tối ưu nhất.

### 2.2. Xây dựng Quy trình Đóng gói Gói `.deb` (Debian, Ubuntu, Mint)
- [ ] Xây dựng cây thư mục đóng gói chuẩn theo Linux FHS:
  - Tệp metadata `DEBIAN/control` (Package, Version, Architecture, Dependencies, Description).
  - Tệp script cài đặt `DEBIAN/postinst` (cấp quyền thực thi `0755` cho `/usr/bin/podman-fui`).
  - Hướng dẫn sử dụng manpage `/usr/share/man/man1/podman-fui.1.gz`.
- [ ] Sử dụng công cụ `dpkg-deb` để biên dịch gói thành file `podman-fui_<version>_amd64.deb`.
- [ ] Kiểm thử cài đặt và gỡ cài đặt sạch bằng `sudo dpkg -i` và `sudo apt remove`.

### 2.3. Xây dựng Quy trình Đóng gói Gói `.rpm` (Fedora, RHEL, CentOS)
- [ ] Viết tệp đặc tả kỹ thuật RPM Spec File (`podman-fui.spec`):
  - Khai báo Metadata, quyền sở hữu `root:root` đối với `%{_bindir}/podman-fui`.
  - Khai báo phụ thuộc hệ thống: yêu cầu có sẵn `podman` và thư viện C chuẩn.
- [ ] Sử dụng công cụ `rpmbuild` để đóng gói thành file `podman-fui-<version>-1.x86_64.rpm`.
- [ ] Kiểm thử cài đặt và gỡ cài đặt sạch bằng `sudo dnf install ./podman-fui.rpm` và `sudo dnf remove`.

### 2.4. Đóng gói Bản Nén Độc lập (Portable Tarball)
- [ ] Đóng gói file nén `podman-fui-linux-x64.tar.gz`.
- [ ] Viết script cài đặt nhanh `install.sh` để người dùng có thể tự động cài đặt vào `$HOME/.local/bin` chỉ bằng một lệnh `curl | bash`.

### 2.5. Xây dựng Quy trình CI/CD Tự động trên GitHub Actions
- [ ] Tạo workflow `.github/workflows/release.yml`:
  - Kích hoạt khi có Git Tag phiên bản mới (ví dụ `v1.0.0`).
  - Tự động chạy Unit Test và Integration Test.
  - Tự động build ra các artifact: Single-file binary, gói `.deb`, gói `.rpm`, tệp `.tar.gz`.
  - Tự động tạo bản phát hành GitHub Release và đính kèm các gói cài đặt kèm mã băm SHA256 để người dùng tải về.

---

## 3. TIÊU CHÍ HOÀN THÀNH (DEFINITION OF DONE - DOD)

1. Người dùng trên Debian/Ubuntu/Mint có thể tải về file `.deb`, cài đặt bằng `dpkg -i` và chạy ngay lệnh `podman-fui` mà không gặp lỗi thiếu thư viện.
2. Người dùng trên Fedora/RHEL/CentOS có thể tải về file `.rpm`, cài đặt bằng `dnf install` và chạy ngay lập tức.
3. Khi đẩy một Git Tag mới lên repository GitHub, toàn bộ quy trình build và tạo Release hoàn tất tự động mà không cần can thiệp thủ công.
