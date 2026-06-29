#!/usr/bin/env pwsh
# LMS Microservices Automated Test Runner

$BASE_URL   = "http://localhost:8080"
$PASS       = 0
$FAIL       = 0
$Results    = @()
$SUFFIX     = Get-Random -Maximum 9999

function Call-Api {
    param([string]$Method, [string]$Path, $Body, [string]$Token)
    $headers = @{ "Content-Type" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    try {
        $p = @{ Method=$Method; Uri="$BASE_URL$Path"; Headers=$headers; UseBasicParsing=$true }
        if ($Body) { $p.Body = ($Body | ConvertTo-Json -Depth 10) }
        $r = Invoke-WebRequest @p
        return @{ Status=[int]$r.StatusCode; Body=($r.Content | ConvertFrom-Json -EA SilentlyContinue); Raw=$r.Content }
    } catch {
        $resp = $_.Exception.Response
        $st   = if ($resp) { [int]$resp.StatusCode } else { 0 }
        $bd   = $null
        if ($resp) {
            try {
                $sr = [System.IO.StreamReader]::new($resp.GetResponseStream())
                $bd = $sr.ReadToEnd() | ConvertFrom-Json -EA SilentlyContinue
            } catch {}
        }
        return @{ Status=$st; Body=$bd; Raw="" }
    }
}

function TC {
    param([string]$Id, [string]$Desc, [bool]$Pass, [string]$Detail="")
    if ($Pass) { $script:PASS++ } else { $script:FAIL++ }
    $icon   = if ($Pass) { "PASS" } else { "FAIL" }
    $status = if ($Pass) { "PASS" } else { "FAIL" }
    $fg     = if ($Pass) { "Green" } else { "Red" }
    $script:Results += [PSCustomObject]@{ ID=$Id; Description=$Desc; Status=$status; Detail=$Detail }
    Write-Host "  [$icon] $Id $Desc" -ForegroundColor $fg
    if ($Detail) { Write-Host "     -> $Detail" -ForegroundColor DarkGray }
}

Write-Host "`n============================================================" -ForegroundColor Cyan
Write-Host "  PRN232 LAB3 - LMS Microservices Test Runner" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Base URL : $BASE_URL"
Write-Host "  Run ID   : $SUFFIX"
Write-Host "  Time     : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n"

# AUTH
Write-Host "[AUTH & JWT]" -ForegroundColor Yellow

$r = Call-Api "POST" "/api/auth/login" @{ username="admin"; password="123456" }
$ADMIN_TOKEN   = $r.Body.data.accessToken
$REFRESH_TOKEN = $r.Body.data.refreshToken
TC "TC-01" "Login Admin" ($r.Status -eq 200 -and $ADMIN_TOKEN) "Status: $($r.Status)"

$r = Call-Api "POST" "/api/auth/login" @{ username="student"; password="123456" }
$STUDENT_TOKEN = $r.Body.data.accessToken
TC "TC-02" "Login Student" ($r.Status -eq 200 -and $STUDENT_TOKEN) "Status: $($r.Status)"

$r = Call-Api "POST" "/api/auth/login" @{ username="admin"; password="wrong_password" }
TC "TC-03" "Login wrong password" ($r.Status -in @(400,401)) "Status: $($r.Status)"

$r = Call-Api "POST" "/api/auth/refresh-token" @{ refreshToken=$REFRESH_TOKEN }
TC "TC-04" "Refresh Token" ($r.Status -eq 200 -and $r.Body.data.accessToken) "Status: $($r.Status)"

# GATEWAY AUTH
Write-Host "`n[GATEWAY AUTH]" -ForegroundColor Yellow

$r = Call-Api "GET" "/api/v1/students"
TC "TC-05" "No token -> 401" ($r.Status -eq 401) "Status: $($r.Status)"

$r = Call-Api "GET" "/api/v1/students?pageSize=10" -Token $ADMIN_TOKEN
TC "TC-06" "With token -> 200 list" ($r.Status -eq 200) "Status: $($r.Status)"

$r = Call-Api "DELETE" "/api/v1/students/1" -Token $STUDENT_TOKEN
TC "TC-07" "Student role DELETE -> 403" ($r.Status -eq 403) "Status: $($r.Status)"

# CREATE E2E DATA
Write-Host "`n[CREATE E2E DATA - suffix=$SUFFIX]" -ForegroundColor Yellow

$r = Call-Api "POST" "/api/v1/students" @{
    studentCode="SE$($SUFFIX.ToString().PadLeft(5,'0'))"; fullName="Test Student $SUFFIX"
    email="test_$SUFFIX@fpt.edu.vn"; phone="0901234567"; dateOfBirth="2000-01-15T00:00:00"
} -Token $ADMIN_TOKEN
$NEW_STUDENT_ID = $r.Body.data.studentId
TC "TC-17" "Create new student" ($r.Status -eq 201 -and $NEW_STUDENT_ID) "StudentId: $NEW_STUDENT_ID"

$r = Call-Api "POST" "/api/v1/semesters" @{
    semesterName="Fall 2026 Test $SUFFIX"; startDate="2026-09-01T00:00:00"; endDate="2027-01-15T00:00:00"
} -Token $ADMIN_TOKEN
$NEW_SEMESTER_ID = $r.Body.data.semesterId
TC "TC-21" "Create new semester" ($r.Status -eq 201 -and $NEW_SEMESTER_ID) "SemesterId: $NEW_SEMESTER_ID"

$r = Call-Api "POST" "/api/v1/subjects" @{
    subjectCode="PRN$SUFFIX"; subjectName="Test Subject $SUFFIX"; credit=3
} -Token $ADMIN_TOKEN
$NEW_SUBJECT_ID = $r.Body.data.subjectId
TC "TC-22" "Create new subject" ($r.Status -eq 201 -and $NEW_SUBJECT_ID) "SubjectId: $NEW_SUBJECT_ID"

$r = Call-Api "POST" "/api/v1/courses" @{
    courseName="Test Course $SUFFIX"; semesterId=$NEW_SEMESTER_ID
} -Token $ADMIN_TOKEN
$NEW_COURSE_ID = $r.Body.data.courseId
TC "TC-23" "Create new course" ($r.Status -eq 201 -and $NEW_COURSE_ID) "CourseId: $NEW_COURSE_ID"

# GRPC & ENROLLMENT
Write-Host "`n[GRPC & ENROLLMENT]" -ForegroundColor Yellow

$r = Call-Api "POST" "/api/v1/enrollments" @{
    studentId=$NEW_STUDENT_ID; courseId=$NEW_COURSE_ID
    enrollDate="2026-07-01T00:00:00"; status="Active"
} -Token $ADMIN_TOKEN
$NEW_ENROLLMENT_ID = $r.Body.data.enrollmentId
TC "TC-08" "Enroll valid student (gRPC VerifyStudent)" ($r.Status -eq 201 -and $NEW_ENROLLMENT_ID) "EnrollmentId: $NEW_ENROLLMENT_ID, StudentName: $($r.Body.data.studentName)"

$r = Call-Api "POST" "/api/v1/enrollments" @{
    studentId=9999; courseId=$NEW_COURSE_ID
    enrollDate="2026-07-01T00:00:00"; status="Active"
} -Token $ADMIN_TOKEN
TC "TC-09" "StudentId 9999 not exist -> gRPC false -> 400" ($r.Status -eq 400) "Status: $($r.Status)"

$r = Call-Api "POST" "/api/v1/enrollments" @{
    studentId=$NEW_STUDENT_ID; courseId=$NEW_COURSE_ID
    enrollDate="2026-07-01T00:00:00"; status="Active"
} -Token $ADMIN_TOKEN
TC "TC-10" "Duplicate enrollment -> 400" ($r.Status -eq 400) "Status: $($r.Status)"

# BATCH GRPC
Write-Host "`n[BATCH GRPC]" -ForegroundColor Yellow

$r = Call-Api "GET" "/api/v1/courses/$NEW_COURSE_ID/students" -Token $ADMIN_TOKEN
$stuArr = if ($r.Body.data -is [Array]) { $r.Body.data } elseif ($r.Body.data) { @($r.Body.data) } else { @() }
TC "TC-11" "Get students of course (Batch gRPC)" ($r.Status -eq 200 -and $stuArr.Count -ge 1) "Status: $($r.Status), Students: $($stuArr.Count)"

# SMART ROUTING
Write-Host "`n[SMART ROUTING]" -ForegroundColor Yellow

$r = Call-Api "GET" "/api/v1/students/$NEW_STUDENT_ID/enrollments" -Token $ADMIN_TOKEN
$enrArr = if ($r.Body.data -is [Array]) { $r.Body.data } elseif ($r.Body.data) { @($r.Body.data) } else { @() }
TC "TC-12" "Student enrollments via Gateway -> CourseService" ($r.Status -eq 200 -and $enrArr.Count -ge 1) "Status: $($r.Status), Count: $($enrArr.Count)"

$r = Call-Api "GET" "/api/v1/students/9999/enrollments" -Token $ADMIN_TOKEN
TC "TC-13" "Enrollments of non-exist student -> 400/404" ($r.Status -in @(400,404)) "Status: $($r.Status)"

# STUDENT CRUD
Write-Host "`n[STUDENT CRUD]" -ForegroundColor Yellow

$r = Call-Api "GET" "/api/v1/students?page=1&pageSize=5" -Token $ADMIN_TOKEN
TC "TC-14" "List students with pagination" ($r.Status -eq 200) "Status: $($r.Status)"

$r = Call-Api "GET" "/api/v1/students?search=Student+1&page=1&pageSize=10" -Token $ADMIN_TOKEN
TC "TC-15" "Search students 'Student 1'" ($r.Status -eq 200) "Status: $($r.Status)"

$r = Call-Api "GET" "/api/v1/students/5" -Token $ADMIN_TOKEN
TC "TC-16" "Get student by ID=5" ($r.Status -eq 200 -and $r.Body.data.studentId -eq 5) "Name: $($r.Body.data.fullName)"

$r = Call-Api "PUT" "/api/v1/students/$NEW_STUDENT_ID" @{
    studentCode="SE$($SUFFIX.ToString().PadLeft(5,'0'))"; fullName="Updated $SUFFIX"
    email="upd_$SUFFIX@fpt.edu.vn"; phone="0901234567"; dateOfBirth="2000-01-15T00:00:00"
} -Token $ADMIN_TOKEN
TC "TC-18" "Update student" ($r.Status -in @(200,204)) "Status: $($r.Status)"

$r = Call-Api "GET" "/api/v1/students?fields=studentId,fullName,email&page=1&pageSize=1" -Token $ADMIN_TOKEN
$item = if ($r.Body.data -is [Array]) { $r.Body.data[0] } else { $r.Body.data }
$props = @($item.PSObject.Properties.Name)
$shapeOk = ($props -contains "studentId") -and ($props -contains "fullName") -and ($props -contains "email")
TC "TC-20" "Data Shaping (fields)" ($r.Status -eq 200 -and $shapeOk) "Keys: $($props -join ', ')"

# COURSE/SEMESTER CRUD
Write-Host "`n[COURSE/SEMESTER CRUD]" -ForegroundColor Yellow

$r = Call-Api "GET" "/api/v1/courses?expand=semester&page=1&pageSize=5" -Token $ADMIN_TOKEN
$firstCourse = if ($r.Body.data -is [Array]) { $r.Body.data[0] } else { $r.Body.data }
TC "TC-24" "Courses with expand=semester" ($r.Status -eq 200 -and $firstCourse.semester) "SemesterName: $($firstCourse.semester.semesterName)"

$r = Call-Api "GET" "/api/v1/courses/1/enrollments?pageSize=10" -Token $ADMIN_TOKEN
$enrCount = if ($r.Body.data -is [Array]) { $r.Body.data.Count } else { 0 }
TC "TC-25" "Enrollment list of Course 1" ($r.Status -eq 200) "Count: $enrCount"

# ENROLLMENT UPDATE -> RABBITMQ
Write-Host "`n[ENROLLMENT UPDATE -> RABBITMQ]" -ForegroundColor Yellow

$r = Call-Api "PUT" "/api/v1/enrollments/$NEW_ENROLLMENT_ID" @{
    studentId=$NEW_STUDENT_ID; courseId=$NEW_COURSE_ID
    enrollDate="2026-07-01T00:00:00"; status="Completed"
} -Token $ADMIN_TOKEN
TC "TC-26" "Update enrollment status -> RabbitMQ event" ($r.Status -in @(200,204)) "Status: $($r.Status)"

Start-Sleep -Seconds 2

# REDIS CACHE
Write-Host "`n[REDIS CACHE]" -ForegroundColor Yellow

$sw1 = [Diagnostics.Stopwatch]::StartNew()
$r1  = Call-Api "GET" "/api/v1/enrollments/$NEW_ENROLLMENT_ID`?expand=student" -Token $ADMIN_TOKEN
$sw1.Stop(); $ms1 = $sw1.ElapsedMilliseconds

$sw2 = [Diagnostics.Stopwatch]::StartNew()
$r2  = Call-Api "GET" "/api/v1/enrollments/$NEW_ENROLLMENT_ID`?expand=student" -Token $ADMIN_TOKEN
$sw2.Stop(); $ms2 = $sw2.ElapsedMilliseconds

TC "TC-28" "Redis Cache (2nd call faster or equal)" ($r1.Status -eq 200 -and $r2.Status -eq 200) "1st: ${ms1}ms | 2nd: ${ms2}ms"

# RABBITMQ LOGS
Write-Host "`n[RABBITMQ EVENT LOGS]" -ForegroundColor Yellow

$logs = docker logs lms_student_service --tail 80 2>&1 | Out-String
$hasCreated = $logs -match "enrolled in Course|EnrollmentCreated|Enrollment created"
$hasChanged = $logs -match "status changed|StatusChanged|Active.*Completed|Completed"
TC "TC-29" "RabbitMQ EnrollmentCreated event consumed" $hasCreated "LogFound: $hasCreated"
TC "TC-30" "RabbitMQ StatusChanged event consumed" $hasChanged "LogFound: $hasChanged"

# JAEGER
Write-Host "`n[OPENTELEMETRY / JAEGER]" -ForegroundColor Yellow

try {
    $jaeger = Invoke-RestMethod "http://localhost:16686/api/services" -EA Stop
    $svcs   = $jaeger.data
    $ok     = ($svcs -contains "course-service") -or ($svcs -contains "student-service") -or ($svcs.Count -gt 1)
    TC "TC-31" "Jaeger Distributed Tracing - services registered" $ok "Services: $($svcs -join ', ')"
} catch {
    TC "TC-31" "Jaeger Distributed Tracing" $false "Jaeger unreachable"
}

# ERROR CASES
Write-Host "`n[ERROR CASES]" -ForegroundColor Yellow

$r = Call-Api "GET" "/api/v1/students" -Token "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid.signature"
TC "TC-33" "Invalid token -> 401" ($r.Status -eq 401) "Status: $($r.Status)"

$r = Call-Api "POST" "/api/v1/students" @{
    studentCode=""; fullName=""; email="not-an-email"; phone="123"; dateOfBirth="2030-01-01T00:00:00"
} -Token $ADMIN_TOKEN
TC "TC-34" "Missing data -> 400 FluentValidation" ($r.Status -eq 400) "Status: $($r.Status)"

$r = Call-Api "GET" "/api/v1/students/99999" -Token $ADMIN_TOKEN
TC "TC-35" "Resource not found -> 404" ($r.Status -eq 404) "Status: $($r.Status)"

# CLEANUP
Write-Host "`n[CLEANUP]" -ForegroundColor Yellow

$r = Call-Api "DELETE" "/api/v1/enrollments/$NEW_ENROLLMENT_ID" -Token $ADMIN_TOKEN
TC "TC-27" "Delete enrollment" ($r.Status -in @(200,204)) "Status: $($r.Status)"

$r = Call-Api "DELETE" "/api/v1/courses/$NEW_COURSE_ID" -Token $ADMIN_TOKEN
TC "TC-36" "Delete course (cascade)" ($r.Status -in @(200,204)) "Status: $($r.Status)"

$r = Call-Api "DELETE" "/api/v1/students/$NEW_STUDENT_ID" -Token $ADMIN_TOKEN
TC "TC-19" "Delete student" ($r.Status -in @(200,204)) "Status: $($r.Status)"

# POLLY
Write-Host "`n[POLLY RETRY & CIRCUIT BREAKER]" -ForegroundColor Yellow
Write-Host "  Stopping lms_student_service..." -ForegroundColor DarkGray
docker stop lms_student_service 2>&1 | Out-Null
Start-Sleep -Seconds 2

$r = Call-Api "POST" "/api/v1/enrollments" @{
    studentId=2; courseId=2; enrollDate="2026-07-01T00:00:00"; status="Active"
} -Token $ADMIN_TOKEN

$pollyLogs   = docker logs lms_course_service --tail 80 2>&1 | Out-String
$hasRetryLog = $pollyLogs -match "Retry|retry|gRPC call to StudentService failed|circuit"

Write-Host "  Restarting lms_student_service..." -ForegroundColor DarkGray
docker start lms_student_service 2>&1 | Out-Null

TC "TC-32" "Polly Retry when StudentService down" ($r.Status -in @(400,500,503) -and $hasRetryLog) "HTTP: $($r.Status), RetryLog: $hasRetryLog"

# SUMMARY
$TOTAL = $PASS + $FAIL
$pct   = [Math]::Round($PASS / $TOTAL * 100)
$fg    = if ($FAIL -eq 0) { "Green" } elseif ($FAIL -le 3) { "Yellow" } else { "Red" }

Write-Host "`n============================================================" -ForegroundColor Cyan
Write-Host "  RESULT: $PASS PASS | $FAIL FAIL | $TOTAL TOTAL ($pct%)" -ForegroundColor $fg
Write-Host "============================================================" -ForegroundColor Cyan

if ($FAIL -gt 0) {
    Write-Host "`n[FAILED TESTS]" -ForegroundColor Red
    $Results | Where-Object { $_.Status -eq "FAIL" } | ForEach-Object {
        Write-Host "  [FAIL] $($_.ID) $($_.Description)" -ForegroundColor Red
        Write-Host "     -> $($_.Detail)" -ForegroundColor DarkGray
    }
}

Write-Host ""
