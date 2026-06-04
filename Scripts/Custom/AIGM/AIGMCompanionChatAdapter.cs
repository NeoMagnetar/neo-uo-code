using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionChatAdapter
    {
        public static bool TryRespond(Mobile speaker, AIGMCompanionDakeyras companion, string question, out string spokenReply)
        {
            spokenReply = null;
            Log("TryRespond start speaker=" + SafeName(speaker) + " companion=" + SafeName(companion) + " question=" + (question ?? String.Empty));

            if (speaker == null || companion == null || companion.Deleted || String.IsNullOrWhiteSpace(question))
            {
                Log("TryRespond abort due to null/empty inputs.");
                return false;
            }

            AIGMTargetInfo target = new AIGMTargetInfo();
            target.Kind = "Mobile";
            target.Serial = companion.Serial;
            target.Name = companion.Name;
            target.TypeName = companion.GetType().Name;
            target.MapName = companion.Map != null ? companion.Map.Name : null;
            target.RegionName = companion.Region != null ? companion.Region.Name : null;
            target.X = companion.X;
            target.Y = companion.Y;
            target.Z = companion.Z;
            target.IsNpc = true;
            target.IsAlive = companion.Alive;

            AIGMResponse response = AIGMBridgeClient.Ask(speaker, question, target);
            if (response == null)
            {
                Log("TryRespond bridge returned null response.");
                return false;
            }

            string reply = response.ReplyText;
            Log("TryRespond bridge ok=" + response.Ok + " reply=" + (reply ?? String.Empty) + " error=" + (response.ErrorMessage ?? String.Empty));
            if (String.IsNullOrWhiteSpace(reply))
            {
                Log("TryRespond replyText empty.");
                return false;
            }

            reply = StripHtml(reply);
            if (String.IsNullOrWhiteSpace(reply))
                return false;

            if (reply.Length > 220)
                reply = reply.Substring(0, 220).Trim() + "...";

            spokenReply = reply;
            Log("TryRespond success spokenReply=" + spokenReply);
            return true;
        }

        private static void Log(string message)
        {
            try
            {
                string path = System.IO.Path.Combine(Core.BaseDirectory, "Logs", "AIGMCompanionChatAdapter.log");
                System.IO.File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
            }
        }

        private static string SafeName(Mobile mob)
        {
            if (mob == null)
                return "(null)";

            return (mob.Name ?? mob.GetType().Name) + "[0x" + mob.Serial.Value.ToString("X8") + "]";
        }

        private static string StripHtml(string input)
        {
            if (String.IsNullOrWhiteSpace(input))
                return input;

            string text = input.Replace("<BR>", " ").Replace("<br>", " ").Replace("<br/>", " ").Replace("<BR/>", " ");
            while (true)
            {
                int start = text.IndexOf('<');
                if (start < 0)
                    break;

                int end = text.IndexOf('>', start + 1);
                if (end < 0)
                    break;

                text = text.Remove(start, (end - start) + 1);
            }

            return text.Replace("&nbsp;", " ").Trim();
        }
    }
}
