Giai đoạn 1: Khởi tạo Project & Cấu hình Database (Tuần 1)
Mục tiêu: Chạy thành công ứng dụng với giao diện trống và tạo được file database .sqlite trong thư mục project.

Bước 1: Khởi tạo cấu trúc thư mục
Tạo project WPF (.NET Framework 4.7.2). Tạo sẵn các thư mục sau để áp dụng MVVM: Models, ViewModels, Views, Services, Helpers, Data.

Bước 2: Cài đặt NuGet Packages
Mở NuGet Package Manager và cài đặt chính xác các thư viện sau: EntityFramework (v6.4.4), System.Data.SQLite, System.Data.SQLite.EF6, MaterialDesignThemes (để làm đẹp giao diện).

Bước 3: Xây dựng các class Model
Trong thư mục Models, tạo các file .cs cho User, Customer, Category, Product, Order, OrderDetail. Cần định nghĩa rõ các Data Annotations ([Key], [Required], [StringLength]) và các thuộc tính điều hướng (Navigation Properties) như ICollection<OrderDetail>.

Bước 4: Cấu hình StoreDbContext
Trong thư mục Data, tạo class StoreDbContext kế thừa DbContext. Định nghĩa các DbSet<Product>, DbSet<Order>... Ghi đè phương thức OnModelCreating nếu cần mapping tên bảng.

Bước 5: Cấu hình App.config
Thêm Connection String trỏ đến file SQLite (ví dụ: Data Source=.\StoreDB.sqlite;Version=3;). Khai báo Provider Services cho SQLite trong phần <entityFramework>.

Bước 6: Tạo Database
Viết một đoạn code nhỏ trong file App.xaml.cs (hàm OnStartup) gọi context.Database.CreateIfNotExists() để hệ thống tự động sinh file .sqlite theo cấu trúc Model vừa tạo.

Giai đoạn 2: Xây dựng Core MVVM & Khung giao diện (Tuần 2)
Mục tiêu: Hoàn thiện cơ sở hạ tầng của kiến trúc MVVM và chuyển đổi qua lại giữa các màn hình.

Bước 1: Tạo các helper class cho MVVM
Trong thư mục Helpers, tạo class ViewModelBase (implement INotifyPropertyChanged để cập nhật UI) và class RelayCommand (implement ICommand để bắt sự kiện click nút từ View).

Bước 2: Thiết kế MainView (Layout chính)
Mở MainWindow.xaml. Chia Grid làm 2 phần: Cột trái (độ rộng cố định) làm Menu Navigation (các nút Dashboard, Bán hàng, Kho hàng...). Cột phải dùng thẻ <ContentControl> làm khu vực hiển thị nội dung động.

Bước 3: Xử lý Navigation (Chuyển trang)
Tạo MainViewModel. Viết logic chuyển đổi giá trị của thuộc tính CurrentViewModel khi người dùng bấm các nút ở menu. Bind thuộc tính này vào <ContentControl> ở MainView.

Bước 4: Chuẩn bị các View & ViewModel cơ bản
Tạo các cặp file: DashboardView.xaml - DashboardViewModel.cs, ProductView.xaml - ProductViewModel.cs. Liên kết chúng lại bằng DataTemplates trong file App.xaml.

Giai đoạn 3: Triển khai CRUD Quản lý Danh mục & Sản phẩm (Tuần 3)
Mục tiêu: Có thể Thêm, Sửa, Xóa, Tìm kiếm sản phẩm và lưu trực tiếp xuống SQLite.

Bước 1: Thiết kế giao diện ProductView
Sử dụng DataGrid để hiển thị danh sách sản phẩm. Tạo các TextBox, ComboBox (để chọn Category) ở phía trên hoặc bên cạnh để nhập liệu. Tạo các nút Thêm, Lưu, Xóa.

Bước 2: Viết logic ProductViewModel
Khai báo ObservableCollection<Product> để bind lên DataGrid. Khai báo thuộc tính SelectedProduct để bắt dòng dữ liệu đang được chọn.

