namespace A3;

public class Ia
{
    public static System.Threading.Tasks.Task<string> AskAsync(string q) => new System.Net.Http.HttpClient().SendAsync(new(System.Net.Http.HttpMethod.Post, "https://genesis.decem.vip/api/")
    {
        Headers = { { "Authorization", "Bearer KT/Cuxm23DmpWcaOeM7pgO2nliclGAMC04Uqs2YeQIA=" } },
        Content = new System.Net.Http.FormUrlEncodedContent(new KeyValuePair<string, string>[] { new("content", q), new("tokens", "4000") })
    }).ContinueWith(t => t.Result.Content.ReadAsStringAsync()).Unwrap();
}
