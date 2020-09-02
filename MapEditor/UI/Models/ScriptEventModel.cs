using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Scripts;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI.Models
{
    public sealed class ScriptEventModel : INotifyPropertyChanged, IDisposable
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int X
        {
            get => Ev.X;
            set
            {
                if (value != Ev.X)
                {
                    Ev.X = value;
                    OnPropertyChanged(nameof(X));
                }
            }
        }
        public int Y
        {
            get => Ev.Y;
            set
            {
                if (value != Ev.Y)
                {
                    Ev.Y = value;
                    OnPropertyChanged(nameof(Y));
                }
            }
        }
        public byte Elevation
        {
            get => Ev.Elevation;
            set
            {
                if (value != Ev.Elevation)
                {
                    Ev.Elevation = value;
                    OnPropertyChanged(nameof(Elevation));
                }
            }
        }

        public Var Var
        {
            get => Ev.Var;
            set
            {
                if (value != Ev.Var)
                {
                    Ev.Var = value;
                    OnPropertyChanged(nameof(Var));
                }
            }
        }
        public short VarValue
        {
            get => Ev.VarValue;
            set
            {
                if (value != Ev.VarValue)
                {
                    Ev.VarValue = value;
                    OnPropertyChanged(nameof(VarValue));
                }
            }
        }
        public ScriptConditional VarConditional
        {
            get => Ev.VarConditional;
            set
            {
                if (value != Ev.VarConditional)
                {
                    Ev.VarConditional = value;
                    OnPropertyChanged(nameof(VarConditional));
                }
            }
        }
        public string Script
        {
            get => Ev.Script;
            set
            {
                if (value != Ev.Script)
                {
                    Ev.Script = value;
                    OnPropertyChanged(nameof(Script));
                }
            }
        }

        public static IEnumerable<ScriptConditional> SelectableConditionals { get; } = Utils.GetEnumValues<ScriptConditional>();
        public static IEnumerable<Var> SelectableVars { get; } = Utils.GetEnumValues<Var>();

        internal readonly Map.Events.ScriptEvent Ev;

        internal ScriptEventModel(Map.Events.ScriptEvent ev)
        {
            Ev = ev;
        }

        public void Dispose()
        {
            PropertyChanged = null;
        }
    }
}
