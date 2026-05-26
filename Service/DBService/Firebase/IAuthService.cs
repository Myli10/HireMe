using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseWorkout.Service.DBService.Firebase
{
	public interface IAuthService
	{
		Task<string> SignIn(string usreEmail, string userPassword);
		Task<string> CreateAuth(string email, string password);
		Task RemoveAuth(string email, string password);
		Task SignOut();
		Task<string?> TryRestoreSessionAsync();
	}
}
