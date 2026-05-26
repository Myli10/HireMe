using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;
using System.Text.Json;

namespace FirebaseWorkout.ViewModels
{
    // ===================================================
    // AddJobViewModel — לוגיקת מסך פרסום משרה חדשה
    // ===================================================
    // מאפשר לכל משתמש עם מספר טלפון לפרסם משרה.
    // תהליך הפרסום:
    //   1. בדיקת תקינות (Validate)
    //   2. קידוד גיאוגרפי של הכתובת (OpenStreetMap/Nominatim)
    //   3. שמירה ב-Firebase
    // ===================================================
    public partial class AddJobViewModel : ObservableObject
    {
        private readonly IWorkPlaceRepository _workPlaceRepository;
        private readonly IAlertService _alertService;
        private readonly IAppLogger _appLogger;

        // ————— שדות הטופס —————
        [ObservableProperty] private string _jobName = string.Empty;       // שם המשרה
        [ObservableProperty] private string _category = string.Empty;      // קטגוריה נבחרת
        [ObservableProperty] private string _customCategory = string.Empty; // קטגוריה מותאמת (כשבוחרים "אחר")
        [ObservableProperty] private bool _showCustomCategory;             // האם להציג שדה קטגוריה חופשית

        // נקרא אוטומטית כש-Category משתנה.
        // אם בחרו "אחר" — מציג שדה קלט לקטגוריה מותאמת.
        partial void OnCategoryChanged(string value)
        {
            ShowCustomCategory = value == "אחר";
            if (!ShowCustomCategory) CustomCategory = string.Empty; // ניקוי אם לא "אחר"
        }

        [ObservableProperty] private string _description = string.Empty;   // תיאור המשרה (מינימום 20 תווים)
        [ObservableProperty] private string _address = string.Empty;       // רחוב ומספר
        [ObservableProperty] private string _city = string.Empty;          // עיר (נבחרת מרשימה)
        [ObservableProperty] private string _managerPhone = string.Empty;  // טלפון מנהל (חייב להתחיל ב-05)
        [ObservableProperty] private string _salaryText = string.Empty;    // שכר שעתי (טקסט, יומר למספר)
        [ObservableProperty] private string _shiftHours = string.Empty;    // שעות המשמרת (לדוגמה: "09:00-17:00")
        [ObservableProperty] private string _openingHours = string.Empty;  // שעות פתיחה
        [ObservableProperty] private bool _isBusy;                         // האם בתהליך שמירה
        [ObservableProperty] private string _locationStatus = "No location set"; // סטטוס קידוד גיאוגרפי

        // קואורדינטות GPS — מחושבות מהכתובת או מהמיקום הנוכחי
        private double _latitude;
        private double _longitude;

        // רשימת קטגוריות קבועות לבחירה ב-Picker
        public List<string> Categories { get; } = new()
        {
            "מזון ומסעדות", "קמעונאות", "בתי קפה", "סופרמרקט",
            "טכנולוגיה", "משלוחים", "ניקיון", "אבטחה", "אחר"
        };

        // רשימת ערים ישראליות למיון אלפביתי לבחירה ב-Picker
        public List<string> IsraeliCities { get; } = new List<string>
        {
            // ערים גדולות
            "תל אביב-יפו", "ירושלים", "חיפה", "ראשון לציון", "פתח תקווה",
            "אשדוד", "נתניה", "בני ברק", "באר שבע", "חולון",
            "רמת גן", "רחובות", "בת ים", "אשקלון", "בית שמש",
            "מודיעין", "נצרת", "מודיעין עילית", "רמת השרון", "כפר סבא",
            "הרצליה", "חדרה", "נס ציונה", "לוד", "רעננה",
            "אור יהודה", "אילת", "קרית גת", "רמלה", "גבעתיים",
            "ראש העין", "יבנה", "עכו", "קרית אתא", "עפולה",
            "טבריה", "קרית ביאליק", "קרית מוצקין", "קרית אונו", "נהריה",
            "קרית שמונה", "נתיבות", "אופקים", "ערד", "דימונה",
            "יהוד-מונוסון", "צפת", "שדרות", "בית שאן", "אור עקיבא",
            "מגדל העמק", "מעלה אדומים", "ביתר עילית", "אריאל", "אלעד",
            "טירת כרמל", "גדרה", "הוד השרון", "יוקנעם", "קרית מלאכי",
            "רהט", "טירה", "טייבה", "באקה-ג'ת", "אום אל-פחם",
            "תמרה", "שפרעם", "כפר יונה", "פרדס חנה-כרכור", "זכרון יעקב",
            // ערים ועיירות נוספות
            "מבשרת ציון", "גבעת זאב", "נשר", "רכסים", "דלית אל-כרמל",
            "עספיא", "נוף הגליל", "כפר תבור", "בנימינה", "קיסריה",
            "אבן יהודה", "כוכב יאיר", "צור יגאל", "גן יבנה", "קרית עקרון",
            "עומר", "מיתר", "שגב שלום", "תל שבע", "חורה",
            "לקיה", "כסיפה", "אבו בסמה", "שקיב אל-סלאם", "ערערה",
            "בקה אל-גרביה", "ג'לג'וליה", "קלנסווה", "טייבה", "כפר קאסם",
            "כפר ברא", "נילי", "גבעת שמואל", "שוהם", "גני תקווה",
            "עזור", "בת חפר", "צור משה", "נירית", "כפר נטר",
            "בית דגן", "נס הרים", "צור הדסה", "מכבים-רעות", "אבו גוש",
            // מושבים ויישובים
            "מושב בית יצחק", "מושב כפר ויתקין", "מושב הבונים", "מושב ריש̀פון",
            "מושב כפר שמריהו", "מושב סביון", "מושב מזור", "מושב צור יצחק",
            "כפר טרומן", "מושב גדרה", "מושב תלמי יוסף", "מושב פטיש",
            "קיבוץ עין גדי", "קיבוץ שדה בוקר", "קיבוץ רביבים",
            "קיבוץ יטבתה", "מושב פארן", "מושב קדש ברנע",
            // צפון
            "כפר גלעדי", "קיבוץ דן", "מטולה", "ראש פינה", "חצור הגלילית",
            "כרמיאל", "כפר מנדא", "ערבה", "דיר אל-אסד", "נחף",
            "טורעאן", "כפר כנא", "יפיע", "איכסאל",
            // דרום
            "מצפה רמון", "ירוחם", "להבים", "תפרח",
            "קיבוץ בארי", "קיבוץ ניר עם", "קיבוץ שעד", "קיבוץ אלומים",
            "קיבוץ דביר", "קיבוץ להב", "קיבוץ משאבי שדה"
        }.OrderBy(c => c).ToList(); // מיון אלפביתי של כל הערים

