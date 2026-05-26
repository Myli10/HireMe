using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseWorkout.Service
{
	public class LogService : IAppLogger
	{
		private readonly string TAG = "KASATA";

		public void LogDebug(string message)
		{
#if ANDROID
			Android.Util.Log.Debug(TAG,message);
#endif
			//Debug.WriteLine($"Log {TAG}: {message}");

		}

		public void LogError(string message)
		{
#if ANDROID
			Android.Util.Log.Error(TAG,message);
#endif
			//Debug.WriteLine($"Error {TAG}: {message}");
		}
	}
}
