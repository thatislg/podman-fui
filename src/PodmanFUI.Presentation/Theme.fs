namespace PodmanFUI.Presentation

open Spectre.Console
open Terminal.Gui.Drawing

module Theme =

    // =========================================================================
    // 1. Spectre.Console Palette (Milestone 1 PoC)
    // =========================================================================
    let titleColor = Spectre.Console.Color.Cyan1
    let successColor = Spectre.Console.Color.Green
    let warningColor = Spectre.Console.Color.Yellow
    let failureColor = Spectre.Console.Color.Red
    let borderColor = Spectre.Console.Color.DeepSkyBlue1
    let labelColor = Spectre.Console.Color.Grey
    let valueColor = Spectre.Console.Color.White

    // =========================================================================
    // 2. Terminal.Gui v2 Color Palette (Milestone 2+ Cyberpunk / Modern Dark)
    // =========================================================================
    type GuiColor = Terminal.Gui.Drawing.Color

    // Định nghĩa bảng màu RGB chuẩn hiện đại
    let cCyan = GuiColor(0, 215, 255)         // #00D7FF: Cyan1 rực rỡ cho tiêu đề và tiêu điểm
    let cDeepSkyBlue = GuiColor(0, 150, 230)  // #0096E6: Deep Sky Blue cho viền và điểm nhấn
    let cDarkNavy = GuiColor(15, 20, 30)      // #0F141E: Nền tối hiện đại sang trọng
    let cSelectBg = GuiColor(0, 80, 150)      // #005096: Nền hàng container được chọn
    let cTextWhite = GuiColor(240, 245, 250)  // #F0F5FA: Chữ sáng rõ nét
    let cTextDim = GuiColor(140, 150, 165)    // #8C96A5: Chữ phụ, hướng dẫn
    let cGreen = GuiColor(50, 205, 50)        // #32CD32: Running dot
    let cYellow = GuiColor(255, 200, 50)      // #FFC832: Paused dot / Warning
    let cRed = GuiColor(240, 60, 60)          // #F03C3C: Dead / Error / Delete

    let makeScheme (normFg: GuiColor) (normBg: GuiColor) (focusFg: GuiColor) (focusBg: GuiColor) =
        let mutable nF = normFg
        let mutable nB = normBg
        let mutable fF = focusFg
        let mutable fB = focusBg
        let normAttr = Attribute(&nF, &nB)
        let focusAttr = Attribute(&fF, &fB)
        Scheme(Normal = normAttr, Focus = focusAttr, HotNormal = focusAttr, HotFocus = focusAttr, Highlight = focusAttr)

    /// Scheme cho khung viền khi đang được Focus (viền Cyan sáng rực rỡ)
    let panelFocusedScheme =
        makeScheme cCyan cDarkNavy cTextWhite cSelectBg

    /// Scheme cho khung viền khi KHÔNG có Focus (viền dịu mắt)
    let panelUnfocusedScheme =
        makeScheme cTextDim cDarkNavy cTextWhite cDarkNavy

    /// Scheme cho thanh điều hướng TopBar (nền Dark Navy, chữ DeepSkyBlue)
    let topBarScheme =
        makeScheme cCyan cDarkNavy cTextWhite cSelectBg

    /// Scheme cho thanh Footer (nền Dark Navy, chữ hướng dẫn rõ ràng)
    let footerScheme =
        makeScheme cTextDim cDarkNavy cCyan cDarkNavy

    /// Scheme cho danh sách ListView container (hàng được chọn nổi bật trên nền xanh navy)
    let listViewScheme =
        makeScheme cTextWhite cDarkNavy cTextWhite cSelectBg

    /// Scheme cho khung Text hiển thị Inspect / Logs / Env
    let detailTextScheme =
        makeScheme cTextWhite cDarkNavy cCyan cDarkNavy

    /// Scheme cho hộp thoại Modal xác nhận xóa (viền cảnh báo đỏ)
    let modalConfirmScheme =
        makeScheme cRed cDarkNavy cTextWhite cRed

    /// Scheme cho hộp thoại Modal lọc tìm kiếm (viền Cyan)
    let modalFilterScheme =
        makeScheme cCyan cDarkNavy cTextWhite cSelectBg