        public AddJobViewModel(IWorkPlaceRepository workPlaceRepository, IAlertService alertService, IAppLogger appLogger)
        {
            _workPlaceRepository = workPlaceRepository;
            _alertService = alertService;
            _appLogger = appLogger;
        }

        // שליחת המשרה ל-Firebase.
        // תהליך: בדיקת תקינות → קידוד גיאוגרפי → שמירה ב-Firebase
        [RelayCommand]
        private async Task SubmitJob()
        {
            var currentUser = (App.Current as App)!.CurrentUser!;

            // חובה שיהיה מספר טלפון בפרופיל לפני פרסום משרה
            if (string.IsNullOrWhiteSpace(currentUser.UserMobile))
            {
                await _alertService.ShowAlertAsync(
                    "Phone Number Required",
                    "You must add a verified phone number to your account before posting a job. Go to Account Settings to add it.",
                    "OK");
                return;
            }

            // בדיקת תקינות השדות — Validate מחזיר null אם הכל תקין, אחרת הודעת שגיאה
            var error = Validate();
            if (error != null)
            {
                await _alertService.ShowAlertAsync("Invalid Input", error, "OK");
                return;
            }

            double.TryParse(SalaryText, out double salary);

            IsBusy = true;
            try
            {
                // בניית כתובת מלאה: רחוב + עיר
                var fullAddress = string.IsNullOrWhiteSpace(City)
                    ? Address.Trim()
                    : $"{Address.Trim()}, {City.Trim()}";

                // קידוד גיאוגרפי — המרת כתובת לקואורדינטות GPS (דרך OpenStreetMap)
                // נדרש לתצוגה במפה ולסינון לפי מיקום
                if (_latitude == 0 && _longitude == 0)
                {
                    LocationStatus = "מאמת כתובת...";
                    var (lat, lng) = await GeocodeAddressAsync($"{fullAddress}, Israel");
                    _latitude = lat;
                    _longitude = lng;

                    // אם הכתובת לא נמצאה — עצור ובקש מהמשתמש לתקן
                    if (_latitude == 0 && _longitude == 0)
                    {
                        LocationStatus = "כתובת לא נמצאה";
                        IsBusy = false;
                        await _alertService.ShowAlertAsync(
                            "כתובת לא קיימת",
                            $"הכתובת '{fullAddress}' לא נמצאה במפה.\nוודא שהרחוב קיים בעיר שבחרת ונסה שנית.",
                            "OK");
                        return;
                    }

                    LocationStatus = "📍 נמצא במפה";
                }

                // קביעת הקטגוריה הסופית:
                // אם בחרו "אחר" ומלאו CustomCategory — השתמש בה
                // אחרת השתמש בקטגוריה שנבחרה
                var finalCategory = Category == "אחר" && !string.IsNullOrWhiteSpace(CustomCategory)
                    ? CustomCategory.Trim()
                    : (string.IsNullOrWhiteSpace(Category) ? "אחר" : Category.Trim());

                // יצירת אובייקט המשרה לשמירה ב-Firebase
                var workPlace = new WorkPlace
                {
                    Name = JobName.Trim(),
                    Category = finalCategory,
                    Description = Description.Trim(),
                    Address = fullAddress,
                    ManagerPhone = ManagerPhone.Trim(),
                    SalaryPerHour = salary,
                    ShiftHours = ShiftHours.Trim(),
                    OpeningHours = OpeningHours.Trim(),
                    WorkerRating = 0,                                                              // דירוג התחלתי
                    CreatedByUserId = currentUser.Id,                                              // מזהה בעל המשרה
                    CreatedByUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim(), // שם בעל המשרה
                    Latitude = _latitude,
                    Longitude = _longitude
                };

                await _workPlaceRepository.AddWorkPlaceAsync(workPlace);
                await _alertService.ShowAlertAsync("Success", $"'{JobName}' was posted successfully!", "OK");
                await Shell.Current.GoToAsync(".."); // חזרה אחרי פרסום
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"SubmitJob failed: {ex.Message}");
                await _alertService.ShowAlertAsync("Error", "Could not post job. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // שימוש במיקום GPS הנוכחי של המכשיר כמיקום המשרה.
        // מבקש הרשאת מיקום מהמשתמש.
        [RelayCommand]
        private async Task UseMyLocation()
        {
            try
            {
                // בקשת הרשאת מיקום מהמשתמש
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    LocationStatus = "Location permission denied";
                    return;
                }

                LocationStatus = "Getting location...";

                // מנסה לקבל מיקום ידוע אחרון (מהיר) — אם אין, שואל ב-GPS (לוקח עד 6 שניות)
                var loc = await Geolocation.GetLastKnownLocationAsync()
                    ?? await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(6)));

                if (loc != null)
                {
                    _latitude = loc.Latitude;
                    _longitude = loc.Longitude;
                    LocationStatus = $"📍 {_latitude:F4}, {_longitude:F4}"; // הצגת קואורדינטות
                }
                else
                {
                    LocationStatus = "Could not get location";
                }
            }
            catch
            {
                LocationStatus = "Location unavailable";
            }
        }

