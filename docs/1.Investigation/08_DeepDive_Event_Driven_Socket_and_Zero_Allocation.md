# ĐIỀU TRA KỸ THUẬT 08: KIẾN TRÚC HƯỚNG SỰ KIỆN (EVENT-DRIVEN) & TỐI ƯU HÓA BỘ NHỚ ZERO-ALLOCATION
## DỰ ÁN: PODMAN-FUI

- **Mã tài liệu:** INV-08-EVENT-DRIVEN-ZERO-ALLOC
- **Trạng thái:** Kế hoạch dự phòng / Chờ thực hiện (Scheduled)
- **Thời điểm tiến hành:** **Milestone 3** (khi hoàn thiện luồng dữ liệu thời gian thực) và **Milestone 5** (khi tối ưu hóa hiệu năng đỉnh cao và mức tiêu thụ tài nguyên)

---

## 1. MỤC TIÊU ĐIỀU TRA
Tài liệu này xác định phương án kỹ thuật chuyên sâu nhằm giải quyết hai vấn đề cốt lõi về hiệu năng và tài nguyên hệ thống:
1. Chuyển đổi hoàn toàn từ mô hình thăm dò định kỳ (Polling) sang mô hình **Hướng sự kiện (Event-Driven)** thông qua Podman Event Stream API, giúp mức chiếm dụng CPU tiệm cận **0.0%** khi ứng dụng ở trạng thái nhàn rỗi (idle).
2. Áp dụng các kỹ thuật quản lý bộ nhớ **Zero-Allocation / Low-Allocation** của .NET, triệt tiêu áp lực dọn rác của Garbage Collector (GC), ngăn chặn hiện tượng giật khung hình (micro-stutter) khi stream log dung lượng lớn.

---

## 2. BỐI CẢNH & CÁC NGUY CƠ KỸ THUẬT CỐT LÕI

### 2.1. Tác hại của Mô hình Thăm dò (Polling Overhead)
- Nếu ứng dụng thực hiện gọi API định kỳ (`GET /libpod/containers/json` cứ 1 đến 2 giây một lần):
  - Podman Engine phải đọc liên tục hệ thống tệp `/var/lib/containers` và quét các cgroup của hệ điều hành.
  - Khi có nhiều container chạy đồng thời, việc này tạo ra tải I/O liên tục, gây nóng máy và tiêu hao pin vô ích ngay cả khi không có bất kỳ container nào thay đổi trạng thái.

### 2.2. Áp lực Dọn rác (GC Pressure) khi Xử lý Luồng Dữ liệu Lớn
- Khi một container in ra hàng chục nghìn dòng log mỗi giây (ví dụ stress test hoặc log debug):
  - Việc cấp phát các mảng `byte[]` tạm thời và tạo mới các đối tượng `string` liên tục sẽ làm đầy bộ nhớ thế hệ 0 (Gen 0) và thế hệ 1 (Gen 1) của .NET GC.
  - Quá trình GC Pause (tạm dừng luồng để thu gom rác) dù chỉ diễn ra trong vài mili-giây cũng đủ làm đơ giao diện TUI, gây cảm giác lag giật khi người dùng bấm phím điều hướng.

---

## 3. DANH MỤC HẠNG MỤC CẦN ĐIỀU TRA CHI TIẾT

- [ ] **Đặc tả luồng sự kiện thời gian thực (Podman Event Stream API):**
  - Endpoint khảo sát: `GET /v4.0.0/libpod/events?stream=true`.
  - Phân tích cấu trúc sự kiện JSON:
    - Loại thực thể (`Type`): `container`, `pod`, `image`, `volume`, `network`.
    - Hành động (`Action`): `create`, `start`, `stop`, `die`, `pause`, `unpause`, `remove`, `prune`.
    - Dấu thời gian (`Time`) và định danh thực thể (`Actor.ID`, `Actor.Attributes`).
  - Xây dựng quy tắc phản ứng UI (Reactive Trigger Matrix):
    - Chỉ kích hoạt nạp lại danh sách Container khi nhận được sự kiện loại `container` với hành vi làm thay đổi vòng đời.
    - Duy trì trạng thái nghỉ hoàn toàn (Sleep/Await) khi không có sự kiện phát sinh.
- [ ] **Khảo sát cơ chế Bộ đệm Tái sử dụng (Memory Pooling):**
  - Sử dụng cơ chế cấp phát bộ nhớ tái sử dụng (`ArrayPool` của .NET) để mượn và trả các mảng byte đệm khi đọc socket, không cấp phát mảng mới trên Heap.
  - Ứng dụng cấu trúc dữ liệu con trỏ bộ nhớ ảo (`Span` và `ReadOnlyMemory`) để cắt lát và phân tích dòng log trực tiếp trên vùng nhớ đệm mà không cần tạo mới chuỗi văn bản trung gian.
- [ ] **Cấu trúc Hàng đợi Vòng đệm Cố định (Fixed-size Ring Buffer):**
  - Thiết kế cấu trúc dữ liệu hàng đợi vòng lặp với dung lượng cố định cho danh sách dòng log.
  - Cơ chế trỏ chỉ số (Index Pointer): Khi đầy bộ đệm, dòng log mới nhất sẽ ghi đè trực tiếp vào ô nhớ của dòng log cũ nhất theo nguyên tắc FIFO mà không cần thực hiện thao tác xóa hay dồn mảng.

---

## 4. TIÊU CHÍ NGHIỆM THU (ACCEPTANCE CRITERIA)

1. Mức chiếm dụng CPU đo được qua lệnh `top` hoặc `htop` đối với tiến trình `podman-fui` phải duy trì dưới **0.5%** khi không có sự kiện container mới.
2. Ngay khi có sự kiện (ví dụ gõ lệnh `podman run` ở terminal bên ngoài), giao diện `podman-FUI` phải tự động cập nhật container mới trong vòng dưới **150ms**.
3. Khi stream log liên tục ở tốc độ 5.000 dòng/giây trong 5 phút, mức chiếm dụng bộ nhớ RAM của ứng dụng không được tăng lũy tiến (tuyệt đối không bị Memory Leak) và không xảy ra hiện tượng GC Pause quá 10ms.
