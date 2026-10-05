# TỔNG QUAN LỘ TRÌNH PHÁT TRIỂN (ROADMAP OVERVIEW)
## DỰ ÁN: PODMAN-FUI (F# TERMINAL USER INTERFACE FOR PODMAN)

- **Mã tài liệu:** PDM-FUI-ROADMAP-01
- **Vị trí lưu trữ:** `docs/3.Progress/ROADMAP_OVERVIEW.md`
- **Phiên bản:** 1.2.0
- **Ngày cập nhật:** 2026-10-05
- **Trạng thái tổng thể:** **Milestone 1 & 2 - Đã hoàn thành (100%) | Chuyển tiếp Milestone 3**

---

## 1. MỤC TIÊU VÀ TẦM NHÌN DỰ ÁN

**podman-FUI** là công cụ giao diện dòng lệnh (TUI) hiện đại dành cho hệ sinh thái Podman, được phát triển bằng ngôn ngữ hàm **F# trên nền tảng .NET**, kết hợp sức mạnh của bộ đôi **Terminal.Gui v2** và **Spectre.Console**:
- **Sức mạnh Podman-native (Kế thừa từ `podman-tui`):** Quản lý toàn diện Pods (chuẩn Kubernetes), Containers, Images, Volumes, Networks, Secrets, System df/prune và tích hợp Quadlet.
- **Trải nghiệm người dùng 1-chạm (Kế thừa từ `lazydocker`):** Bố cục đa bảng trực quan, phím tắt nhanh một chạm, stream log trực tiếp, đồ thị tài nguyên Sparklines Unicode thời gian thực và hỗ trợ chuột hoàn chỉnh.
- **Khả năng co giãn động (Responsive Auto-scaling):** Tự động thích ứng mượt mà theo kích thước cửa sổ của bất kỳ terminal nào (non-fixed resolution).
- **Tính độc lập & Phân phối:** Single-file nhị phân tự chứa (Self-contained, không yêu cầu cài .NET Runtime), đóng gói chuẩn **`.deb`** (Debian/Ubuntu/Mint) và **`.rpm`** (Fedora/RHEL/CentOS).
- **Đa ngôn ngữ (i18n):** Ngôn ngữ thực thi mặc định là **Tiếng Anh (EN)** ở giai đoạn đầu, kiến trúc sẵn sàng mở rộng cho **Tiếng Việt (VI)** và **Tiếng Nhật (JA)**; tài liệu duy trì bằng **Tiếng Việt**.

---

## 2. MA TRẬN CÁC CỘT MỐC LỚN (MILESTONE MATRIX)

Dưới đây là ma trận tổng quan 6 cột mốc phát triển lớn của dự án. Chi tiết kế hoạch, danh mục công việc và tiêu chí nghiệm thu (DoD) của từng cột mốc được lưu trữ tại các tệp tài liệu lộ trình chuyên biệt tương ứng:

| Cột mốc | Tên cột mốc | Trọng tâm công việc | Tài liệu kỹ thuật liên kết | Bản thiết kế chi tiết (Detail Design) | Kế hoạch chi tiết từng phần | Trạng thái |
| :---: | :--- | :--- | :--- | :---: | :---: | :---: |
| **M1** | **Nền tảng & Khảo sát** | Nghiên cứu API, SRS, thiết lập Solution F#, PoC Unix Socket | `SRS`, `INV-01` -> `INV-06` | [**`DD-M1`**](../2.Design/Design_M1_Foundation_and_Socket.md) | [**`Milestone_1_Foundation_and_Investigation.md`**](Milestone_1_Foundation_and_Investigation.md) | **Đã hoàn thành (100%)** |
| **M2** | **Core MVP Dashboard** | Khung MVU (Elmish), layout co giãn, danh sách & thao tác Container | `INV-02`, `INV-03`, `INV-07` | [**`DD-M2`**](../2.Design/Design_M2_Core_MVP_Dashboard.md) | [**`Milestone_2_Core_MVP_Dashboard.md`**](Milestone_2_Core_MVP_Dashboard.md) | **Đã hoàn thành (100%)** |
| **M3** | **Realtime & Metrics** | Stream log có lọc ANSI, đồ thị Sparkline/Gauge, Event Stream | `INV-02`, `INV-07`, `INV-08` | [**`DD-M3`**](../2.Design/Design_M3_Realtime_Metrics_and_Logs.md) | [**`Milestone_3_Realtime_Metrics_and_Logs.md`**](Milestone_3_Realtime_Metrics_and_Logs.md) | **Sẵn sàng triển khai** |
| **M4** | **Podman-Native** | Quản lý Pods, Images, Volumes, Networks, System prune, Quadlet | `INV-01`, `INV-04`, `INV-09`, `INV-10` | [**`DD-M4`**](../2.Design/Design_M4_Podman_Native_Architecture.md) | [**`Milestone_4_Podman_Native_Features.md`**](Milestone_4_Podman_Native_Features.md) | *Đã có DD* |
| **M5** | **Hoàn thiện UX** | Vim-keys, chuột toàn diện, tự phục hồi socket, đa nền tảng | `INV-05`, `INV-06`, `INV-09` | [**`DD-M5`**](../2.Design/Design_M5_UX_Interaction_and_Resilience.md) | [**`Milestone_5_UX_Polish_and_Cross_Distro.md`**](Milestone_5_UX_Polish_and_Cross_Distro.md) | *Đã có DD* |
| **M6** | **Đóng gói & CI/CD** | Single-file binary, đóng gói `.deb`, `.rpm`, `.tar.gz`, GitHub CI | `INV-05` | [**`DD-M6`**](../2.Design/Design_M6_Packaging_and_Distribution.md) | [**`Milestone_6_Packaging_and_CICD.md`**](Milestone_6_Packaging_and_CICD.md) | *Đã có DD* |