        // בדיקת תקינות כל שדות הטופס לפני שמירה.
        // מחזירה null אם הכל תקין, אחרת מחרוזת הודעת שגיאה.
        private string? Validate()
        {
            if (string.IsNullOrWhiteSpace(JobName) || JobName.Trim().Length < 2)
                return "Job name must be at least 2 characters.";

            if (string.IsNullOrWhiteSpace(Description) || Description.Trim().Length < 20)
                return "Description must be at least 20 characters. Describe the work, atmosphere and tips.";

            if (string.IsNullOrWhiteSpace(Address) || Address.Trim().Length < 5)
                return "Please enter a full address (street and city).";

            if (!double.TryParse(SalaryText, out double salary) || string.IsNullOrWhiteSpace(SalaryText))
                return "Please enter a valid salary per hour.";

            if (salary == 0)
                return "Salary cannot be 0 ₪. Please enter the actual hourly wage.";

            // שכר מינימום בישראל — 32 ₪ לשעה (נכון לנתוני הפרויקט)
            if (salary < 32)
                return "Salary cannot be below the minimum wage (32 ₪/hour).";

            if (string.IsNullOrWhiteSpace(ShiftHours))
                return "Please fill in shift hours (e.g. 09:00 - 17:00).";

            // בדיקת פורמט טלפון — 10 ספרות מתחילות ב-05
            if (!string.IsNullOrWhiteSpace(ManagerPhone))
            {
                var phone = ManagerPhone.Replace("-", "").Replace(" ", "");
                if (phone.Length != 10 || !phone.StartsWith("05"))
                    return "Phone number must be 10 digits and start with 05.";
            }

            return null; // הכל תקין
        }

        // ביטול — חזרה למסך הקודם
        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }

        // קידוד גיאוגרפי — המרת כתובת טקסטואלית לקואורדינטות GPS.
        // משתמש ב-OpenStreetMap Nominatim API (חינמי, ללא מפתח API).
        // מחזיר (0, 0) אם הכתובת לא נמצאה.
        private static async Task<(double lat, double lng)> GeocodeAddressAsync(string address)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "HireMeApp/1.0"); // Nominatim דורש User-Agent

                var encoded = Uri.EscapeDataString(address); // קידוד הכתובת לURL תקני
                var json = await client.GetStringAsync(
                    $"https://nominatim.openstreetmap.org/search?q={encoded}&format=json&limit=1");

                using var doc = JsonDocument.Parse(json);
                var arr = doc.RootElement;
                if (arr.GetArrayLength() > 0)
                {
                    var first = arr[0]; // תוצאה ראשונה (הרלוונטית ביותר)
                    double lat = double.Parse(first.GetProperty("lat").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                    double lng = double.Parse(first.GetProperty("lon").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                    return (lat, lng);
                }
            }
            catch { }
            return (0, 0); // כתובת לא נמצאה
        }
    }
}
