using System;
using DataConnector.JSON;

namespace JsonIntegrationTest
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			JsonDataBackend backend = new JsonDataBackend ("TestData.json");
			Blog blog = new Blog ();
			blog.Name = "CatLand";
			blog.Description = "Describing my cat";
			backend.SaveObject (blog);
		}
	}
}