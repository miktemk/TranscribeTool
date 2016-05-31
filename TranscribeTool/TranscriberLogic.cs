using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscribeTool
{
    public class TranscriberLogic
    {
        /// <summary>
        /// Adds meta header to text
        /// </summary>
        public string AddInvisibleMetadata(string text, AudioPlaya playa, string filenameMp3)
        {
            var playerPos = (playa != null)
                ? playa.Position.ToString()
                : TimeSpan.Zero.ToString();
            return String.Format("> MP3:{0}\n> LastTime:{1}\n\n{2}", filenameMp3, playerPos, text);
        }

        /// <summary>
        /// Returns text without the meta header
        /// </summary>
        public string ParseApplyAndRemoveInvisibleMetadata(string text, AudioPlaya Playa)
        {
            var lines = text.Split('\n');
            var lines2 = lines.AsEnumerable();
            if (lines.Length >= 2)
            {
                var line1 = lines2.FirstOrDefault();
                if (!String.IsNullOrEmpty(line1) && line1.StartsWith("> MP3:"))
                {
                    var mp3Filename = line1.Replace("> MP3:", ""); // never used... used to open MP3, but now they are linked via filename
                    lines2 = lines2.Skip(1);
                    var line2 = lines2.FirstOrDefault();
                    if (!String.IsNullOrEmpty(line2) && line2.StartsWith("> LastTime:"))
                    {
                        var prevTs = TimeSpan.Zero;
                        if (Playa != null && TimeSpan.TryParse(line2.Replace("> LastTime:", ""), out prevTs))
                            Playa.Position = prevTs;
                        lines2 = lines2.Skip(1);
                        // skip the extra newline after header (see SaveTextFile below)
                        if (String.IsNullOrEmpty(lines2.FirstOrDefault()))
                            lines2 = lines2.Skip(1);
                    }
                }
            }
            var text2 = String.Join("\n", lines2);
            return text2;
        }

        public string FnameMp3ToText(string filenameMp3)
        {
            if (filenameMp3 == null)
                return null;
            return filenameMp3 + "-trans.md";
        }

        public string FnameTextToMp3(string filenameTxt)
        {
            if (!filenameTxt.ToLower().EndsWith(".mp3-trans.md"))
                return Miktemk_GetFullPathNoExtension(filenameTxt) + ".mp3";
            return filenameTxt.Substring(0, filenameTxt.Length-9);
        }

        public static string Miktemk_GetFullPathNoExtension(string file)
        {
            if (file == null)
                return null;
            return Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
        }
    }
}
