namespace FirebaseWorkout.Model
{
    // ===================================================
    // WorkPlace — מודל מקום עבודה (משרה)
    // ===================================================
    // מחלקה זו מייצגת מקום עבודה שפורסם באפליקציה.
    // הנתונים שלה נשמרים ב-Firebase Realtime Database
    // תחת הנתיב: workplaces/{workPlaceId}/
    // ===================================================
    public class WorkPlace
    {
        // מזהה ייחודי של המשרה — נוצר אוטומטית ע"י Firebase
        public string Id { get; set; } = string.Empty;

        // שם מקום העבודה (לדוגמה: "מלצר בקפה שלנו")
        public string? Name { get; set; }

        // תיאור המשרה — מה העובד יעשה, אווירה, טיפים וכו'.
        // חייב להיות לפחות 20 תווים (Validate ב-AddJobViewModel)
        public string? Description { get; set; }

        // כתובת מלאה כולל עיר (לדוגמה: "רחוב הרצל 5, תל אביב")
        public string? Address { get; set; }

        // קטגוריה (לדוגמה: "מזון ומסעדות", "טכנולוגיה", "אחר")
        public string? Category { get; set; }

        // דירוג ממוצע של העובדים (0-5) — מחושב מסיכום הביקורות
        public double WorkerRating { get; set; }

        // טלפון המנהל — חייב להתחיל ב-05 ולהיות 10 ספרות
        public string? ManagerPhone { get; set; }

        // שכר לשעה בשקלים — לא יכול להיות פחות מ-32 ₪ (שכר מינימום)
        public double SalaryPerHour { get; set; }

        // שעות המשמרת (לדוגמה: "09:00 - 17:00")
        public string? ShiftHours { get; set; }

        // שעות פתיחה של המקום (לדוגמה: "א-ה 08:00-22:00")
        public string? OpeningHours { get; set; }

        // מזהה המשתמש שיצר את המשרה — לבדיקה אם המשתמש הוא הבעלים (IsCreator)
        public string? CreatedByUserId { get; set; }

        // שם המשתמש שיצר את המשרה — מוצג כ"פורסם על ידי:"
        public string? CreatedByUserName { get; set; }

        // קואורדינטות גיאוגרפיות לתצוגה במפה (Leaflet.js).
        // מחושבות בזמן יצירת המשרה דרך OpenStreetMap/Nominatim API.
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // האם המשרה מאוישת (מלאה) — מוצג כ"מאויש" בכרטיסיית המשרה.
        // ניתן לשינוי רק ע"י בעל המשרה דרך ToggleFilled
        public bool IsFilled { get; set; }

        // מספר הביקורות שנכתבו על המשרה — מוצג ליד הדירוג
        public int ReviewCount { get; set; }
    }
}
