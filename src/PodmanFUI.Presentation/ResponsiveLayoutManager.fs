namespace PodmanFUI.Presentation

open PodmanFUI.Domain.NavigationModels

module ResponsiveLayoutManager =

    /// Các ngưỡng độ phân giải theo thiết kế DD-M2-CORE-MVP-DASHBOARD (Mục 4.2)
    type LayoutBreakpoint =
        | Compact     // < 80 cột hoặc < 24 dòng: Màn hình đơn (Stacked Single View)
        | Standard    // 80 - 120 cột: Bố cục song song 38% Sidebar - 62% Detail
        | Expanded    // > 120 cột: Màn hình rộng đầy đủ thông tin

    /// Tọa độ và kích thước của từng khung nhìn
    type ViewBounds =
        { X: int
          Y: int
          Width: int
          Height: int
          IsVisible: bool }

    /// Kế hoạch phân bổ toàn bộ khung nhìn giao diện
    type LayoutPlan =
        { Mode: LayoutBreakpoint
          TopBar: ViewBounds
          Sidebar: ViewBounds
          DetailPane: ViewBounds
          FooterBar: ViewBounds }

    /// Tính toán kế hoạch phân bổ tọa độ dựa trên kích thước thực tế của terminal
    let computeLayout (width: int) (height: int) (currentFocus: ActiveFocus) : LayoutPlan =
        let safeWidth = max 10 width
        let safeHeight = max 5 height

        // 1. Xác định Breakpoint
        let mode =
            if safeWidth < 80 || safeHeight < 24 then
                Compact
            elif safeWidth <= 120 then
                Standard
            else
                Expanded

        // 2. Tọa độ TopBar và FooterBar
        let topBar =
            { X = 0
              Y = 0
              Width = safeWidth
              Height = 1
              IsVisible = true }

        let footerBar =
            { X = 0
              Y = safeHeight - 1
              Width = safeWidth
              Height = 1
              IsVisible = true }

        let availHeight = max 1 (safeHeight - 2)

        // 3. Phân bổ không gian làm việc theo từng chế độ
        match mode with
        | Compact ->
            // Ở chế độ Compact: Màn hình đơn chồng nhau, chỉ hiển thị khung đang giữ tiêu điểm
            let isSidebarFocused = (currentFocus = Sidebar)
            let sidebar =
                { X = 0
                  Y = 1
                  Width = safeWidth
                  Height = availHeight
                  IsVisible = isSidebarFocused }

            let detail =
                { X = 0
                  Y = 1
                  Width = safeWidth
                  Height = availHeight
                  IsVisible = not isSidebarFocused }

            { Mode = mode
              TopBar = topBar
              Sidebar = sidebar
              DetailPane = detail
              FooterBar = footerBar }

        | Standard
        | Expanded ->
            // Bố cục song song Master-Detail: Sidebar chiếm ~38% (giới hạn từ 28 đến 50 cột)
            let rawSidebarWidth = (safeWidth * 38) / 100
            let sidebarWidth = min 50 (max 28 rawSidebarWidth)
            let detailWidth = max 10 (safeWidth - sidebarWidth)

            let sidebar =
                { X = 0
                  Y = 1
                  Width = sidebarWidth
                  Height = availHeight
                  IsVisible = true }

            let detail =
                { X = sidebarWidth
                  Y = 1
                  Width = detailWidth
                  Height = availHeight
                  IsVisible = true }

            { Mode = mode
              TopBar = topBar
              Sidebar = sidebar
              DetailPane = detail
              FooterBar = footerBar }
