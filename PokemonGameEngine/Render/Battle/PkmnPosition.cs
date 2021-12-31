using Kermalis.PokemonBattleEngine.Battle;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Battle
{
    internal sealed class PkmnPosition
    {
        public readonly Vector3 DefaultPosition;
        private readonly Vector2 _barPos;

        public bool InfoVisible;
        public BattlePokemon BattlePkmn;
        public readonly BattleSprite Sprite;

        public PkmnPosition(in Vector3 defaultPos, Vector2 barPos, bool ally)
        {
            DefaultPosition = defaultPos;
            _barPos = barPos;

            Sprite = new BattleSprite(defaultPos, false, scale: ally ? 2f : 1f); // Double size of ally
        }

        /// <summary>Assumes <see cref="BattlePokemon.DetachPos"/> was called first</summary>
        public void Clear()
        {
            InfoVisible = false;
            Sprite.IsVisible = false;
            Sprite.Image = null;
        }

        public void RenderMonInfo()
        {
            BattlePkmn.InfoBarImg.Render(Pos2D.FromRelative(_barPos, BattleGUI.RenderSize));
        }

        public static PkmnPosition[][] CreatePositions(PBEBattleFormat f)
        {
            var a = new PkmnPosition[2][]; // 0 is ally, 1 is foe
            switch (f)
            {
                case PBEBattleFormat.Single:
                {
                    const float floorY = 0.02f;
                    a[0] = new PkmnPosition[1]
                    {
                        new PkmnPosition(new Vector3( 0.00f, floorY,   2.0f), new Vector2(0.015f, 0.25f), true)  // Center
                    };
                    a[1] = new PkmnPosition[1]
                    {
                        new PkmnPosition(new Vector3( 0.75f, floorY, -12.0f), new Vector2(0.10f, 0.015f), false)  // Center
                    };
                    break;
                }
                case PBEBattleFormat.Double:
                {
                    const float floorY = 0.015f;
                    a[0] = new PkmnPosition[2]
                    {
                        new PkmnPosition(new Vector3(-1.50f, floorY,   1.5f), new Vector2(0.015f, 0.250f), true), // Left
                        new PkmnPosition(new Vector3( 2.25f, floorY,   1.5f), new Vector2(0.295f, 0.270f), true)  // Right
                    };
                    a[1] = new PkmnPosition[2]
                    {
                        new PkmnPosition(new Vector3( 1.75f, floorY, -12.5f), new Vector2(0.380f, 0.035f), false), // Left
                        new PkmnPosition(new Vector3(-1.75f, floorY, -12.5f), new Vector2(0.100f, 0.015f), false)  // Right
                    };
                    break;
                }
                case PBEBattleFormat.Triple:
                {
                    const float floorY = 0.02f;
                    a[0] = new PkmnPosition[3]
                    {
                        new PkmnPosition(new Vector3(-2.00f, floorY,   1.0f), new Vector2(0.015f, 0.25f), true), // Left
                        new PkmnPosition(new Vector3( 0.50f, floorY,   1.0f), new Vector2(0.295f, 0.27f), true), // Center
                        new PkmnPosition(new Vector3( 3.25f, floorY,   1.0f), new Vector2(0.575f, 0.29f), true)  // Right
                    };
                    a[1] = new PkmnPosition[3]
                    {
                        new PkmnPosition(new Vector3( 3.25f, floorY, -12.5f), new Vector2(0.66f, 0.055f), false), // Left
                        new PkmnPosition(new Vector3( 0.75f, floorY, -11.0f), new Vector2(0.38f, 0.035f), false), // Center
                        new PkmnPosition(new Vector3(-2.75f, floorY, -12.5f), new Vector2(0.10f, 0.015f), false)  // Right
                    };
                    break;
                }
                case PBEBattleFormat.Rotation: // TODO: Rotation Battles are not functional so there's time to figure out the camera before then
                {
                    const float floorY = 0.5f; // TODO
                    a[0] = new PkmnPosition[3]
                    {
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.015f, 0.25f), true), // Left
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.295f, 0.27f), true), // Center
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.575f, 0.29f), true)  // Right
                    };
                    a[1] = new PkmnPosition[3]
                    {
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.66f, 0.055f), false), // Left
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.38f, 0.035f), false), // Center
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.10f, 0.015f), false)  // Right
                    };
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(f));
            }
            return a;
        }
    }
}
