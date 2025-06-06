using Discord;
using Discord.Rest;

namespace Echo.Services
{
    public class DiscordBotService
    {
        private readonly ulong _channelId;
        private readonly string _token;
        private readonly DiscordRestClient _client;

        public DiscordBotService()
        {
            _channelId = 685835681725284356;
            _token = "MTE3OTg4MzQ5MzYxMDc2MjM5MQ.GwD-KN.5tyUfDbJjV-GM1VYMpUoy_mxJStQlviwP6rmac";
            _client = new DiscordRestClient();

        }


        public async Task SendMessageToChannel(string message)
        {
            try
            {
                if (_client.LoginState != LoginState.LoggedIn)
                {
                    await _client.LoginAsync(TokenType.Bot, _token);
                }

                var channel = await _client.GetChannelAsync(_channelId) as RestTextChannel;
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