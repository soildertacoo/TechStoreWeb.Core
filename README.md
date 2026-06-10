# 🛒 TechStore - Modern E-Commerce Web Application

![.NET Core](https://img.shields.io/badge/.NET%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoft%20sql%20server&logoColor=white)
![CSS](https://img.shields.io/badge/css-%23663399.svg?style=for-the-badge&logo=css&logoColor=white)
![HTML5](https://img.shields.io/badge/html5-%23E34F26.svg?style=for-the-badge&logo=html5&logoColor=white)
![JavaScript](https://img.shields.io/badge/javascript-%23323330.svg?style=for-the-badge&logo=javascript&logoColor=%23F7DF1E)
![Bootstrap](https://img.shields.io/badge/bootstrap-%238511FA.svg?style=for-the-badge&logo=bootstrap&logoColor=white)


## 📖 Overview
**TechStore** is a full-stack, feature-rich e-commerce web application built with **ASP.NET Core MVC** and **Entity Framework Core**. Designed with a focus on performance, security, and user experience, this project serves as a comprehensive solution for online tech gadget retail.

## ✨ Key Features
* **Authentication & Security:** Implemented robust user login/registration with **Two-Factor Authentication (2FA)** using QR codes.
* **Shopping Cart Management:** Seamless cart operations (Add, Update, Remove) with dynamic database tracking and session fallback.
* **Product Catalog:** Advanced product listing with dynamic star ratings, "items sold" tracking, and responsive UI.
* **Optimized Performance:** Solved the N+1 query problem using LINQ `.Join()` and optimized data lookups using `Dictionary<TKey, TValue>` for O(1) time complexity.
* **Asynchronous Processing:** Applied `async/await` patterns across Controllers and Database operations for high scalability and responsiveness.

## 🛠️ Tech Stack
* **Backend:** C#, ASP.NET Core MVC, .NET Framework / Modern .NET
* **Database:** SQL Server, Entity Framework Core (Code-First / Database-First Hybrid)
* **Frontend:** HTML5, CSS3, JavaScript, Razor Pages, Bootstrap/Tailwind
* **Hosting/Deployment:** Database hosted on Somee.com

## 📸 Screenshots
*(Add your project screenshots here - Homepage, Cart, 2FA Screen, etc.)*


## 🚀 Getting Started

### Prerequisites
* Visual Studio 2022,VS Code
* .NET SDK (10)
* SQL Server Management Studio 2022 (SSMS)

### Installation
1. Clone the repository:
   ```bash
   git clone [https://github.com/soildertacoo/TechStoreWeb.Core.git](https://github.com/soildertacoo/TechStoreWeb.Core.git)
#Vietnamese Readme.md
# 🛒 TechStore - Ứng Dụng Web Thương Mại Điện Tử Hiện Đại

## 📖 Tổng quan dự án
**TechStore** là một ứng dụng web thương mại điện tử đa nền tảng được xây dựng bằng kiến trúc **ASP.NET Core MVC** và **Entity Framework Core**. Dự án được thiết kế không chỉ để đáp ứng các nghiệp vụ bán lẻ các thiết bị công nghệ mà còn tập trung sâu vào việc tối ưu hóa hiệu năng hệ thống, bảo mật dữ liệu và mang lại trải nghiệm mượt mà cho người dùng cuối.

## ✨ Điểm nhấn Kỹ thuật (Technical Highlights)
Trong quá trình phát triển dự án, tôi đã nghiên cứu và giải quyết các bài toán kỹ thuật thực tế:

* **Bảo mật nâng cao (Two-Factor Authentication):** Tích hợp thành công Xác thực hai yếu tố (2FA) thông qua mã QR, cung cấp thêm một lớp bảo mật vững chắc cho tài khoản của khách hàng.
* **Tối ưu hóa Truy vấn Cơ sở dữ liệu (EF Core Performance):** * Khắc phục triệt để lỗi truy vấn **N+1** khi hiển thị danh sách sản phẩm.
    * Sử dụng cấu trúc dữ liệu `Dictionary<TKey, TValue>` kết hợp với Tuple để tra cứu Điểm đánh giá (Rating) và Số lượng đã bán với độ phức tạp thuật toán **O(1)**. Giúp trang chủ tải cực nhanh ngay cả khi lượng dữ liệu lớn.
* **Xử lý Bất đồng bộ (Asynchronous Programming):** Áp dụng triệt để kiến trúc `async/await` cho toàn bộ các thao tác I/O với cơ sở dữ liệu, giúp Server giải phóng luồng (thread) và chịu tải tốt hơn khi có nhiều truy cập cùng lúc.
* **Quản lý Giỏ hàng thông minh:** Xây dựng logic giỏ hàng (Cart) kết hợp linh hoạt. Xử lý tốt các vấn đề ghi đè/cộng dồn số lượng và giải quyết triệt để lỗi `IDENTITY_INSERT` trong quá trình mapping dữ liệu.

## 🛠️ Công nghệ sử dụng
* **Backend:** C#, ASP.NET Core MVC
* **Cơ sở dữ liệu:** SQL Server (Được triển khai thực tế trên Somee), Entity Framework Core (Sử dụng kỹ thuật Migration linh hoạt)
* **Frontend:** HTML5, CSS3, ES6 JavaScript, Razor View Engine, Bootstrap
* **Công cụ khác:** Git & GitHub, Visual Studio / VS Code

## 📸 Giao diện ứng dụng (Screenshots)


## 🚀 Hướng dẫn cài đặt (Getting Started)

### Yêu cầu hệ thống
* Visual Studio 2022 (hoặc Visual Studio Code)
* .NET SDK 
* SQL Server / SQL Server Management Studio (SSMS)

### Các bước triển khai
1. Clone mã nguồn về máy local:
   ```bash
   git clone [https://github.com/soildertacoo/TechStoreWeb.Core.git](https://github.com/soildertacoo/TechStoreWeb.Core.git)
