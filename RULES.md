# QUY CHUẨN VÀ NGUYÊN TẮC DỰ ÁN (PROJECT RULES & CONVENTIONS)
## DỰ ÁN: PODMAN-FUI (F# TERMINAL USER INTERFACE FOR PODMAN)

- **Mã tài liệu:** PDM-FUI-PROJECT-RULES
- **Vị trí lưu trữ:** `RULES.md` (Thư mục gốc Repository)
- **Phiên bản:** 1.0.0
- **Ngày ban hành:** 2026-10-05
- **Đối tượng áp dụng:** Toàn bộ thành viên phát triển và các tác nhân AI (Antigravity Agents).

---

## 1. QUY ĐỊNH VỀ NGÔN NGỮ (LANGUAGE POLICY)

### 1.1. Trong Mã Nguồn Lập Trình (`src/`)
- **Thông điệp hiển thị & Giao diện (User-Facing Text):**
  - **BẮT BUỘC 100% TIẾNG ANH (ENGLISH ONLY):** Toàn bộ chuỗi ký tự hiển thị ra màn hình người dùng, lệnh in (`printfn`, `eprintfn`, `writeln`), thông điệp log (`AnsiConsole`, Logger), nhãn bảng, nút bấm, mã lỗi, nội dung ngoại lệ (`Exception.Message`), và hướng dẫn khắc phục (`Remediation / Advice`) phải được viết bằng **Tiếng Anh**.
- **Tên Định danh Mã Nguồn (Identifiers):**
  - Toàn bộ tên Namespace, Module, Type, Class, Interface, Function, Variable, Parameter phải tuân thủ chuẩn quy ước đặt tên của F# / .NET bằng **Tiếng Anh**.
- **Chú thích Mã Nguồn (Code Comments):**
  - **ĐƯỢC PHÉP DÙNG TIẾNG VIỆT:** Chỉ duy nhất các dòng bình luận mã nguồn (`/// XML doc comments`, `// inline comments`, `(* block comments *)`) được phép sử dụng **Tiếng Việt** để giải thích thuật toán, nghiệp vụ, và ngữ cảnh kỹ thuật.

### 1.2. Trong Tài Liệu Dự Án (`docs/`)
- **Tài liệu Kỹ thuật & Báo cáo:**
  - Toàn bộ tài liệu Đặc tả Yêu cầu ([`SRS_podman-FUI.md`](docs/1.Investigation/SRS_podman-FUI.md)), tài liệu Khảo sát (`1.Investigation/`), Thiết kế (`2.Design/`), Tiến độ (`3.Progress/`), và Báo cáo (`4.Report/`) được soạn thảo bằng **Tiếng Việt**.
  - Các thuật ngữ chuyên ngành công nghệ, tên giao diện, phím tắt, và tên hàm/kiểu dữ liệu giữ nguyên gốc **Tiếng Anh**.

---

## 2. NGUYÊN TẮC THIẾT KẾ: ZERO CODE SAMPLES TRONG TÀI LIỆU

- **TUYỆT ĐỐI KHÔNG CHÈN ĐOẠN CODE MẪU:** Không sử dụng các khối mã lập trình thực thi (`let ...`, `type ... = { ... }`, `code blocks`) trong các tệp tài liệu khảo sát, thiết kế và tiến độ.
- **Phương thức đặc tả thay thế:**
  1. Danh mục phân rã tệp và mô-đun (Module Inventory).
  2. Bảng hợp đồng dữ liệu nguyên tử (Data Contracts Table: trường dữ liệu, kiểu dữ liệu, ánh xạ JSON, quy tắc kiểm tra).
  3. Thuật toán tuần tự từng bước (Procedural Steps: 1, 2, 3, 4...).
  4. Ma trận chuyển đổi trạng thái (FSM State Transitions) và Ma trận mã lỗi (Error Matrix).
  5. Đặc tả bố cục khung nhìn (Spectre / Terminal.Gui Layout) và Ma trận ca kiểm thử nghiệm thu (Test Cases Matrix).

---

## 3. QUY TRÌNH PHÁT TRIỂN & CHẤT LƯỢNG (DEVELOPMENT & QUALITY GATES)

### 3.1. Thứ tự Triển khai Bắt buộc
Mỗi Cột mốc (Milestone) phải tuân thủ nghiêm ngặt quy trình 3 bước:
1. **Khảo sát (Investigation):** Hoàn thành tài liệu phân tích kỹ thuật cơ sở và chuyên sâu.
2. **Thiết kế Chi tiết (Detail Design - DD):** Nâng cấp tài liệu thiết kế lên mức chi tiết (DD) với đầy đủ cấu trúc dữ liệu nguyên tử, thuật toán, ma trận lỗi, và test cases. **Chỉ bắt đầu viết code khi bản Thiết kế Chi tiết đã được phê duyệt.**
3. **Hiện thực hóa & Nghiệm thu (Implementation & Acceptance):** Triển khai code bám sát 100% bản DD, vượt qua toàn bộ Test Cases, cập nhật danh mục subtask và tiêu chí DoD trong file tiến độ.

### 3.2. Quản Trị Mã Nguồn Git
- Trước khi bắt đầu một chu trình phát triển mới hoặc sau khi hoàn tất một nhóm công việc, phải kiểm tra trạng thái, commit sạch sẽ với thông điệp Conventional Commits (`feat:`, `docs:`, `chore:`, `fix:`) và push lên remote repository.
- Tuyệt đối không commit các thư mục build trung gian (`bin/`, `obj/`), tệp cấu hình IDE cá nhân (`.vs/`, `.idea/`, `.ionide/`). Cấu hình `.gitignore` phải luôn loại trừ triệt để các tệp này.
- Giữ vững tính toàn vẹn của tệp Solution chính `podman-FUI.sln`, đảm bảo lệnh `dotnet build` luôn đạt **0 Warning(s), 0 Error(s)**.

---

## 4. QUY CHUẨN GIAO DIỆN & TƯƠNG TÁC (TUI UX CONVENTIONS)

- **Co giãn Động Thích Ứng (Responsive Auto-scaling):** Không cố định độ phân giải màn hình. Giao diện phải tự động thích ứng mượt mà qua các ngưỡng Breakpoints:
  - `Compact`: Dưới 80 cột hoặc dưới 24 dòng -> Tự động chuyển chế độ màn hình đơn (Stacked Single View).
  - `Standard`: Từ 80 đến 120 cột -> Bố cục song song Master-Detail (38% danh sách - 62% chi tiết).
  - `Expanded`: Trên 120 cột -> Hiển thị đầy đủ toàn bộ các cột thông tin.
- **Trải nghiệm 1 Phím Nhấn (1-Keypress):** Các thao tác quản lý cốt lõi (`s`: Start/Stop, `r`: Restart, `p`: Pause, `d`: Delete, `e`: Shell) phản hồi tức thời dưới 2 giây.
- **An toàn Terminal PTY:** Mọi thao tác tạm dừng TUI để mở Interactive Shell vào container phải được bọc trong khối bảo vệ tối cao (`try ... finally`), luôn luôn hoàn nguyên cấu hình `termios` gốc và khôi phục giao diện hoàn chỉnh sau khi thoát shell.
