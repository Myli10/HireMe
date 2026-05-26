using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Auth.Repository;
using FirebaseWorkout.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseWorkout.Service.DBService.Firebase
{
	internal class FirebaseAuthService : IAuthService
	{
		private FirebaseAuthClient? _authClient;
		private IAppLogger _logger;

		public FirebaseAuthService(IAppLogger logger)
		{
			_logger = logger;

			// Initialize Firebase Authentication Client
			var config = new FirebaseAuthConfig()
			{
				ApiKey = "AIzaSyBfuUKAoRD0bMJvqgh8sxV5DuOAgSMjIp4",
				AuthDomain = "big17datafb.firebaseapp.com",
				Providers = new FirebaseAuthProvider[]
					{
						new EmailProvider()
					},
				UserRepository = new FileUserRepository("AppCurrentUser") // שמירת session מקומית
			};
			_authClient = new FirebaseAuthClient(config);
			_logger = logger;
		}

		public async Task<string> SignIn(string userEmail, string userPassword)
		{
			string errorMessage = string.Empty;
			try
			{
				await _authClient!.SignInWithEmailAndPasswordAsync(userEmail, userPassword);
				return _authClient.User.Info.Uid;
			}
			catch (FirebaseAuthException ex)
			{
				if (ex.Message.Contains("INVALID_LOGIN_CREDENTIALS"))
				{
					errorMessage = "Incorrect email or password!"; //"אימייל או סיסמה אינם נכונים";
					_logger.LogDebug($" SignInAuth failed: {userEmail} {userPassword}, {errorMessage}");
				}
				else
				{
					errorMessage = "SignInAuth failed: Unknown exception!";
					_logger.LogDebug($"SignInAuth failed: {userEmail} {userPassword}, Unknown exception!");
				}
				throw new Exception(errorMessage);
			}
			catch (Exception ex)
			{
				_logger.LogDebug($"SignInAuth failed: {userEmail} {userPassword}, {ex.Message}");
				throw new Exception("SignIn failed!");
			}

		}		
		public async Task<string> CreateAuth(string userEmail, string userPassword)
		{
			try
			{
				await _authClient!.CreateUserWithEmailAndPasswordAsync(userEmail, userPassword);
				_logger.LogDebug($"AppUser Auth {userEmail} created successfully");
				return _authClient.User.Uid;
			}
			catch (FirebaseAuthException ex)
			{
				string errorMessage = string.Empty;

				if (ex.Message.Contains("INVALID_EMAIL")) //Email failed validation - not real email
				{
					errorMessage = "Invalid email adress!";
				}
				if (ex.Message.Contains("EMAIL_EXISTS"))
				{
					errorMessage = "This email already exists!";
				}
				if (ex.Message.Contains("WEAK_PASSWORD"))
				{
					errorMessage = "Weak password!";
				}

				_logger.LogDebug($"CreateUserAuth failed: {ex.Message}");
				throw new Exception(errorMessage);

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
			}
			catch (Exception ex)
			{
				_logger.LogDebug($"CreateUserAuth failed: {ex.Message}");
				return "SignUp new user failed!";
			}
		}
		public async Task RemoveAuth(string userEmail, string userPassword)
		{
			try
			{
				//1 Authenticate the user to be deleted
				await _authClient!.SignInWithEmailAndPasswordAsync(userEmail, userPassword);
				//2 Delete the authenticated user
				await _authClient.User.DeleteAsync();
				//3 Re-authenticate the previous logged in user
				await _authClient!.SignInWithEmailAndPasswordAsync(
					(App.Current as App)!.CurrentUser!.UserEmail,
					(App.Current as App)!.CurrentUser!.UserPassword);

				_logger.LogDebug($"User {userEmail} removed from Auth successfully");
			}
			catch (Exception ex)
			{
				_logger.LogDebug($"Remove user {userEmail} from Auth failed: {ex.Message}");
				throw new Exception("Remove user from Auth failed!");
			}
		}

		public async Task SignOut()
		{
			await Task.CompletedTask;
			_authClient?.SignOut();
		}

		public async Task<string?> TryRestoreSessionAsync()
		{
			try
			{
				if (_authClient?.User != null)
				{
					// מרענן טוקן אם פג תוקף
					await _authClient.User.GetIdTokenAsync();
					return _authClient.User.Uid;
				}
				return null;
			}
			catch
			{
				return null;
			}
		}
	}
}
