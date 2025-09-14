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
  email varchar
  user_id varchar [ref: - users.id]
}

Table students {
  id integer [primary key]
    user_id varchar [ref: - users.id]
  firstname varchar
  lastname varchar
  email varchar
}

Table admins {
  id integer [primary key]
  user_id varchar [ref: - users.id]
  firstname varchar
  lastname varchar
  email varchar
}

Table sections {
  id integer [primary key]
  name varchar
  instructor_id varchar [ref: - instructors.id]
  course_id integer [ref: - courses.id]
}

Table courses {
  id integer [primary key]
  name varchar
  code varchar
  description varchar
}

Table schedules {
  id integer [primary key]
  time_in timestamp
  time_out timestamp
  day_of_week varchar
  subject_id integer [null]
  section_id integer [ref: - sections.id]
  course_id integer [ref: - courses.id]
  classroom_id integer [ref: - classrooms.id]
  //session_id integer [ref: - sessions.id]
}

Table classrooms {
  id integer [primary key]
  name varchar
}

Table sessions {
  id integer [primary key]
  status bool 
  session_date timestamp
  actual_start_time timestamp
  actual_end_time timestamp
  attendance_cut_off timestamp
  description varchar
  actual_room_id integer [ref: - classrooms.id]
  started_by integer [ref: -instructors.id]
  ended_by integer [ref: -instructors.id]
  schedule_id integer [null]
}

Table attendance {
  id integer [primary key]
  schedule_id integer [ref: -schedules.id]
  student_id integer [ref: -students.id]
  session_id integer [ref: -sessions.id]
  status bool
  scan_timestamp timestamp
}

Table qr {
  id integer [primary key]
  schedule_id integer [ref: -schedules.id]
  session_id integer [ref: -sessions.id]
  qr_hash varchar
  generated_at timestamp
  expires_at timestamp
  is_active bool
}

Table scan_log {
  id integer [primary key]
  student_id integer [ref: -students.id]
  session_id integer [ref: -sessions.id]
  qr_id integer [ref: -qr.id]
  scan_timestamp timestamp
  status varchar
}

Table notifications {
  id integer [primary key]
  sender integer [ref: > users.id]
  body varchar
  read_status bool
}

