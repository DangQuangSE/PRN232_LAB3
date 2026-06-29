# Kịch Bản Test Toàn Bộ Chức Năng — PRN232 LAB3 LMS Microservices

## Chuẩn Bị

| Thành phần | URL |
|---|---|
| **API Gateway** (điểm vào chính) | http://localhost:8080 |
| **Swagger – IdentityService** | http://localhost:8081/swagger |
| **Swagger – StudentService** | http://localhost:8082/swagger |
| **Swagger – CourseService** | http://localhost:8083/swagger |
| **Jaeger UI** (distributed tracing) | http://localhost:16686 |
| **RabbitMQ Management** | http://localhost:15672 (guest/guest) |

**Tài khoản seed sẵn** (password đều là `123456`):

| Username | Role |
|---|---|
| `admin` | Admin |
| `student` | Student |

**Dữ liệu seed sẵn:**
- 50 sinh viên (StudentId 1–50, code `SE190001`–`SE190050`)
- 5 học kỳ (SemesterId 1–5)
- 10 môn học (SubjectId 1–10)
- 20 khóa học (CourseId 1–20)
- ~500 lượt đăng ký ngẫu nhiên

---

## LUỒNG 1 — Đăng Nhập & Cấp Phát JWT Token

> **Mục đích:** Xác thực người dùng và nhận AccessToken + RefreshToken.

### TC-01: Đăng nhập thành công với tài khoản Admin

**Swagger:** http://localhost:8081/swagger → `POST /api/auth/login`

**Request:**
```json
POST http://localhost:8080/api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "123456"
}
```

**Kết quả mong đợi:** `200 OK`
```json
{
  "accessToken": "<JWT_TOKEN>",
  "refreshToken": "<REFRESH_TOKEN>"
}
```
> **Lưu lại** `accessToken` — dùng cho tất cả các test tiếp theo.

---

### TC-02: Đăng nhập thành công với tài khoản Student

```json
POST http://localhost:8080/api/auth/login
{
  "username": "student",
  "password": "123456"
}
```

**Kết quả mong đợi:** `200 OK` với token có `role: Student`

---

### TC-03: Đăng nhập sai mật khẩu

```json
POST http://localhost:8080/api/auth/login
{
  "username": "admin",
  "password": "wrong_password"
}
```

**Kết quả mong đợi:** `401 Unauthorized`

---

### TC-04: Làm mới Access Token (Refresh Token Flow)

```json
POST http://localhost:8080/api/auth/refresh-token
{
  "refreshToken": "<REFRESH_TOKEN_từ_TC-01>"
}
```

**Kết quả mong đợi:** `200 OK` với AccessToken mới

---

## LUỒNG 2 — Xác Thực & Phân Quyền Tại Gateway

> **Mục đích:** Kiểm tra Gateway chặn request không hợp lệ trước khi tới service.

### TC-05: Gọi API bảo mật không có token → bị chặn tại Gateway

```
GET http://localhost:8080/api/v1/students
(không có Authorization header)
```

**Kết quả mong đợi:** `401 Unauthorized` — trả về ngay tại Gateway, không tới StudentService

---

### TC-06: Gọi API bảo mật với token hợp lệ → thành công

```
GET http://localhost:8080/api/v1/students
Authorization: Bearer <ACCESS_TOKEN_từ_TC-01>
```

**Kết quả mong đợi:** `200 OK` với danh sách sinh viên

---

### TC-07: Tài khoản `student` xóa dữ liệu → bị từ chối (403)

```
DELETE http://localhost:8080/api/v1/students/1
Authorization: Bearer <TOKEN_của_student_TC-02>
```

**Kết quả mong đợi:** `403 Forbidden` — Role Student không có quyền DELETE

---

## LUỒNG 3 — Đăng Ký Học Phần & Xác Thực Sinh Viên Qua gRPC

> **Mục đích:** CourseService gọi gRPC sang StudentService để kiểm tra sinh viên tồn tại trước khi lưu enrollment.

### TC-08: Đăng ký học phần với sinh viên hợp lệ — thành công

**Swagger:** http://localhost:8083/swagger → `POST /api/v1/enrollments`

```json
POST http://localhost:8080/api/v1/enrollments
Authorization: Bearer <ACCESS_TOKEN>

{
  "studentId": 1,
  "courseId": 1,
  "enrollDate": "2026-07-01T00:00:00",
  "status": "Active"
}
```

**Kết quả mong đợi:** `201 Created`
```json
{
  "enrollmentId": <ID mới>,
  "studentId": 1,
  "courseId": 1,
  "status": "Active"
}
```
> **Ghi chú:** CourseService gọi gRPC `VerifyStudent(studentId=1)` sang StudentService trước khi lưu.

