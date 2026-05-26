using CommunityToolkit.Mvvm.ComponentModel;

namespace FirebaseWorkout.Model
{
    public partial class FavoriteWorkPlaceItem : ObservableObject
    {
        [ObservableProperty]
        private bool _isFavorite = true;

        public WorkPlace WorkPlace { get; set; } = new();
    }
}