Bước 3: Tích hợp Entity Framework
Viết các ICommand xử lý logic: Khi bấm Thêm (Khởi tạo Product mới), khi bấm Lưu (gọi context.Products.Add() hoặc context.Entry().State = EntityState.Modified rồi context.SaveChanges()). Khi gọi SaveChanges(), UI sẽ tự động update nhờ ObservableCollection.

Bước 4: Tính năng tìm kiếm
Thêm một thanh Search. Viết logic dùng LINQ để lọc danh sách: context.Products.Where(p => p.Name.Contains(keyword)).ToList().

(Thực hiện lặp lại quy trình này cho màn hình Quản lý Khách hàng và Quản lý Nhân viên).

Giai đoạn 4: Module Bán hàng (POS) & Giao dịch (Tuần 4)
Mục tiêu: Xử lý được luồng bán hàng cốt lõi nhất, liên quan đến nhiều bảng cùng lúc.

Bước 1: Giao diện POS (Point of Sale)
Thiết kế SalesView. Bên trái là danh sách sản phẩm (dạng Card hoặc DataGrid nhỏ). Bên phải là "Giỏ hàng" (Chi tiết hóa đơn đang lập) kèm TextBlock hiển thị "Tổng tiền". Cần có ô nhập SĐT để tìm nhanh Khách hàng.

Bước 2: Logic Giỏ hàng (Cart)
Khai báo ObservableCollection<OrderDetail> trong SalesViewModel. Khi double-click vào sản phẩm ở bên trái, thêm item vào collection này (nếu đã có thì cộng dồn Quantity). Tự động dùng LINQ tính Sum(Quantity * UnitPrice) để cập nhật Tổng tiền.

Bước 3: Xử lý Transaction (Cực kỳ quan trọng)
Khi bấm nút "Thanh toán", bọc toàn bộ code trong using (var transaction = context.Database.BeginTransaction()).

Bước 4: Lưu dữ liệu đồng bộ
Tạo object Order mới (lưu OrderDate, TotalAmount, CustomerId). Thêm các OrderDetail vào Order này. Duyệt qua từng sản phẩm trong giỏ để lấy object Product từ DB, trừ đi StockQuantity. Gọi context.SaveChanges(). Nếu mọi thứ OK, gọi transaction.Commit(). Nếu lỗi (ví dụ: kho không đủ hàng), gọi transaction.Rollback().

Giai đoạn 5: Báo cáo, Thống kê & Đóng gói (Tuần 5)
Mục tiêu: Hoàn thiện tính năng cho Quản lý và chuẩn bị file chạy .exe.

Bước 1: Tích hợp thư viện Biểu đồ
Cài đặt NuGet LiveCharts.Wpf. Trong DashboardView, kéo thả các control biểu đồ (Cột, Tròn).

Bước 2: Viết câu query Thống kê
Trong DashboardViewModel, dùng LINQ để group by dữ liệu: Thống kê doanh thu theo 7 ngày gần nhất, đếm số đơn hàng, truy vấn Top 5 sản phẩm có Quantity bán ra cao nhất. Bind dữ liệu này vào LiveCharts.

Bước 3: Validation và Bắt lỗi
Kiểm tra kỹ các trường hợp ngoại lệ: Khách đưa số tiền nhỏ hơn tổng hóa đơn, nhập sai định dạng số, bỏ trống trường bắt buộc. Bọc các thao tác DB bằng try...catch và hiển thị MessageBox thông báo thân thiện.

Bước 4: Đóng gói (Deployment)
Chuyển chế độ build từ Debug sang Release. Build project. Đảm bảo file .exe sinh ra trong thư mục bin/Release chạy ổn định và database .sqlite tự động được tạo/copy sang máy khác mà không cần cài SQL Server.

Việc setup file App.config để chạy SQLite với Entity Framework 6 (Code First) thường là bước hay gây lỗi nhất lúc mới bắt đầu.