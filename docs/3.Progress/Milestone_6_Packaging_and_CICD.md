# LỘ TRÌNH CHI TIẾT: MILESTONE 6 - ĐÓNG GÓI PHÂN PHỐI & CI/CD TỰ ĐỘNG
## DỰ ÁN: PODMAN-FUI (F# TERMINAL USER INTERFACE FOR PODMAN)

- **Mã cột mốc:** M6-PACKAGING-CICD
- **Vị trí lưu trữ:** `docs/3.Progress/Milestone_6_Packaging_and_CICD.md`
- **Trạng thái:** **Chờ thực hiện (Scheduled)**
- **Bản thiết kế kỹ thuật thực thi (Design Specification):**
  - [**`Design_M6_Packaging_and_Distribution.md`**](../2.Design/Design_M6_Packaging_and_Distribution.md) *(Bản thiết kế chi tiết DD-M6-PACKAGING-DISTRIBUTION đã phê duyệt)*
- **Tài liệu điều tra liên kết (Investigation References):**
  - [**`05_Packaging_and_Cross_Distro_Investigation.md`**](../1.Investigation/05_Packaging_and_Cross_Distro_Investigation.md)

---

## 1. MỤC TIÊU CỘT MỐC 6
Cung cấp các gói cài đặt hoàn chỉnh cho người dùng cuối trên hệ Debian và Fedora dưới dạng Single-File Self-Contained Binary (không bắt buộc cài đặt .NET SDK hoặc Runtime), đóng gói chuẩn `.deb` và `.rpm`, bản nén di động `.tar.gz`, đồng thời thiết lập quy trình tự động hóa kiểm thử và phát hành phiên bản mới qua GitHub Actions kèm mã băm SHA256.

---

## 2. DANH MỤC CÔNG VIỆC CHI TIẾT (TASK CHECKLIST)

### 2.1. Thiết kế Chi tiết & Nghiên cứu Kỹ thuật
*[Căn cứ thiết kế: `Design_M6_Packaging_and_Distribution.md` - Toàn văn bản vẽ DD-M6-PACKAGING-DISTRIBUTION]*
- [x] Soạn thảo tài liệu thiết kế chi tiết phân rã mô-đun đóng gói FHS, cấu hình .NET Publish và pipeline GitHub Actions.
- [x] Đặc tả chi tiết Hợp đồng siêu dữ liệu `DEBIAN/control`, RPM Spec, cấu trúc staging và ma trận kiểm thử nghiệm thu.

### 2.2. Cấu hình Xuất bản Nhị phân Độc lập (Single-File Self-Contained)
*[Căn cứ thiết kế: `Design_M6_Packaging_and_Distribution.md` - Mục 4.1: Thuật Toán Xuất Bản .NET Publish]*
- [ ] **Subtask 2.2.1 - Cấu hình Profile Xuất bản trong `PodmanFUI.App.fsproj`:**
  - Thiết lập thuộc tính `PublishSingleFile = true` (Gộp IL, metadata và native libs thành 1 file).
  - Thiết lập thuộc tính `SelfContained = true` (Đóng gói kèm toàn bộ runtime .NET 10, người dùng không cần cài đặt .NET).
  - Thiết lập thuộc tính `EnableCompressionInSingleFile = true` (Nén nhị phân giảm dung lượng xuống ~35MB).
  - Thiết lập thuộc tính `PublishTrimmed = false` (Đảm bảo an toàn phản xạ cho Terminal.Gui).
- [ ] **Subtask 2.2.2 - Kịch bản Biên dịch cho 2 Kiến trúc Vi xử lý:**
  - Tạo script biên dịch tự động cho `linux-x64` (Intel/AMD 64-bit).
  - Tạo script biên dịch tự động cho `linux-arm64` (ARM 64-bit, Raspberry Pi 4/5).

### 2.3. Hiện thực hóa Cấu trúc Đóng gói Chuẩn Linux
*[Căn cứ thiết kế: `Design_M6_Packaging_and_Distribution.md` - Mục 2.1, 2.2, 2.3 & Mục 3: Cấu Trúc FHS, Mục 4.2, 4.3]*
- [ ] **Subtask 2.3.1 - Đóng gói Debian (`packaging/debian/`):**
  - Soạn thảo tệp `packaging/debian/control` với đầy đủ các trường FHS: `Package`, `Version`, `Architecture`, `Depends: libc6 (>= 2.31), podman (>= 4.0.0)` theo Mục 3.1.
  - Soạn thảo kịch bản `packaging/debian/postinst` (Phân quyền `0755` cho `/usr/bin/podman-fui`, cập nhật `mandb -q`).
  - Soạn thảo kịch bản `packaging/debian/prerm` dọn dẹp khi gỡ cài đặt.
  - Hiện thực thuật toán dàn dựng thư mục staging FHS và đóng gói bằng lệnh `dpkg-deb --build --root-owner-group` theo Mục 4.2.
