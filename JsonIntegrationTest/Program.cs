using System;
using DataConnector.JSON;

namespace JsonIntegrationTest
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			JsonDataBackend backend = new JsonDataBackend ("TestData.json");

			User user = new User ();
			user.BirthDate = DateTime.MinValue;
			user.Name = "Glen";
			user.Username = "glen3b";
			user.PasswordHash = new string ('0', 64);
			backend.SaveObject (user);

			Blog blog = new Blog ();
			blog.OwnerUserID = user.ID;
			blog.Name = "CatLand";
			blog.Description = "Describing my cat";
			backend.SaveObject (blog);

			JsonDataBackend readBackend = new JsonDataBackend ("TestData.json");
			readBackend.AssembliesToLoad.Add (typeof(MainClass).Assembly);
			foreach (var usr in readBackend.GetAllObjectsOfType<User>()) {
				Console.WriteLine ("UserID:{0} is {1} ({2})", usr.ID, usr.Username, usr.Name);
			}
			foreach (var lblog in readBackend.GetAllObjectsOfType<Blog>()) {
				Console.WriteLine ("BlogID:{0} is {1} ({2})", lblog.ID, lblog.Name, lblog.Description);
			}
		}
	}
}