---

### TC-09: Đăng ký học phần với studentId KHÔNG tồn tại — gRPC trả false → 400

```json
POST http://localhost:8080/api/v1/enrollments
Authorization: Bearer <ACCESS_TOKEN>

{
  "studentId": 9999,
  "courseId": 1,
  "enrollDate": "2026-07-01T00:00:00",
  "status": "Active"
}
```

**Kết quả mong đợi:** `400 Bad Request`
```json
{
  "errors": {
    "studentId": ["Student with ID 9999 does not exist."]
  }
}
```

---

### TC-10: Đăng ký học phần trùng lặp — 400 Conflict

> Chạy lại **TC-08** với cùng `studentId` và `courseId` (nếu cặp đó chưa bị seed).

**Kết quả mong đợi:** `400 Bad Request` — "Could not create enrollment. It might already exist."

---

## LUỒNG 4 — Lấy Danh Sách Sinh Viên Của Khóa Học (Batch gRPC)

> **Mục đích:** CourseService lấy danh sách StudentId từ DB rồi gom lô gọi `GetStudentsByIds` qua gRPC sang StudentService.

### TC-11: Lấy sinh viên của một khóa học

```
GET http://localhost:8080/api/v1/courses/1/students
Authorization: Bearer <ACCESS_TOKEN>
```

**Kết quả mong đợi:** `200 OK` — danh sách sinh viên (có FullName, Email) của course 1
> **Ghi chú:** CourseService gọi 1 lần gRPC `GetStudentsByIds([...])` thay vì gọi lẻ từng sinh viên.

---

## LUỒNG 5 — Định Tuyến Thông Minh: Enrollments Của Sinh Viên

> **Mục đích:** Gateway nhận `/api/v1/students/{id}/enrollments` nhưng định tuyến sang **CourseService** (không phải StudentService).

### TC-12: Lấy danh sách enrollment của sinh viên qua Gateway

```
GET http://localhost:8080/api/v1/students/1/enrollments
Authorization: Bearer <ACCESS_TOKEN>
```

**Kết quả mong đợi:** `200 OK` — danh sách khóa học mà sinh viên 1 đã đăng ký
> **Xác minh:** Kiểm tra log của `lms_course_service` — request phải tới CourseService, không phải StudentService.

---

### TC-13: Lấy enrollment của sinh viên không tồn tại

```
GET http://localhost:8080/api/v1/students/9999/enrollments
Authorization: Bearer <ACCESS_TOKEN>
```

**Kết quả mong đợi:** `400 Bad Request` hoặc lỗi "Student does not exist"

---

## CRUD — Student Service

> **Swagger trực tiếp:** http://localhost:8082/swagger

### TC-14: Lấy danh sách sinh viên (phân trang)

```
GET http://localhost:8080/api/v1/students?page=1&pageSize=5
Authorization: Bearer <ACCESS_TOKEN>
```

**Kết quả mong đợi:** `200 OK` với 5 sinh viên đầu và metadata phân trang

---

### TC-15: Tìm kiếm sinh viên theo keyword

```
GET http://localhost:8080/api/v1/students?search=Student+1&page=1&pageSize=10
Authorization: Bearer <ACCESS_TOKEN>
```

**Kết quả mong đợi:** Danh sách sinh viên có tên chứa "Student 1"

---

### TC-16: Lấy chi tiết một sinh viên

```
GET http://localhost:8080/api/v1/students/5
Authorization: Bearer <ACCESS_TOKEN>
```

**Kết quả mong đợi:** `200 OK` thông tin sinh viên ID=5

---

### TC-17: Tạo sinh viên mới

```json
POST http://localhost:8080/api/v1/students
Authorization: Bearer <ACCESS_TOKEN>

{
  "studentCode": "SE999999",
  "fullName": "Nguyen Van Test",
  "email": "test@fpt.edu.vn",
  "phone": "0901234567",
  "dateOfBirth": "2000-01-15T00:00:00"
}
```

**Kết quả mong đợi:** `201 Created` với studentId mới (ví dụ: 51)

---

### TC-18: Cập nhật sinh viên

```json
PUT http://localhost:8080/api/v1/students/51
Authorization: Bearer <ACCESS_TOKEN>

{
  "studentCode": "SE999999",
  "fullName": "Nguyen Van Test Updated",
  "email": "test_updated@fpt.edu.vn",
  "phone": "0901234567",
  "dateOfBirth": "2000-01-15T00:00:00"
}
```

**Kết quả mong đợi:** `200 OK` hoặc `204 No Content`

---

### TC-19: Xóa sinh viên (Admin only)

```
DELETE http://localhost:8080/api/v1/students/51
Authorization: Bearer <ACCESS_TOKEN_của_admin>
```

