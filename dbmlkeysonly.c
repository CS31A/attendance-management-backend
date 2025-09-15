Table users {
  id varchar [primary key]
}

Table instructors {
  id integer [primary key]
  user_id varchar [ref: - users.id]
}

Table students {
  id integer [primary key]
  user_id varchar [ref: - users.id]
}

Table admins {
  id integer [primary key]
  user_id varchar [ref: - users.id]
}

Table sections {
  id integer [primary key]
  instructor_id varchar [ref: - instructors.id]
  course_id integer [ref: - courses.id]
}

Table courses {
  id integer [primary key]
}

Table schedules {
  id integer [primary key]
  subject_id integer [null]
  section_id integer [ref: - sections.id]
  course_id integer [ref: - courses.id]
  classroom_id integer [ref: - classrooms.id]
  //session_id integer [ref: - sessions.id]
}

Table classrooms {
  id integer [primary key]
}

Table sessions {
  id integer [primary key]
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
}

Table qr {
  id integer [primary key]
  schedule_id integer [ref: -schedules.id]
  session_id integer [ref: -sessions.id]
}

Table scan_log {
  id integer [primary key]
  student_id integer [ref: -students.id]
  session_id integer [ref: -sessions.id]
  qr_id integer [ref: -qr.id]
}

Table notifications {
  id integer [primary key]
  sender integer [ref: > users.id]
}


