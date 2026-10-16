using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RepairServiceTracker;

// Lớp lưu thông tin một đơn sửa chữa
public sealed class RepairOrder
{
    public string MaDon { get; set; } = "";
    public string TenKhachHang { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public string LoaiThietBi { get; set; } = "";
    public string MoTaLoi { get; set; } = "";
    public DateTime NgayTiepNhan { get; set; }
    public decimal ChiPhiDuKien { get; set; }
    public string TrangThai { get; set; } = "Mới tiếp nhận";
    public string? DuongDanAnh { get; set; }
}

public class Form1 : Form
{
    // Danh sách các trạng thái của đơn sửa chữa
    private static readonly string[] DanhSachTrangThai =
    {
        "Mới tiếp nhận",
        "Đang sửa",
        "Chờ linh kiện",
        "Hoàn tất",
        "Đã hủy"
    };

    // Danh sách dữ liệu gốc và các thành phần hỗ trợ
    private readonly BindingList<RepairOrder> _orders = new();
    private readonly BindingSource _bindingSource = new();
    private readonly ErrorProvider _errorProvider = new();

    private RepairOrder? _selectedOrder;
    private string? _imagePath;
    private bool _refreshingGrid;

    // Các điều khiển trên Form
    private readonly TextBox txtMaDon = new();
    private readonly TextBox txtTenKhachHang = new();
    private readonly TextBox txtSoDienThoai = new();
    private readonly ComboBox cboLoaiThietBi = new();
    private readonly TextBox txtMoTaLoi = new();
    private readonly DateTimePicker dtpNgayTiepNhan = new();
    private readonly TextBox txtChiPhiDuKien = new();
    private readonly ComboBox cboTrangThai = new();
    private readonly PictureBox picThietBi = new();
    private readonly TextBox txtTimKiem = new();
    private readonly ComboBox cboLocTrangThai = new();
    private readonly DataGridView dgvDonSuaChua = new();

    // Các nhãn ở thanh trạng thái
    private readonly ToolStripStatusLabel lblSoDon = new();
    private readonly ToolStripStatusLabel lblTongChiPhi = new();

    public Form1()
    {
        // Thiết lập cửa sổ chính
        Text = "Repair Service Tracker - Quản lý đơn sửa chữa";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1280;
        Height = 800;
        MinimumSize = new Size(980, 620);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        AutoScaleMode = AutoScaleMode.Font;

        // Thiết lập thông báo lỗi dữ liệu
        _errorProvider.ContainerControl = this;
        _errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

        // Khởi tạo giao diện và liên kết dữ liệu
        BuildInterface();
        ConfigureGrid();
        ConfigureBinding();
        ApplyFilters();
    }

    // Tạo giao diện chính
    private void BuildInterface()
    {
        // MenuStrip
        var menuStrip = new MenuStrip();
        var menuFile = new ToolStripMenuItem("Tệp");
        var menuExportCsv =
            new ToolStripMenuItem("Xuất danh sách đang hiển thị ra CSV");
        var menuExit = new ToolStripMenuItem("Thoát");

        menuExportCsv.Click += (_, _) => ExportCsv();
        menuExit.Click += (_, _) => Close();

        menuFile.DropDownItems.Add(menuExportCsv);
        menuFile.DropDownItems.Add(new ToolStripSeparator());
        menuFile.DropDownItems.Add(menuExit);
        menuStrip.Items.Add(menuFile);

        MainMenuStrip = menuStrip;

        // StatusStrip
        var statusStrip = new StatusStrip();

        lblSoDon.Text = "Đang hiển thị: 0/0 đơn";
        lblSoDon.Spring = true;
        lblSoDon.TextAlign = ContentAlignment.MiddleLeft;

        lblTongChiPhi.Text =
            "Tổng chi phí dự kiến các đơn chưa hoàn tất: 0 đ";

        statusStrip.Items.Add(lblSoDon);
        statusStrip.Items.Add(new ToolStripStatusLabel(" | "));
        statusStrip.Items.Add(lblTongChiPhi);

        // Chia cửa sổ thành khu vực nhập và khu vực danh sách
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            // Do not set SplitterDistance or Panel min sizes here to avoid validation before parented
            FixedPanel = FixedPanel.Panel1,
            BorderStyle = BorderStyle.FixedSingle
        };