**Kết quả mong đợi:** `204 No Content`

---

### TC-20: Lấy sinh viên với Data Shaping (field selection)

```
GET http://localhost:8080/api/v1/students?fields=studentId,fullName,email&page=1&pageSize=5
Authorization: Bearer <ACCESS_TOKEN>
```

**Kết quả mong đợi:** Chỉ trả về 3 trường `studentId`, `fullName`, `email`

---

## CRUD — Semester, Subject, Course

### TC-21: Tạo học kỳ mới

```json
POST http://localhost:8080/api/v1/semesters
Authorization: Bearer <ACCESS_TOKEN>

{
  "semesterName": "Fall 2026",
  "startDate": "2026-09-01T00:00:00",
  "endDate": "2027-01-15T00:00:00"
}
```

**Kết quả mong đợi:** `201 Created` với semesterId mới (ví dụ: 6)

---

### TC-22: Tạo môn học mới

```json
POST http://localhost:8080/api/v1/subjects
Authorization: Bearer <ACCESS_TOKEN>

{
  "subjectCode": "PRN232",
  "subjectName": "Advanced Web with .NET",
  "credit": 3
}
```

**Kết quả mong đợi:** `201 Created`

---

### TC-23: Tạo khóa học mới (thuộc học kỳ vừa tạo)

```json
POST http://localhost:8080/api/v1/courses
Authorization: Bearer <ACCESS_TOKEN>

{
  "courseName": "PRN232 Fall 2026",
  "semesterId": 6
}
```

**Kết quả mong đợi:** `201 Created` với courseId mới (ví dụ: 21)

---

### TC-24: Lấy danh sách khóa học với expand

```
GET http://localhost:8080/api/v1/courses?expand=semester&page=1&pageSize=5
Authorization: Bearer <ACCESS_TOKEN>
```

**Kết quả mong đợi:** `200 OK` — mỗi course có thêm field `semester` lồng vào

---

### TC-25: Lấy danh sách enrollment của một khóa học

```
GET http://localhost:8080/api/v1/courses/1/enrollments
Authorization: Bearer <ACCESS_TOKEN>
```

**Kết quả mong đợi:** `200 OK` — danh sách học viên đăng ký course 1

---

## CRUD — Enrollment (thêm các trường hợp phụ)

### TC-26: Cập nhật trạng thái enrollment (kích hoạt RabbitMQ event)

```json
PUT http://localhost:8080/api/v1/enrollments/1
Authorization: Bearer <ACCESS_TOKEN>

{
  "studentId": 1,
  "courseId": 1,
  "enrollDate": "2026-07-01T00:00:00",
  "status": "Completed"
}
```

**Kết quả mong đợi:** `200 OK`
> **Xác minh RabbitMQ:** Truy cập http://localhost:15672 → Queues → tìm queue `enrollment-status-changed-consumer` → kiểm tra message đã được publish và consumed.

---

### TC-27: Xóa enrollment (Admin only)

```
DELETE http://localhost:8080/api/v1/enrollments/<ID_vừa_tạo>
Authorization: Bearer <ACCESS_TOKEN_admin>
```

**Kết quả mong đợi:** `204 No Content`

---

## TÍNH NĂNG NÂNG CAO

### TC-28: Redis Cache — Kiểm Tra Hiệu Năng gRPC

> Mục đích: Lần 2 gọi cùng studentId sẽ lấy từ Redis thay vì gọi gRPC.

**Bước 1:** Gọi endpoint enrollment list để warm cache:
```
GET http://localhost:8080/api/v1/enrollments/1?expand=student
Authorization: Bearer <ACCESS_TOKEN>
```

**Bước 2:** Gọi lại lần 2 và quan sát log của `lms_course_service`:
```bash
docker logs lms_course_service --tail 20
```

**Kết quả mong đợi:** Lần 1 log xuất hiện gRPC call tới StudentService. Lần 2 **không** có gRPC call (lấy từ Redis cache).

---

### TC-29: RabbitMQ — Enrollment Created Event

**Bước 1:** Mở RabbitMQ Management → http://localhost:15672 → tab **Queues**

**Bước 2:** Tạo enrollment mới (TC-08 với studentId và courseId chưa tồn tại)

**Kết quả mong đợi:**
- Queue `prn232-lmssystem-studentservice-enrollmentcreatedconsumer` nhận được 1 message
- Log của `lms_student_service` xuất hiện dòng:
  ```
  [RabbitMQ] Enrollment created — StudentId=X enrolled in Course 'Y'...
  ```

**Xác minh:**
```bash
docker logs lms_student_service --tail 10
```

---

### TC-30: RabbitMQ — Enrollment Status Changed Event

**Bước 1:** Cập nhật trạng thái enrollment sang trạng thái khác (TC-26)

