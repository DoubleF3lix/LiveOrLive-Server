namespace liveorlive_server.Extensions {
    public static class HttpContextExtension {
        public static string? GetStringQueryParam(this HttpContext context, string param) {
            var queryParam = context.Request.Query[param].ToString();
            if (string.IsNullOrEmpty(queryParam)) return null;
            return queryParam;
        }
    }
}
