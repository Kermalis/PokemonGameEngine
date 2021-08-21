using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Scripts;

namespace Kermalis.PokemonGameEngine.World.Maps
{
    internal sealed class MapEvents
    {
        public sealed class WarpEvent
        {
            public readonly WorldPos Pos;
            public readonly Warp Warp;

            public WarpEvent(EndianBinaryReader r)
            {
                Pos = new WorldPos(r.ReadInt32(), r.ReadInt32(), r.ReadByte());
                Warp = new Warp(r.ReadInt32(), new WorldPos(r.ReadInt32(), r.ReadInt32(), r.ReadByte()));
            }
        }
        public sealed class ObjEvent
        {
            public readonly WorldPos Pos;

            public readonly ushort Id;
            public readonly string ImageId;
            public readonly ObjMovementType MovementType;
            public readonly int MovementX;
            public readonly int MovementY;
            public readonly TrainerType TrainerType;
            public readonly byte TrainerSight;
            public readonly string Script;
            public readonly Flag Flag;

            public ObjEvent(EndianBinaryReader r)
            {
                Pos = new WorldPos(r.ReadInt32(), r.ReadInt32(), r.ReadByte());

                Id = r.ReadUInt16();
                ImageId = r.ReadStringNullTerminated();
                MovementType = r.ReadEnum<ObjMovementType>();
                MovementX = r.ReadInt32();
                MovementY = r.ReadInt32();
                TrainerType = r.ReadEnum<TrainerType>();
                TrainerSight = r.ReadByte();
                Script = r.ReadStringNullTerminated();
                Flag = r.ReadEnum<Flag>();
            }
        }
        public sealed class ScriptEvent
        {
            public readonly WorldPos Pos;

            public readonly Var Var;
            public readonly short VarValue;
            public readonly ScriptConditional VarConditional;
            public readonly string Script;

            public ScriptEvent(EndianBinaryReader r)
            {
                Pos = new WorldPos(r.ReadInt32(), r.ReadInt32(), r.ReadByte());

                Var = r.ReadEnum<Var>();
                VarValue = r.ReadInt16();
                VarConditional = r.ReadEnum<ScriptConditional>();
                Script = r.ReadStringNullTerminated();
            }
        }

        public readonly WarpEvent[] Warps;
        public readonly ObjEvent[] Objs;
        public readonly ScriptEvent[] ScriptTiles;

        public MapEvents(EndianBinaryReader r)
        {
            ushort count = r.ReadUInt16();
            Warps = new WarpEvent[count];
            for (int i = 0; i < count; i++)
            {
                Warps[i] = new WarpEvent(r);
            }
            count = r.ReadUInt16();
            Objs = new ObjEvent[count];
            for (int i = 0; i < count; i++)
            {
                Objs[i] = new ObjEvent(r);
            }
            count = r.ReadUInt16();
            ScriptTiles = new ScriptEvent[count];
            for (int i = 0; i < count; i++)
            {
                ScriptTiles[i] = new ScriptEvent(r);
            }
        }
    }
}
