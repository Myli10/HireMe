using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseWorkout.Model
{
    // ===================================================
    // AppUser — מודל המשתמש באפליקציה
    // ===================================================
    // מחלקה זו מייצגת משתמש רשום במערכת.
    // הנתונים שלה נשמרים ב-Firebase Realtime Database
    // תחת הנתיב: users/{userId}/
    // ===================================================
    public class AppUser
    {
        // מזהה ייחודי של המשתמש — נוצר אוטומטית ע"י Firebase
        public string Id { get; set; } = string.Empty;

        // שם פרטי של המשתמש
        public string? FirstName { get; set; }

        // שם משפחה של המשתמש
        public string? LastName { get; set; }

        // כתובת האימייל — משמשת גם להתחברות ב-Firebase Auth
        public string? UserEmail { get; set; }

        // סיסמה — נשמרת בצורה מוצפנת ב-Firebase Auth, לא ב-Database
        public string? UserPassword { get; set; }

        // מספר טלפון נייד — חובה לפרסום משרה
        public string? UserMobile { get; set; }

        // תאריך לידה (UBDate = User Birth Date)
        public string? UBDate { get; set; }

        // תאריך הרשמה לאפליקציה
        public string? RegDate { get; set; }

        // האם המשתמש הוא מנהל מערכת — מאפשר גישה ל-AdminView
        public bool IsAdmin { get; set; } = false;

        // כתובת URL לתמונת פרופיל (שמירה ב-Firebase Storage) — שדה ישן, לא בשימוש פעיל
        public string? ProfileImageUrl { get; set; }

        // תמונת פרופיל מקודדת ב-Base64 (מחרוזת טקסט של התמונה).
        // נשמרת ישירות ב-Firebase Database — לא דורשת Firebase Storage.
        // גודל מקסימלי: 1.5MB לפי הגדרות AccountViewModel.
        public string? ProfileImageBase64 { get; set; }
    }
}