        BuildEntryPanel(split.Panel1);
        BuildListPanel(split.Panel2);

        // Bố cục tổng thể gồm MenuStrip, nội dung và StatusStrip
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        root.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(
            new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(
            new RowStyle(SizeType.AutoSize));

        root.Controls.Add(menuStrip, 0, 0);
        root.Controls.Add(split, 0, 1);
        root.Controls.Add(statusStrip, 0, 2);

        Controls.Add(root);

        // Now the SplitContainer has been parented and has a valid width; set min sizes and splitter distance
        // Use clamping to ensure SplitterDistance falls in a valid range
        split.Panel1MinSize = 390;
        split.Panel2MinSize = 300; // reduced if needed

        int desiredSplitter = 455;
        int minDistance = split.Panel1MinSize;
        int maxDistance = split.Width - split.Panel2MinSize;
        if (maxDistance < minDistance) maxDistance = minDistance;
        int finalDistance = desiredSplitter;
        if (finalDistance < minDistance) finalDistance = minDistance;
        if (finalDistance > maxDistance) finalDistance = maxDistance;

        try
        {
            split.SplitterDistance = finalDistance;
        }
        catch
        {
            // In rare cases layout may still not be ready; defer setting until load
            this.Load += (_, __) =>
            {
                try
                {
                    split.SplitterDistance = finalDistance;
                }
                catch
                {
                    // swallow - best effort to avoid crashing during initialization
                }
            };
        }
    }

    // Tạo khu vực nhập thông tin đơn sửa chữa
    private void BuildEntryPanel(Control parent)
    {
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(10)
        };

        var title = new Label
        {
            Text = "THÔNG TIN ĐƠN SỬA CHỮA",
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 10)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Padding = new Padding(0, 5, 5, 5),
            Margin = Padding.Empty
        };

