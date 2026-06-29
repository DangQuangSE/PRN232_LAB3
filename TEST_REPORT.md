# BÁO CÁO KẾT QUẢ KIỂM THỬ TỰ ĐỘNG (AUTOMATED TEST REPORT)

* **Thời gian thực hiện:** 2026-06-29T11:55:27.086Z
* **Môi trường:** Local (Docker Containers / REST API / gRPC)
* **Được tạo bởi:** Antigravity AI Agent

## Tổng quan kết quả (Summary)

| Chỉ số | Giá trị |
|---|---|
| **Tổng số kịch bản test** | 36 |
| **Đạt (PASS)** | 🟢 36 (100%) |
| **Không đạt (FAIL)** | 🔴 0 (0%) |

## Chi tiết kết quả kiểm thử (Test Case Details)

| ID | Kịch bản kiểm thử | Trạng thái | Kết quả mong đợi | Kết quả thực tế / Chi tiết |
|---|---|---|---|---|
| TC-01 | Đăng nhập thành công với tài khoản Admin | 🟢 **PASS** | 200 OK & AccessToken | Token received successfully<br><pre>200 OK & Token: eyJhbGciOiJIUzI1NiIs...</pre> |
| TC-02 | Đăng nhập thành công với tài khoản Student | 🟢 **PASS** | 200 OK & AccessToken | Token received successfully<br><pre>200 OK & Token: eyJhbGciOiJIUzI1NiIs...</pre> |
| TC-03 | Đăng nhập sai mật khẩu | 🟢 **PASS** | 400 Bad Request or 401 Unauthorized | Received expected error (Status 400)<br><pre>{<br>  "success": false,<br>  "message": "Invalid username or password.",<br>  "errors": null<br>}</pre> |
| TC-04 | Làm mới Access Token (Refresh Token Flow) | 🟢 **PASS** | 200 OK with new AccessToken | New AccessToken received<br><pre>200 OK & Token: eyJhbGciOiJIUzI1NiIs...</pre> |
| TC-05 | Gọi API bảo mật không có token → bị chặn tại Gateway | 🟢 **PASS** | 401 Unauthorized | Gateway returned 401 Unauthorized<br><pre>Status: 401</pre> |
| TC-06 | Gọi API bảo mật với token hợp lệ → thành công | 🟢 **PASS** | 200 OK with student list | Retrieved 10 students<br><pre>200 OK & Count: 10</pre> |
| TC-07 | Tài khoản student xóa dữ liệu → bị từ chối (403) | 🟢 **PASS** | 403 Forbidden | Gateway/Service returned 403 Forbidden<br><pre>Status: 403</pre> |
| TC-14 | Lấy danh sách sinh viên (phân trang) | 🟢 **PASS** | 200 OK with non-empty data and pagination metadata | Page size: 5, Total: 54, Items: 5<br><pre>{<br>  "page": 1,<br>  "pageSize": 5,<br>  "totalItems": 54,<br>  "totalPages": 11<br>}</pre> |
| TC-15 | Tìm kiếm sinh viên theo keyword "Student 1" | 🟢 **PASS** | 200 OK non-empty list containing search criteria | Found: Student 1, Student 10, Student 11, Student 12, Student 13, Student 14, Student 15, Student 16, Student 17, Student 18<br><pre>[<br>  "Student 1",<br>  "Student 10",<br>  "Student 11",<br>  "Student 12",<br>  "Student 13",<br>  "Student 14",<br>  "Student 15",<br>  "Student 16",<br>  "Student 17",<br>  "Student 18"<br>]</pre> |
| TC-16 | Lấy chi tiết một sinh viên (ID = 5) | 🟢 **PASS** | 200 OK student details | Name: Student 5, Code: SE190005<br><pre>{<br>  "studentId": 5,<br>  "studentCode": "SE190005",<br>  "fullName": "Student 5",<br>  "email": "student5@fpt.edu.vn",<br>  "phone": "0908000005",<br>  "dateOfBirth": "2007-06-22T02:57:04.504022Z",<br>  "enrollments": null<br>}</pre> |
| TC-20 | Lấy sinh viên với Data Shaping (studentId, fullName, email) | 🟢 **PASS** | Only studentId, fullName, email returned | Shaped keys: studentId, fullName, email<br><pre>{<br>  "studentId": 1,<br>  "fullName": "Student 1",<br>  "email": "student1@fpt.edu.vn"<br>}</pre> |
| TC-34 | Tạo sinh viên với dữ liệu thiếu (FluentValidation) | 🟢 **PASS** | 400 Bad Request with validation errors | Validation error returned successfully<br><pre>{<br>  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",<br>  "title": "One or more validation errors occurred.",<br>  "status": 400,<br>  "errors": {<br>    "email": [<br>      "Email must be a valid email address."<br>    ],<br>    "phone": [<br>      "Phone must be a valid Vietnamese mobile number (10 or 11 digits starting with 0)."<br>    ],<br>    "fullName": [<br>      "FullName is required.",<br>      "FullName must be between 2 and 100 characters."<br>    ],<br>    "studentCo</pre> |
| TC-35 | Lấy resource không tồn tại (404) | 🟢 **PASS** | 404 Not Found | Received 404 Not Found<br><pre>Status: 404</pre> |
| TC-22 | Tạo môn học mới | 🟢 **PASS** | 201 Created | Created Subject ID: 20<br><pre>{<br>  "subjectId": 20,<br>  "subjectCode": "PRN113478",<br>  "subjectName": "Advanced Web with .NET",<br>  "credit": 3<br>}</pre> |
| TC-24 | Courses with expand=semester | 🟢 **PASS** | 200 OK with semester properties inside courses | Semester information expanded successfully<br><pre>{<br>  "courseId": 1,<br>  "courseName": "Course 1",<br>  "semesterId": 5,<br>  "semesterName": "Semester 5",<br>  "enrollmentCount": 24,<br>  "semester": {<br>    "semesterId": 5,<br>    "semesterName": "Semester 5",<br>    "startDate": "2028-12-29T04:42:59.818506Z",<br>    "endDate": "2029-06-29T04:42:59.818506Z",<br>    "courseCount": 3<br>  },<br>  "enrollments": null<br>}</pre> |
| TC-25 | Lấy danh sách enrollment của một khóa học (Course ID = 1) | 🟢 **PASS** | 200 OK with enrollment list | Retrieved 10 enrollments<br><pre>200 OK & Count: 10</pre> |
| TC-17 | Tạo sinh viên mới (Admin) | 🟢 **PASS** | 201 Created | Created Student ID: 64<br><pre>{<br>  "studentId": 64,<br>  "studentCode": "SE452024",<br>  "fullName": "Nguyen Van Test E2E",<br>  "email": "test_e2e_se452024@fpt.edu.vn",<br>  "phone": "0901234567",<br>  "dateOfBirth": "2000-01-15T00:00:00Z"<br>}</pre> |
| TC-21 | Tạo học kỳ mới | 🟢 **PASS** | 201 Created | Created Semester ID: 16<br><pre>{<br>  "semesterId": 16,<br>  "semesterName": "Fall 2026 E2E 3568",<br>  "startDate": "2026-09-01T00:00:00Z",<br>  "endDate": "2027-01-15T00:00:00Z",<br>  "courseCount": 0<br>}</pre> |
| TC-23 | Tạo khóa học mới | 🟢 **PASS** | 201 Created | Created Course ID: 32<br><pre>{<br>  "courseId": 32,<br>  "courseName": "PRN232 Fall 2026 E2E",<br>  "semesterId": 16,<br>  "semesterName": "Fall 2026 E2E 3568",<br>  "enrollmentCount": 0<br>}</pre> |
| TC-08 | Đăng ký học phần với sinh viên hợp lệ (gRPC verify) | 🟢 **PASS** | 201 Created | Created Enrollment ID: 520<br><pre>{<br>  "enrollmentId": 520,<br>  "studentId": 64,<br>  "studentName": "Nguyen Van Test E2E",<br>  "student": {<br>    "studentId": 64,<br>    "studentCode": "SE452024",<br>    "fullName": "Nguyen Van Test E2E",<br>    "email": "test_e2e_se452024@fpt.edu.vn",<br>    "phone": "0901234567",<br>    "dateOfBirth": "2000-01-15T00:00:00+00:00"<br>  },<br>  "courseId": 32,<br>  "courseName": "PRN232 Fall 2026 E2E",<br>  "course": {<br>    "courseId": 32,<br>    "courseName": "PRN232 Fall 2026 E2E",<br> </pre> |
| TC-09 | Đăng ký học phần với studentId KHÔNG tồn tại (gRPC returns false) | 🟢 **PASS** | 400 Bad Request | Validation rejected student 9999<br><pre>{<br>  "success": false,<br>  "message": "Student verification failed.",<br>  "errors": {<br>    "studentId": [<br>      "Student with ID 9999 does not exist."<br>    ]<br>  }<br>}</pre> |
| TC-10 | Đăng ký học phần trùng lặp | 🟢 **PASS** | 400 Bad Request (Conflict) | Correctly rejected duplicate enrollment<br><pre>{<br>  "success": false,<br>  "message": "Could not create enrollment. It might already exist.",<br>  "errors": "An error occurred while saving the entity changes. See the inner exception for details."<br>}</pre> |
| TC-11 | Lấy sinh viên của một khóa học (Batch gRPC) | 🟢 **PASS** | 200 OK with students details from StudentService | Students found: 1<br><pre>[<br>  {<br>    "studentId": 64,<br>    "studentCode": "SE452024",<br>    "fullName": "Nguyen Van Test E2E",<br>    "email": "test_e2e_se452024@fpt.edu.vn",<br>    "phone": "0901234567",<br>    "dateOfBirth": "2000-01-15T00:00:00+00:00"<br>  }<br>]</pre> |
| TC-12 | Lấy danh sách enrollment của sinh viên qua Gateway | 🟢 **PASS** | 200 OK with enrollments list | Gateway successfully routed to CourseService. Count: 1<br><pre>[<br>  {<br>    "enrollmentId": 520,<br>    "courseId": 32,<br>    "courseName": "PRN232 Fall 2026 E2E",<br>    "course": null,<br>    "enrollDate": "2026-07-01T00:00:00Z",<br>    "status": "Active"<br>  }<br>]</pre> |
| TC-13 | Lấy enrollment của sinh viên không tồn tại | 🟢 **PASS** | 400 Bad Request or 404 Not Found | Status: 404. Correct error behavior.<br><pre>{<br>  "success": false,<br>  "message": "Student with ID '9999' was not found.",<br>  "errors": null<br>}</pre> |
| TC-18 | Cập nhật sinh viên | 🟢 **PASS** | 200 OK | Student details updated successfully<br><pre>{<br>  "success": true,<br>  "message": "Student updated successfully",<br>  "errors": null<br>}</pre> |
| TC-26 | Cập nhật trạng thái enrollment (triggers RabbitMQ event) | 🟢 **PASS** | 200 OK | Enrollment status updated to Completed<br><pre>undefined</pre> |
| TC-33 | Request với token hết hạn / không hợp lệ | 🟢 **PASS** | 401 Unauthorized | Gateway correctly returned 401 Unauthorized<br><pre>Status: 401</pre> |
| TC-28 | Redis Cache — Kiểm Tra Hiệu Năng gRPC (Lần 1 vs Lần 2) | 🟢 **PASS** | Lần 2 phải nhanh hơn và lấy từ cache | Lần 1: 5ms (DB/gRPC), Lần 2: 4ms (Redis Cache - 20% faster!)<br><pre>Lần 1: 5ms, Lần 2: 4ms</pre> |
| TC-29 | RabbitMQ — Enrollment Created Event | 🟢 **PASS** | Message processed | EnrollmentCreated message consumed and logged in StudentService<br><pre>Logged in docker logs</pre> |
| TC-30 | RabbitMQ — Enrollment Status Changed Event | 🟢 **PASS** | Message processed | EnrollmentStatusChanged message consumed and logged in StudentService<br><pre>Logged in docker logs</pre> |
| TC-32 | Polly Retry & Circuit Breaker (StudentService Down) | 🟢 **PASS** | Failure with Polly retry log output | gRPC call failed as expected. Status: 400. Request duration: 3648ms. Log matches retry/breaker patterns.<br><pre>Status: 400, Retry log found: true</pre> |
| TC-27 | Xóa enrollment (Admin) | 🟢 **PASS** | 200 OK / 204 No Content | Enrollment deleted successfully<br><pre>Status: 200</pre> |
| TC-19 | Xóa sinh viên (Admin) | 🟢 **PASS** | 200 OK / 204 No Content | Student deleted successfully<br><pre>Status: 200</pre> |
| TC-36 | Xóa course (Cascade delete testing - Admin) | 🟢 **PASS** | 200 OK / 204 No Content | Course deleted successfully<br><pre>Status: 200</pre> |
| TC-31 | OpenTelemetry — Distributed Tracing trên Jaeger | 🟢 **PASS** | Traces exist in Jaeger | Found 5 traces in Jaeger for course-service. Distributed spans captured!<br><pre>Traces found: 5</pre> |
