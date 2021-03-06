namespace RiftBot;

public class ClanModule : ModuleBase<SocketCommandContext>
{
    private readonly RiftBotContext _context;

    public ClanService ClanService { get; set; }

    public ClanModule(RiftBotContext context)
    {
        _context = context;
    }

    [Command("getxp", RunMode = RunMode.Async)]
    [Summary("!getxp <player name> - Get a members clan xp")]
    public async Task GetMemberXp([Remainder] string memberName)
    {
        if (memberName.StartsWith('\"') || memberName.EndsWith('\"'))
            memberName = memberName.Trim('\"');

        string xp = await ClanService.GetClanMemberXp(memberName);

        var embed = new EmbedBuilder()
            .WithDescription(xp)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("getpreference", RunMode=RunMode.Async)]
    [Summary("!getpreference <playername> - returns the members current preference")]
    public async Task GetMemberPreference([Remainder] string memberName)
    {
        try
        {
            if (memberName.StartsWith('\"') || memberName.EndsWith('\"'))
                memberName = memberName.Trim('\"');

            string preference = await ClanService.GetPreference(memberName) ? "pvm" : "skilling";
            await ReplyAsync($"{memberName}'s preference is set to {preference}");
        }
        catch (ArgumentException ex)
        {
            await ReplyAsync(ex.Message);
        }
    }

    [Command("preferpvm", RunMode = RunMode.Async)]
    [Summary("Admin: !preferpvm <playername> - Removes player from skilling rankup consideration")]
    public async Task PreferPvm([Remainder] string playerName)
    {
        try
        {
            BotSetting restrictedCommandChannelSetting = await _context.BotSettings.FirstOrDefaultAsync(x => x.Name == "RestrictedCommandChannel");
            BotSetting restrictedCommandGuildSetting = await _context.BotSettings.FirstOrDefaultAsync(x => x.Name == "RestrictedCommandGuild");
            if (Context.Channel.Name != restrictedCommandChannelSetting.Value && Context.Guild.Id != ulong.Parse(restrictedCommandGuildSetting.Value)) return;

            await ClanService.SetPreference(playerName, true);
        }
        catch (ArgumentException ex)
        {
            await ReplyAsync(ex.Message);
        }
    }

    [Command("preferskilling", RunMode = RunMode.Async)]
    [Summary("Admin: !preferskilling <playername> - Adds player to skilling rankup consideration")]
    public async Task PreferSkilling([Remainder] string playerName)
    {
        try
        {
            BotSetting restrictedCommandChannelSetting = await _context.BotSettings.FirstOrDefaultAsync(x => x.Name == "RestrictedCommandChannel");
            BotSetting restrictedCommandGuildSetting = await _context.BotSettings.FirstOrDefaultAsync(x => x.Name == "RestrictedCommandGuild");
            if (Context.Channel.Name != restrictedCommandChannelSetting.Value && Context.Guild.Id != ulong.Parse(restrictedCommandGuildSetting.Value)) return;

            await ClanService.SetPreference(playerName, false);
        }
        catch (ArgumentException ex)
        {
            await ReplyAsync(ex.Message);
        }
    }

    private class RandomMessage
    {
        public RandomMessage(string message, bool runAnyway, int timeout = 0)
        {
            Message = message;
            RunAnyway = runAnyway;
            Timeout = timeout;
        }

        public string Message { get; set; }
        public bool RunAnyway { get; set; }
        public int Timeout { get; set; }
    }

    [Command("rankups", RunMode = RunMode.Async)]
    [Summary("Admin: !rankups - Get potential rank ups")]
    public async Task GetRankUps(bool force = false)
    {
        BotSetting restrictedCommandChannelSetting = await _context.BotSettings.FirstOrDefaultAsync(x => x.Name == "RestrictedCommandChannel");
        BotSetting restrictedCommandGuildSetting = await _context.BotSettings.FirstOrDefaultAsync(x => x.Name == "RestrictedCommandGuild");
        if (Context.Channel.Name != restrictedCommandChannelSetting.Value && Context.Guild.Id != ulong.Parse(restrictedCommandGuildSetting.Value)) return;

        if (!force)
        {
            List<RandomMessage> randomMessages = new()
            {
                new($"No {Emote.Parse("<:Thefinger:725207592950956153>")}", false),
                new("Do it yourself!", false),
                new("Fuck off!", false),
                new("Fine... give me a few seconds", true, 3000),
                new("Zzzzzzzzz", false),
                new("Huh? Who? What?...... Oh, you again.... Go away", false),
                new("I need a raise", true, 3000)
            };

            BotSetting rankupsTrollChanceSetting = await _context.BotSettings.FirstOrDefaultAsync(x => x.Name == "RankupsTrollChance");
                
            Random r = new();
            if (r.NextSingle() <= float.Parse(rankupsTrollChanceSetting.Value))
            {
                int index = r.Next(0, randomMessages.Count);
                RandomMessage randomMessage = randomMessages[index];

                await ReplyAsync(randomMessage.Message);
                        
                if (!randomMessage.RunAnyway) return;

                IDisposable typingContext = Context.Channel.EnterTypingState();
                await Task.Delay(randomMessage.Timeout);
                typingContext.Dispose();
            }
        }

        var embed = new EmbedBuilder();
        List<MemberRankUp> memberRankUps = await ClanService.GetRankUps();
        if (memberRankUps.Count > 0)
        {
            memberRankUps = memberRankUps.OrderBy(x => x.MemberName).ToList();
            foreach (MemberRankUp memberRankUp in memberRankUps)
            {
                embed.AddField($"{memberRankUp.MemberName}",
                    $"From {memberRankUp.CurrentRank} to {memberRankUp.NewRank}\nExperience: {memberRankUp.CurrentXp:N0}/{memberRankUp.RequiredXp:N0}");
            }
        }
        else
        {
            embed.WithDescription("Ranks are all up to date!");
        }

        await ReplyAsync(embed: embed.Build());
    }

    [Command("update", RunMode = RunMode.Async)]
    [Summary("!update")]
    [RequireOwner]
    public async Task UpdateClanMembers()
    {
        await ClanService.UpdateClanMembers();
        await ReplyAsync("Clan xp updated");
    }
}