        table.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 122F));
        table.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));

        // Cấu hình các TextBox
        ConfigureTextBox(txtMaDon);
        ConfigureTextBox(txtTenKhachHang);
        ConfigureTextBox(txtSoDienThoai);
        ConfigureTextBox(txtChiPhiDuKien);

        txtMaDon.PlaceholderText = "Ví dụ: SC001";
        txtTenKhachHang.PlaceholderText = "Nhập tên khách hàng";
        txtSoDienThoai.PlaceholderText = "Nhập số điện thoại";
        txtChiPhiDuKien.PlaceholderText = "Ví dụ: 1500000";

        // ComboBox loại thiết bị
        cboLoaiThietBi.DropDownStyle = ComboBoxStyle.DropDownList;
        cboLoaiThietBi.Dock = DockStyle.Fill;

        cboLoaiThietBi.Items.AddRange(new object[]
        {
            "Laptop",
            "Điện thoại",
            "Máy tính bảng",
            "Máy tính để bàn",
            "Máy in",
            "Thiết bị khác"
        });

        cboLoaiThietBi.SelectedIndex = 0;

        // TextBox mô tả lỗi
        txtMoTaLoi.Dock = DockStyle.Fill;
        txtMoTaLoi.Multiline = true;
        txtMoTaLoi.ScrollBars = ScrollBars.Vertical;
        txtMoTaLoi.Height = 72;
        txtMoTaLoi.PlaceholderText =
            "Mô tả triệu chứng hoặc lỗi thiết bị...";

        // Ngày tiếp nhận
        dtpNgayTiepNhan.Dock = DockStyle.Fill;
        dtpNgayTiepNhan.Format = DateTimePickerFormat.Custom;
        dtpNgayTiepNhan.CustomFormat = "dd/MM/yyyy";
        dtpNgayTiepNhan.Value = DateTime.Today;

        // ComboBox trạng thái
        cboTrangThai.DropDownStyle = ComboBoxStyle.DropDownList;
        cboTrangThai.Dock = DockStyle.Fill;
        cboTrangThai.Items.AddRange(
            DanhSachTrangThai.Cast<object>().ToArray());
        cboTrangThai.SelectedIndex = 0;

        // PictureBox hiển thị ảnh thiết bị đúng tỷ lệ
        picThietBi.Size = new Size(155, 105);
        picThietBi.SizeMode = PictureBoxSizeMode.Zoom;
        picThietBi.BorderStyle = BorderStyle.FixedSingle;
        picThietBi.BackColor = SystemColors.ControlLight;
        picThietBi.Margin = new Padding(0, 0, 8, 0);

        // Khu vực ảnh và nút chọn ảnh
        var imagePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var btnChonAnh = MakeButton("Chọn ảnh...", 105);
        btnChonAnh.Height = 32;
        btnChonAnh.Margin = Padding.Empty;
        btnChonAnh.Click += (_, _) => SelectImage();

        imagePanel.Controls.Add(picThietBi);
        imagePanel.Controls.Add(btnChonAnh);

        // Thêm các trường nhập vào bảng
        AddField(table, "Mã đơn:", txtMaDon, 32);
        AddField(table, "Tên khách hàng:", txtTenKhachHang, 32);
        AddField(table, "Số điện thoại:", txtSoDienThoai, 32);
        AddField(table, "Loại thiết bị:", cboLoaiThietBi, 32);
        AddField(table, "Mô tả lỗi:", txtMoTaLoi, 78);
        AddField(table, "Ngày tiếp nhận:", dtpNgayTiepNhan, 32);
        AddField(table, "Chi phí dự kiến:", txtChiPhiDuKien, 32);
        AddField(table, "Trạng thái:", cboTrangThai, 32);
        AddField(table, "Ảnh thiết bị:", imagePanel, 115);

        // Các nút chức năng
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 12, 0, 0),
            Margin = Padding.Empty
        };

        var btnThem = MakeButton("Thêm đơn", 95);
        var btnCapNhat = MakeButton("Cập nhật", 95);
        var btnXoa = MakeButton("Xóa đơn", 85);
        var btnMoi = MakeButton("Làm mới", 85);

        btnThem.Click += (_, _) => AddOrder();
        btnCapNhat.Click += (_, _) => UpdateOrder();
        btnXoa.Click += (_, _) => DeleteOrder();
        btnMoi.Click += (_, _) => ClearForm();

        buttonPanel.Controls.AddRange(
            new Control[] { btnThem, btnCapNhat, btnXoa, btnMoi });

        // Sắp xếp các thành phần của khu vực nhập
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };

        content.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));
        content.RowStyles.Add(
            new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(
            new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(
            new RowStyle(SizeType.AutoSize));

        title.Margin = new Padding(0, 0, 0, 8);
        table.Dock = DockStyle.Fill;

        content.Controls.Add(title, 0, 0);
        content.Controls.Add(table, 0, 1);
        content.Controls.Add(buttonPanel, 0, 2);

        scrollPanel.Controls.Add(content);
        parent.Controls.Add(scrollPanel);
    }

    // Tạo khu vực tìm kiếm, lọc và danh sách đơn
    private void BuildListPanel(Control parent)
    {
        var rightLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
            Margin = Padding.Empty
        };

        rightLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));
        rightLayout.RowStyles.Add(
            new RowStyle(SizeType.AutoSize));
        rightLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));

        var searchLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 8)
        };

        searchLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 85F));
        searchLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));
        searchLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 90F));
        searchLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 150F));

        searchLayout.RowStyles.Add(
            new RowStyle(SizeType.AutoSize));
        searchLayout.RowStyles.Add(
            new RowStyle(SizeType.AutoSize));

        var lblSearch = new Label
        {
            Text = "Tìm kiếm:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 7, 5, 5)
        };

        txtTimKiem.Dock = DockStyle.Fill;
        txtTimKiem.PlaceholderText =
            "Mã đơn, tên khách hàng hoặc số điện thoại...";
        txtTimKiem.Margin = new Padding(0, 2, 8, 5);

        var lblFilter = new Label
        {
            Text = "Trạng thái:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 7, 5, 5)
        };

        cboLocTrangThai.Dock = DockStyle.Fill;
        cboLocTrangThai.DropDownStyle = ComboBoxStyle.DropDownList;
        cboLocTrangThai.Items.Add("Tất cả trạng thái");
        cboLocTrangThai.Items.AddRange(
            DanhSachTrangThai.Cast<object>().ToArray());
        cboLocTrangThai.SelectedIndex = 0;
        cboLocTrangThai.Margin = new Padding(0, 2, 0, 5);

        searchLayout.Controls.Add(lblSearch, 0, 0);
        searchLayout.Controls.Add(txtTimKiem, 1, 0);
        searchLayout.Controls.Add(lblFilter, 2, 0);
        searchLayout.Controls.Add(cboLocTrangThai, 3, 0);

        var hint = new Label
        {
            Text = "Chọn một dòng trong bảng để xem hoặc cập nhật thông tin đơn.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 0, 0, 3)
        };

        searchLayout.Controls.Add(hint, 0, 1);
        searchLayout.SetColumnSpan(hint, 4);

        // DataGridView hiển thị danh sách
        dgvDonSuaChua.Dock = DockStyle.Fill;
        dgvDonSuaChua.Margin = Padding.Empty;

        rightLayout.Controls.Add(searchLayout, 0, 0);
        rightLayout.Controls.Add(dgvDonSuaChua, 0, 1);
        parent.Controls.Add(rightLayout);

        // Tìm kiếm ngay khi người dùng nhập
        txtTimKiem.TextChanged +=
            (_, _) => ApplyFilters(selectFirst: true);

        cboLocTrangThai.SelectedIndexChanged +=
            (_, _) => ApplyFilters(selectFirst: true);

        // Chọn dòng để nạp thông tin lên Form
        dgvDonSuaChua.SelectionChanged += (_, _) =>
        {
            if (_refreshingGrid)
                return;

            if (dgvDonSuaChua.CurrentRow?.DataBoundItem
                is RepairOrder order)
            {
                _selectedOrder = order;
                LoadOrderToForm(order);
            }
        };
    }

    // Thiết lập cách hiển thị cho TextBox
    private static void ConfigureTextBox(TextBox textBox)
    {
        textBox.Dock = DockStyle.Fill;
        textBox.Margin = new Padding(0, 2, 0, 5);
    }

    // Tạo nút bấm
    private static Button MakeButton(string text, int width)
    {
        return new Button
        {
            Text = text,
            Width = width,
            Height = 34,
            AutoSize = false,
            Margin = new Padding(0, 0, 6, 6),
            UseVisualStyleBackColor = true
        };
    }

    // Thêm một trường gồm nhãn và điều khiển nhập
    private static void AddField(
        TableLayoutPanel table,
        string labelText,
        Control control,
        int rowHeight)
    {
        int row = table.RowCount++;

        table.RowStyles.Add(
            new RowStyle(SizeType.Absolute, rowHeight));

        var label = new Label
        {
            Text = labelText,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 2, 8, 5)
        };

        control.Margin = control is TextBox
            ? control.Margin
            : new Padding(0, 2, 0, 5);

        table.Controls.Add(label, 0, row);
        table.Controls.Add(control, 1, row);
    }

    // Cấu hình DataGridView với các cột thủ công
    private void ConfigureGrid()
    {
        dgvDonSuaChua.AutoGenerateColumns = false;
        dgvDonSuaChua.AllowUserToAddRows = false;
        dgvDonSuaChua.AllowUserToDeleteRows = false;
        dgvDonSuaChua.ReadOnly = true;
        dgvDonSuaChua.MultiSelect = false;
        dgvDonSuaChua.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;
        dgvDonSuaChua.RowHeadersVisible = false;
        dgvDonSuaChua.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;
        dgvDonSuaChua.BackgroundColor = SystemColors.Window;
        dgvDonSuaChua.BorderStyle = BorderStyle.FixedSingle;
        dgvDonSuaChua.EnableHeadersVisualStyles = true;
        dgvDonSuaChua.Columns.Clear();

        AddGridColumn("MaDon", "Mã đơn", 70, 0.8F);
        AddGridColumn("TenKhachHang", "Khách hàng", 120, 1.2F);
        AddGridColumn("SoDienThoai", "Số điện thoại", 100, 1F);
        AddGridColumn("LoaiThietBi", "Thiết bị", 95, 1F);
        AddGridColumn("MoTaLoi", "Mô tả lỗi", 140, 1.5F);

        var dateColumn = AddGridColumn(
            "NgayTiepNhan", "Ngày tiếp nhận", 95, 1F);

        dateColumn.DefaultCellStyle.Format = "dd/MM/yyyy";

        var costColumn = AddGridColumn(
            "ChiPhiDuKien", "Chi phí (đ)", 100, 1F);

        // Định dạng chi phí không có chữ số thập phân
        costColumn.DefaultCellStyle.Format = "N0";
        costColumn.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleRight;

        AddGridColumn("TrangThai", "Trạng thái", 100, 1F);
    }

    // Tạo một cột trong DataGridView
    private DataGridViewTextBoxColumn AddGridColumn(
        string propertyName,
        string headerText,
        int minWidth,
        float fillWeight)
    {
        var column = new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            Name = propertyName,
            HeaderText = headerText,
            MinimumWidth = minWidth,
            FillWeight = fillWeight,
            SortMode = DataGridViewColumnSortMode.Automatic
        };

        dgvDonSuaChua.Columns.Add(column);
        return column;
    }

    // Liên kết dữ liệu với BindingSource
    private void ConfigureBinding()
    {
        _bindingSource.DataSource = _orders;
        dgvDonSuaChua.DataSource = _bindingSource;
    }

    // Tìm kiếm và lọc danh sách
    private void ApplyFilters(
        RepairOrder? preferredOrder = null,
        bool selectFirst = false)
    {
        string keyword = txtTimKiem.Text.Trim();

        string statusFilter =
            cboLocTrangThai.SelectedItem?.ToString()
            ?? "Tất cả trạng thái";

        // Chỉ giữ những đơn phù hợp với từ khóa và trạng thái
        var visibleOrders = _orders.Where(order =>
        {
            bool matchesText =
                keyword.Length == 0
                || order.MaDon.Contains(
                    keyword, StringComparison.OrdinalIgnoreCase)
                || order.TenKhachHang.Contains(
                    keyword, StringComparison.OrdinalIgnoreCase)
                || order.SoDienThoai.Contains(
                    keyword, StringComparison.OrdinalIgnoreCase);

            bool matchesStatus =
                statusFilter == "Tất cả trạng thái"
                || string.Equals(
                    order.TrangThai,
                    statusFilter,
                    StringComparison.Ordinal);

            return matchesText && matchesStatus;
        }).ToList();

        _refreshingGrid = true;

        // Tạo danh sách hiển thị theo điều kiện lọc
        _bindingSource.DataSource =
            new BindingList<RepairOrder>(visibleOrders);

        dgvDonSuaChua.ClearSelection();
        dgvDonSuaChua.CurrentCell = null;
        _selectedOrder = null;

        RepairOrder? orderToSelect = null;

        if (preferredOrder != null
            && visibleOrders.Any(
                item => ReferenceEquals(item, preferredOrder)))
        {
            orderToSelect = preferredOrder;
        }
        else if (selectFirst && visibleOrders.Count > 0)
        {
            orderToSelect = visibleOrders[0];
        }

        // Khôi phục dòng được chọn nếu dòng đó còn trong danh sách
        if (orderToSelect != null)
        {
            int rowIndex = visibleOrders.FindIndex(
                item => ReferenceEquals(item, orderToSelect));

            if (rowIndex >= 0
                && rowIndex < dgvDonSuaChua.Rows.Count)
            {
                dgvDonSuaChua.CurrentCell =
                    dgvDonSuaChua.Rows[rowIndex].Cells[0];

                dgvDonSuaChua.Rows[rowIndex].Selected = true;

                _selectedOrder = orderToSelect;
                LoadOrderToForm(orderToSelect);
            }
        }
        else
        {
            ClearFormFields();
        }

        _refreshingGrid = false;

        _bindingSource.ResetBindings(false);
        UpdateStatusStrip(visibleOrders.Count);
    }

    // Cập nhật số lượng đơn và tổng chi phí
    private void UpdateStatusStrip(int visibleCount)
    {
        lblSoDon.Text =
            $"Đang hiển thị: {visibleCount}/{_orders.Count} đơn";

        // Không tính đơn đã hoàn tất hoặc đã hủy vào chi phí đang xử lý
        decimal totalPendingCost = _orders
            .Where(order =>
                order.TrangThai != "Hoàn tất"
                && order.TrangThai != "Đã hủy")
            .Sum(order => order.ChiPhiDuKien);

        lblTongChiPhi.Text =
            $"Tổng chi phí dự kiến các đơn chưa hoàn tất: {totalPendingCost:N0} đ";
    }

    // Kiểm tra dữ liệu trước khi thêm hoặc cập nhật
    // orderToIgnore là đơn hiện tại cần bỏ qua khi kiểm tra mã trùng lúc sửa.
    private bool ValidateForm(
        out string maDon,
        out string tenKhachHang,
        out string soDienThoai,
        out string loaiThietBi,
        out string moTaLoi,
        out decimal chiPhi,
        RepairOrder? orderToIgnore)
    {
        _errorProvider.Clear();

        bool isValid = true;

        maDon = txtMaDon.Text.Trim();

        // Sao chép mã đơn sang biến cục bộ.
        // Cách này tránh lỗi CS1628 khi dùng tham số out trong lambda.
        string maDonKiemTra = maDon;

        tenKhachHang = txtTenKhachHang.Text.Trim();
        soDienThoai = txtSoDienThoai.Text.Trim();

        loaiThietBi =
            cboLoaiThietBi.SelectedItem?.ToString() ?? "";

        moTaLoi = txtMoTaLoi.Text.Trim();
        chiPhi = 0;

        // Kiểm tra mã đơn
        if (maDon.Length == 0)
        {
            _errorProvider.SetError(
                txtMaDon,
                "Mã đơn không được để trống.");

            isValid = false;
        }
        else if (_orders.Any(order =>
            string.Equals(
                order.MaDon,
                maDonKiemTra,
                StringComparison.OrdinalIgnoreCase)
            && !ReferenceEquals(order, orderToIgnore)))
        {
            _errorProvider.SetError(
                txtMaDon,
                "Mã đơn đã tồn tại. Hãy nhập mã khác.");

            isValid = false;
        }

        // Kiểm tra tên khách hàng
        if (tenKhachHang.Length == 0)
        {
            _errorProvider.SetError(
                txtTenKhachHang,
                "Tên khách hàng không được để trống.");

            isValid = false;
        }

        // Kiểm tra số điện thoại
        if (soDienThoai.Length == 0)
        {
            _errorProvider.SetError(
                txtSoDienThoai,
                "Số điện thoại không được để trống.");

            isValid = false;
        }

        // Kiểm tra loại thiết bị
        if (cboLoaiThietBi.SelectedIndex < 0)
        {
            _errorProvider.SetError(
                cboLoaiThietBi,
                "Hãy chọn loại thiết bị.");

            isValid = false;
        }

        // Kiểm tra mô tả lỗi
        if (moTaLoi.Length == 0)
        {
            _errorProvider.SetError(
                txtMoTaLoi,
                "Mô tả lỗi là bắt buộc.");

            isValid = false;
        }

        // Kiểm tra chi phí là số hợp lệ và không âm
        string costText = txtChiPhiDuKien.Text.Trim();

        if (!decimal.TryParse(
                costText,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out chiPhi)
            && !decimal.TryParse(
                costText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out chiPhi))
        {
            _errorProvider.SetError(
                txtChiPhiDuKien,
                "Chi phí phải là một số hợp lệ và không âm.");

            isValid = false;
        }
        else if (chiPhi < 0)
        {
            _errorProvider.SetError(
                txtChiPhiDuKien,
                "Chi phí dự kiến không được là số âm.");

            isValid = false;
        }

        // Kiểm tra trạng thái
        if (cboTrangThai.SelectedIndex < 0)
        {
            _errorProvider.SetError(
                cboTrangThai,
                "Hãy chọn trạng thái đơn.");

            isValid = false;
        }

        return isValid;
    }

    // Tạo đối tượng đơn sửa chữa từ thông tin nhập trên Form
    private RepairOrder BuildOrderFromForm(
        string maDon,
        string tenKhachHang,
        string soDienThoai,
        string loaiThietBi,
        string moTaLoi,
        decimal chiPhi)
    {
        return new RepairOrder
        {
            MaDon = maDon,
            TenKhachHang = tenKhachHang,
            SoDienThoai = soDienThoai,
            LoaiThietBi = loaiThietBi,
            MoTaLoi = moTaLoi,
            NgayTiepNhan = dtpNgayTiepNhan.Value.Date,
            ChiPhiDuKien = chiPhi,
            TrangThai =
                cboTrangThai.SelectedItem?.ToString()
                ?? "Mới tiếp nhận",
            DuongDanAnh = _imagePath
        };
    }

    // Thêm đơn sửa chữa
    private void AddOrder()
    {
        // Khi thêm mới, không bỏ qua đơn nào trong kiểm tra mã trùng.
        if (!ValidateForm(
                out string maDon,
                out string ten,
                out string phone,
                out string device,
                out string description,
                out decimal cost,
                orderToIgnore: null))
        {
            return;
        }

        RepairOrder order = BuildOrderFromForm(
            maDon, ten, phone, device, description, cost);

        _orders.Add(order);

        // Xóa từ khóa và bỏ bộ lọc để thấy đơn vừa thêm
        txtTimKiem.Clear();
        cboLocTrangThai.SelectedIndex = 0;

        ApplyFilters(order, selectFirst: true);

        MessageBox.Show(
            "Đã thêm đơn sửa chữa.",
            "Thông báo",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    // Cập nhật đơn đang chọn
    private void UpdateOrder()
    {
        if (_selectedOrder == null)
        {
            MessageBox.Show(
                "Hãy chọn một đơn trong bảng trước khi cập nhật.",
                "Chưa chọn đơn",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        // Khi cập nhật, chỉ bỏ qua chính đơn đang sửa khi kiểm tra mã trùng.
        if (!ValidateForm(
                out string maDon,
                out string ten,
                out string phone,
                out string device,
                out string description,
                out decimal cost,
                _selectedOrder))
        {
            return;
        }

        RepairOrder order = _selectedOrder;

        order.MaDon = maDon;
        order.TenKhachHang = ten;
        order.SoDienThoai = phone;
        order.LoaiThietBi = device;
        order.MoTaLoi = description;
        order.NgayTiepNhan = dtpNgayTiepNhan.Value.Date;
        order.ChiPhiDuKien = cost;
        order.TrangThai =
            cboTrangThai.SelectedItem?.ToString()
            ?? "Mới tiếp nhận";
        order.DuongDanAnh = _imagePath;

        // Làm mới bảng và thống kê sau khi cập nhật
        ApplyFilters(order, selectFirst: true);

        MessageBox.Show(
            "Đã cập nhật đơn sửa chữa.",
            "Thông báo",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    // Xóa đơn đang chọn sau khi xác nhận
    private void DeleteOrder()
    {
        if (_selectedOrder == null)
        {
            MessageBox.Show(
                "Hãy chọn một đơn trong bảng trước khi xóa.",
                "Chưa chọn đơn",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        DialogResult result = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa đơn '{_selectedOrder.MaDon}' không?",
            "Xác nhận xóa",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        _orders.Remove(_selectedOrder);

        ApplyFilters(selectFirst: true);
    }

    // Làm mới các trường nhập
    private void ClearForm()
    {
        _refreshingGrid = true;

        dgvDonSuaChua.ClearSelection();
        dgvDonSuaChua.CurrentCell = null;

        _selectedOrder = null;

        ClearFormFields();

        _refreshingGrid = false;
    }

    // Xóa nội dung các trường nhập và ảnh xem trước
    private void ClearFormFields()
    {
        _errorProvider.Clear();

        txtMaDon.Clear();
        txtTenKhachHang.Clear();
        txtSoDienThoai.Clear();

        if (cboLoaiThietBi.Items.Count > 0)
            cboLoaiThietBi.SelectedIndex = 0;

        txtMoTaLoi.Clear();

        dtpNgayTiepNhan.Value = DateTime.Today;
        txtChiPhiDuKien.Text = "0";

        if (cboTrangThai.Items.Count > 0)
            cboTrangThai.SelectedIndex = 0;

        _imagePath = null;

        SetPreviewImage(null);
    }

    // Nạp thông tin của đơn đang chọn lên Form
    private void LoadOrderToForm(RepairOrder order)
    {
        _selectedOrder = order;

        _errorProvider.Clear();

        txtMaDon.Text = order.MaDon;
        txtTenKhachHang.Text = order.TenKhachHang;
        txtSoDienThoai.Text = order.SoDienThoai;

        cboLoaiThietBi.SelectedItem = order.LoaiThietBi;

        txtMoTaLoi.Text = order.MoTaLoi;

        dtpNgayTiepNhan.Value = order.NgayTiepNhan;

        txtChiPhiDuKien.Text =
            order.ChiPhiDuKien.ToString(
                "0.##", CultureInfo.CurrentCulture);

        cboTrangThai.SelectedItem = order.TrangThai;

        _imagePath = order.DuongDanAnh;

        SetPreviewImage(_imagePath);
    }

    // Mở hộp thoại để chọn ảnh thiết bị
    private void SelectImage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh thiết bị",
            Filter =
                "Tệp ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Tất cả tệp|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            // Thử tải ảnh trước khi lưu đường dẫn
            using Image image = LoadImageWithoutLock(dialog.FileName);

            _imagePath = dialog.FileName;

            SetPreviewImage(_imagePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Không thể mở ảnh đã chọn.\nChi tiết: {ex.Message}",
                "Lỗi ảnh",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // Hiển thị ảnh mà không khóa file nguồn
    private void SetPreviewImage(string? path)
    {
        Image? oldImage = picThietBi.Image;

        picThietBi.Image = null;

        // Giải phóng ảnh cũ để tránh tốn bộ nhớ
        oldImage?.Dispose();

        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            picThietBi.Image = LoadImageWithoutLock(path);

            // Giữ đúng tỷ lệ ảnh
            picThietBi.SizeMode = PictureBoxSizeMode.Zoom;
        }
        catch (Exception)
        {
            _imagePath = null;
            picThietBi.Image = null;
        }
    }

    // Đọc ảnh thành bản sao để không giữ khóa file
    private static Image LoadImageWithoutLock(string path)
    {
        using Image source = Image.FromFile(path);

        return new Bitmap(source);
    }

    // Xuất danh sách hiện đang hiển thị ra CSV
    private void ExportCsv()
    {
        var visibleOrders = _bindingSource.List
            .Cast<object>()
            .OfType<RepairOrder>()
            .ToList();

        if (visibleOrders.Count == 0)
        {
            MessageBox.Show(
                "Không có đơn nào trong danh sách đang hiển thị để xuất.",
                "Không có dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Xuất danh sách đơn sửa chữa ra CSV",
            Filter = "CSV (*.csv)|*.csv",
            FileName = "DanhSachDonSuaChua.csv",
            DefaultExt = "csv",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            using var writer = new StreamWriter(
                dialog.FileName,
                false,
                new UTF8Encoding(true));

            // Ghi dòng tiêu đề
            writer.WriteLine(string.Join(",", new[]
            {
                "Mã đơn",
                "Tên khách hàng",
                "Số điện thoại",
                "Loại thiết bị",
                "Mô tả lỗi",
                "Ngày tiếp nhận",
                "Chi phí dự kiến",
                "Trạng thái",
                "Đường dẫn ảnh"
            }.Select(CsvEscape)));

            // Ghi các đơn đang được hiển thị
            foreach (RepairOrder order in visibleOrders)
            {
                string[] values =
                {
                    order.MaDon,
                    order.TenKhachHang,
                    order.SoDienThoai,
                    order.LoaiThietBi,
                    order.MoTaLoi,
                    order.NgayTiepNhan.ToString(
                        "dd/MM/yyyy", CultureInfo.InvariantCulture),
                    order.ChiPhiDuKien.ToString(
                        "0.##", CultureInfo.InvariantCulture),
                    order.TrangThai,
                    order.DuongDanAnh ?? ""
                };

                writer.WriteLine(
                    string.Join(",", values.Select(CsvEscape)));
            }

            MessageBox.Show(
                $"Đã xuất {visibleOrders.Count} đơn ra file CSV.",
                "Xuất dữ liệu thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Không thể xuất file CSV.\nChi tiết: {ex.Message}",
                "Lỗi xuất dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // Xử lý dấu phẩy, dấu ngoặc kép và xuống dòng trong CSV
    private static string CsvEscape(string value)
    {
        if (value.Contains(',')
            || value.Contains('"')
            || value.Contains('\n')
            || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}