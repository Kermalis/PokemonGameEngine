using Kermalis.PokemonGameEngine.Render.World;
using Kermalis.PokemonGameEngine.Scripts;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;

namespace Kermalis.PokemonGameEngine.Script
{
    internal sealed partial class ScriptContext
    {
        private void MoveObjCommand()
        {
            ushort id = (ushort)ReadVarOrValue();
            uint offset = _reader.ReadUInt32();
            long returnOffset = _reader.BaseStream.Position;
            _reader.BaseStream.Position = offset;
            var obj = Obj.GetObj(id);
            while (true)
            {
                ScriptMovement m = _reader.ReadEnum<ScriptMovement>();
                if (m == ScriptMovement.End)
                {
                    break;
                }
                obj.QueuedScriptMovements.Enqueue(m);
            }
            _reader.BaseStream.Position = returnOffset;
            obj.IsScriptMoving = true;
        }
        private void UnloadObjCommand()
        {
            ushort id = (ushort)ReadVarOrValue();
            Obj.LoadedObjs.Remove(Obj.GetObj(id));
        }
        private void AwaitObjMovementCommand()
        {
            ushort id = (ushort)ReadVarOrValue();
            _waitMovementObj = Obj.GetObj(id);
        }
        private void LookTowardsObjCommand()
        {
            ushort id1 = (ushort)ReadVarOrValue();
            ushort id2 = (ushort)ReadVarOrValue();
            var looker = Obj.GetObj(id1);
            var target = Obj.GetObj(id2);
            looker.LookTowards(target);
        }

        private static void CreateCameraObjCommand()
        {
            Obj cur = OverworldGUI.Instance.CamAttachedTo;
            _ = new CameraObj(cur.Map, cur.Pos);
        }
        private void AttachCameraCommand()
        {
            ushort id = (ushort)ReadVarOrValue();
            var obj = Obj.GetObj(id);
            OverworldGUI.Instance.SetCamAttachment_HandleMapChange(obj);
        }

        private void WarpCommand()
        {
            int mapId = _reader.ReadInt32();
            WorldPos pos;
            pos.XY.X = _reader.ReadInt32();
            pos.XY.Y = _reader.ReadInt32();
            pos.Elevation = (byte)ReadVarOrValue();
            OverworldGUI.Instance.StartPlayerWarp(new Warp(mapId, pos));
        }

        private void SetLock(bool locked)
        {
            ushort id = (ushort)ReadVarOrValue();
            var obj = Obj.GetObj(id);
            obj.IsLocked = locked;
        }
        private void LockObjCommand()
        {
            SetLock(true);
        }
        private void UnlockObjCommand()
        {
            SetLock(false);
        }

        private static void LockAllObjsCommand()
        {
            Obj.SetAllLock(true);
        }
        private static void UnlockAllObjsCommand()
        {
            Obj.SetAllLock(false);
        }

        private void AwaitReturnToFieldCommand()
        {
            _waitReturnToField = true;
        }

        private static void UseSurfCommand()
        {
            OverworldGUI.Instance.StartSurfTasks();
        }
    }
}
