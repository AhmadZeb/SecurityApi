namespace SecurityApi.Helpers
{
    public static class UriHelpers
    {
        public static string CombineBaseWithRelative(Uri baseUri, string relativeUri)
        {
            if (string.IsNullOrWhiteSpace(relativeUri))
            {
                return null;
            }
            return new Uri(baseUri, relativeUri).ToString();
        }
    }
}
