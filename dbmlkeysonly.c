Table users {
  id varchar [primary key]
  normalized_email varchar [unique]
}

Table instructors {
  id integer [primary key]
  user_id varchar [unique, ref: - users.id]
}

Table students {
  id integer [primary key]
  user_id varchar [unique, ref: - users.id]
  section_id integer [ref: > sections.id]
}

Table admins {
  id integer [primary key]
  user_id varchar [unique, ref: - users.id]
}

Table sections {
  id integer [primary key]
  course_id integer [ref: > courses.id]
}

Table courses {
  id integer [primary key]
}

Table subjects {
  id integer [primary key]
}

Table schedules {
  id integer [primary key]
  subject_id integer [ref: > subjects.id]
  section_id integer [ref: > sections.id]
  classroom_id integer [ref: > classrooms.id]
  instructor_id integer [ref: > instructors.id]
}

Table classrooms {
  id integer [primary key]
}

Table sessions {
  id integer [primary key]
  schedule_id integer [ref: > schedules.id]
  actual_room_id integer [null, ref: > classrooms.id]
  started_by integer [null, ref: > instructors.id]
  ended_by integer [null, ref: > instructors.id]
}

Table attendance_records {
  id integer [primary key]
  student_id integer [ref: > students.id]
  session_id integer [ref: > sessions.id]
  qr_code_id integer [null, ref: > qr_codes.id]
}

Table qr_codes {
  id integer [primary key]
  session_id integer [ref: > sessions.id]
}

Table refresh_tokens {
  id integer [primary key]
  user_id varchar [ref: > users.id]
  token_hash varchar [unique]
}

Table blacklisted_tokens {
  id integer [primary key]
  jti varchar [unique]
}

Table student_enrollments {
  id integer [primary key]
  student_id integer [ref: > students.id]
  section_id integer [ref: > sections.id]
  subject_id integer [ref: > subjects.id]
}

Table scan_log {
  id integer [primary key]
  student_id integer [ref: > students.id]
  session_id integer [ref: > sessions.id]
  qr_id integer [ref: > qr_codes.id]
}

Table notifications {
  id integer [primary key]
  sender varchar [ref: > users.id]
}


