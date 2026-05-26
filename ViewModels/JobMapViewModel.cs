using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

namespace FirebaseWorkout.ViewModels
{
    // ===================================================
    // JobMapViewModel — לוגיקת מסך המפה
    // ===================================================
    // מציג מפה אינטראקטיבית עם כל המשרות כנעצים.
    // טכנולוגיה: WebView + Leaflet.js + OpenStreetMap (ללא עלות).
    //
    // זרימת העבודה:
    //   1. מקבל מיקום GPS של המשתמש
    //   2. טוען את כל המשרות מ-Firebase
    //   3. ממיר כתובות חסרות קואורדינטות דרך OpenStreetMap API
    //   4. בונה HTML מלא עם JavaScript ומחדיר אותו ל-WebView
    //
    // תקשורת WebView ← MAUI:
    //   לחיצה על "View Details" → URL: "hireme://workplace/{id}"
    //   הקוד ב-JobMapView.xaml.cs מיירט את ה-URL ומנווט ל-JobDetailsView
    // ===================================================
    public partial class JobMapViewModel : ObservableObject
    {
        private readonly IWorkPlaceRepository _workPlaceRepository;
        private readonly IAppLogger _appLogger;

        // האם בתהליך טעינה — מציג Spinner
        [ObservableProperty] private bool _isBusy;

        // קוד ה-HTML המלא שמוזן ל-WebView — כולל JavaScript עם כל הנעצים
        [ObservableProperty] private string _mapHtml = string.Empty;

        // מיקום ישראל כברירת מחדל (תל אביב) — אם GPS לא זמין
        private double _userLat = 32.0853;
        private double _userLng = 34.7818;

        // כל המשרות שנטענו — נשמרות לחיפוש לפי ID בלחיצה על נעץ
        private List<WorkPlace> _workPlaces = new();

        public JobMapViewModel(IWorkPlaceRepository workPlaceRepository, IAppLogger appLogger)
        {
            _workPlaceRepository = workPlaceRepository;
            _appLogger = appLogger;
        }

        // נקרא כשהמסך מופיע — טוען ומציג את המפה
        internal async void OnAppearing()
        {
            await LoadMapAsync();
        }

        // טוען מיקום המשתמש, משרות מ-Firebase, ומייצר HTML של המפה
        private async Task LoadMapAsync()
        {
            IsBusy = true;
            try
            {
                // שלב 1: קבל מיקום GPS של המשתמש (לסמן כחול במפה)
                await TryGetUserLocationAsync();

                // שלב 2: טען כל המשרות מ-Firebase
                _workPlaces = await _workPlaceRepository.GetAllWorkPlacesAsync();

                // שלב 3: ממיר כתובות למשרות שנשמרו ללא קואורדינטות
                // (משרות שנוצרו לפני שהוספנו את שדה Latitude/Longitude)
                foreach (var wp in _workPlaces.Where(w => w.Latitude == 0 && w.Longitude == 0 && !string.IsNullOrWhiteSpace(w.Address)))
                {
                    var query = wp.Address!.Contains("Israel", StringComparison.OrdinalIgnoreCase)
                        ? wp.Address!
                        : $"{wp.Address}, Israel"; // הוסף "Israel" לחיפוש מדויק יותר
                    var (lat, lng) = await GeocodeAddressAsync(query);
                    wp.Latitude = lat;
                    wp.Longitude = lng;
                }

                // שלב 4: בנה HTML מלא ושלח ל-WebView
                MapHtml = GenerateMapHtml();
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"LoadMapAsync failed: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // קידוד גיאוגרפי — המרת כתובת לקואורדינטות דרך OpenStreetMap Nominatim.
        // זהה לפונקציה ב-AddJobViewModel — שתיהן ללא מפתח API.
        private static async Task<(double lat, double lng)> GeocodeAddressAsync(string address)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "HireMeApp/1.0");
                var encoded = Uri.EscapeDataString(address);
                var json = await client.GetStringAsync(
                    $"https://nominatim.openstreetmap.org/search?q={encoded}&format=json&limit=1");

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.GetArrayLength() > 0)
                {
                    var first = doc.RootElement[0];
                    double lat = double.Parse(first.GetProperty("lat").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                    double lng = double.Parse(first.GetProperty("lon").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                    return (lat, lng);
                }
            }
            catch { }
            return (0, 0); // כתובת לא נמצאה
        }

