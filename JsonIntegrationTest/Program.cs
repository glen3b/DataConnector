using System;
using DataConnector;

namespace JsonIntegrationTest
{
	class MainClass
	{
		public static void Main (string[] args)
		{
            MemoryDataBackend backend = new MemoryDataBackend();

            User user = new User ();
			user.BirthDate = DateTime.MinValue;
			user.Name = "Glen";
			user.Username = "glen3b";
			user.PasswordHash = new string ('0', 64);
			backend.SaveObject (user);

            User user2 = new User();
            user2.BirthDate = DateTime.MaxValue;
            user2.Name = "John Smith";
            user2.Username = "person";
            user2.PasswordHash = new string('2', 64);
            backend.SaveObject(user2);

            Blog blog = new Blog ();
			blog.OwnerUserID = user.ID;
			blog.Name = "CatLand";
			blog.Description = "Describing my cat";
			backend.SaveObject (blog);

            Blog blog2 = new Blog();
            blog2.OwnerUserID = user2.ID;
            blog2.Name = "DogLand";
            blog2.Description = "Describing my dog";
            backend.SaveObject(blog2);

            // readBackend.AssembliesToLoad.Add (typeof(MainClass).Assembly);
            foreach (var usr in backend.GetAllObjectsOfType<User>()) {
				Console.WriteLine ("UserID:{0} is {1} ({2})", usr.ID, usr.Username, usr.Name);
			}
			foreach (var lblog in backend.GetAllObjectsOfType<Blog>()) {
				Console.WriteLine ("BlogID:{0} is {1} ({2})", lblog.ID, lblog.Name, lblog.Description);
			}

            foreach (var lblog in backend.GetChildrenOf<User, Blog>(1))
            {
                Console.WriteLine("BlogID:{0} is {1} ({2})", lblog.ID, lblog.Name, lblog.Description);
            }

            Console.ReadKey(true);
		}
	}
}