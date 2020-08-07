using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI.Models
{
    public sealed class ObjectEventModel : INotifyPropertyChanged, IDisposable
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int X
        {
            get => Obj.X;
            set
            {
                if (value != Obj.X)
                {
                    Obj.X = value;
                    OnPropertyChanged(nameof(X));
                }
            }
        }
        public int Y
        {
            get => Obj.Y;
            set
            {
                if (value != Obj.Y)
                {
                    Obj.Y = value;
                    OnPropertyChanged(nameof(Y));
                }
            }
        }
        public byte Elevation
        {
            get => Obj.Elevation;
            set
            {
                if (value != Obj.Elevation)
                {
                    Obj.Elevation = value;
                    OnPropertyChanged(nameof(Elevation));
                }
            }
        }

        public ushort Id
        {
            get => Obj.Id;
            set
            {
                if (value != Obj.Id)
                {
                    Obj.Id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }
        public string Sprite
        {
            get => Obj.Sprite;
            set
            {
                if (value != Obj.Sprite)
                {
                    Obj.Sprite = value;
                    OnPropertyChanged(nameof(Sprite));
                }
            }
        }
        public ObjMovementType MovementType
        {
            get => Obj.MovementType;
            set
            {
                if (value != Obj.MovementType)
                {
                    Obj.MovementType = value;
                    OnPropertyChanged(nameof(MovementType));
                }
            }
        }
        public int MovementX
        {
            get => Obj.MovementX;
            set
            {
                if (value != Obj.MovementX)
                {
                    Obj.MovementX = value;
                    OnPropertyChanged(nameof(MovementX));
                }
            }
        }
        public int MovementY
        {
            get => Obj.MovementY;
            set
            {
                if (value != Obj.MovementY)
                {
                    Obj.MovementY = value;
                    OnPropertyChanged(nameof(MovementY));
                }
            }
        }
        public TrainerType TrainerType
        {
            get => Obj.TrainerType;
            set
            {
                if (value != Obj.TrainerType)
                {
                    Obj.TrainerType = value;
                    OnPropertyChanged(nameof(TrainerType));
                }
            }
        }
        public byte TrainerSight
        {
            get => Obj.TrainerSight;
            set
            {
                if (value != Obj.TrainerSight)
                {
                    Obj.TrainerSight = value;
                    OnPropertyChanged(nameof(TrainerSight));
                }
            }
        }
        public string Script
        {
            get => Obj.Script;
            set
            {
                if (value != Obj.Script)
                {
                    Obj.Script = value;
                    OnPropertyChanged(nameof(Script));
                }
            }
        }
        public Flag Flag
        {
            get => Obj.Flag;
            set
            {
                if (value != Obj.Flag)
                {
                    Obj.Flag = value;
                    OnPropertyChanged(nameof(Flag));
                }
            }
        }

        public static IEnumerable<ObjMovementType> SelectableMovementTypes { get; } = Utils.GetEnumValues<ObjMovementType>();
        public static IEnumerable<TrainerType> SelectableTrainerTypes { get; } = Utils.GetEnumValues<TrainerType>();
        public static IEnumerable<Flag> SelectableFlags { get; } = Utils.GetEnumValues<Flag>();

        internal readonly Map.Events.ObjEvent Obj;

        internal ObjectEventModel(Map.Events.ObjEvent obj)
        {
            Obj = obj;
        }

        public void Dispose()
        {
            PropertyChanged = null;
        }
    }
}
