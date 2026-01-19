using UnityEngine;

namespace FunClass.Core
{
    /// <summary>
    /// Helper class to determine student location (inside/outside classroom)
    /// Used for location-based influence filtering
    /// </summary>
    public static class StudentLocationHelper
    {
        /// <summary>
        /// Check if student is outside classroom
        /// </summary>
        public static bool IsStudentOutsideClassroom(StudentAgent student)
        {
            if (student == null) return false;

            LevelConfig currentLevel = GetCurrentLevelConfig();
            if (currentLevel == null || currentLevel.classroomDoor == null)
            {
                return false;
            }

            Vector3 doorPosition = currentLevel.classroomDoor.position;
            Vector3 studentPosition = student.transform.position;
            float distanceFromDoor = Vector3.Distance(studentPosition, doorPosition);
            
            // Student is outside if they're past the door (z > door.z)
            bool isPastDoor = studentPosition.z > doorPosition.z;
            
            // Also check distance from seat
            float distanceFromSeat = Vector3.Distance(studentPosition, student.OriginalSeatPosition);
            bool isFarFromSeat = distanceFromSeat > 5f;
            
            bool isOutside = isPastDoor && isFarFromSeat;
            
            return isOutside;
        }

        /// <summary>
        /// Check if student is inside classroom
        /// </summary>
        public static bool IsStudentInsideClassroom(StudentAgent student)
        {
            return !IsStudentOutsideClassroom(student);
        }

        /// <summary>
        /// Check if two students are in the same location (both inside or both outside)
        /// </summary>
        public static bool AreInSameLocation(StudentAgent student1, StudentAgent student2)
        {
            if (student1 == null || student2 == null) return false;

            bool student1Outside = IsStudentOutsideClassroom(student1);
            bool student2Outside = IsStudentOutsideClassroom(student2);

            return student1Outside == student2Outside;
        }

        /// <summary>
        /// Get location string for logging
        /// </summary>
        public static string GetLocationString(StudentAgent student)
        {
            if (student == null) return "Unknown";
            return IsStudentOutsideClassroom(student) ? "Outside" : "Inside";
        }

        private static LevelConfig GetCurrentLevelConfig()
        {
            if (LevelManager.Instance == null) return null;
            return LevelManager.Instance.GetCurrentLevelConfig();
        }
    }
}
