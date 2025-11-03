using System;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus;

namespace MyDiscordBot.commands
{
    public class TestCommands : BaseCommandModule

    {
        [Command("help")]
        public async Task Help(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync(content: "Write "/" to see all commands =)");
        }
        
        
        [Command(name: "hello")]
        public async Task Hello(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Hi!");
        }

        
        [Command(name: "random")] // random number generator, for example: !random 1 100
        public async Task Random(CommandContext ctx, int min, int max)
        {
            var randomValue = new System.Random().Next(min, max);
            await ctx.Channel.SendMessageAsync( content:ctx.User.Mention + " - your number is " + randomValue);
        }
        
        private const ulong TargetUserId = 1234567890123456789; // Insert here any ID of anyone you want to be sent to timeout for 1 minute..

        [Command("!")]
        public async Task TimeoutUser(CommandContext ctx)
        {
            if (ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                var guild = ctx.Guild;
                var member = await guild.GetMemberAsync(TargetUserId);
                
                if (member != null)
                {
                    var duration = TimeSpan.FromMinutes(1);
                    await member.TimeoutAsync(DateTime.UtcNow + duration);
                    await ctx.RespondAsync($"{member.Username} has been timeouted for 1 minute.");
                }
                else
                {
                    await ctx.RespondAsync("User not found.");
                }
            }
            else
            {
                await ctx.RespondAsync("You don't have permission to use this command.");
            }
        }
        
    }
}
