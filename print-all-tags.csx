using System.Text.RegularExpressions;

var postsDir = @"_posts";
var posts = Directory.EnumerateFiles(postsDir, "*.md", SearchOption.AllDirectories);
// print found posts
//posts.ToList().ForEach(Console.WriteLine);

var tags = posts.SelectMany(GetTags).Distinct().OrderBy(x=>x).ToList();
tags.ForEach(Console.WriteLine);

IEnumerable<string> GetTags(string postPath)
{
	using (var file = File.OpenText(postPath))
	{
		string ln;
		do
		{
			ln = file.ReadLine().Trim();
		} while(!ln.StartsWith("categories"));

		var toRemove = new List<string> {
			"categories",
			@"\:",
			@"\[",
			@"\]", 
			@"\s+" };
		
		toRemove.ForEach(x => ln = Regex.Replace(ln, x, ""));
		ln = ln.Trim();
		
		return ln.Split(',').ToList();
	}
}
