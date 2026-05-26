using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Auth.Repository;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using FirebaseWorkout.Model;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseWorkout.Service
{
	public class FirebaseRealtimeService2 
	{		
		private FirebaseAuthClient? _authClient;
		private FirebaseClient? _firebaseClient;
		private IAppLogger _appLogger;
		

		public FirebaseRealtimeService2(IAppLogger appLogger)
		{
			_appLogger = appLogger;

			// Initialize Firebase Authentication Client
			var config = new FirebaseAuthConfig()
			{
				ApiKey = "AIzaSyBfuUKAoRD0bMJvqgh8sxV5DuOAgSMjIp4",
				AuthDomain = "big17datafb.firebaseapp.com",
				Providers = new FirebaseAuthProvider[]
					{
						new EmailProvider()
					},
				//UserRepository = new FileUserRepository("AppCurrentUser")
			};
			_authClient = new FirebaseAuthClient(config);

			// Initialize Firebase Realtime Database Client
			//_firebaseClient = new FirebaseClient("https://big17datafb-default-rtdb.europe-west1.firebasedatabase.app/",
			//	new FirebaseOptions
			//	{
			//		AuthTokenAsyncFactory = () => Task.FromResult(_authClient.User.Credential.IdToken)
			//	});
			_firebaseClient = new FirebaseClient("https://big17datafb-default-rtdb.europe-west1.firebasedatabase.app/");
		}
		public async Task<AppUser?> SignIn(string userEmail, string userPassword)
		{
			string errorMessage = string.Empty;
			
			try
			{
				await _authClient!.SignInWithEmailAndPasswordAsync(userEmail, userPassword);
				AppUser? appUser = await GetUserData(_authClient.User.Uid);
				return appUser;
			}
			catch (FirebaseAuthException ex)
			{
				if (ex.Message.Contains("INVALID_LOGIN_CREDENTIALS"))
				{
					errorMessage = "Incorrect email or password!"; //"אימייל או סיסמה אינם נכונים";
				}
				else
				{
					errorMessage = "Unknown exception!";
				}

				_appLogger.LogDebug($"SignIn failed: {userEmail} {userPassword}, {errorMessage}");
				throw new Exception(errorMessage);
			}
			catch (Exception ex)
			{
				_appLogger.LogDebug($"SignIn failed: {userEmail} {userPassword}, {ex.Message}");
				throw new Exception($"SignIn failed!");
			}			
		}
		public async Task<string?> CreateUserAuth(string userEmail, string userPassword)
		{
			try
			{
				await _authClient!.CreateUserWithEmailAndPasswordAsync(userEmail, userPassword);
				_appLogger.LogDebug($"AppUser Auth {userEmail} created successfully");				
				return _authClient.User.Uid;
			}
			catch (FirebaseAuthException ex)
			{
				_appLogger.LogDebug($"{ex.Message}");

				string errorMessage = string.Empty;

				if (ex.Message.Contains("INVALID_EMAIL")) //Email failed validation - not real email
				{
					errorMessage = "אימייל או סיסמה אינם נכונים";
				}
				if (ex.Message.Contains("EMAIL_EXISTS")) 
				{
					errorMessage = "אימייל או סיסמה אינם נכונים";
				}
				if (ex.Message.Contains("WEAK_PASSWORD"))
				{
					errorMessage = "אימייל או סיסמה אינם נכונים";
				}




				//// Exception reason
				//AuthErrorReason reason = ex.Reason;

				//string errorMessage = reason switch
				//{
				//	AuthErrorReason.InvalidEmailAddress => "Error: Incorrect email adress", // "כתובת האימייל לא תקינה",
				//	AuthErrorReason.WrongPassword => "Error: Incorrect password", // "סיסמה שגויה",					
				//	AuthErrorReason.EmailExists => "Error: This email allready exist", //"האימייל כבר רשום במערכת",
				//	_ => "Error: Unknown exception" // "אירעה שגיאה לא ידועה"
				//};

				//_appLogger.LogDebug($"Firebase Auth creation failed: {errorMessage}");				
				return null; // Indicate failure
			}
			catch (Exception ex)
			{
				_appLogger.LogDebug($"CreateUserAuth failed: {ex.Message}");
				return "Error: SignUp new user failed!";
			}
		}

		//Get AppUser from DB
		public async Task<AppUser?> GetUserData(string userId)
		{
			string errorMessage = string.Empty;
			try
			{
				var user = await _firebaseClient!
					.Child("users")
					.Child(userId)
					.OnceSingleAsync<AppUser>();

				return user;				
			}
			catch (FirebaseException ex)
			{
				if (ex.Message.Contains("401") || ex.Message.Contains("Permission denied"))
				{
					errorMessage = "Permissions denied!";
				}
				else if (ex.Message.Contains("404"))
				{
					errorMessage = "Wrong db path!";
				}
				else
				{
					errorMessage = "Unknown exception!";
				}

				_appLogger.LogDebug($"GetUserData failed: {errorMessage}");
				throw new Exception(errorMessage);
			}
			catch(Exception ex) 
			{
				_appLogger.LogDebug($"GetUserData failed: {ex.Message}");
				throw new Exception("GetUserData failed");
			}

			//var users = await _firebaseClient
			//.Child("Users")
			//.OrderBy("Id")
			//.EqualTo(userId)
			//.OnceAsync<AppUser>();

			//	להגדיר ב-Firebase Console תחת לשונית Rules אינדקס לשדה Id:
			//			{
			//				"rules": {
			//					"Users": {
			//						".indexOn": ["Id"]
			//					}
			//				}
			//			}	

			// מדפיס את תוכן התשובה מהשרת - כאן תראה את הסיבה האמיתית
			//Debug.WriteLine($"Database Error Content: {ex.ResponseContent}");
			//Debug.WriteLine($"Database Error Message: {ex.Message}");

			//string userMessage = "אירעה שגיאה בתקשורת עם בסיס הנתונים.";

			//if (ex.Message.Contains("401") || ex.ResponseContent.Contains("Permission denied"))
			//{
			//	userMessage = "אין לך הרשאות לבצע את הפעולה הזו (בדוק את ה-Rules).";
			//}
			//else if (ex.Message.Contains("404"))
			//{
			//	userMessage = "הנתיב בבסיס הנתונים לא נמצא.";
			//}
		}

		//update user data
		//update specific field if needed
		public async Task<bool> UpdateUser(AppUser user)
		{
			try
			{
				await _firebaseClient!
				.Child("Users")
				.Child(user.Id)
				.PatchAsync(new { UBDate = user.UBDate }); // מעדכן רק את השדה הזה

				return true;
			}
			catch (Exception ex)
			{
				_appLogger.LogDebug($"RealTimeDB update user fail: {ex.Message}");
				return false;
			}
			
		}

		//Add AppUser to RealtimeDB
		public async Task<bool> RegisterAppUser(AppUser appUser)
		{
			try
			{
				 await _firebaseClient!
				.Child("users")
				.Child(appUser.Id)
				.PutAsync(new AppUser()
				{
					Id = appUser.Id,
					FirstName = appUser.FirstName,
					LastName = appUser.LastName,
					UserEmail = appUser.UserEmail,
					UserPassword = appUser.UserPassword,
					RegDate = appUser.RegDate,
					UBDate = appUser.UBDate,
					IsAdmin = appUser.IsAdmin
				});

				return true;
			}
			catch (FirebaseException ex)
			{
				_appLogger.LogDebug($"RealTimeDB create new user fail: {ex.Message}");				
				return false; // Indicate failure;
			}
			catch (Exception ex)
			{
				_appLogger.LogDebug($"RegisterAppUser failed: {ex.Message}");
				return false;
			}

		}
		public async Task<List<AppUser>> GetAllUserAsync()
		{
			try
			{
				var users = await _firebaseClient!
					.Child("users")
					.OnceAsync<AppUser>();

				//users - collection of Firebase objects => Convert to List<AppUser>
				return users.Select(u => new AppUser()
				{
					Id = u.Object.Id,
					FirstName = u.Object.FirstName,
					LastName = u.Object.LastName,
					UserEmail = u.Object.UserEmail,
					UserPassword = u.Object.UserPassword,
					RegDate = u.Object.RegDate,
					UBDate = u.Object.UBDate,
					IsAdmin = u.Object.IsAdmin
				}).ToList();
			}
			catch (FirebaseException ex)
			{
				_appLogger.LogDebug($"GetAllUsers failed: {ex.Message}");
				return new List<AppUser>();
			}
		}
		public async Task<bool> DeleteAppUser(string userId)
		{
			try
			{
				var toDeleteUser = (await _firebaseClient!
					.Child("Users")
					.OnceAsync<AppUser>())
					.FirstOrDefault(u => u.Object.Id == userId);

				if (toDeleteUser != null)
				{
					await _firebaseClient!
						.Child("Users")
						.Child(toDeleteUser.Key!)
						.DeleteAsync();
					return true;
				}
				return false;
			}
			catch (FirebaseException ex)
			{
				_appLogger.LogDebug($"DeleteAppUser failed: {ex.Message}");
				return false;
			}
		}

		// Example of a method that returns an observable stream of Firebase events
		// Get automatic updates when data changes in the Realtime Database

		public IObservable<FirebaseEvent<AppUser>> SubscribeToUserChanges()
		{
			return _firebaseClient!
				.Child("users")
				.AsObservable<AppUser>();
				//.ObserveOn(System.Reactive.Concurrency.Scheduler.Default);
		}
	}
}
