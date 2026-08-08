class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            string msg = """
                Usage: SaveSync <add|remove> [game-name];
                       SaveSync <upload|download> [save-name];
            """;   
            Console.WriteLine(msg);
            return;
        }
        
        switch (args[0])
        {
            case "upload":
                if(args.Length > 1)
                    await DriveSync.UploadSave(args[1]);
                else
                    Console.WriteLine("Usage: upload <game-name>");
                break;
            case "download":
                if(args.Length > 1)
                    await DriveSync.DownloadSave(args[1]);
                else
                    Console.WriteLine("Usage: download <game-name>");
                break;
            case "list":
                Game.ListGames();
                break;
            case "add":
                if(args.Length == 3)
                {
                    Game.AddGame(args[1], args[2]);
                }
                else if(args.Length == 2)
                {
                    var game = args[1];
                    string? path = Prompt("Save location: ");
                    if(string.IsNullOrEmpty(path))
                    {
                        Console.WriteLine("Invalid path.");
                        return;
                    }
                    Game.AddGame(game, path);
                }
                else if(args.Length == 1)
                {
                    string? game = Prompt("Game name: ");
                    if(string.IsNullOrEmpty(game))
                    {
                        Console.WriteLine("Invalid game name.");
                        return;
                    }
                    string? path = Prompt("Save location: ");
                    if(string.IsNullOrEmpty(path))
                    {
                        Console.WriteLine("Invalid path.");
                        return;
                    }
                    Game.AddGame(game, path);
                }
                break;
            case "remove":
                if(args.Length > 1)
                    Game.RemoveGame(args[1]);
                else
                    Console.WriteLine("Usage: remove <game-name>");
                break;
            default:
                Console.WriteLine($"Unknown command: {args[0]}");
                break;
        }
    }

    static string? Prompt(string label)
    {
        Console.WriteLine(label);
        return Console.ReadLine();
    }
}