using FirebaseWorkout.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseWorkout.Service.DBService
{
	public interface IAppUserRepository
	{
		Task<string> CreateAsync(AppUser appUser);
		Task UpdateAsync(AppUser appUser);
		Task DeleteAsync(AppUser appUser);
		Task<AppUser> SignInAsync(string userEmail, string userPassword);
		Task<AppUser> GetUserByIdAsync(string userId);
		List<AppUser> GetAllAsync();
		Task SetToAdmin(string userId);
	}
}
