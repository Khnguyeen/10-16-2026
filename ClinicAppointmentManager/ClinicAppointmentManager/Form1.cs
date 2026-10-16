
#nullable enable
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ClinicAppointmentManager
{
    public partial class Form1 : Form
    {
        private readonly BindingList<Appointment> appointments = new();
        private readonly BindingSource source = new();
        private readonly ErrorProvider errors = new();

        private TextBox txtName = null!;
        private TextBox txtPhone = null!;
        private TextBox txtReason = null!;
        private TextBox txtSearch = null!;

        private ComboBox cboDoctor = null!;
        private ComboBox cboStatus = null!;
        private ComboBox cboFilterStatus = null!;

        private DateTimePicker dtpDate = null!;
        private DateTimePicker dtpFilterDate = null!;
        private DataGridView dgv = null!;

        private Label lblTotal = null!;
        private Label lblWaiting = null!;
        private Label lblProgress = null!;
        private Label lblCompleted = null!;

        private int nextId = 1;
        private int selectedId = -1;

        private readonly Color navy = Color.FromArgb(31, 50, 81);
        private readonly Color blue = Color.FromArgb(55, 116, 220);
        private readonly Color background = Color.FromArgb(244, 247, 251);
        private readonly Color green = Color.FromArgb(25, 145, 110);
        private readonly Color red = Color.FromArgb(210, 70, 70);
        private readonly Color gray = Color.FromArgb(105, 118, 138);

        public Form1()
        {
            InitializeComponent();

            // Xóa giao diện mặc định trong Designer.
            Controls.Clear();

            Text = "ClinicCare | Quản lý lịch khám";
            Size = new Size(1250, 800);
            MinimumSize = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = background;
            Font = new Font("Segoe UI", 10);

            errors.ContainerControl = this;

            BuildInterface();
            RefreshGrid();
        }

        private Label MakeLabel(string text, int width = 300)
        {
            return new Label
            {
                Text = text,
                Width = width,
                Height = 24,
                ForeColor = navy,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
        }

        private TextBox MakeTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 4, 0, 12)
            };
        }

        private Button MakeButton(
            string text, Color color, EventHandler click)
        {
            Button button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Height = 40,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4)
            };

            button.FlatAppearance.BorderSize = 0;
            button.Click += click;
            return button;
        }

        private void BuildInterface()
        {
            // Bố cục chính: tiêu đề, thống kê, nội dung và trạng thái.
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = background,
                Padding = new Padding(14)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

            Controls.Add(layout);

            // Tiêu đề.
            Panel header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = navy,
                Margin = new Padding(0, 0, 0, 10)
            };

            Label title = new Label
            {
                Text = "ClinicCare",
                Location = new Point(20, 10),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 22, FontStyle.Bold)
            };

            Label subtitle = new Label
            {
                Text = "HỆ THỐNG QUẢN LÝ LỊCH KHÁM",
                Location = new Point(24, 48),
                AutoSize = true,
                ForeColor = Color.LightSteelBlue,
                Font = new Font("Segoe UI", 9)
            };

            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            layout.Controls.Add(header, 0, 0);

            // Bốn thẻ thống kê chia đều.
            TableLayoutPanel stats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = background,
                Margin = new Padding(0, 0, 0, 12)
            };

            for (int i = 0; i < 4; i++)
                stats.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 25));

            lblTotal = MakeStatCard(stats, "TỔNG LỊCH HẸN", 0);
            lblWaiting = MakeStatCard(stats, "CHỜ KHÁM", 1);
            lblProgress = MakeStatCard(stats, "ĐANG KHÁM", 2);
            lblCompleted = MakeStatCard(stats, "ĐÃ HOÀN THÀNH", 3);

            layout.Controls.Add(stats, 0, 1);

            // Khu vực nhập liệu và danh sách.
            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = background,
                Margin = new Padding(0)
            };

            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            layout.Controls.Add(content, 0, 2);

            // Khung nhập thông tin.
            Panel editor = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                Margin = new Padding(0, 0, 12, 0)
            };

            content.Controls.Add(editor, 0, 0);
            BuildEditor(editor);

            // Khung danh sách.
            Panel right = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(14),
                Margin = new Padding(0)
            };

            content.Controls.Add(right, 1, 0);
            BuildList(right);

            // Thanh trạng thái.
            StatusStrip statusStrip = new StatusStrip
            {
                Dock = DockStyle.Fill,
                SizingGrip = false
            };

            statusStrip.Items.Add(
                new ToolStripStatusLabel(
                    "ClinicCare | Quản lý lịch khám bệnh"));

            layout.Controls.Add(statusStrip, 0, 3);
        }

        private Label MakeStatCard(
            TableLayoutPanel parent, string caption, int column)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(4)
            };

            Label heading = new Label
            {
                Text = caption,
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(10, 8, 0, 0),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };

            Label value = new Label
            {
                Text = "0",
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 0, 0, 0),
                ForeColor = blue,
                Font = new Font("Segoe UI", 18, FontStyle.Bold)
            };

            card.Controls.Add(value);
            card.Controls.Add(heading);
            parent.Controls.Add(card, column, 0);

            return value;
        }

        private void BuildEditor(Panel editor)
        {
            TableLayoutPanel form = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 13,
                BackColor = Color.White,
                AutoScroll = true
            };

            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            for (int i = 0; i < 6; i++)
                form.RowStyles.Add(
                    new RowStyle(SizeType.Absolute, 24));

            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

            Label title = MakeLabel("THÔNG TIN LỊCH HẸN");
            title.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            title.Height = 32;
            form.Controls.Add(title);

            AddFormLabel(form, "Họ và tên bệnh nhân");
            txtName = MakeTextBox();
            form.Controls.Add(txtName);

            AddFormLabel(form, "Số điện thoại");
            txtPhone = MakeTextBox();
            form.Controls.Add(txtPhone);

            AddFormLabel(form, "Ngày và giờ khám");
            dtpDate = new DateTimePicker
            {
                Dock = DockStyle.Top,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm",
                ShowUpDown = true,
                Value = DateTime.Now.AddHours(1),
                Margin = new Padding(0, 4, 0, 12)
            };
            form.Controls.Add(dtpDate);

            AddFormLabel(form, "Bác sĩ phụ trách");
            cboDoctor = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 4, 0, 12)
            };
            cboDoctor.Items.AddRange(new object[]
            {
                "BS. Nguyễn Minh Anh",
                "BS. Trần Quốc Bảo",
                "BS. Lê Thu Hà",
                "BS. Phạm Đức Long"
            });
            form.Controls.Add(cboDoctor);

            AddFormLabel(form, "Lý do khám");
            txtReason = MakeTextBox();
            form.Controls.Add(txtReason);

            AddFormLabel(form, "Trạng thái");
            cboStatus = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 4, 0, 12)
            };
            cboStatus.Items.AddRange(new object[]
            {
                "Chờ khám", "Đang khám",
                "Đã hoàn thành", "Đã hủy"
            });
            cboStatus.SelectedIndex = 0;
            form.Controls.Add(cboStatus);

            // Nút thao tác chia hai cột bằng nhau.
            TableLayoutPanel buttons = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 92,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0, 8, 0, 0)
            };

            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            buttons.Controls.Add(MakeButton(
                "Thêm lịch", blue, (s, e) => AddAppointment()), 0, 0);

            buttons.Controls.Add(MakeButton(
                "Cập nhật", green, (s, e) => UpdateAppointment()), 1, 0);

            buttons.Controls.Add(MakeButton(
                "Xóa lịch", red, (s, e) => DeleteAppointment()), 0, 1);

            buttons.Controls.Add(MakeButton(
                "Làm mới", gray, (s, e) => ClearForm()), 1, 1);

            form.Controls.Add(buttons);
            editor.Controls.Add(form);
        }

        private void AddFormLabel(TableLayoutPanel form, string text)
        {
            Label label = MakeLabel(text);
            label.Dock = DockStyle.Fill;
            label.Margin = new Padding(0, 5, 0, 0);
            form.Controls.Add(label);
        }

        private void BuildList(Panel right)
        {
            TableLayoutPanel listLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.White
            };

            listLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            listLayout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 32));

            listLayout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 46));

            listLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            listLayout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 48));

            right.Controls.Add(listLayout);

            Label title = MakeLabel("DANH SÁCH LỊCH HẸN");
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            listLayout.Controls.Add(title, 0, 0);

            // Thanh tìm kiếm và bộ lọc.
            TableLayoutPanel filters = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 3, 0, 8)
            };

            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "Tìm tên hoặc số điện thoại",
                Margin = new Padding(2, 4, 5, 4)
            };
            txtSearch.TextChanged += (s, e) => RefreshGrid();
            filters.Controls.Add(txtSearch, 0, 0);

            cboFilterStatus = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(2, 4, 5, 4)
            };

            cboFilterStatus.Items.AddRange(new object[]
            {
                "Tất cả trạng thái", "Chờ khám", "Đang khám",
                "Đã hoàn thành", "Đã hủy"
            });

            cboFilterStatus.SelectedIndex = 0;
            cboFilterStatus.SelectedIndexChanged +=
                (s, e) => RefreshGrid();

            filters.Controls.Add(cboFilterStatus, 1, 0);

            dtpFilterDate = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                ShowCheckBox = true,
                Checked = false,
                Margin = new Padding(2, 4, 2, 4)
            };

            dtpFilterDate.ValueChanged += (s, e) => RefreshGrid();
            filters.Controls.Add(dtpFilterDate, 2, 0);

            listLayout.Controls.Add(filters, 0, 1);

            // Bảng dữ liệu.
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeight = 42,
                RowTemplate = { Height = 36 },
                EnableHeadersVisualStyles = false,
                Margin = new Padding(0, 0, 0, 6)
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor = navy;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9, FontStyle.Bold);

            dgv.DefaultCellStyle.SelectionBackColor =
                Color.FromArgb(215, 230, 252);
            dgv.DefaultCellStyle.SelectionForeColor = navy;
            dgv.AlternatingRowsDefaultCellStyle.BackColor =
                Color.FromArgb(247, 249, 252);

            AddColumn("AppointmentId", "Mã", 45);
            AddColumn("PatientName", "Bệnh nhân", 120);
            AddColumn("Phone", "Điện thoại", 95);
            AddColumn("AppointmentDate", "Thời gian", 125,
                "dd/MM/yyyy HH:mm");
            AddColumn("Doctor", "Bác sĩ", 120);
            AddColumn("Status", "Trạng thái", 100);

            dgv.CellClick += Dgv_CellClick;
            dgv.CellFormatting += Dgv_CellFormatting;

            listLayout.Controls.Add(dgv, 0, 2);

            TableLayoutPanel bottomButtons = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };

            bottomButtons.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 50));

            bottomButtons.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 50));

            bottomButtons.Controls.Add(MakeButton(
                "Xuất CSV", green, (s, e) => ExportCsv()), 0, 0);

            bottomButtons.Controls.Add(MakeButton(
                "Bỏ lọc ngày", gray, (s, e) =>
                {
                    dtpFilterDate.Checked = false;
                    RefreshGrid();
                }), 1, 0);

            listLayout.Controls.Add(bottomButtons, 0, 3);
        }

        private void AddColumn(
            string property, string title, int minWidth,
            string? format = null)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = property,
                HeaderText = title,
                MinimumWidth = minWidth
            };

            if (format != null)
                column.DefaultCellStyle.Format = format;

            dgv.Columns.Add(column);
        }

        private bool ValidateForm()
        {
            errors.Clear();
            bool valid = true;

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                errors.SetError(txtName, "Vui lòng nhập tên bệnh nhân.");
                valid = false;
            }

            string phone = txtPhone.Text.Trim();

            if (phone.Length < 9 || phone.Length > 11 ||
                !phone.All(char.IsDigit))
            {
                errors.SetError(txtPhone,
                    "Số điện thoại phải có 9–11 chữ số.");
                valid = false;
            }

            if (cboDoctor.SelectedIndex < 0)
            {
                errors.SetError(cboDoctor, "Vui lòng chọn bác sĩ.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtReason.Text))
            {
                errors.SetError(txtReason, "Vui lòng nhập lý do khám.");
                valid = false;
            }

            if (dtpDate.Value < DateTime.Now)
            {
                errors.SetError(dtpDate,
                    "Thời gian khám không được ở trong quá khứ.");
                valid = false;
            }

            return valid;
        }

        private Appointment ReadForm()
        {
            return new Appointment
            {
                PatientName = txtName.Text.Trim(),
                Phone = txtPhone.Text.Trim(),
                AppointmentDate = dtpDate.Value,
                Doctor = cboDoctor.Text,
                Reason = txtReason.Text.Trim(),
                Status = cboStatus.Text
            };
        }

        private void AddAppointment()
        {
            if (!ValidateForm())
            {
                MessageBox.Show("Vui lòng kiểm tra lại thông tin.",
                    "Dữ liệu chưa hợp lệ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Appointment item = ReadForm();
            item.AppointmentId = nextId++;
            appointments.Add(item);

            RefreshGrid();
            ClearForm();
            MessageBox.Show("Đã thêm lịch hẹn thành công.");
        }

        private void UpdateAppointment()
        {
            if (selectedId < 0)
            {
                MessageBox.Show("Hãy chọn lịch hẹn cần cập nhật.");
                return;
            }

            if (!ValidateForm())
                return;

            Appointment? item = appointments.FirstOrDefault(
                a => a.AppointmentId == selectedId);

            if (item == null)
                return;

            Appointment edited = ReadForm();

            item.PatientName = edited.PatientName;
            item.Phone = edited.Phone;
            item.AppointmentDate = edited.AppointmentDate;
            item.Doctor = edited.Doctor;
            item.Reason = edited.Reason;
            item.Status = edited.Status;

            RefreshGrid();
            ClearForm();
            MessageBox.Show("Đã cập nhật lịch hẹn.");
        }

        private void DeleteAppointment()
        {
            if (selectedId < 0)
            {
                MessageBox.Show("Hãy chọn lịch hẹn cần xóa.");
                return;
            }

            DialogResult result = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa lịch hẹn này?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            Appointment? item = appointments.FirstOrDefault(
                a => a.AppointmentId == selectedId);

            if (item != null)
                appointments.Remove(item);

            RefreshGrid();
            ClearForm();
        }

        private void ClearForm()
        {
            selectedId = -1;
            txtName.Clear();
            txtPhone.Clear();
            txtReason.Clear();
            cboDoctor.SelectedIndex = -1;
            cboStatus.SelectedIndex = 0;
            dtpDate.Value = DateTime.Now.AddHours(1);
            errors.Clear();
            dgv.ClearSelection();
        }

        private void Dgv_CellClick(
            object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            Appointment? item =
                dgv.Rows[e.RowIndex].DataBoundItem as Appointment;

            if (item == null)
                return;

            selectedId = item.AppointmentId;
            txtName.Text = item.PatientName;
            txtPhone.Text = item.Phone;
            dtpDate.Value = item.AppointmentDate;
            cboDoctor.Text = item.Doctor;
            txtReason.Text = item.Reason;
            cboStatus.Text = item.Status;
        }

        private void RefreshGrid()
        {
            if (dgv == null)
                return;

            var query = appointments.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                string keyword = txtSearch.Text.Trim();

                query = query.Where(a =>
                    a.PatientName.Contains(keyword,
                        StringComparison.OrdinalIgnoreCase) ||
                    a.Phone.Contains(keyword,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (cboFilterStatus.SelectedIndex > 0)
            {
                query = query.Where(a => a.Status == cboFilterStatus.Text);
            }

            if (dtpFilterDate.Checked)
            {
                DateTime date = dtpFilterDate.Value.Date;
                query = query.Where(a => a.AppointmentDate.Date == date);
            }

            source.DataSource = query.ToList();
            dgv.DataSource = source;

            lblTotal.Text = appointments.Count.ToString();
            lblWaiting.Text = appointments.Count(
                a => a.Status == "Chờ khám").ToString();
            lblProgress.Text = appointments.Count(
                a => a.Status == "Đang khám").ToString();
            lblCompleted.Text = appointments.Count(
                a => a.Status == "Đã hoàn thành").ToString();
        }

        private void Dgv_CellFormatting(
            object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 ||
                dgv.Columns[e.ColumnIndex].DataPropertyName != "Status")
                return;

            string? status = e.Value?.ToString();

            if (status == "Chờ khám")
                e.CellStyle.ForeColor = Color.FromArgb(200, 130, 20);
            else if (status == "Đang khám")
                e.CellStyle.ForeColor = blue;
            else if (status == "Đã hoàn thành")
                e.CellStyle.ForeColor = green;
            else if (status == "Đã hủy")
                e.CellStyle.ForeColor = red;

            e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        }

        private void ExportCsv()
        {
            using SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv",
                FileName = "DanhSachLichKham.csv"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            StringBuilder csv = new StringBuilder();

            csv.AppendLine(
                "Ma lich,Ten benh nhan,So dien thoai,Ngay kham,Bac si,Ly do,Trang thai");

            foreach (Appointment a in appointments)
            {
                csv.AppendLine(string.Join(",",
                    Csv(a.AppointmentId.ToString()),
                    Csv(a.PatientName),
                    Csv(a.Phone),
                    Csv(a.AppointmentDate.ToString("dd/MM/yyyy HH:mm")),
                    Csv(a.Doctor),
                    Csv(a.Reason),
                    Csv(a.Status)));
            }

            File.WriteAllText(dialog.FileName, csv.ToString(),
                new UTF8Encoding(true));

            MessageBox.Show("Xuất CSV thành công.");
        }

        private string Csv(string? value)
        {
            return "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        }
    }
}
