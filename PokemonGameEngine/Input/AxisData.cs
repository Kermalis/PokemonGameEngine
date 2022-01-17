using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Input
{
    internal sealed class AxisData
    {
        private const float DEADZONE = 0.50f; // 50% deadzone

        private readonly Dictionary<Key, PressData> _dpadSimulation;
        public float X;
        public float Y;

        public AxisData()
        {
            var buttons = new Key[4] { Key.Left, Key.Right, Key.Up, Key.Down };
            _dpadSimulation = PressData.CreateDict(buttons);
        }

        public void Prepare()
        {
            PressData.PrepareMany(_dpadSimulation.Values);
        }
        private void Set(Key key, bool down)
        {
            PressData d = _dpadSimulation[key];
            // Only send state changes
            if (down)
            {
                if (!d.IsPressed)
                {
                    d.OnChanged(true); // Sets IsNew to true
                }
            }
            else
            {
                if (d.IsPressed)
                {
                    d.OnChanged(false); // Sets WasReleased to true
                }
            }
        }
        public void Update(bool isX, float value)
        {
            // Left/Up activated
            if (value < -DEADZONE)
            {
                if (isX)
                {
                    X = value;
                    Set(Key.Left, true);
                    Set(Key.Right, false);
                }
                else
                {
                    Y = value;
                    Set(Key.Up, true);
                    Set(Key.Down, false);
                }
            }
            // Right/down activated
            else if (value > DEADZONE)
            {
                if (isX)
                {
                    X = value;
                    Set(Key.Left, false);
                    Set(Key.Right, true);
                }
                else
                {
                    Y = value;
                    Set(Key.Up, false);
                    Set(Key.Down, true);
                }
            }
            // Not reached past deadzone, kill to 0
            else
            {
                if (isX)
                {
                    X = 0f;
                    Set(Key.Left, false);
                    Set(Key.Right, false);
                }
                else
                {
                    Y = 0f;
                    Set(Key.Up, false);
                    Set(Key.Down, false);
                }
            }
        }

        public PressData GetDPADSimPressData(Key k)
        {
            if (k == Key.Left || k == Key.Right || k == Key.Up || k == Key.Down)
            {
                return _dpadSimulation[k];
            }
            return null;
        }
    }
}
