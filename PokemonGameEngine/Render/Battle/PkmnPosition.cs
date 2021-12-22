using Kermalis.PokemonBattleEngine.Battle;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Battle
{
    internal sealed class PkmnPosition
    {
        public readonly Vector3 DefaultPosition;
        private readonly RelPos2D _barPos;

        public bool InfoVisible;
        public BattlePokemon BattlePkmn;
        public readonly BattleSprite Sprite;

        public PkmnPosition(in Vector3 defaultPos, Vector2 spriteScale, RelPos2D barPos)
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
            const float floorY = 0.25f;

            var a = new PkmnPosition[2][]; // 0 is ally, 1 is foe
            switch (f)
            {
                case PBEBattleFormat.Single:
                {
                    a[0] = new PkmnPosition[1]
                    {
                        new PkmnPosition(new Vector3( 0.00f, floorY,   2.0f), new Vector2(0.04100f, 0.05400f), new RelPos2D(0.015f, 0.25f))  // Center (cam dist sqrd = 263.5625)
                    };
                    a[1] = new PkmnPosition[1]
                    {
                        new PkmnPosition(new Vector3( 0.75f, floorY, -12.0f), new Vector2(0.03660f, 0.04800f), new RelPos2D(0.10f, 0.015f))  // Center (cam dist sqrd = 813.6250)
                    };
                    break;
                }
                case PBEBattleFormat.Double:
                {
                    a[0] = new PkmnPosition[2]
                    {
                        new PkmnPosition(new Vector3(-1.50f, floorY,   1.5f), new Vector2(0.04370f, 0.05750f), new RelPos2D(0.015f, 0.250f)), // Left  (cam dist sqrd = 300.0625)
                        new PkmnPosition(new Vector3( 2.25f, floorY,   1.5f), new Vector2(0.04010f, 0.05300f), new RelPos2D(0.295f, 0.270f))  // Right (cam dist sqrd = 250.3750)
                    };
                    a[1] = new PkmnPosition[2]
                    {
                        new PkmnPosition(new Vector3( 1.75f, floorY, -12.5f), new Vector2(0.03671f, 0.04860f), new RelPos2D(0.380f, 0.035f)), // Left  (cam dist sqrd = 829.3750)
                        new PkmnPosition(new Vector3(-1.75f, floorY, -12.5f), new Vector2(0.03835f, 0.05040f), new RelPos2D(0.100f, 0.015f))  // Right (cam dist sqrd = 878.3750)
                    };
                    break;
                }
                case PBEBattleFormat.Triple:
                {
                    a[0] = new PkmnPosition[3]
                    {
                        new PkmnPosition(new Vector3(-2.00f, floorY,   1.0f), new Vector2(0.04540f, 0.05980f), new RelPos2D(0.015f, 0.25f)), // Left   (cam dist sqrd = 322.5625)
                        new PkmnPosition(new Vector3( 0.50f, floorY,  -0.5f), new Vector2(0.04646f, 0.06120f), new RelPos2D(0.295f, 0.27f)), // Center (cam dist sqrd = 328.0625)
                        new PkmnPosition(new Vector3( 3.25f, floorY,   1.0f), new Vector2(0.04030f, 0.05310f), new RelPos2D(0.575f, 0.29f))  // Right  (cam dist sqrd = 255.6250)
                    };
                    a[1] = new PkmnPosition[3]
                    {
                        new PkmnPosition(new Vector3( 3.25f, floorY, -12.5f), new Vector2(0.03600f, 0.04730f), new RelPos2D(0.66f, 0.055f)), // Left   (cam dist sqrd = 815.8750)
                        new PkmnPosition(new Vector3( 0.75f, floorY, -11.0f), new Vector2(0.03550f, 0.04700f), new RelPos2D(0.38f, 0.035f)), // Center (cam dist sqrd = 760.6250)
                        new PkmnPosition(new Vector3(-2.75f, floorY, -12.5f), new Vector2(0.03880f, 0.05150f), new RelPos2D(0.10f, 0.015f))  // Right  (cam dist sqrd = 896.8750)
                    };
                    break;
                }
                case PBEBattleFormat.Rotation: // TODO: Rotation Battles are not functional so there's time to figure out the camera before then
                {
                    a[0] = new PkmnPosition[3]
                    {
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.00000f, 0.00000f), new RelPos2D(0.015f, 0.25f)), // Left
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.00000f, 0.00000f), new RelPos2D(0.295f, 0.27f)), // Center
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.00000f, 0.00000f), new RelPos2D(0.575f, 0.29f))  // Right
                    };
                    a[1] = new PkmnPosition[3]
                    {
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.00000f, 0.00000f), new RelPos2D(0.66f, 0.055f)), // Left
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.00000f, 0.00000f), new RelPos2D(0.38f, 0.035f)), // Center
                        new PkmnPosition(new Vector3( 0.00f, floorY,   0.0f), new Vector2(0.00000f, 0.00000f), new RelPos2D(0.10f, 0.015f))  // Right
                    };
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(f));
            }
            return a;
        }
    }
}
