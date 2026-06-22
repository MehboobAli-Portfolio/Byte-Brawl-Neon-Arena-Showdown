using UnityEngine;
using Supabase;

public class DatabaseManager : MonoBehaviour
{
    // The Singleton: makes sure only one database connection exists
    public static DatabaseManager Instance;

    [Header("Database Credentials")]
    public string supabaseURL = "YOUR_SUPABASE_URL";
    public string supabaseAnonKey = "YOUR_SUPABASE_ANON_KEY";

    // The actual database instance other scripts will talk to
    public Client supabase { get; private set; }

    private async void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keeps the database alive when loading the Arena
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Connect to your live Supabase project
        var options = new SupabaseOptions { AutoConnectRealtime = true };
        supabase = new Client(supabaseURL, supabaseAnonKey, options);
        await supabase.InitializeAsync();

        Debug.Log("Supabase Database is connected!");
    }
}