# Repair Service Tracker (.NET 10 Windows Forms)

## Mở và chạy
1. Giải nén thư mục dự án.
2. Mở `RepairServiceTracker.sln` bằng Visual Studio có hỗ trợ .NET 10.
3. Kiểm tra đã cài workload **.NET desktop development** và .NET 10 SDK.
4. Nhấn `Ctrl + Shift + B` để Build, sau đó nhấn `F5` để chạy.

## Chức năng
- Thêm, cập nhật, xóa đơn (xóa có xác nhận).
- Kiểm tra mã đơn trùng; tên khách hàng, số điện thoại, mô tả lỗi bắt buộc; chi phí phải là số không âm.
- Tìm kiếm tức thời theo mã đơn, tên khách hàng hoặc số điện thoại.
- Lọc theo trạng thái.
- Chọn ảnh thiết bị qua OpenFileDialog; PictureBox hiển thị bằng Zoom để giữ đúng tỷ lệ.
- DataGridView cấu hình cột thủ công (`AutoGenerateColumns = false`), dùng BindingSource và BindingList<T>.
- Menu xuất các đơn đang hiển thị ra CSV và thoát chương trình.
- StatusStrip hiện số đơn đang hiển thị/tổng số đơn; tổng chi phí dự kiến của các đơn đang xử lý (loại trừ Hoàn tất và Đã hủy).

## Lưu ý
Dữ liệu chỉ lưu trong bộ nhớ lúc chương trình chạy, chưa có cơ sở dữ liệu. Đường dẫn ảnh được lưu cùng đơn; nên giữ nguyên file ảnh tại vị trí đã chọn.