        // ניסיון לקבל מיקום GPS של המשתמש.
        // אם נכשל (הרשאה נדחתה / GPS לא זמין) — ממשיך עם ברירת מחדל (תל אביב).
        private async Task TryGetUserLocationAsync()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted) return;

                // ניסיון ראשון: מיקום ידוע אחרון (מהיר)
                // ניסיון שני: GPS עדכני (עד 5 שניות)
                var location = await Geolocation.GetLastKnownLocationAsync()
                    ?? await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium,
                        TimeSpan.FromSeconds(5)));

                if (location != null)
                {
                    _userLat = location.Latitude;
                    _userLng = location.Longitude;
                }
            }
            catch
            {
                // ממשיכים עם ברירת המחדל (תל אביב) אם GPS לא זמין
            }
        }

        // מחזיר WorkPlace לפי ID — נקרא כשלוחצים על נעץ במפה
        public WorkPlace? GetWorkPlaceById(string id)
            => _workPlaces.FirstOrDefault(w => w.Id == id);

        // בונה HTML מלא עם Leaflet.js לתצוגה ב-WebView.
        // StringBuilder משמש לבניית מחרוזת ארוכה ביעילות (לא + על מחרוזות).
        private string GenerateMapHtml()
        {
            var sb = new StringBuilder();

            // ראש ה-HTML: קישורי CSS ו-JS של Leaflet מ-CDN
            sb.AppendLine("<!DOCTYPE html><html><head>");
            sb.AppendLine("<meta charset='utf-8'/>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'/>");
            sb.AppendLine("<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>");
            sb.AppendLine("<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>");
            sb.AppendLine("<style>html,body,#map{height:100%;margin:0;padding:0;}</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine("<div id='map'></div>");
            sb.AppendLine("<script>");

            // אתחול המפה — ממורכזת על מיקום המשתמש, zoom 13
            sb.AppendLine($"var map = L.map('map').setView([{_userLat},{_userLng}], 13);");
            sb.AppendLine("L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png',{attribution:'© OpenStreetMap'}).addTo(map);");

            // סמן כחול = מיקום המשתמש (circleMarker = עיגול, לא נעץ)
            sb.AppendLine($"L.circleMarker([{_userLat},{_userLng}],{{radius:12,color:'#1565C0',fillColor:'#42A5F5',fillOpacity:0.9,weight:3}})" +
                          $".addTo(map).bindPopup('<b>📍 You are here</b>').openPopup();");

            // הגדרת אייקון נעץ אדום לכל המשרות
            sb.AppendLine("var redIcon = L.icon({iconUrl:'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-red.png',shadowUrl:'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',iconSize:[25,41],iconAnchor:[12,41],popupAnchor:[1,-34],shadowSize:[41,41]});");

            // הוספת נעץ אדום לכל משרה שיש לה קואורדינטות
            foreach (var wp in _workPlaces.Where(w => w.Latitude != 0 && w.Longitude != 0))
            {
                // ניקוי תווים מיוחדים שעלולים לשבור את ה-JavaScript
                var name = wp.Name?.Replace("'", "\\'").Replace("\"", "&quot;") ?? "";
                var salary = wp.SalaryPerHour;
                var id = wp.Id;

                // כל לחיצה על "View Details" → ניווט לURL מיוחד שיירוט ע"י MAUI
                // "hireme://workplace/{id}" → JobMapView.xaml.cs → GoToAsync("JobDetailsView")
                sb.AppendLine($"L.marker([{wp.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{wp.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}],{{icon:redIcon}})" +
                              $".addTo(map)" +
                              $".bindPopup('<b>{name}</b><br/>₪{salary}/hr<br/><a href=\"hireme://workplace/{id}\">View Details</a>');");
            }

            sb.AppendLine("</script></body></html>");
            return sb.ToString();
        }
    }
}
