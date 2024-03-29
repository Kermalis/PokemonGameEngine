﻿using Kermalis.PokemonGameEngine.Player;
using Kermalis.PokemonGameEngine.Render.World;
using System;
#if DEBUG_CALLBACKS
using Kermalis.PokemonGameEngine.Debug;
using System.IO;
using System.Runtime.CompilerServices;
#endif

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class Game
    {
        public static Game Instance { get; private set; } = null!; // Set in constructor

        public Save Save { get; }
        public StringBuffers StringBuffers { get; }

        /// <summary>For use with Script command "AwaitReturnToField"</summary>
        public bool IsOnOverworld; // TODO: Convert into a sort of general purpose "WaitState"/"WaitSignal" command

        public Action Callback;

        public Game()
        {
            Instance = this;

            _ = new OverworldGUI(); // Create first so the save can load the PlayerObj and init the camera
            Save = new Save();
            Save.Debug_Create(); // Load/initialize Save
            StringBuffers = new StringBuffers();

            // Finish init and set initial callback
            OverworldGUI.Instance.FinishInit();
            OverworldGUI.Instance.Debug_SetDefaultCallback();
        }

#if DEBUG_CALLBACKS
        public void SetCallback(Action main, [CallerMemberName] string caller = null, [CallerFilePath] string callerFile = null)
        {
            Log.ModifyIndent(+1);
            Log.WriteLine("Callback changed");
            Log.ModifyIndent(+1);
            Log.WriteLine(string.Format("New callback:\t{0}.{1}()", main.Method.DeclaringType.Name, main.Method.Name));
            Log.WriteLine(string.Format("Changed by:\t\t{0}()", caller));
            Log.WriteLine(string.Format("Above method's file:\t{0}", Path.GetFileName(callerFile)));
            Log.ModifyIndent(-2);
            Callback = main;
        }
#else
        public void SetCallback(Action main)
        {
            Callback = main;
        }
#endif

        public void RunCallback()
        {
            Callback.Invoke();
        }
    }
}
