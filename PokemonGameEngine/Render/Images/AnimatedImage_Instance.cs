﻿using Kermalis.SimpleGIF;
using Kermalis.SimpleGIF.Decoding;
using Silk.NET.OpenGL;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Render.Images
{
    internal sealed partial class AnimatedImage
    {
        private sealed partial class Instance
        {
            public const ushort REPEAT_FOREVER = 0;

            private static readonly Dictionary<string, Instance> _loadedImages = new();

            private readonly string _id;
            private int _numReferences;

            public readonly Frame[] Frames;
            public readonly Vec2I Size;
            public readonly ushort RepeatCount;

            private Instance(string assetPath, string id, (uint PID, bool Shiny)? spindaSpots)
            {
                _id = id;
                _numReferences = 1;
                _loadedImages.Add(id, this);

                DecodedGIF gif = GIFRenderer.DecodeAllFrames(File.OpenRead(assetPath), ColorFormat.RGBA);

                if (spindaSpots is not null)
                {
                    (uint pid, bool shiny) = spindaSpots.Value;
                    SpindaSpotRenderer.Render(gif, pid, shiny);
                }

                Size = new Vec2I(gif.Width, gif.Height);
                Frames = new Frame[gif.Frames.Count];
                for (int i = 0; i < gif.Frames.Count; i++)
                {
                    Frames[i] = new Frame(gif.Frames[i], Size);
                }
                RepeatCount = gif.RepeatCount;
            }

            public static Instance LoadOrGet(string assetPath, (uint PID, bool Shiny)? spindaSpots)
            {
                // Add spinda spot data to the asset to use it uniquely
                string id;
                if (spindaSpots is not null)
                {
                    (uint pid, bool shiny) = spindaSpots.Value;
                    id = assetPath + string.Format("_{0:X8}_{1}", pid, shiny);
                }
                else
                {
                    id = assetPath;
                }
                // LoadOrGet now
                if (_loadedImages.TryGetValue(id, out Instance img))
                {
                    img._numReferences++;
                }
                else
                {
                    img = new Instance(assetPath, id, spindaSpots);
                }
                return img;
            }

            public void DeductReference()
            {
                if (--_numReferences <= 0)
                {
                    GL gl = Display.OpenGL;
                    for (int i = 0; i < Frames.Length; i++)
                    {
                        gl.DeleteTexture(Frames[i].Texture);
                    }
                    _loadedImages.Remove(_id);
                }
            }
        }
    }
}
