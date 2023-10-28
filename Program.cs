using DeepMorphy;
using DeepMorphy.Model;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using System.Reflection.Metadata;

internal class Program
{
    private const string _pivo = "пиво";
    private const string _nevskoe = "Невское";

    private static async Task Main(string[] args)
    {
        var morph = new MorphAnalyzer();

        var discord = new DiscordClient(new DiscordConfiguration()
        {
            Token = "your_token",
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
        });

        discord.MessageCreated += async (s, e) =>
        {
            if (e.Message.Content.ToLower().StartsWith("[p]"))
            {
                string message = e.Message.Content.Replace("[p]", "").Trim();
                List<string> clusteredMessage = message.Split(' ').ToList();
                var result = morph.Parse(clusteredMessage).ToArray();

                List<int> nounsIndecies = new List<int>();

                for (int i = 0; i < result.Length; i++)
                {
                    bool isNoun = result[i].BestTag["чр"] == "сущ";

                    if (isNoun)
                    {
                        if (result[i].Tags[0].Power >= 0.33f)
                        {
                            nounsIndecies.Add(i);
                        }
                    }
                }

                Random random = new Random();
                int selectedIndex = nounsIndecies[random.Next(nounsIndecies.Count)];
                var resultWord = result[selectedIndex];

                var tasks = new[]
                {
                     new InflectTask(_pivo,
                         morph.TagHelper.CreateTag("прил", gndr: "ср", nmbr: "ед", @case: "им"),
                         morph.TagHelper.CreateTag("прил", gndr: "ср", nmbr: "ед", @case: resultWord.BestTag["падеж"])),
                     new InflectTask(_nevskoe,
                         morph.TagHelper.CreateTag("прил", gndr: "ср", nmbr: "ед", @case: "им"),
                         morph.TagHelper.CreateTag("прил", gndr: "ср", nmbr: "ед", @case: resultWord.BestTag["падеж"]))
                };

                var morphedPivo = morph.Inflect(tasks).ToArray();
                clusteredMessage[selectedIndex] = morphedPivo[0]; // + $"[{resultWord.BestTag["чр"]}]";
                clusteredMessage.Insert(selectedIndex + 1, morphedPivo[1]);

                string pivoMessage = string.Join(" ", clusteredMessage).Trim();

                await SendMessage(e, pivoMessage);
            }
        };

        await discord.ConnectAsync();
        await Task.Delay(-1);
    }

    private static async Task SendMessage(MessageCreateEventArgs e, string message)
    {
        await e.Message.RespondAsync(message);
    }
}