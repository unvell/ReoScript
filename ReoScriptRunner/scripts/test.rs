


function get_system_path(key) {
  get_env('path=' + key); // throw exception that get_env is not defined
}

function get_current_instance() {
  get_system_path('startup-path');
}

function get_login_user() {
  get_system_path();
}

get_login_user();