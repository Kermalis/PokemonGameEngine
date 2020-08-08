using Kermalis.PokemonGameEngine.Core;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal sealed class EventObj : VisualObj
    {
        public ObjMovementType MovementType;
        public int MovementX;
        public int MovementY;
        public TrainerType TrainerType;
        public byte TrainerSight;
        public string Script;
        public Flag Flag;

        public EventObj(Map.Events.ObjEvent oe, Map map)
            : base(oe.Id, oe.Sprite, new Position(oe))
        {
            MovementType = oe.MovementType;
            switch (MovementType)
            {
                case ObjMovementType.FaceSouth: Facing = FacingDirection.South; break;
                case ObjMovementType.FaceSouthwest: Facing = FacingDirection.Southwest; break;
                case ObjMovementType.FaceSoutheast: Facing = FacingDirection.Southeast; break;
                case ObjMovementType.FaceNorth: Facing = FacingDirection.North; break;
                case ObjMovementType.FaceNorthwest: Facing = FacingDirection.Northwest; break;
                case ObjMovementType.FaceNortheast: Facing = FacingDirection.Northeast; break;
                case ObjMovementType.FaceWest: Facing = FacingDirection.West; break;
                case ObjMovementType.FaceEast: Facing = FacingDirection.East; break;
            }
            MovementX = oe.MovementX;
            MovementY = oe.MovementY;
            TrainerType = oe.TrainerType;
            TrainerSight = oe.TrainerSight;
            Script = oe.Script;
            Flag = oe.Flag;
            map.Objs.Add(this);
            Map = map;
        }


    }
}