**Kết quả mong đợi:**
- Log của `lms_student_service` xuất hiện dòng:
  ```
  [RabbitMQ] Enrollment status changed — EnrollmentId=X ... Active → Completed
  ```

**Xác minh:**
```bash
docker logs lms_student_service --tail 10
```

---

### TC-31: OpenTelemetry — Distributed Tracing trên Jaeger

**Bước 1:** Thực hiện 1 request qua Gateway:
```
GET http://localhost:8080/api/v1/enrollments?page=1&pageSize=3
Authorization: Bearer <ACCESS_TOKEN>
```

**Bước 2:** Mở Jaeger UI → http://localhost:16686

**Bước 3:** Service: chọn `course-service` → nhấn **Find Traces**

**Kết quả mong đợi:**
- Trace xuất hiện với các spans: `api-gateway` → `course-service` → HTTP calls
- Span hiển thị đúng method, URL, và status code

---

### TC-32: Polly Retry — CourseService Gọi gRPC Khi StudentService Tạm Ngắt

> Mục đích: Kiểm tra Polly retry + circuit breaker hoạt động khi StudentService bị tắt.

**Bước 1:** Dừng StudentService:
```bash
docker stop lms_student_service
```

**Bước 2:** Tạo enrollment (sẽ kích hoạt gRPC call):
```json
POST http://localhost:8080/api/v1/enrollments
Authorization: Bearer <ACCESS_TOKEN>

{
  "studentId": 2,
  "courseId": 2,
  "enrollDate": "2026-07-01T00:00:00",
  "status": "Active"
}
```

**Bước 3:** Quan sát log CourseService:
```bash
docker logs lms_course_service --tail 30
```

**Kết quả mong đợi:**
- Log hiện thị: `"gRPC call to StudentService failed. Retry 1 in 2s..."`
- Sau 3 lần retry: `"Circuit breaker OPENED..."`

**Bước 4:** Khởi động lại StudentService:
```bash
docker start lms_student_service
```

**Kết quả mong đợi khi reset:**
- Log: `"Circuit breaker HALF-OPEN. Testing StudentService..."`
- Rồi: `"Circuit breaker CLOSED. StudentService is back."`

---

## KIỂM TRA LỖI & EDGE CASES

### TC-33: Request với token hết hạn

> Giả lập bằng cách dùng token cũ sau thời gian hết hạn hoặc tạo token giả.

**Kết quả mong đợi:** `401 Unauthorized`

---

### TC-34: Tạo sinh viên với dữ liệu thiếu (FluentValidation)

```json
POST http://localhost:8080/api/v1/students
Authorization: Bearer <ACCESS_TOKEN>

{
  "studentCode": "",
  "fullName": "",
  "email": "not-an-email",
  "phone": "123",
  "dateOfBirth": "2030-01-01T00:00:00"
}
```

**Kết quả mong đợi:** `400 Bad Request` với chi tiết lỗi validation từng field

---

### TC-35: Lấy resource không tồn tại

```
GET http://localhost:8080/api/v1/students/99999
Authorization: Bearer <ACCESS_TOKEN>
```

**Kết quả mong đợi:** `404 Not Found`

---

### TC-36: Xóa course có enrollments (Cascade delete)

```
DELETE http://localhost:8080/api/v1/courses/21
Authorization: Bearer <ACCESS_TOKEN_admin>
```

> Course 21 là khóa học vừa tạo ở TC-23, chưa có enrollment.

**Kết quả mong đợi:** `204 No Content`

---

## TỔNG HỢP LUỒNG END-TO-END

Chạy theo thứ tự sau để test toàn bộ hệ thống trong 1 luồng:

```
1.  TC-01  → Đăng nhập admin, lấy JWT
2.  TC-17  → Tạo sinh viên mới (studentId = 51)
3.  TC-21  → Tạo học kỳ mới (semesterId = 6)
4.  TC-23  → Tạo khóa học mới (courseId = 21)
5.  TC-08* → Đăng ký sinh viên 51 vào course 21 (gRPC verify)
             → Kiểm tra RabbitMQ: EnrollmentCreated event
6.  TC-12  → Lấy enrollment list của sinh viên 51
7.  TC-11  → Lấy sinh viên list của course 21
8.  TC-26* → Cập nhật enrollment status Active→Completed
             → Kiểm tra RabbitMQ: StatusChanged event
9.  TC-31  → Xem trace trên Jaeger
10. TC-27  → Xóa enrollment (admin)
11. TC-19  → Xóa sinh viên 51 (admin)
12. TC-36  → Xóa course 21 (admin)
```

> \* Dùng `studentId: 51` và `courseId: 21` thay cho giá trị ví dụ.
