using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    public sealed class SpriteSheet
    {
        internal static Dictionary<string, SpriteSheet> Sheets { get; }
        public static Dictionary<string, SpriteSheet>.KeyCollection SheetsNames => Sheets.Keys;

        static SpriteSheet()
        {
            var json = JObject.Parse(File.ReadAllText(Path.Combine(Program.AssetPath, "ObjSprites", "ObjSprites.json")));
            Sheets = new Dictionary<string, SpriteSheet>(json.Count);
            foreach (KeyValuePair<string, JToken> kvp in json)
            {
                Sheets.Add(kvp.Key, new SpriteSheet(kvp.Value));
            }
        }

        internal readonly string Sprites;
        internal readonly int Width;
        internal readonly int Height;

        internal SpriteSheet(JToken j)
        {
            Sprites = j[nameof(Sprites)].Value<string>();
            Width = j[nameof(Width)].Value<int>();
            Height = j[nameof(Height)].Value<int>();
        }
    }
}
