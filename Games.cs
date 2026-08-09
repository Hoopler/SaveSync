using System.Text.Json;

public class Game
{
    public static List<Game> Games = new List<Game>();

    public string Name { get; set; } = string.Empty;
    public string SaveLocation { get; set; } = string.Empty;

    public Game(string name, string saveLocation)
    {
        Name = name;
        SaveLocation = saveLocation;
    }

    public Game() { }

    private static string GetConfigFilePath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string saveDir = Path.Combine(appDataPath, "SaveSync");
        Directory.CreateDirectory(saveDir);
        return Path.Combine(saveDir, "games.json");
    }
    
    public static void AddGame(string name, string saveLocation)
    {
        ReadGames();

        Game newgame = new Game(name, saveLocation);
        Games.Add(newgame);
        SaveGames();
    }

    public static void RemoveGame(string name)
    {
        ReadGames();

        Game game = Games.Find(x => x.Name == name);
        if (game == null)
        {
            Console.WriteLine($"Game '{name}' not found.");
            return;
        }

        Games.Remove(game);
        SaveGames();
    }

    public static void ReadGames()
    {
        string filePath = GetConfigFilePath();

        if (!File.Exists(filePath))
        {
            Games = new List<Game>();
            return;
        }

        string json = File.ReadAllText(filePath);
        Games = JsonSerializer.Deserialize<List<Game>>(json) ?? new List<Game>();
    }

    public static void SaveGames()
    {
        string filePath = GetConfigFilePath();
        string json = JsonSerializer.Serialize(Games, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    public static void ListGames()
    {
        ReadGames();

        if(Games.Count == 0)
        {
            Console.WriteLine("No games added yet.");
            return;
        }

        foreach (Game game in Games)
        {
            Console.WriteLine($"{game.Name} | {game.SaveLocation}");
        }
    }
}