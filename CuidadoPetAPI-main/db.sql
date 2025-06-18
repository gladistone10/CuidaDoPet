CREATE DATABASE pet_management;
USE pet_management;

CREATE TABLE users (
    user_id INT AUTO_INCREMENT PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,
    name VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE pets (
    pet_id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    breed VARCHAR(255),
    birthdate DATE,
    gender ENUM('M', 'F') NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE pet_permissions (
    permission_id INT AUTO_INCREMENT PRIMARY KEY,
    pet_id INT,
    user_id INT,
    permission_type ENUM('view', 'edit') NOT NULL,
    granted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (pet_id) REFERENCES pets(pet_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE TABLE notifications (
    notification_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT,
    title VARCHAR(255),
    message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    userread BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE TABLE appointments (
    appointment_id INT AUTO_INCREMENT PRIMARY KEY,
    pet_id INT,
    user_id INT,
    appointment_type ENUM('vet', 'grooming', 'bath', 'other') NOT NULL,
    appointment_date DATETIME NOT NULL,
    status ENUM('scheduled', 'completed', 'cancelled') DEFAULT 'scheduled',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (pet_id) REFERENCES pets(pet_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE TABLE care (
    care_id INT AUTO_INCREMENT PRIMARY KEY,
    pet_id INT,
    user_id INT,
    care_type ENUM('feeding', 'exercise', 'treatment', 'other') NOT NULL,
    care_description TEXT,
    care_date DATETIME NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (pet_id) REFERENCES pets(pet_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE TABLE pet_vaccines (
    vaccine_id INT AUTO_INCREMENT PRIMARY KEY,
    pet_id INT,
    vaccine_name VARCHAR(255) NOT NULL,
    vaccine_date DATE NOT NULL,
    expiration_date DATE,
    FOREIGN KEY (pet_id) REFERENCES pets(pet_id) ON DELETE CASCADE
);

CREATE TABLE pet_health (
    health_id INT AUTO_INCREMENT PRIMARY KEY,
    pet_id INT,
    health_condition VARCHAR(255),
    diagnosis_date DATE,
    treatment_description TEXT,
    treatment_start_date DATE,
    treatment_end_date DATE,
    FOREIGN KEY (pet_id) REFERENCES pets(pet_id) ON DELETE CASCADE
);

CREATE TABLE pet_exercise (
    exercise_id INT AUTO_INCREMENT PRIMARY KEY,
    pet_id INT,
    exercise_type VARCHAR(255),
    exercise_duration INT,
    exercise_date DATETIME,
    FOREIGN KEY (pet_id) REFERENCES pets(pet_id) ON DELETE CASCADE
);

CREATE TABLE pet_feeding (
    feeding_id INT AUTO_INCREMENT PRIMARY KEY,
    pet_id INT,
    food_type VARCHAR(255),
    quantity INT,
    feeding_time DATETIME,
    FOREIGN KEY (pet_id) REFERENCES pets(pet_id) ON DELETE CASCADE
);

CREATE TABLE pet_medications (
    medication_id INT AUTO_INCREMENT PRIMARY KEY,
    pet_id INT,
    medication_name VARCHAR(255),
    medication_dosage VARCHAR(255),
    prescribed_by VARCHAR(255),
    prescription_date DATE,
    FOREIGN KEY (pet_id) REFERENCES pets(pet_id) ON DELETE CASCADE
);

CREATE TABLE pet_points (
    pet_id INT PRIMARY KEY,
    points INT NOT NULL DEFAULT 0,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE pet_badges (
    pet_id INT,
    badge_name VARCHAR(100),
    earned_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (pet_id, badge_name)
);
