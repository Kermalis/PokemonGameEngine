using Kermalis.PokemonGameEngine.Scripts;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Overworld
{
    internal sealed partial class Obj
    {
        public readonly Queue<ScriptMovement> QueuedScriptMovements = new Queue<ScriptMovement>();

        public void RunNextScriptMovement()
        {
            ScriptMovement m = QueuedScriptMovements.Dequeue();
            switch (m)
            {
                case ScriptMovement.Face_S: Face(FacingDirection.South); break;
                case ScriptMovement.Face_N: Face(FacingDirection.North); break;
                case ScriptMovement.Face_W: Face(FacingDirection.West); break;
                case ScriptMovement.Face_E: Face(FacingDirection.East); break;
                case ScriptMovement.Face_SW: Face(FacingDirection.Southwest); break;
                case ScriptMovement.Face_SE: Face(FacingDirection.Southeast); break;
                case ScriptMovement.Face_NW: Face(FacingDirection.Northwest); break;
                case ScriptMovement.Face_NE: Face(FacingDirection.Northeast); break;
                case ScriptMovement.Walk_S: Move(FacingDirection.South, false, true); break;
                case ScriptMovement.Walk_N: Move(FacingDirection.North, false, true); break;
                case ScriptMovement.Walk_W: Move(FacingDirection.West, false, true); break;
                case ScriptMovement.Walk_E: Move(FacingDirection.East, false, true); break;
                case ScriptMovement.Walk_SW: Move(FacingDirection.Southwest, false, true); break;
                case ScriptMovement.Walk_SE: Move(FacingDirection.Southeast, false, true); break;
                case ScriptMovement.Walk_NW: Move(FacingDirection.Northwest, false, true); break;
                case ScriptMovement.Walk_NE: Move(FacingDirection.Northeast, false, true); break;
                case ScriptMovement.Run_S: Move(FacingDirection.South, true, true); break;
                case ScriptMovement.Run_N: Move(FacingDirection.North, true, true); break;
                case ScriptMovement.Run_W: Move(FacingDirection.West, true, true); break;
                case ScriptMovement.Run_E: Move(FacingDirection.East, true, true); break;
                case ScriptMovement.Run_SW: Move(FacingDirection.Southwest, true, true); break;
                case ScriptMovement.Run_SE: Move(FacingDirection.Southeast, true, true); break;
                case ScriptMovement.Run_NW: Move(FacingDirection.Northwest, true, true); break;
                case ScriptMovement.Run_NE: Move(FacingDirection.Northeast, true, true); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
