Table users {
  id varchar [primary key]
  username varchar
  email varchar
  role varchar
  created_at timestamp
}

Table instructors {
  id integer [primary key]
  firstname varchar
  lastname varchar
  user_id varchar [ref: - users.id]
  created_at timestamp
  updated_at timestamp
}

Table students {
  id integer [primary key]
  user_id varchar [ref: - users.id]
  firstname varchar
  lastname varchar
  created_at timestamp
  updated_at timestamp
}

Table admins {
  id integer [primary key]
  user_id varchar [ref: - users.id]
  firstname varchar
  lastname varchar
  created_at timestamp
  updated_at timestamp
}

Table sections {
  id integer [primary key]
  name varchar
  course_id integer [ref: - courses.id]
  created_at timestamp
  updated_at timestamp
}

Table courses {
  id integer [primary key]
  name varchar
  code varchar
  description varchar
  created_at timestamp
  updated_at timestamp
}

Table subjects {
  id integer [primary key]
  name varchar
  code varchar
  created_at timestamp
  updated_at timestamp
}

Table schedules {
  id integer [primary key]
  subject_id integer [ref: - subjects.id]
  section_id integer [ref: - sections.id]
  classroom_id integer [ref: - classrooms.id]
  instructor_id integer [ref: - instructors.id]
  day_of_week varchar
  start_time time
  end_time time
  created_at timestamp
  updated_at timestamp
}

Table classrooms {
  id integer [primary key]
  name varchar
  building varchar [null]
  capacity integer [null]
  created_at timestamp
  updated_at timestamp
}

Table sessions {
  id integer [primary key]
  schedule_id integer [ref: - schedules.id]
  status varchar [note: 'Values: not_started, active, ended, cancelled']
  session_date date
  actual_start_time timestamp [null]
  actual_end_time timestamp [null]
  attendance_cut_off timestamp [null]
  description varchar [null]
  actual_room_id integer [null, ref: - classrooms.id]
  started_by integer [null, ref: - instructors.id]
  ended_by integer [null, ref: - instructors.id]
  created_at timestamp
  updated_at timestamp
}

Table attendance {
  id integer [primary key]
  student_id integer [ref: - students.id]
  session_id integer [ref: - sessions.id]
  qr_code_id integer [null, ref: - qr.id]
  check_in_time timestamp
  status varchar [note: 'Values: Present, Late, Excused, Absent']
  notes varchar [null]
  is_manual_entry bool [default: false]
  entered_by varchar [null]
  created_at timestamp
  updated_at timestamp
}

Table qr {
  id integer [primary key]
  session_id integer [ref: - sessions.id]
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

Table scan_log {
  id integer [primary key]
  student_id integer [ref: - students.id]
  session_id integer [ref: - sessions.id]
  qr_id integer [ref: - qr.id]
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

