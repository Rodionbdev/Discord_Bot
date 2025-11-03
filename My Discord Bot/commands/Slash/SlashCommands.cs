using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Linq;
using System.Threading.Tasks;

namespace MyDiscordBot.Commands
{
    public class SlashCommands : ApplicationCommandModule
    {
        private static Dictionary<ulong, int> _warns = new Dictionary<ulong, int>(); // Temporary in-memory warning database (resets after restart)

        private bool IsUserAdmin(InteractionContext ctx) // Simple check if the user is an Administrator
        {
            return ctx.Member.Permissions.HasPermission(Permissions.Administrator);
        }

        private bool IsUserModerator(InteractionContext ctx) // Simple check if the user is a Moderator
        {
            return ctx.Member.Permissions.HasPermission(Permissions.ModerateMembers);
        }
        
        // 1. Mute in chat
        [SlashCommand("mutechat", "Mute a user in text chat")]
        public async Task MuteChat(InteractionContext ctx, [Option("user", "User to mute")] DiscordUser user)
        {
            if (!IsUserModerator(ctx))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You don't have permission to use this command.")
                        .AsEphemeral(true));
                return;
            }

            var member = await ctx.Guild.GetMemberAsync(user.Id);
            var muteRole = ctx.Guild.Roles.Values.FirstOrDefault(r => r.Name == "MutedFromChat");

            if (muteRole == null)
            {
                muteRole = await ctx.Guild.CreateRoleAsync("MutedFromChat", Permissions.None, DiscordColor.DarkGray,
                    false, true);
                foreach (var channel in ctx.Guild.Channels.Values)
                {
                    await channel.AddOverwriteAsync(muteRole, Permissions.None, Permissions.SendMessages);
                }
            }

