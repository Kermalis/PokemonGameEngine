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

        public PkmnPosition(in Vector3 defaultPos, Vector2 spriteScale, Vector2 barPos)
        {
            DefaultPosition = defaultPos;
            _barPos = barPos;

            Sprite = new BattleSprite(spriteScale, defaultPos, false);
        }

        /// <summary>Assumes <see cref="BattlePokemon.DetachPos"/> was called first</summary>
        public void Clear()
        {
            InfoVisible = false;
            Sprite.IsVisible = false;
            Sprite.AnimImage = null;
        }

        public void RenderMonInfo()
        {
            BattlePkmn.InfoBarImg.Render(_barPos.Absolute(BattleGUI.RenderSize));
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
                        new PkmnPosition(new Vector3( 0.00f, floorY,   2.0f), new Vector2(0.04120f, 0.05430f), new Vector2(0.015f, 0.25f))  // Center (cam dist sqrd = 266.720400)
                    };
                    a[1] = new PkmnPosition[1]
                    {
                        new PkmnPosition(new Vector3( 0.75f, floorY, -12.0f), new Vector2(0.03660f, 0.04850f), new Vector2(0.10f, 0.015f))  // Center (cam dist sqrd = 816.782900)
                    };
                    break;
                }
                case PBEBattleFormat.Double:
                {
                    const float floorY = 0.015f;
                    a[0] = new PkmnPosition[2]
                    {
                        new PkmnPosition(new Vector3(-1.50f, floorY,   1.5f), new Vector2(0.04370f, 0.05750f), new Vector2(0.015f, 0.250f)), // Left  (cam dist sqrd = 303.290222)
                        new PkmnPosition(new Vector3( 2.25f, floorY,   1.5f), new Vector2(0.04025f, 0.05300f), new Vector2(0.295f, 0.270f))  // Right (cam dist sqrd = 253.602722)
                    };
                    a[1] = new PkmnPosition[2]
                    {
                        new PkmnPosition(new Vector3( 1.75f, floorY, -12.5f), new Vector2(0.03671f, 0.04860f), new Vector2(0.380f, 0.035f)), // Left  (cam dist sqrd = 832.602700)
                        new PkmnPosition(new Vector3(-1.75f, floorY, -12.5f), new Vector2(0.03845f, 0.05040f), new Vector2(0.100f, 0.015f))  // Right (cam dist sqrd = 881.602700)
                    };
                    break;
                }
                case PBEBattleFormat.Triple:
                {
                    const float floorY = 0.02f;
                    a[0] = new PkmnPosition[3]
                    {
                        new PkmnPosition(new Vector3(-2.00f, floorY,   1.0f), new Vector2(0.04545f, 0.05990f), new Vector2(0.015f, 0.25f)), // Left   (cam dist sqrd = 325.720400)
                        new PkmnPosition(new Vector3( 0.50f, floorY,  -0.5f), new Vector2(0.04660f, 0.06140f), new Vector2(0.295f, 0.27f)), // Center (cam dist sqrd = 331.220400)
                        new PkmnPosition(new Vector3( 3.25f, floorY,   1.0f), new Vector2(0.04050f, 0.05320f), new Vector2(0.575f, 0.29f))  // Right  (cam dist sqrd = 258.782900)
                    };
                    a[1] = new PkmnPosition[3]
                    {
                        new PkmnPosition(new Vector3( 3.25f, floorY, -12.5f), new Vector2(0.03600f, 0.04730f), new Vector2(0.66f, 0.055f)), // Left   (cam dist sqrd = 819.032900)
                        new PkmnPosition(new Vector3( 0.75f, floorY, -11.0f), new Vector2(0.03550f, 0.04700f), new Vector2(0.38f, 0.035f)), // Center (cam dist sqrd = 763.782900)
                        new PkmnPosition(new Vector3(-2.75f, floorY, -12.5f), new Vector2(0.03880f, 0.05130f), new Vector2(0.10f, 0.015f))  // Right  (cam dist sqrd = 900.032900)
                    };
                    break;
                }
                case PBEBattleFormat.Rotation: // TODO: Rotation Battles are not functional so there's time to figure out the camera before then
                {
                    const float floorY = 0.5f; // TODO
                    a[0] = new PkmnPosition[3]
                    {
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.00000f, 0.00000f), new Vector2(0.015f, 0.25f)), // Left
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.00000f, 0.00000f), new Vector2(0.295f, 0.27f)), // Center
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.00000f, 0.00000f), new Vector2(0.575f, 0.29f))  // Right
                    };
                    a[1] = new PkmnPosition[3]
                    {
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.00000f, 0.00000f), new Vector2(0.66f, 0.055f)), // Left
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.00000f, 0.00000f), new Vector2(0.38f, 0.035f)), // Center
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.00000f, 0.00000f), new Vector2(0.10f, 0.015f))  // Right
                    };
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(f));
            }
            return a;
        }
    }
}
