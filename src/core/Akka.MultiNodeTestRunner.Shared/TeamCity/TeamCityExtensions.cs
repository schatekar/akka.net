namespace Akka.MultiNodeTestRunner.Shared.TeamCity
{
    /// <summary>
    /// Holds extension method to escape strings as per TeamCity's string escaping rules
    /// </summary>
    public static class TeamCityExtensions
    {
        public static string Escape(this string output)
        {
            if (output == null) return null;
            return output.Replace("|", "||").Replace("'", "|'").Replace("]", "|]").Replace("\n", "|n").Replace("\r", "|r");
        }
    }
}
