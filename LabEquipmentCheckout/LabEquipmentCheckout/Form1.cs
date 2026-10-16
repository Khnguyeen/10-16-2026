using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LabEquipmentCheckout
{
    // Lưu thông tin của một phiếu mượn thiết bị.
    public class PhieuMuon
    {
        public string MaPhieu { get; set; }
        public string MaSinhVien { get; set; }
        public string HoTenSinhVien { get; set; }
        public string ThietBi { get; set; }
        public int SoLuong { get; set; }
        public DateTime NgayMuon { get; set; }
        public DateTime HanTra { get; set; }
        public DateTime? NgayTraThucTe { get; set; }

        // Trạng thái được tính tự động, không nhập thủ công.
        public string TrangThai
        {
            get
            {
                if (NgayTraThucTe.HasValue)
                    return "Đã trả";

                if (HanTra.Date < DateTime.Today)
                    return "Quá hạn";

                return "Đang mượn";
            }
        }
    }

    public partial class Form1 : Form
    {
        // Danh sách chính và danh sách đang hiển thị sau khi tìm kiếm/lọc.
        private readonly BindingList<PhieuMuon> danhSach = new BindingList<PhieuMuon>();
        private readonly BindingList<PhieuMuon> danhSachHienThi = new BindingList<PhieuMuon>();
        private readonly BindingSource bindingSource = new BindingSource();
        private PhieuMuon phieuDangChon;

        // Các điều khiển của giao diện.
        private TextBox txtMaPhieu;
        private TextBox txtMaSinhVien;
        private TextBox txtHoTen;
        private ComboBox cboThietBi;
        private NumericUpDown nudSoLuong;
        private DateTimePicker dtpNgayMuon;
        private DateTimePicker dtpHanTra;
        private DateTimePicker dtpNgayTraThucTe;
        private TextBox txtTrangThai;
        private TextBox txtTimKiem;
        private ComboBox cboLocTrangThai;
        private DataGridView dgvPhieuMuon;
        private ErrorProvider errorProvider;
        private ToolStripStatusLabel lblTongSoPhieu;
        private ToolStripStatusLabel lblSoPhieuQuaHan;

        public Form1()
        {
            // Giao diện được tạo bằng code nên không cần kéo thả điều khiển trong Designer.
            TaoGiaoDien();

            bindingSource.DataSource = danhSachHienThi;
            dgvPhieuMuon.DataSource = bindingSource;

            CapNhatDanhSachHienThi();
            LamMoiForm();
        }

        private void TaoGiaoDien()
        {
            Text = "Lab Equipment Checkout - Quản lý mượn trả thiết bị";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1050, 700);
            Size = new Size(1250, 820);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            AutoScaleMode = AutoScaleMode.Font;

            // Bố cục chính tự co giãn khi thay đổi kích thước cửa sổ.
            TableLayoutPanel layoutChinh = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(8),
                Margin = new Padding(0)
            };
            layoutChinh.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutChinh.RowStyles.Add(new RowStyle(SizeType.Absolute, 300F));
            layoutChinh.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            layoutChinh.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            GroupBox grpThongTin = new GroupBox
            {
                Text = "Thông tin phiếu mượn",
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                Margin = new Padding(0, 0, 0, 6)
            };
            TableLayoutPanel bangNhap = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(5),
                Margin = new Padding(0)
            };
            bangNhap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            bangNhap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            bangNhap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            bangNhap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            for (int i = 0; i < 6; i++)
                bangNhap.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6667F));

            txtMaPhieu = new TextBox { Name = "txtMaPhieu" };
            txtMaSinhVien = new TextBox { Name = "txtMaSinhVien" };
            txtHoTen = new TextBox { Name = "txtHoTen" };

            cboThietBi = new ComboBox
            {
                Name = "cboThietBi",
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboThietBi.Items.AddRange(new object[]
            {
                "Laptop", "Máy tính để bàn", "Màn hình", "Bàn phím", "Chuột",
                "Tai nghe", "Máy chiếu", "Arduino", "Raspberry Pi", "Cáp mạng", "Khác"
            });
            cboThietBi.SelectedIndex = -1;

            nudSoLuong = new NumericUpDown
            {
                Name = "nudSoLuong",
                Minimum = 1,
                Maximum = 10000,
                Value = 1,
                ThousandsSeparator = true
            };

            dtpNgayMuon = TaoDateTimePicker("dtpNgayMuon");
            dtpNgayMuon.Value = DateTime.Today;

            dtpHanTra = TaoDateTimePicker("dtpHanTra");
            dtpHanTra.Value = DateTime.Today.AddDays(7);

            dtpNgayTraThucTe = TaoDateTimePicker("dtpNgayTraThucTe");
            dtpNgayTraThucTe.ShowCheckBox = true;
            dtpNgayTraThucTe.Checked = false;

            txtTrangThai = new TextBox
            {
                Name = "txtTrangThai",
                ReadOnly = true,
                TabStop = false,
                BackColor = SystemColors.Control,
                Text = "Đang mượn"
            };

            ThemTruong(bangNhap, 0, 0, "Mã phiếu:", txtMaPhieu);
            ThemTruong(bangNhap, 0, 2, "Mã sinh viên:", txtMaSinhVien);
            ThemTruong(bangNhap, 1, 0, "Họ tên sinh viên:", txtHoTen);
            ThemTruong(bangNhap, 1, 2, "Thiết bị:", cboThietBi);
            ThemTruong(bangNhap, 2, 0, "Số lượng:", nudSoLuong);
            ThemTruong(bangNhap, 2, 2, "Ngày mượn:", dtpNgayMuon);
            ThemTruong(bangNhap, 3, 0, "Hạn trả:", dtpHanTra);
            ThemTruong(bangNhap, 3, 2, "Ngày trả thực tế:", dtpNgayTraThucTe);
            ThemTruong(bangNhap, 4, 0, "Tình trạng:", txtTrangThai);

            Label lblGhiChu = new Label
            {
                Text = "Tình trạng được tự động xác định theo hạn trả và ngày trả thực tế.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = SystemColors.GrayText,
                AutoEllipsis = true,
                Margin = new Padding(5, 3, 3, 3)
            };
            bangNhap.Controls.Add(lblGhiChu, 2, 4);
            bangNhap.SetColumnSpan(lblGhiChu, 2);

            FlowLayoutPanel pnlNut = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                Margin = new Padding(0, 3, 0, 0),
                Padding = new Padding(0, 2, 0, 0)
            };

            Button btnLamMoi = TaoButton("Làm mới", 100);
            Button btnThem = TaoButton("Thêm phiếu", 110);
            Button btnCapNhat = TaoButton("Cập nhật", 105);
            Button btnXoa = TaoButton("Xóa phiếu", 100);
            Button btnDanhDauDaTra = TaoButton("Đánh dấu đã trả", 145);

            btnLamMoi.Click += (s, e) => LamMoiForm();
            btnThem.Click += (s, e) => ThemPhieu();
            btnCapNhat.Click += (s, e) => CapNhatPhieu();
            btnXoa.Click += (s, e) => XoaPhieu();
            btnDanhDauDaTra.Click += (s, e) => DanhDauDaTra();

            pnlNut.Controls.Add(btnLamMoi);
            pnlNut.Controls.Add(btnThem);
            pnlNut.Controls.Add(btnCapNhat);
            pnlNut.Controls.Add(btnXoa);
            pnlNut.Controls.Add(btnDanhDauDaTra);
            bangNhap.Controls.Add(pnlNut, 0, 5);
            bangNhap.SetColumnSpan(pnlNut, 4);

            grpThongTin.Controls.Add(bangNhap);
            layoutChinh.Controls.Add(grpThongTin, 0, 0);

            // Thanh tìm kiếm, lọc và xuất dữ liệu.
            FlowLayoutPanel pnlTimKiem = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(3, 7, 3, 0),
                Margin = new Padding(0)
            };

            Label lblTimKiem = new Label
            {
                Text = "Tìm mã SV / thiết bị:",
                AutoSize = true,
                Margin = new Padding(3, 7, 5, 0)
            };
            txtTimKiem = new TextBox
            {
                Name = "txtTimKiem",
                Width = 235,
                Margin = new Padding(0, 2, 15, 0)
            };

            Label lblLoc = new Label
            {
                Text = "Lọc trạng thái:",
                AutoSize = true,
                Margin = new Padding(0, 7, 5, 0)
            };
            cboLocTrangThai = new ComboBox
            {
                Name = "cboLocTrangThai",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 160,
                Margin = new Padding(0, 2, 12, 0)
            };
            cboLocTrangThai.Items.AddRange(new object[] { "Tất cả", "Đang mượn", "Quá hạn", "Đã trả" });
            cboLocTrangThai.SelectedIndex = 0;

            Button btnXoaLoc = TaoButton("Xóa lọc", 95);
            btnXoaLoc.Margin = new Padding(0, 0, 8, 0);
            Button btnXuatCsv = TaoButton("Xuất CSV", 105);
            btnXuatCsv.Click += (s, e) => XuatCsv();
            btnXoaLoc.Click += (s, e) =>
            {
                txtTimKiem.Clear();
                cboLocTrangThai.SelectedIndex = 0;
            };

            txtTimKiem.TextChanged += (s, e) => CapNhatDanhSachHienThi();
            cboLocTrangThai.SelectedIndexChanged += (s, e) => CapNhatDanhSachHienThi();

            pnlTimKiem.Controls.Add(lblTimKiem);
            pnlTimKiem.Controls.Add(txtTimKiem);
            pnlTimKiem.Controls.Add(lblLoc);
            pnlTimKiem.Controls.Add(cboLocTrangThai);
            pnlTimKiem.Controls.Add(btnXoaLoc);
            pnlTimKiem.Controls.Add(btnXuatCsv);
            layoutChinh.Controls.Add(pnlTimKiem, 0, 1);

            // Bảng danh sách: cột được tạo thủ công, không dùng AutoGenerateColumns.
            dgvPhieuMuon = new DataGridView
            {
                Name = "dgvPhieuMuon",
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToResizeRows = false
            };
            dgvPhieuMuon.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPhieuMuon.Columns.Add(TaoCot("MaPhieu", "Mã phiếu", 75F));
            dgvPhieuMuon.Columns.Add(TaoCot("MaSinhVien", "Mã sinh viên", 85F));
            dgvPhieuMuon.Columns.Add(TaoCot("HoTenSinhVien", "Họ tên sinh viên", 125F));
            dgvPhieuMuon.Columns.Add(TaoCot("ThietBi", "Thiết bị", 105F));
            dgvPhieuMuon.Columns.Add(TaoCot("SoLuong", "Số lượng", 55F));
            dgvPhieuMuon.Columns.Add(TaoCot("NgayMuon", "Ngày mượn", 80F, "dd/MM/yyyy"));
            dgvPhieuMuon.Columns.Add(TaoCot("HanTra", "Hạn trả", 80F, "dd/MM/yyyy"));
            dgvPhieuMuon.Columns.Add(TaoCot("NgayTraThucTe", "Ngày trả thực tế", 95F, "dd/MM/yyyy"));
            dgvPhieuMuon.Columns.Add(TaoCot("TrangThai", "Tình trạng", 85F));
            dgvPhieuMuon.CellClick += DgvPhieuMuon_CellClick;
            layoutChinh.Controls.Add(dgvPhieuMuon, 0, 2);

            // ErrorProvider hiển thị lỗi ngay cạnh trường dữ liệu không hợp lệ.
            errorProvider = new ErrorProvider
            {
                ContainerControl = this,
                BlinkStyle = ErrorBlinkStyle.NeverBlink
            };

            StatusStrip statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
            lblTongSoPhieu = new ToolStripStatusLabel("Tổng số phiếu: 0");
            ToolStripStatusLabel lblPhanCach = new ToolStripStatusLabel("|");
            lblSoPhieuQuaHan = new ToolStripStatusLabel("Số phiếu quá hạn: 0");
            ToolStripStatusLabel lblKhoangTrong = new ToolStripStatusLabel { Spring = true };
            ToolStripStatusLabel lblTenUngDung = new ToolStripStatusLabel("Lab Equipment Checkout");
            statusStrip.Items.Add(lblTongSoPhieu);
            statusStrip.Items.Add(lblPhanCach);
            statusStrip.Items.Add(lblSoPhieuQuaHan);
            statusStrip.Items.Add(lblKhoangTrong);
            statusStrip.Items.Add(lblTenUngDung);

            // Thêm StatusStrip trước để vùng nội dung Dock=Fill chừa đúng phần đáy.
            Controls.Add(layoutChinh);
            Controls.Add(statusStrip);

            // Cập nhật phần xem trước trạng thái khi thay đổi ngày.
            dtpNgayMuon.ValueChanged += (s, e) => CapNhatTrangThaiXemTruoc();
            dtpHanTra.ValueChanged += (s, e) => CapNhatTrangThaiXemTruoc();
            dtpNgayTraThucTe.ValueChanged += (s, e) => CapNhatTrangThaiXemTruoc();
            dtpNgayTraThucTe.MouseUp += (s, e) => CapNhatTrangThaiXemTruoc();
            dtpNgayTraThucTe.KeyUp += (s, e) => CapNhatTrangThaiXemTruoc();
        }

        private DateTimePicker TaoDateTimePicker(string ten)
        {
            return new DateTimePicker
            {
                Name = ten,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 4, 8, 4)
            };
        }

        private void ThemTruong(TableLayoutPanel bang, int dong, int cotNhan, string tenNhan, Control dieuKhien)
        {
            Label lbl = new Label
            {
                Text = tenNhan,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Margin = new Padding(3, 3, 4, 3)
            };
            dieuKhien.Dock = DockStyle.Fill;
            dieuKhien.Margin = new Padding(3, 4, 8, 4);
            bang.Controls.Add(lbl, cotNhan, dong);
            bang.Controls.Add(dieuKhien, cotNhan + 1, dong);
        }

        private Button TaoButton(string noiDung, int chieuRong)
        {
            return new Button
            {
                Text = noiDung,
                Width = chieuRong,
                Height = 32,
                AutoSize = false,
                Margin = new Padding(3, 2, 5, 2),
                UseVisualStyleBackColor = true
            };
        }

        private DataGridViewTextBoxColumn TaoCot(string tenThuocTinh, string tieuDe, float tiLe, string dinhDang = null)
        {
            DataGridViewTextBoxColumn cot = new DataGridViewTextBoxColumn
            {
                DataPropertyName = tenThuocTinh,
                HeaderText = tieuDe,
                FillWeight = tiLe,
                MinimumWidth = 60,
                ReadOnly = true
            };

            if (!string.IsNullOrEmpty(dinhDang))
                cot.DefaultCellStyle.Format = dinhDang;

            return cot;
        }

        private void DgvPhieuMuon_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            PhieuMuon phieu = dgvPhieuMuon.Rows[e.RowIndex].DataBoundItem as PhieuMuon;
            if (phieu == null)
                return;

            phieuDangChon = phieu;
            DoDuLieuLenForm(phieu);
        }

        private void DoDuLieuLenForm(PhieuMuon phieu)
        {
            errorProvider.Clear();
            txtMaPhieu.Text = phieu.MaPhieu;
            txtMaSinhVien.Text = phieu.MaSinhVien;
            txtHoTen.Text = phieu.HoTenSinhVien;
            cboThietBi.SelectedItem = phieu.ThietBi;
            nudSoLuong.Value = Math.Max(nudSoLuong.Minimum, Math.Min(nudSoLuong.Maximum, phieu.SoLuong));
            dtpNgayMuon.Value = phieu.NgayMuon;
            dtpHanTra.Value = phieu.HanTra;
            dtpNgayTraThucTe.Value = phieu.NgayTraThucTe ?? DateTime.Today;
            dtpNgayTraThucTe.Checked = phieu.NgayTraThucTe.HasValue;
            txtTrangThai.Text = phieu.TrangThai;
        }

        private void LamMoiForm()
        {
            phieuDangChon = null;
            errorProvider.Clear();
            txtMaPhieu.Clear();
            txtMaSinhVien.Clear();
            txtHoTen.Clear();
            cboThietBi.SelectedIndex = -1;
            nudSoLuong.Value = 1;
            dtpNgayMuon.Value = DateTime.Today;
            dtpHanTra.Value = DateTime.Today.AddDays(7);
            dtpNgayTraThucTe.Value = DateTime.Today;
            dtpNgayTraThucTe.Checked = false;
            txtTrangThai.Text = "Đang mượn";
            dgvPhieuMuon.ClearSelection();
        }

        private bool KiemTraDuLieu(PhieuMuon phieuDangSua)
        {
            errorProvider.Clear();
            bool hopLe = true;
            string maPhieu = txtMaPhieu.Text.Trim();
            string maSinhVien = txtMaSinhVien.Text.Trim();

            if (maPhieu.Length == 0)
            {
                errorProvider.SetError(txtMaPhieu, "Vui lòng nhập mã phiếu.");
                hopLe = false;
            }
            else if (danhSach.Any(p =>
                string.Equals(p.MaPhieu, maPhieu, StringComparison.OrdinalIgnoreCase) &&
                !object.ReferenceEquals(p, phieuDangSua)))
            {
                errorProvider.SetError(txtMaPhieu, "Mã phiếu đã tồn tại.");
                hopLe = false;
            }

            if (maSinhVien.Length == 0)
            {
                errorProvider.SetError(txtMaSinhVien, "Vui lòng nhập mã sinh viên.");
                hopLe = false;
            }

            if (cboThietBi.SelectedIndex < 0)
            {
                errorProvider.SetError(cboThietBi, "Vui lòng chọn thiết bị.");
                hopLe = false;
            }

            if (nudSoLuong.Value <= 0)
            {
                errorProvider.SetError(nudSoLuong, "Số lượng phải lớn hơn 0.");
                hopLe = false;
            }

            if (dtpHanTra.Value.Date < dtpNgayMuon.Value.Date)
            {
                errorProvider.SetError(dtpHanTra, "Hạn trả không được trước ngày mượn.");
                hopLe = false;
            }

            if (dtpNgayTraThucTe.Checked && dtpNgayTraThucTe.Value.Date < dtpNgayMuon.Value.Date)
            {
                errorProvider.SetError(dtpNgayTraThucTe, "Ngày trả thực tế không được trước ngày mượn.");
                hopLe = false;
            }

            return hopLe;
        }

        private PhieuMuon TaoPhieuTuForm()
        {
            return new PhieuMuon
            {
                MaPhieu = txtMaPhieu.Text.Trim(),
                MaSinhVien = txtMaSinhVien.Text.Trim(),
                HoTenSinhVien = txtHoTen.Text.Trim(),
                ThietBi = cboThietBi.SelectedItem.ToString(),
                SoLuong = Decimal.ToInt32(nudSoLuong.Value),
                NgayMuon = dtpNgayMuon.Value.Date,
                HanTra = dtpHanTra.Value.Date,
                NgayTraThucTe = dtpNgayTraThucTe.Checked
                    ? (DateTime?)dtpNgayTraThucTe.Value.Date
                    : null
            };
        }

        private void GanThongTin(PhieuMuon dich, PhieuMuon nguon)
        {
            dich.MaPhieu = nguon.MaPhieu;
            dich.MaSinhVien = nguon.MaSinhVien;
            dich.HoTenSinhVien = nguon.HoTenSinhVien;
            dich.ThietBi = nguon.ThietBi;
            dich.SoLuong = nguon.SoLuong;
            dich.NgayMuon = nguon.NgayMuon;
            dich.HanTra = nguon.HanTra;
            dich.NgayTraThucTe = nguon.NgayTraThucTe;
        }

        private void ThemPhieu()
        {
            if (!KiemTraDuLieu(null))
                return;

            danhSach.Add(TaoPhieuTuForm());
            CapNhatDanhSachHienThi();
            LamMoiForm();
            MessageBox.Show("Đã thêm phiếu mượn.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CapNhatPhieu()
        {
            if (phieuDangChon == null)
            {
                MessageBox.Show("Hãy chọn một phiếu trong bảng trước khi cập nhật.", "Chưa chọn phiếu",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!KiemTraDuLieu(phieuDangChon))
                return;

            GanThongTin(phieuDangChon, TaoPhieuTuForm());
            txtTrangThai.Text = phieuDangChon.TrangThai;
            CapNhatDanhSachHienThi();
            MessageBox.Show("Đã cập nhật phiếu mượn.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void XoaPhieu()
        {
            if (phieuDangChon == null)
            {
                MessageBox.Show("Hãy chọn một phiếu trong bảng trước khi xóa.", "Chưa chọn phiếu",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult ketQua = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa phiếu " + phieuDangChon.MaPhieu + " không?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            if (ketQua != DialogResult.Yes)
                return;

            danhSach.Remove(phieuDangChon);
            LamMoiForm();
            CapNhatDanhSachHienThi();
        }

        private void DanhDauDaTra()
        {
            if (phieuDangChon == null)
            {
                MessageBox.Show("Hãy chọn phiếu cần đánh dấu đã trả trong bảng.", "Chưa chọn phiếu",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (phieuDangChon.NgayTraThucTe.HasValue)
            {
                MessageBox.Show("Phiếu này đã được trả trước đó.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (DateTime.Today < phieuDangChon.NgayMuon.Date)
            {
                MessageBox.Show("Không thể đánh dấu đã trả vì ngày mượn nằm trong tương lai.", "Dữ liệu không hợp lệ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Khi bấm nút này, ngày trả thực tế được đặt là ngày hiện tại.
            phieuDangChon.NgayTraThucTe = DateTime.Today;
            dtpNgayTraThucTe.Value = DateTime.Today;
            dtpNgayTraThucTe.Checked = true;
            txtTrangThai.Text = phieuDangChon.TrangThai;

            CapNhatDanhSachHienThi();
            MessageBox.Show("Đã cập nhật ngày trả thực tế và trạng thái trên phiếu.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CapNhatTrangThaiXemTruoc()
        {
            if (txtTrangThai == null || dtpHanTra == null || dtpNgayMuon == null || dtpNgayTraThucTe == null)
                return;

            if (dtpNgayTraThucTe.Checked)
                txtTrangThai.Text = "Đã trả";
            else if (dtpHanTra.Value.Date < DateTime.Today)
                txtTrangThai.Text = "Quá hạn";
            else
                txtTrangThai.Text = "Đang mượn";
        }

        private void CapNhatDanhSachHienThi()
        {
            if (dgvPhieuMuon == null || txtTimKiem == null || cboLocTrangThai == null)
                return;

            string tuKhoa = txtTimKiem.Text.Trim();
            string trangThaiLoc = cboLocTrangThai.SelectedItem == null
                ? "Tất cả"
                : cboLocTrangThai.SelectedItem.ToString();

            var ketQua = danhSach.AsEnumerable();

            if (tuKhoa.Length > 0)
            {
                ketQua = ketQua.Where(p =>
                    p.MaSinhVien.IndexOf(tuKhoa, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.ThietBi.IndexOf(tuKhoa, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (trangThaiLoc != "Tất cả")
                ketQua = ketQua.Where(p => p.TrangThai == trangThaiLoc);

            var danhSachKetQua = ketQua.OrderBy(p => p.MaPhieu).ToList();

            // Tạm tắt thông báo từng dòng, sau đó báo BindingSource cập nhật một lần.
            danhSachHienThi.RaiseListChangedEvents = false;
            try
            {
                danhSachHienThi.Clear();
                foreach (PhieuMuon phieu in danhSachKetQua)
                    danhSachHienThi.Add(phieu);
            }
            finally
            {
                danhSachHienThi.RaiseListChangedEvents = true;
            }

            bindingSource.ResetBindings(false);
            CapNhatThanhTrangThai();
        }

        private void CapNhatThanhTrangThai()
        {
            if (lblTongSoPhieu == null || lblSoPhieuQuaHan == null)
                return;

            int tongSoPhieu = danhSach.Count;
            int soPhieuQuaHan = danhSach.Count(p => p.TrangThai == "Quá hạn");
            lblTongSoPhieu.Text = "Tổng số phiếu: " + tongSoPhieu;
            lblSoPhieuQuaHan.Text = "Số phiếu quá hạn: " + soPhieuQuaHan;
        }

        private void XuatCsv()
        {
            using (SaveFileDialog hopThoai = new SaveFileDialog())
            {
                hopThoai.Title = "Xuất danh sách phiếu mượn đang hiển thị";
                hopThoai.Filter = "Tệp CSV (*.csv)|*.csv";
                hopThoai.DefaultExt = "csv";
                hopThoai.AddExtension = true;
                hopThoai.FileName = "DanhSachPhieuMuon_" + DateTime.Now.ToString("yyyyMMdd_HHmm");

                if (hopThoai.ShowDialog(this) != DialogResult.OK)
                    return;

                // UTF-8 BOM giúp Excel nhận diện tiếng Việt tốt hơn.
                using (StreamWriter writer = new StreamWriter(hopThoai.FileName, false, new UTF8Encoding(true)))
                {
                    writer.WriteLine("Mã phiếu,Mã sinh viên,Họ tên sinh viên,Thiết bị,Số lượng,Ngày mượn,Hạn trả,Ngày trả thực tế,Tình trạng");

                    foreach (PhieuMuon p in danhSachHienThi)
                    {
                        string[] cot =
                        {
                            p.MaPhieu,
                            p.MaSinhVien,
                            p.HoTenSinhVien,
                            p.ThietBi,
                            p.SoLuong.ToString(CultureInfo.InvariantCulture),
                            p.NgayMuon.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                            p.HanTra.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                            p.NgayTraThucTe.HasValue
                                ? p.NgayTraThucTe.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                                : "",
                            p.TrangThai
                        };

                        writer.WriteLine(string.Join(",", cot.Select(ChuanHoaCsv)));
                    }
                }

                MessageBox.Show("Đã xuất " + danhSachHienThi.Count + " phiếu ra tệp CSV.", "Xuất dữ liệu thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string ChuanHoaCsv(string giaTri)
        {
            // Bao giá trị trong dấu ngoặc kép và nhân đôi dấu ngoặc kép bên trong theo chuẩn CSV.
            return "\"" + (giaTri ?? string.Empty).Replace("\"", "\"\"") + "\"";
        }
    }
}
