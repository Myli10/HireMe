using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseWorkout.Service
{
	public class AlertService : IAlertService
	{
		public Task ShowAlertAsync(string title, string message, string cancel)
		{
			return Shell.Current.DisplayAlert(title, message, cancel);
		}
	}
}
