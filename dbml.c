Table users {
  id varchar [primary key]
  username varchar
  normalized_username varchar
  email varchar
  normalized_email varchar [unique]
  email_confirmed bool
  password_hash varchar
  security_stamp varchar
  concurrency_stamp varchar
  phone_number varchar [null]
  phone_number_confirmed bool
  two_factor_enabled bool
  lockout_end timestamp [null]
  lockout_enabled bool
  access_failed_count integer
}

Table instructors {
  id integer [primary key]
  firstname varchar [null]
  lastname varchar [null]
  user_id varchar [unique, ref: - users.id]
  is_deleted bool [default: false]
  deleted_at timestamp [null]
  created_at timestamp
  updated_at timestamp
}

Table students {
  id integer [primary key]
  firstname varchar
  lastname varchar
  user_id varchar [unique, ref: - users.id]
  section_id integer [ref: > sections.id]
  is_regular bool [default: false]
  is_deleted bool [default: false]
  deleted_at timestamp [null]
  created_at timestamp
  updated_at timestamp
}

Table admins {
  id integer [primary key]
  firstname varchar [null]
  lastname varchar [null]
  user_id varchar [unique, ref: - users.id]
  created_at timestamp
  updated_at timestamp
}

Table sections {
  id integer [primary key]
  name varchar [unique]
  course_id integer [ref: > courses.id]
  created_at timestamp
  updated_at timestamp
}

Table courses {
  id integer [primary key]
  name varchar [unique]
  created_at timestamp
  updated_at timestamp
}

Table subjects {
  id integer [primary key]
  name varchar [unique]
  code varchar [unique]
  created_at timestamp
  updated_at timestamp
}

Table schedules {
  id integer [primary key]
  subject_id integer [ref: > subjects.id]
  section_id integer [ref: > sections.id]
  classroom_id integer [ref: > classrooms.id]
  instructor_id integer [ref: > instructors.id]
  day_of_week varchar
  time_in time
  time_out time
  created_at timestamp
  updated_at timestamp
}

Table classrooms {
  id integer [primary key]
  name varchar [unique]
  created_at timestamp
  updated_at timestamp
}

Table sessions {
  id integer [primary key]
  schedule_id integer [ref: > schedules.id]
  status varchar [default: 'not_started', note: 'Values: not_started, active, ended, cancelled']
  session_date date
  actual_start_time timestamp [null]
  actual_end_time timestamp [null]
  attendance_cut_off timestamp [null]
  description varchar [null]
  actual_room_id integer [null, ref: > classrooms.id]
  started_by integer [null, ref: > instructors.id]
  ended_by integer [null, ref: > instructors.id]
  created_at timestamp
  updated_at timestamp
}

Table attendance_records {
  id integer [primary key]
  student_id integer [ref: > students.id]
  session_id integer [ref: > sessions.id]
  qr_code_id integer [null, ref: > qr_codes.id]
  check_in_time timestamp
  status varchar [default: 'Present', note: 'Values: Present, Late, Excused, Absent']
  notes varchar [null]
  is_manual_entry bool [default: false]
  entered_by varchar [null]
  created_at timestamp
  updated_at timestamp
  
  indexes {
    (student_id, session_id) [unique, name: 'IX_AttendanceRecords_StudentId_SessionId']
  }
}

Table qr_codes {
  id integer [primary key]
  session_id integer [ref: > sessions.id]
  qr_hash varchar [unique]
  generated_at timestamp
  expires_at timestamp
  max_usage integer [null]
  is_active bool [default: true]
  usage_count integer [default: 0]
  revoked_at timestamp [null]
  revoked_by varchar [null]
  revocation_reason varchar [null]
  created_at timestamp
  updated_at timestamp
}

Table refresh_tokens {
  id integer [primary key]
  user_id varchar [ref: > users.id]
  token_hash varchar [unique]
  expires_at timestamp
  created_at timestamp
  is_revoked bool [default: false]
  revoked_at timestamp [null]
  replaced_by_token_hash varchar [null]
  
  indexes {
    (user_id, is_revoked, expires_at) [name: 'IX_RefreshTokens_UserId_IsRevoked_ExpiresAt']
  }
}

Table blacklisted_tokens {
  id integer [primary key]
  jti varchar [unique]
  blacklisted_at timestamp
  expires_at timestamp
}

Table student_enrollments {
  id integer [primary key]
  student_id integer [ref: > students.id]
  section_id integer [ref: > sections.id]
  subject_id integer [ref: > subjects.id]
  is_active bool [default: true]
  enrollment_type varchar [default: 'Regular']
  academic_year varchar [null]
  semester varchar [null]
  enrolled_at timestamp
  dropped_at timestamp [null]
  created_at timestamp
  updated_at timestamp
  
  indexes {
    (student_id, section_id, subject_id) [unique, name: 'IX_StudentEnrollments_StudentId_SectionId_SubjectId']
  }
}

Table scan_log {
  id integer [primary key]
  student_id integer [ref: > students.id]
  session_id integer [ref: > sessions.id]
  qr_id integer [ref: > qr_codes.id]
  scan_timestamp timestamp
  status varchar
  created_at timestamp
}

Table notifications {
  id integer [primary key]
  sender varchar [ref: > users.id]
  body varchar
  read_status bool [default: false]
  created_at timestamp
  updated_at timestamp
}

