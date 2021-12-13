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
        public void Update(bool isX, float value)
        {
            // Left/Up activated
            if (value < -DEADZONE)
            {
                if (isX)
                {
                    X = value;
                    _dpadSimulation[Key.Left].Update(true);
                    _dpadSimulation[Key.Right].Update(false);
                }
                else
                {
                    Y = value;
                    _dpadSimulation[Key.Up].Update(true);
                    _dpadSimulation[Key.Down].Update(false);
                }
            }
            // Right/down activated
            else if (value > DEADZONE)
            {
                if (isX)
                {
                    X = value;
                    _dpadSimulation[Key.Left].Update(false);
                    _dpadSimulation[Key.Right].Update(true);
                }
                else
                {
                    Y = value;
                    _dpadSimulation[Key.Up].Update(false);
                    _dpadSimulation[Key.Down].Update(true);
                }
            }
            // Not reached past deadzone, kill to 0
            else
            {
                if (isX)
                {
                    X = 0f;
                    _dpadSimulation[Key.Left].Update(false);
                    _dpadSimulation[Key.Right].Update(false);
                }
                else
                {
                    Y = 0f;
                    _dpadSimulation[Key.Up].Update(false);
                    _dpadSimulation[Key.Down].Update(false);
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
