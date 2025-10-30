using Discord;
using Discord.Rest;
using Echo.Models.Settings;

namespace Echo.Services
{
    public class DiscordBotService
    {
        private readonly DiscordRestClient _client;
        private readonly AppSettings _appSettings;

        public DiscordBotService(AppSettings appSettings)
        {
            _client = new DiscordRestClient();
            _appSettings = appSettings;
        }


        public async Task SendMessageToChannel(string message)
        {
            try
            {
                if (_client.LoginState != LoginState.LoggedIn)
                {
                    await _client.LoginAsync(TokenType.Bot, _appSettings.DiscordSettings.Token);
                }

                var channel = await _client.GetChannelAsync(_appSettings.DiscordSettings.ChannelId) as RestTextChannel;
                if (channel != null)
                {
                    await channel.SendMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
            }
        }

    }
}