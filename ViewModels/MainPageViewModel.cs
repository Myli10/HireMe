using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Database.Streaming;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;
using FirebaseWorkout.Service.DBService.Firebase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseWorkout.ViewModels
{
    // מסך הבית — מציג ברכה אישית ושני כפתורי ניווט
    public partial class MainPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name;

        public MainPageViewModel()
        {
            _name = "Hello " + (App.Current as App)!.CurrentUser!.FirstName!;
        }

        [RelayCommand]
        private async Task Settings()
        {
            await Shell.Current.GoToAsync("AccountView");
        }

        [RelayCommand]
        private async Task GoToFindJob()
        {
            await Shell.Current.GoToAsync("FindJobView");
        }
    }
}
