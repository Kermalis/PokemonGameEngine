using Kermalis.PokemonGameEngine.GUI;
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
            var obj = Obj.GetObj(id);
            Obj.LoadedObjs.Remove(obj);
            obj.Map.Objs.Remove(obj);
        }
        private void AwaitObjMovementCommand()
        {
            ushort id = (ushort)ReadVarOrValue();
            var obj = Obj.GetObj(id);
            _waitMovementObj = obj;
        }
        private void LookTowardsObjCommand()
        {
            ushort id1 = (ushort)ReadVarOrValue();
            ushort id2 = (ushort)ReadVarOrValue();
            var looker = Obj.GetObj(id1);
            var target = Obj.GetObj(id2);
            looker.LookTowards(target);
        }

        private static void DetachCameraCommand()
        {
            CameraObj.CameraAttachedTo = null;
            // Camera should probably have properties that get its attachment or its own properties
            // Instead of using CameraCopyMovement()
            // Map changing will be tougher though
            //CameraObj.Camera.IsScriptMoving = false;
        }
        private void AttachCameraCommand()
        {
            ushort id = (ushort)ReadVarOrValue();
            var obj = Obj.GetObj(id);
            CameraObj.CameraAttachedTo = obj;
            CameraObj.CameraCopyMovement();
        }

        private void WarpCommand()
        {
            int mapId = _reader.ReadInt32();
            int x = _reader.ReadInt32();
            int y = _reader.ReadInt32();
            byte elevation = (byte)ReadVarOrValue();
            OverworldGUI.Instance.TempWarp(new Warp(mapId, x, y, elevation));
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

        private static void SetAllLock(bool locked)
        {
            foreach (Obj o in Obj.LoadedObjs)
            {
                o.IsLocked = locked;
            }
        }
        private static void LockAllObjsCommand()
        {
            SetAllLock(true);
        }
        private static void UnlockAllObjsCommand()
        {
            SetAllLock(false);
        }

        private void AwaitReturnToFieldCommand()
        {
            _waitReturnToField = true;
        }
    }
}