            await member.GrantRoleAsync(muteRole);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"{member.DisplayName} has been muted in chat."));
        }

        // 2. Full voice mute (kick + block from joining)
        [SlashCommand("mutevoice", "Disconnect a user from voice and block rejoining")]
        public async Task MuteVoice(InteractionContext ctx,
            [Option("user", "User to mute in voice")] DiscordUser user)
        {
            if (!IsUserModerator(ctx))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You don't have permission to use this command.")
                        .AsEphemeral(true));
                return;
            }

            var member = await ctx.Guild.GetMemberAsync(user.Id);
            var muteRole = ctx.Guild.Roles.Values.FirstOrDefault(r => r.Name == "MutedFromVoice");

            if (muteRole == null)
            {
                muteRole = await ctx.Guild.CreateRoleAsync("MutedFromVoice", Permissions.None, DiscordColor.DarkGray,
                    false, true);

                foreach (var channel in ctx.Guild.Channels.Values)
                {
                    if (channel.Type == ChannelType.Voice)
                    {
                        await channel.AddOverwriteAsync(muteRole, DSharpPlus.Permissions.None,
                            DSharpPlus.Permissions.UseVoice);
                    }
                }
            }

            await member.GrantRoleAsync(muteRole);

            if (member.VoiceState?.Channel != null)
            {
                await member.ModifyAsync(x => x.VoiceChannel = null);
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent(
                    $"{member.DisplayName} has been muted in voice channels and cannot join them."));
        }

        // 3. Warn a user (counter + auto-mute/ban)
        [SlashCommand("warn", "Issue a warning to a user")]
        public async Task Warn(InteractionContext ctx, [Option("user", "User to warn")] DiscordUser user)
        {
            if (!IsUserAdmin(ctx))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You don't have permission to use this command.")
                        .AsEphemeral(true));
                return;
            }

            if (!_warns.ContainsKey(user.Id))
                _warns[user.Id] = 0;

            _warns[user.Id]++;

            var member = await ctx.Guild.GetMemberAsync(user.Id);

            if (_warns[user.Id] == 3)
            {
                await MuteChat(ctx, user);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        $"{member.DisplayName} has received 3 warnings and was muted in chat."));
            }
            else if (_warns[user.Id] >= 5)
            {
                await member.BanAsync();
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        $"{member.DisplayName} has received 5 warnings and was banned."));
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        $"{member.DisplayName} has been warned. Total warnings: {_warns[user.Id]}/5"));
            }
        }

        // 4. Ban a user
        [SlashCommand("ban", "Ban a user")]
        public async Task Ban(InteractionContext ctx, [Option("user", "User to ban")] DiscordUser user)
        {
            if (!IsUserAdmin(ctx))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You don't have permission to use this command.")
                        .AsEphemeral(true));
                return;
            }

            var member = await ctx.Guild.GetMemberAsync(user.Id);
            await member.BanAsync();

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"{member.DisplayName} has been banned."));
        }

        // 5. Show rules
        [SlashCommand("rules", "Display the server rules")]
        public async Task Rules(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
            {
                Title = "Server Rules",
                Description =
                    "1. No spamming\n2. Be respectful to others\n3. Do not use prohibited language\n4. Follow Discord TOS",
                Color = DiscordColor.Azure
            };

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        // 6. Bot information
        [SlashCommand("info", "Display bot information")]
        public async Task Info(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
            {
                Title = "Bot Information",
                Description = "A standard Discord moderation bot.\nDeveloper: Rodion\nDiscord: 1255968122754699305\nTelegram: @Rodionbdev",
                Color = DiscordColor.Azure
            };

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        // 7. Unmute in chat
        [SlashCommand("unmutechat", "Unmute a user in text chat")]
        public async Task UnmuteFromChat(InteractionContext ctx, [Option("user", "User to unmute")] DiscordUser user)
        {
            if (!IsUserModerator(ctx))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You don't have permission to use this command.")
                        .AsEphemeral(true));
                return;
            }

            var member = await ctx.Guild.GetMemberAsync(user.Id);
            var muteRole = ctx.Guild.Roles.Values.FirstOrDefault(r => r.Name == "MutedFromChat");
            if (muteRole != null)
            {
                await member.RevokeRoleAsync(muteRole);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"{member.DisplayName} has been unmuted in chat."));
            }
        }

        // 8. Unmute in voice
        [SlashCommand("unmutevoice", "Unmute a user in voice")]
        public async Task UnmuteFromVoice(InteractionContext ctx, [Option("user", "User to unmute")] DiscordUser user)
        {
            if (!IsUserModerator(ctx))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You don't have permission to use this command.")
                        .AsEphemeral(true));
                return;
            }

            var member = await ctx.Guild.GetMemberAsync(user.Id);
            var muteRole = ctx.Guild.Roles.Values.FirstOrDefault(r => r.Name == "MutedFromVoice");
            if (muteRole != null)
            {
                await member.RevokeRoleAsync(muteRole);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"{member.DisplayName} has been unmuted in voice."));
            }
        }

        // 9. Kick a user
        [SlashCommand("kick", "Kick a user from the server")]
        public async Task Kick(InteractionContext ctx, [Option("user", "User to kick")] DiscordUser user)
        {
            if (!IsUserAdmin(ctx))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You don't have permission to use this command.")
                        .AsEphemeral(true));
                return;
            }

            var member = await ctx.Guild.GetMemberAsync(user.Id);
            await member.RemoveAsync();
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"{member.DisplayName} has been kicked from the server."));
        }

        // 10. Unban user (by ID only)
        [SlashCommand("unban", "Unban a user by ID")]
        public async Task Unban(InteractionContext ctx, [Option("userid", "User ID to unban")] string userIdStr)
        {
            if (!IsUserAdmin(ctx))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You don't have permission to use this command.")
                        .AsEphemeral(true));
                return;
            }

            if (!ulong.TryParse(userIdStr, out ulong userId))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Error: invalid user ID format."));
                return;
            }

            var bannedUsers = await ctx.Guild.GetBansAsync();
            var bannedUser = bannedUsers.FirstOrDefault(b => b.User.Id == userId);

            if (bannedUser != null)
            {
                await ctx.Guild.UnbanMemberAsync(userId);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"User {bannedUser.User.Username} has been unbanned."));
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"User with ID {userId} not found in the ban list."));
            }
        }

        // 11. Remove a warning
        [SlashCommand("unwarn", "Remove a warning from a user")]
        public async Task Unwarn(InteractionContext ctx, [Option("user", "User to remove warning from")] DiscordUser user)
        {
            if (!IsUserAdmin(ctx))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You don't have permission to use this command.")
                        .AsEphemeral(true));
                return;
            }

            if (_warns.ContainsKey(user.Id))
            {
                _warns[user.Id]--;
                var member = await ctx.Guild.GetMemberAsync(user.Id);

                // Unmute in chat if warnings drop to 2
                if (_warns[user.Id] == 2)
                {
                    var muteRole = ctx.Guild.Roles.Values.FirstOrDefault(r => r.Name == "MutedFromChat");
                    if (muteRole != null)
                    {
                        await member.RevokeRoleAsync(muteRole);
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder().WithContent($"{member.DisplayName} has been unmuted in chat and now has {_warns[user.Id]} warnings."));
                    }
                }

                // If warnings drop to 0
                if (_warns[user.Id] <= 0)
                {
                    _warns.Remove(user.Id);
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent($"{user.Username} no longer has any warnings."));
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent($"{user.Username} now has {_warns[user.Id]} warnings."));
                }
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"{user.Username} has no warnings."));
            }
        }

        // 12. Check warnings
        [SlashCommand("checkwarns", "Check a user's warnings count")]
        public async Task CheckWarns(InteractionContext ctx, [Option("user", "User to check warnings for")] DiscordUser user)
        {
            if (!IsUserAdmin(ctx))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You don't have permission to use this command.")
                        .AsEphemeral(true));
                return;
            }

            if (_warns.ContainsKey(user.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"{user.Username} has {_warns[user.Id]} warnings."));
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"{user.Username} has no warnings."));
            }
        }

        // 13. List banned users
        [SlashCommand("bannedusers", "Display the list of banned users")]
        public async Task BannedUsers(InteractionContext ctx)
        {
            if (!IsUserAdmin(ctx))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You don't have permission to use this command.")
                        .AsEphemeral(true));
                return;
            }

            var bannedUsers = await ctx.Guild.GetBansAsync();
            var bannedList = string.Join("\n", bannedUsers.Select(b => $"Username - {b.User.Username}  User ID - {b.User.Id}"));

            if (string.IsNullOrEmpty(bannedList))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("There are no banned users on this server."));
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"Banned users:\n{bannedList}"));
            }
        }
    }
}