- [ ] **Subtask 2.3.2 - Đóng gói RPM (`packaging/rpm/`):**
  - Soạn thảo tệp `packaging/rpm/podman-fui.spec` với các khối: `Name`, `Version`, `Release`, `Summary`, `License: Apache-2.0`, `Requires: podman`, `%build`, `%install`, `%files` theo Mục 2.2.
  - Hiện thực thuật toán đóng gói RPM qua `rpmbuild -bb` theo Mục 4.3.
- [ ] **Subtask 2.3.3 - Trang hướng dẫn UNIX Manpage & Bản nén Di động Tarball:**
  - Soạn thảo tệp tài liệu `packaging/man/podman-fui.1` theo chuẩn groff/troff UNIX.
  - Soạn thảo script `packaging/tarball/install.sh` tự động cài binary vào `~/.local/bin/` không cần quyền sudo.
  - Kịch bản nén `podman-fui_<version>_<arch>.tar.gz` chứa binary, manpage, license và install.sh.

### 2.4. Thiết lập Pipeline Tự động hóa CI/CD trên GitHub Actions
*[Căn cứ thiết kế: `Design_M6_Packaging_and_Distribution.md` - Mục 2.4 & Mục 4.4: Lưu Đồ CI/CD]*
- [ ] **Subtask 2.4.1 - Workflow Tích hợp Liên tục (`.github/workflows/ci.yml`):**
  - Kích hoạt khi mở Pull Request hoặc đẩy commit vào nhánh `main`.
  - Thực thi kiểm tra định dạng mã nguồn (Fantomas).
  - Chạy `dotnet test` thực thi toàn bộ Unit Tests trên môi trường Ubuntu runner.
- [ ] **Subtask 2.4.2 - Workflow Tự động Phát hành (`.github/workflows/release.yml`):**
  - Kích hoạt khi có tag phiên bản mới (`git push origin v*.*.*`).
  - Job 1: Build và kiểm thử.
  - Job 2: Biên dịch Single-File Binary cho cả 2 kiến trúc `linux-x64` và `linux-arm64`.
  - Job 3: Đóng gói đồng thời: `.deb`, `.rpm`, `.tar.gz`.
  - Job 4: Tạo tệp mã băm kiểm tra tính toàn vẹn `sha256sum * > SHA256SUMS.txt`.
  - Job 5: Tự động khởi tạo GitHub Release, gắn changelog tự động và đính kèm đầy đủ toàn bộ các file artifact.

### 2.5. Kiểm thử Nghiệm thu Kỹ thuật (Technical Acceptance Testing)
*[Căn cứ thiết kế: `Design_M6_Packaging_and_Distribution.md` - Mục 6: Ma Trận Ca Kiểm Thử Nghiệm Thu]*
- [ ] **Subtask 2.5.1 - Thực thi kiểm thử ca TC-M6-01 (Cài đặt gói `.deb`):**
  - Chạy `sudo dpkg -i podman-fui_*.deb` trên môi trường không có .NET; gõ `podman-fui` chạy mượt mà ngay lập tức.
- [ ] **Subtask 2.5.2 - Thực thi kiểm thử ca TC-M6-02 (Cài đặt gói `.rpm`):**
  - Chạy `sudo dnf install podman-fui-*.rpm` trên máy Fedora; ứng dụng kết nối chuẩn xác tới rootless socket.
- [ ] **Subtask 2.5.3 - Thực thi kiểm thử ca TC-M6-03 (Bản nén di động Tarball):**
  - Người dùng không có quyền sudo chạy `./install.sh`; binary được đặt vào `~/.local/bin/podman-fui` và chạy bình thường.
- [ ] **Subtask 2.5.4 - Thực thi kiểm thử ca TC-M6-04 (Kiểm tra Manpage):**
  - Gõ lệnh `man podman-fui`; hiển thị đầy đủ tài liệu hướng dẫn cú pháp và phím tắt.
- [ ] **Subtask 2.5.5 - Thực thi kiểm thử ca TC-M6-05 (Kiểm tra tính toàn vẹn SHA256):**
  - Chạy `sha256sum -c SHA256SUMS.txt`; tất cả file artifact đều báo `OK`.

---

## 3. TIÊU CHÍ HOÀN THÀNH (DEFINITION OF DONE - DOD)

1. Xuất bản thành công file thực thi nhị phân độc lập Single-file Self-contained cho 2 kiến trúc `linux-x64` và `linux-arm64`, dung lượng file tối ưu khoảng 35MB.
2. Gói `.deb` cài đặt và gỡ bỏ hoàn hảo trên Debian/Ubuntu/Mint qua `dpkg`/`apt`.
3. Gói `.rpm` cài đặt và gỡ bỏ hoàn hảo trên Fedora/RHEL qua `dnf`/`rpm`.
4. Người dùng không cần cài đặt bất kỳ phiên bản .NET SDK hay Runtime nào trên máy vẫn chạy được ứng dụng.
5. Quy trình CI/CD trên GitHub Actions tự động hóa 100% từ khâu kiểm thử, đóng gói đa định dạng, tính mã băm SHA256 đến phát hành bản release.
6. Vượt qua toàn bộ các ca kiểm thử từ TC-M6-01 đến TC-M6-05.
