using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;

namespace PawSharp.DocBot.Commands
{
    public class DocsModule : ApplicationCommandModule
    {
        public static PawSharp.DocBot.DocsService? Docs { get; set; }

        [SlashCommand("docs", "Search PawSharp API reference")]
        public async Task DocsCommand(InteractionContext ctx,
            [Option("query", "Search query (class, method, property)")] string query,
            [Option("mention", "(optional) user to mention in the response")] DiscordUser mention = null)
        {
            var result = Docs?.Search(query);
            string header, body, footer = string.Empty;
            if (result == null)
            {
                header = $"**No results for:** {query}";
                body = "No matching API symbol found in reference.";
            }
            else
            {
                header = $"**{result.Name}**";
                body = string.IsNullOrWhiteSpace(result.Summary) ? "(no summary)" : result.Summary;
                if (!string.IsNullOrWhiteSpace(result.Remarks)) body += "\n\n" + result.Remarks;
                if (!string.IsNullOrWhiteSpace(result.Returns)) body += "\n\nReturns: " + result.Returns;
            }

            if (mention != null)
            {
                footer = $"\n\nRequested by: <@{mention.Id}>";
            }

            var content = header + "\n\n" + body + footer;
            await ctx.CreateResponseAsync(DSharpPlus.Entities.InteractionResponseType.ChannelMessageWithSource,
                new DSharpPlus.Entities.DiscordInteractionResponseBuilder().WithContent(content));
        }
    }
}
